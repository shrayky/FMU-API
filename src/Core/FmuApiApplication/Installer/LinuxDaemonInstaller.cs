using System.Diagnostics;
using FmuApiDomain.Constants;

namespace FmuApiApplication.Installer;

public class LinuxDaemonInstaller
{
    private const string GuardAppName = "Apps-Guardian";
    private const string GuardUserName = "fmu-api-guard";
    
    public void Register()
    {
        var installScript = GenerateInstallScript();
        var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "install.sh");
        File.WriteAllText(scriptPath, installScript);
        
        var uninstallScript = GenerateUninstallScript();
        scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uninstall.sh");
        File.WriteAllText(scriptPath, uninstallScript);

        /*var guardianTask = GenerateGuardianTask();
        var taskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fmu-api-guardian-task.json");
        File.WriteAllText(taskPath, guardianTask);
        
        var fixPermissionsScript = GenerateFixGuardianPermissionsScript();
        scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fix-guardian-permissions.sh");
        File.WriteAllText(scriptPath, fixPermissionsScript);
        
        var checkPermissionsScript = GenerateCheckGuardianPermissionsScript();
        scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "check-guardian-permissions.sh");
        File.WriteAllText(scriptPath, checkPermissionsScript);*/
    
        Console.WriteLine("=== Созданы файлы ===");
        Console.WriteLine($"Установка: install-fmu-api.sh");
        Console.WriteLine($"Удаление: uninstall-fmu-api.sh");
        //Console.WriteLine($"Задача Guardian: fmu-api-guardian-task.json");
        //Console.WriteLine($"Настройка прав Guardian: fix-guardian-permissions.sh");
        //Console.WriteLine($"Проверка прав Guardian: check-guardian-permissions.sh");
    }

