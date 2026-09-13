param(
    [Parameter(Mandatory = $true)]
    [string] $Apk
)

$ErrorActionPreference = "Stop"
$sdk = $env:ANDROID_SDK_ROOT
if (-not $sdk) { $sdk = $env:ANDROID_HOME }
if (-not $sdk) { $sdk = "D:\AndroidSDK" }

$apksigner = Get-ChildItem "$sdk\build-tools" -Recurse -Filter "apksigner.bat" | Select-Object -First 1
if (-not $apksigner) { throw "apksigner.bat не найден в $sdk" }

$keystore = Join-Path $env:USERPROFILE ".android\debug.keystore"
if (-not (Test-Path $keystore)) { throw "Нет $keystore" }

& $apksigner.FullName sign `
    --min-sdk-version 23 `
    --v1-signing-enabled true `
    --v2-signing-enabled true `
    --v3-signing-enabled false `
    --ks $keystore `
    --ks-key-alias androiddebugkey `
    --ks-pass pass:android `
    --key-pass pass:android `
    $Apk

if ($LASTEXITCODE -ne 0) { throw "apksigner sign failed: $LASTEXITCODE" }
