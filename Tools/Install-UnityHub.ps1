$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$taskInstallRoot = 'E:\Unity'
$taskCache = Join-Path $taskInstallRoot 'Downloads'
$taskHubDirectory = Join-Path $taskInstallRoot 'Hub'
New-Item -ItemType Directory -Path $taskCache -Force | Out-Null
$taskInstaller = Join-Path $taskCache 'UnityHubSetup.exe'
if (!(Test-Path -LiteralPath $taskInstaller) -or (Get-AuthenticodeSignature -LiteralPath $taskInstaller).Status -ne 'Valid') {
    Write-Output 'Downloading the official Unity Hub installer...'
    Invoke-WebRequest -Uri 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-x64.exe' -OutFile $taskInstaller
}
$taskSignature = Get-AuthenticodeSignature -LiteralPath $taskInstaller
if ($taskSignature.Status -ne 'Valid' -or $taskSignature.SignerCertificate.Subject -notmatch 'Unity') {
    throw "Installer signature verification failed: $($taskSignature.Status)"
}
Write-Output "Verified publisher: $($taskSignature.SignerCertificate.Subject)"
$taskHubExe = Join-Path $taskHubDirectory 'Unity Hub.exe'
if (!(Test-Path -LiteralPath $taskHubExe)) {
    Write-Output 'Installing Unity Hub in E:\Unity\Hub...'
    $taskProcess = Start-Process -FilePath $taskInstaller -ArgumentList @('/S', "/D=$taskHubDirectory") -WindowStyle Hidden -PassThru -Wait
    if ($taskProcess.ExitCode -ne 0) { throw "Hub installer exited with $($taskProcess.ExitCode)" }
}
if (!(Test-Path -LiteralPath $taskHubExe)) { throw 'Unity Hub executable was not found after installation.' }
Get-Item -LiteralPath $taskHubExe | Select-Object FullName, @{Name='Version';Expression={$_.VersionInfo.ProductVersion}}
Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, @{Name='MemoryGB';Expression={[math]::Round($_.TotalVisibleMemorySize/1MB,1)}}
Get-CimInstance Win32_Processor | Select-Object Name
Get-CimInstance Win32_VideoController | Select-Object Name