private static string GenerateInstallScript()
{
    var appPath = AppDomain.CurrentDomain.BaseDirectory;

    return $@"#!/bin/bash
set -e

echo '=== Установка FMU-API как systemd сервиса ==='

# Проверяем что скрипт запущен с sudo
if [ ""$EUID"" -ne 0 ]; then
    echo 'Ошибка: Скрипт должен быть запущен с правами sudo'
    echo 'Использование: sudo bash install-fmu-api.sh'
    exit 1
fi

# Путь к приложению
APP_PATH=""/opt/{ApplicationInformation.Manufacture}/{ApplicationInformation.AppName}""

# Создаем директорию приложения
echo 'Создаем директорию приложения...'
mkdir -p $APP_PATH

# Копируем файлы приложения
echo 'Копируем файлы приложения...'
cp -r {appPath}* $APP_PATH/

# Создаем пользователя fmu-api если его нет
if ! id ""fmu-api"" &>/dev/null; then
    echo 'Создаем пользователя fmu-api...'
    useradd -r -s /bin/bash -d $APP_PATH -c ""FMU API Service User"" fmu-api
    echo 'Пользователь fmu-api создан'
else
    echo 'Пользователь fmu-api уже существует'
fi

# Настраиваем sudo права для пользователя fmu-api через drop-in файл
echo 'Настраиваем sudo права...'
cat > /etc/sudoers.d/fmu-api << 'EOF'
# FMU-API sudo rights
fmu-api ALL=(ALL) NOPASSWD: /bin/systemctl, /bin/cp, /bin/chmod
EOF

# Устанавливаем права на файл sudoers
chmod 440 /etc/sudoers.d/fmu-api

# Устанавливаем права на директорию приложения
echo 'Устанавливаем права на директорию приложения...'
chown -R fmu-api:fmu-api $APP_PATH
chmod +x $APP_PATH/fmu-api

echo 'Создаем директории для логов и данных...'
mkdir -p /var/log/{ApplicationInformation.AppName}
mkdir -p /var/lib/{ApplicationInformation.AppName}
mkdir -p /tmp/{ApplicationInformation.AppName}

# Устанавливаем права на директории
chown -R fmu-api:fmu-api /var/log/{ApplicationInformation.AppName}
chown -R fmu-api:fmu-api /var/lib/{ApplicationInformation.AppName}
chown -R fmu-api:fmu-api /tmp/{ApplicationInformation.AppName}

# Копируем задачу для guardian если установлен
GUARDIAN_TASKS_PATH=""/var/lib/{GuardAppName}/tasks""

if [ -d ""$GUARDIAN_TASKS_PATH"" ]; then
    echo 'Обнаружен Guardian, настраиваем интеграцию...'
    
    if [ -f ""$APP_PATH/fmu-api-guardian-task.json"" ]; then
        cp ""$APP_PATH/fmu-api-guardian-task.json"" ""$GUARDIAN_TASKS_PATH/fmu-api.json""
        chown {GuardUserName}:{GuardUserName} ""$GUARDIAN_TASKS_PATH/fmu-api.json""
        echo 'Задача для Guardian установлена'
    else
        echo 'Файл задачи не найден'
    fi
    
    echo ''
    echo '=== НАСТРОЙКА ПРАВ GUARDIAN ==='
    echo 'Для корректной работы обновлений выполните:'
    echo '    cd $APP_PATH'
    echo '    sudo bash fix-guardian-permissions.sh'
    echo '    sudo systemctl restart apps-guardian'
    echo ''
    echo 'Для проверки прав выполните:'
    echo '    sudo bash check-guardian-permissions.sh'
    echo '================================'
    echo ''
else
    echo 'Guardian не установлен (папка $GUARDIAN_TASKS_PATH не найдена)'
    echo 'Автообновление не будет работать'
fi

# Создаем systemd сервис
echo 'Создаем systemd сервис...'
cat > /etc/systemd/system/{ApplicationInformation.AppName.ToLower()}.service << EOF
[Unit]
Description={ApplicationInformation.Description}
After=network.target

[Service]
Type=simple
User={ApplicationInformation.AppName.ToLower()}
ExecStart=$APP_PATH/{ApplicationInformation.AppName.ToLower()}
Restart=always
RestartSec=5
WorkingDirectory=$APP_PATH
StandardOutput=journal
StandardError=journal
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

# Перезагружаем systemd и включаем сервис
echo 'Настраиваем systemd сервис...'
systemctl daemon-reload
systemctl enable {ApplicationInformation.AppName.ToLower()}

systemctl start {ApplicationInformation.AppName.ToLower()}

echo '=== Установка завершена ==='
echo 'Приложение установлено в: $APP_PATH'
echo 'Для запуска сервиса: sudo systemctl start {ApplicationInformation.AppName.ToLower()}'
echo 'Для проверки статуса: sudo systemctl status {ApplicationInformation.AppName.ToLower()}'
echo 'Для просмотра логов: sudo journalctl -u {ApplicationInformation.AppName.ToLower()} -f'";
}

