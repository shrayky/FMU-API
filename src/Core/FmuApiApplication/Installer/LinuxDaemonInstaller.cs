using System.Diagnostics;
using FmuApiDomain.Constants;

namespace FmuApiApplication.Installer;

public class LinuxDaemonInstaller
{
    public void Register()
    {
        var installScript = GenerateInstallScript();
        var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "install-fmu-api.sh");
        File.WriteAllText(scriptPath, installScript);
        
        var uninstallScript = GenerateUninstallScript();
        scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uninstall-fmu-api.sh");
        File.WriteAllText(scriptPath, uninstallScript);

        var autoUpdateScript = GenerateUpdateScript();
        scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auto-update-fmu-api.sh");
        File.WriteAllText(scriptPath, autoUpdateScript);
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

# Устанавливаем права на директории
chown -R fmu-api:fmu-api /var/log/{ApplicationInformation.AppName}
chown -R fmu-api:fmu-api /var/lib/{ApplicationInformation.AppName}

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

    private string GenerateUpdateScript()
    {
        var folderWithUpdatesPath = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName);
        var targetPath = $"/opt/{ApplicationInformation.Manufacture}/{ApplicationInformation.AppName}";
        
        return $@"#!/bin/bash
echo 'Начинаем обновление {ApplicationInformation.AppName}...'
sleep 2
sudo systemctl stop {ApplicationInformation.AppName.ToLower()}
sleep 3
# Проверяем, что сервис остановился
if sudo systemctl is-active --quiet {ApplicationInformation.AppName.ToLower()}; then
    echo 'Ошибка: не удалось остановить сервис'
    exit 1
fi
# Копируем все содержимое включая скрытые файлы
cp -r {folderWithUpdatesPath}/. {targetPath}/
chmod +x {targetPath}/*
sudo systemctl start {ApplicationInformation.AppName.ToLower()}
echo 'Обновление завершено'
rm -f -r {folderWithUpdatesPath}
";
    }
}