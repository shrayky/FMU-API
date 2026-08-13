# Публикация fmu-api в winget

Пакет: `Automation.fmu-api`  
Архивы релиза: `fmu-api-win-x64.zip`, `fmu-api-win-x86.zip` (в корне ZIP — `fmu-api.exe`).

## 1. Release на GitHub

Выложите оба ZIP в Assets релиза с тегом `v{{VERSION}}` (например `v12.0.0`).

URL будут такими:

- `https://github.com/shrayky/FMU-API/releases/download/v12.0.0/fmu-api-win-x64.zip`
- `https://github.com/shrayky/FMU-API/releases/download/v12.0.0/fmu-api-win-x86.zip`

## 2. SHA256

```powershell
(Get-FileHash -Algorithm SHA256 .\fmu-api-win-x64.zip).Hash
(Get-FileHash -Algorithm SHA256 .\fmu-api-win-x86.zip).Hash
```

В `InstallerSha256` пишите **только** 64 hex-символа, **без** префикса `sha256:`:

```yaml
InstallerSha256: 229D6E38DF8A843108B4291EE09A08F16CDF6145C3D5565199146B402D7AF9AC
```

## 3. Подстановка в манифесты

В `PackageVersion` и URL укажите версию/тег релиза; в `InstallerSha256` — хеши из шага 2.

## 4. Локальная проверка

```powershell
winget validate .\Automation.fmu-api

# Один раз (админ): разрешить обход локального скана ZIP
winget settings --enable LocalArchiveMalwareScanOverride

# Установка из локального манифеста
winget install --manifest .\Automation.fmu-api --ignore-local-archive-malware-scan
```

Нужны права администратора (`--install` ставит службу).  
Флаг `--ignore-local-archive-malware-scan` нужен только при установке **из локального манифеста** (ложное срабатывание Pure). После публикации в winget-pkgs обычный `winget install Automation.fmu-api` без флага.

## 5. PR в microsoft/winget-pkgs

Путь в репозитории:

`manifests/a/Automation/fmu-api/<VERSION>/`

Файлы:

- `Automation.fmu-api.yaml`
- `Automation.fmu-api.installer.yaml`
- `Automation.fmu-api.locale.en-US.yaml`
- `Automation.fmu-api.locale.ru-RU.yaml`

Удобный способ:

Первый раз (пакета ещё нет в winget-pkgs) — команда `new`, URL без `-u`, версия вводится в мастере:

```powershell
wingetcreate new `
  https://github.com/shrayky/FMU-API/releases/download/fmuapi11-12/11-12-x64-win.zip `
  https://github.com/shrayky/FMU-API/releases/download/fmuapi11-12/11-12-x86-win.zip `
  -o .\Automation.fmu-api -t <GITHUB_TOKEN>
```

В мастере укажите `PackageIdentifier: Automation.fmu-api`, `PackageVersion: 11.12.0`.

Последующие релизы — `update` (здесь уже есть `-u` / `-v`):

```powershell
wingetcreate update Automation.fmu-api `
  -u https://github.com/shrayky/FMU-API/releases/download/fmuapi11-12/11-12-x64-win.zip `
     https://github.com/shrayky/FMU-API/releases/download/fmuapi11-12/11-12-x86-win.zip `
  -v 11.12.0 -s -t <GITHUB_TOKEN>
```

Токен лучше не писать в `-t` (попадёт в историю команд). Предпочтительно: `wingetcreate token -s`, затем команда без `-t`.

### GitHub token для wingetcreate

Нужен **Personal access token (classic)** — [создать](https://github.com/settings/tokens).  
Fine-grained PAT wingetcreate **не поддерживает**.

| Scope | Нужен? | Зачем |
|-------|--------|--------|
| `public_repo` | **обязательно** | fork / ветка / PR в публичный `microsoft/winget-pkgs` |
| `delete_repo` | опционально | удалить fork, если submit упал |

Проще без ручного токена: `wingetcreate token -s` (логин через браузер).

## Архитектура

Winget выбирает установщик по полю `Architecture` (`x64` / `x86`), а не по имени файла. На x64 Windows ставится `fmu-api-win-x64.zip`, на x86 — `fmu-api-win-x86.zip`.