private static string GenerateUninstallScript()
{
    var appPath = AppDomain.CurrentDomain.BaseDirectory;
    var serviceName = ApplicationInformation.AppName.ToLower();

    return $@"#!/bin/bash
set -e

echo '=== Удаление {ApplicationInformation.AppName} ==='

# Проверяем что скрипт запущен с sudo
if [ ""$EUID"" -ne 0 ]; then
    echo 'Ошибка: Скрипт должен быть запущен с правами sudo'
    echo 'Использование: sudo bash uninstall-fmu-api.sh'
    exit 1
fi

# Путь к приложению
APP_PATH=""/opt/{ApplicationInformation.Manufacture}/{ApplicationInformation.AppName}""

# Останавливаем и отключаем сервис
echo 'Останавливаем сервис...'
if systemctl is-active --quiet {serviceName}; then
    systemctl stop {serviceName}
    echo 'Сервис остановлен'
else
    echo 'Сервис уже остановлен'
fi

echo 'Отключаем сервис...'
if systemctl is-enabled --quiet {serviceName}; then
    systemctl disable {serviceName}
    echo 'Сервис отключен'
fi

# Удаляем systemd сервис
echo 'Удаляем systemd сервис...'
if [ -f /etc/systemd/system/{serviceName}.service ]; then
    rm -f /etc/systemd/system/{serviceName}.service
    echo 'Файл сервиса удален'
fi

systemctl daemon-reload

# Удаляем sudo права
echo 'Удаляем sudo права...'
if [ -f /etc/sudoers.d/{serviceName} ]; then
    rm -f /etc/sudoers.d/{serviceName}
    echo 'Sudo права удалены'
fi

# Удаляем sudo права guardian
echo 'Удаляем sudo права guardian...'
if [ -f /etc/sudoers.d/{GuardUserName} ]; then
    rm -f /etc/sudoers.d/{GuardUserName}
    echo 'Sudo права guardian удалены'
fi

# Удаляем guardian из группы fmu-api
if id ""{GuardUserName}"" &>/dev/null && getent group fmu-api &>/dev/null; then
    if groups {GuardUserName} | grep -q '\bfmu-api\b'; then
        echo 'Удаляем guardian из группы fmu-api...'
        gpasswd -d {GuardUserName} fmu-api 2>/dev/null || true
        echo 'Guardian удален из группы'
        echo 'ВАЖНО: Перезапустите Guardian: sudo systemctl restart apps-guardian'
    fi
fi

# Удаляем пользователя
echo 'Удаляем пользователя {serviceName}...'
if id ""{serviceName}"" &>/dev/null; then
    userdel {serviceName}
    echo 'Пользователь удален'
else
    echo 'Пользователь не найден'
fi

# Удаляем директорию приложения
echo 'Удаляем директорию приложения...'
if [ -d ""$APP_PATH"" ]; then
    read -p ""Удалить $APP_PATH со всеми данными? (y/N): "" -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        rm -rf ""$APP_PATH""
        echo 'Директория удалена'
    else
        echo 'Директория сохранена'
    fi
else
    echo 'Директория не найдена'
fi

echo '=== Удаление завершено ==='";
}
    
private static string GenerateGuardianTask()
    {
        var targetPath = $"/opt/{ApplicationInformation.Manufacture}/{ApplicationInformation.AppName}";
        var serviceName = ApplicationInformation.AppName.ToLower();
        var updatePath = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName);
    
        return $@"{{
  ""Name"": ""{ApplicationInformation.AppName}"",
  ""Enabled"": true,
  ""ServiceName"": ""{serviceName}"",
  ""FolderWithUpdates"": ""{updatePath}"",
  ""AppFolder"": ""{targetPath}"",
  ""CheckState"": true,
  ""IsService"": true
}}";
    }

    private static string GenerateFixGuardianPermissionsScript()
    {{
        return $@"#!/bin/bash

echo '=== Исправление прав Guardian для FMU-API ==='
echo ''

# Проверяем что скрипт запущен с sudo
if [ ""$EUID"" -ne 0 ]; then
    echo 'ОШИБКА: Скрипт должен быть запущен с правами sudo'
    echo 'Использование: sudo bash fix-guardian-permissions.sh'
    exit 1
fi

APP_PATH=""/opt/{ApplicationInformation.Manufacture}/{ApplicationInformation.AppName}""
GUARD_USER=""{GuardUserName}""

# Проверяем существование пользователей
if ! id ""fmu-api"" &>/dev/null; then
    echo 'ОШИБКА: Пользователь fmu-api не существует!'
    exit 1
fi

if ! id ""$GUARD_USER"" &>/dev/null; then
    echo 'ОШИБКА: Пользователь $GUARD_USER не существует!'
    exit 1
fi

if ! getent group fmu-api &>/dev/null; then
    echo 'ОШИБКА: Группа fmu-api не существует!'
    exit 1
fi

echo '1. Настраиваем sudo права для guardian...'
cat > /etc/sudoers.d/$GUARD_USER << 'EOF'
# Apps-Guardian sudo rights for FMU-API management
{GuardUserName} ALL=(ALL) NOPASSWD: /bin/systemctl, /bin/rm, /bin/cp, /bin/chmod, /bin/chown, /bin/mkdir
EOF

chmod 440 /etc/sudoers.d/$GUARD_USER
echo '   ✓ Sudo права для guardian установлены'
echo ''

echo '2. Добавляем Guardian в группу fmu-api...'
usermod -a -G fmu-api $GUARD_USER
echo ""   Guardian теперь в группах: $(groups $GUARD_USER)""
echo ''

echo '3. Устанавливаем права на родительские директории...'
chmod o+x /opt
mkdir -p /opt/{ApplicationInformation.Manufacture}
chmod o+x /opt/{ApplicationInformation.Manufacture}

# КРИТИЧНО: Устанавливаем группу и права на /opt/{ApplicationInformation.Manufacture} для возможности удаления
chown root:fmu-api /opt/{ApplicationInformation.Manufacture}
chmod u=rwx,g=rwx,o=rx /opt/{ApplicationInformation.Manufacture}
chmod g+s /opt/{ApplicationInformation.Manufacture}
echo '   ✓ Права на /opt и /opt/{ApplicationInformation.Manufacture} установлены'
ls -ld /opt/{ApplicationInformation.Manufacture}
echo ''

echo '4. Устанавливаем владельца и групповые права на $APP_PATH...'
if [ -d ""$APP_PATH"" ]; then
    chown -R fmu-api:fmu-api ""$APP_PATH""
    
    # Устанавливаем права: владелец и группа - полный доступ
    find ""$APP_PATH"" -type d -exec chmod u=rwx,g=rwx,o= {{}} \;
    find ""$APP_PATH"" -type f -exec chmod u=rw,g=rw,o= {{}} \;
    chmod u=rwx,g=rwx,o= ""$APP_PATH/{ApplicationInformation.AppName.ToLower()}""
    
    # Устанавливаем setgid бит на директории
    find ""$APP_PATH"" -type d -exec chmod g+s {{}} \;
    
    echo '   ✓ Права установлены на все файлы и директории'
    ls -ld ""$APP_PATH""
else
    echo '   ОШИБКА: Директория $APP_PATH не существует!'
    exit 1
fi
echo ''

echo '5. Устанавливаем права на директории логов и данных...'
if [ -d ""/var/log/{ApplicationInformation.AppName}"" ]; then
    chmod u=rwx,g=rwx,o= ""/var/log/{ApplicationInformation.AppName}""
    echo '   ✓ Права на /var/log/{ApplicationInformation.AppName} установлены'
fi

if [ -d ""/var/lib/{ApplicationInformation.AppName}"" ]; then
    chmod u=rwx,g=rwx,o= ""/var/lib/{ApplicationInformation.AppName}""
    echo '   ✓ Права на /var/lib/{ApplicationInformation.AppName} установлены'
fi

# Директория для обновлений
if [ -d ""/tmp/{ApplicationInformation.AppName}"" ]; then
    chown -R fmu-api:fmu-api ""/tmp/{ApplicationInformation.AppName}""
    chmod -R u=rwx,g=rwx,o= ""/tmp/{ApplicationInformation.AppName}""
    echo '   ✓ Права на /tmp/{ApplicationInformation.AppName} установлены'
else
    # Создаем директорию заранее с правильными правами
    mkdir -p ""/tmp/{ApplicationInformation.AppName}""
    chown fmu-api:fmu-api ""/tmp/{ApplicationInformation.AppName}""
    chmod u=rwx,g=rwx,o= ""/tmp/{ApplicationInformation.AppName}""
    echo '   ✓ Директория /tmp/{ApplicationInformation.AppName} создана с правильными правами'
fi
echo ''

echo '6. Проверяем права на .aspnet (если существует)...'
if [ -d ""$APP_PATH/.aspnet"" ]; then
    chown -R fmu-api:fmu-api ""$APP_PATH/.aspnet""
    find ""$APP_PATH/.aspnet"" -type d -exec chmod u=rwx,g=rwx,o= {{}} \;
    find ""$APP_PATH/.aspnet"" -type f -exec chmod u=rw,g=rw,o= {{}} \;
    echo '   ✓ Права на .aspnet установлены'
    ls -ld ""$APP_PATH/.aspnet""
else
    echo '   Директория .aspnet еще не создана'
fi
echo ''

echo '=== Права успешно исправлены! ==='
echo ''
echo 'КРИТИЧЕСКИ ВАЖНО: Теперь перезапустите Guardian:'
echo '    sudo systemctl restart apps-guardian'
echo ''
echo 'Проверьте результат командой:'
echo '    sudo bash check-guardian-permissions.sh'
";
    }}

    private static string GenerateCheckGuardianPermissionsScript()
    {{
        return $@"#!/bin/bash

echo '=== Диагностика прав Guardian для FMU-API ==='
echo ''

APP_PATH=""/opt/{ApplicationInformation.Manufacture}/{ApplicationInformation.AppName}""
GUARD_USER=""{GuardUserName}""

# Проверка 1: Существует ли пользователь guardian
echo '1. Проверка пользователя Guardian:'
if id ""$GUARD_USER"" &>/dev/null; then
    echo ""   ✓ Пользователь $GUARD_USER существует""
    echo ""   Группы: $(groups $GUARD_USER)""
else
    echo ""   ✗ Пользователь $GUARD_USER НЕ НАЙДЕН!""
    exit 1
fi
echo ''

# Проверка 2: Входит ли guardian в группу fmu-api
echo '2. Проверка членства в группе fmu-api:'
if groups $GUARD_USER | grep -q '\bfmu-api\b'; then
    echo '   ✓ Guardian входит в группу fmu-api'
else
    echo '   ✗ Guardian НЕ входит в группу fmu-api!'
    echo '   Выполните: sudo usermod -a -G fmu-api $GUARD_USER'
fi
echo ''

# Проверка 3: Права на родительские директории
echo '3. Проверка прав на родительские директории:'
ls -ld /opt
ls -ld /opt/{ApplicationInformation.Manufacture}
echo ''
echo '   /opt/{ApplicationInformation.Manufacture} должен быть: drwxrwxr-x root:fmu-api'
if [ ""$(stat -c %G /opt/{ApplicationInformation.Manufacture})"" == ""fmu-api"" ]; then
    echo '   ✓ Группа /opt/{ApplicationInformation.Manufacture} правильная'
else
    echo '   ✗ Группа /opt/{ApplicationInformation.Manufacture} НЕ fmu-api!'
    echo '   Выполните: sudo chown root:fmu-api /opt/{ApplicationInformation.Manufacture}'
fi
echo ''

# Проверка 4: Права на директорию приложения
echo '4. Проверка прав на директорию приложения:'
if [ -d ""$APP_PATH"" ]; then
    ls -ld ""$APP_PATH""
    echo ''
    echo '   Владелец должен быть: fmu-api:fmu-api'
    echo '   Права должны быть: drwxrwx--- (770)'
else
    echo '   ✗ Директория $APP_PATH не существует!'
fi
echo ''

# Проверка 5: Права на скрытые директории
echo '5. Проверка прав на .aspnet (если существует):'
if [ -d ""$APP_PATH/.aspnet"" ]; then
    ls -ld ""$APP_PATH/.aspnet""
    if [ -d ""$APP_PATH/.aspnet/DataProtection-Keys"" ]; then
        ls -ld ""$APP_PATH/.aspnet/DataProtection-Keys""
        echo '   Файлы в DataProtection-Keys:'
        ls -la ""$APP_PATH/.aspnet/DataProtection-Keys/"" | head -5
    fi
else
    echo '   Директория .aspnet еще не создана (создается при первом запуске)'
fi
echo ''

# Проверка 5.1: Права на директорию обновлений
echo '5.1. Проверка прав на /tmp/{ApplicationInformation.AppName}:'
if [ -d ""/tmp/{ApplicationInformation.AppName}"" ]; then
    ls -ld ""/tmp/{ApplicationInformation.AppName}""
    echo '   Владелец должен быть: fmu-api:fmu-api'
    echo '   Права должны быть: drwxrwx--- (770)'
    
    # Тест доступа
    if sudo -u $GUARD_USER test -w ""/tmp/{ApplicationInformation.AppName}""; then
        echo '   ✓ Guardian может удалять файлы из /tmp/{ApplicationInformation.AppName}'
    else
        echo '   ✗ Guardian НЕ МОЖЕТ удалять файлы из /tmp/{ApplicationInformation.AppName}!'
    fi
else
    echo '   Директория /tmp/{ApplicationInformation.AppName} не существует (создается при обновлении)'
fi
echo ''

# Проверка 6: Тест доступа от имени guardian
echo '6. Тест доступа Guardian к директории приложения:'
if sudo -u $GUARD_USER test -r ""$APP_PATH""; then
    echo '   ✓ Guardian может ЧИТАТЬ $APP_PATH'
else
    echo '   ✗ Guardian НЕ МОЖЕТ читать $APP_PATH'
fi

if sudo -u $GUARD_USER test -w ""$APP_PATH""; then
    echo '   ✓ Guardian может ПИСАТЬ в $APP_PATH'
else
    echo '   ✗ Guardian НЕ МОЖЕТ писать в $APP_PATH'
fi

if sudo -u $GUARD_USER test -x ""$APP_PATH""; then
    echo '   ✓ Guardian может ВЫПОЛНЯТЬ (войти в) $APP_PATH'
else
    echo '   ✗ Guardian НЕ МОЖЕТ войти в $APP_PATH'
fi
echo ''

# Проверка 7: Может ли Guardian удалить директорию приложения
echo '7. Может ли Guardian удалить $APP_PATH (тест):'
if sudo -u $GUARD_USER test -w /opt/{ApplicationInformation.Manufacture}; then
    echo '   ✓ Guardian МОЖЕТ удалять директории в /opt/{ApplicationInformation.Manufacture}'
else
    echo '   ✗ Guardian НЕ МОЖЕТ удалять директории в /opt/{ApplicationInformation.Manufacture}!'
    echo '   Это критично для восстановления из бэкапа!'
fi
echo ''

# Проверка 8: Статус сервиса Guardian
echo '8. Статус сервиса Guardian:'
systemctl is-active {GuardAppName.ToLower()} &>/dev/null && echo '   ✓ {GuardAppName} запущен' || echo '   ✗ {GuardAppName} остановлен'
echo ""   PID процесса: $(pgrep -u $GUARD_USER -f {GuardAppName.ToLower()} || echo 'не найден')""
echo ''

# Проверка 9: Актуальность групп в процессе
echo '9. Проверка актуальности групп в запущенном процессе Guardian:'
GUARD_PID=$(pgrep -u $GUARD_USER -f {GuardAppName.ToLower()} | head -1)
if [ -n ""$GUARD_PID"" ]; then
    echo ""   PID: $GUARD_PID""
    echo ""   Группы процесса: $(cat /proc/$GUARD_PID/status | grep Groups || echo 'не удалось прочитать')""
    echo ''
    echo '   ВНИМАНИЕ: Если группы изменились, нужен перезапуск:'
    echo '   sudo systemctl restart {GuardAppName.ToLower()}'
else
    echo '   Процесс Guardian не запущен'
fi
echo ''

echo '=== Рекомендации ==='
echo 'Если Guardian не может получить доступ:'
echo '1. Убедитесь что guardian в группе: sudo usermod -a -G fmu-api $GUARD_USER'
echo '2. Установите права на /opt/{ApplicationInformation.Manufacture}: sudo chown root:fmu-api /opt/{ApplicationInformation.Manufacture} && sudo chmod 775 /opt/{ApplicationInformation.Manufacture}'
echo '3. Установите права: sudo chmod -R u=rwx,g=rwx,o= $APP_PATH'
echo '4. ОБЯЗАТЕЛЬНО перезапустите Guardian: sudo systemctl restart {GuardAppName.ToLower()}'
echo '5. Проверьте что .aspnet создается с групповыми правами после запуска fmu-api'
";
    }}
}