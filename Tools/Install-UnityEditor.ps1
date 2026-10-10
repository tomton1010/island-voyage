$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$taskRelease = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'UnityRelease.json') -Raw | ConvertFrom-Json).results[0]
$taskProjectVersion=Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) 'IslandVoyage\ProjectSettings\ProjectVersion.txt') -First 1
if($taskProjectVersion -notmatch [regex]::Escape($taskRelease.version)){throw 'This cached installer metadata does not match the project. Use Unity Hub to install the project version.'}
$taskEditor = $taskRelease.downloads | Where-Object { $_.platform -eq 'WINDOWS' -and $_.architecture -eq 'X86_64' } | Select-Object -First 1
$taskRoot = Join-Path 'E:\Unity\Editors' $taskRelease.version
$taskCache = 'E:\Unity\Downloads'
New-Item -ItemType Directory -Path $taskRoot, $taskCache -Force | Out-Null
$taskParts = @($taskEditor, ($taskEditor.modules | Where-Object { $_.id -eq 'webgl' }))
foreach ($taskPart in $taskParts) {
    $taskFile = Join-Path $taskCache ([IO.Path]::GetFileName(([Uri]$taskPart.url).AbsolutePath))
    $taskExpected = [Convert]::ToHexString([Convert]::FromBase64String($taskPart.integrity.Substring(4)))
    $taskReady = (Test-Path -LiteralPath $taskFile) -and ((Get-Item -LiteralPath $taskFile).Length -eq $taskPart.downloadSize.value)
    if (!$taskReady) {
        Write-Output "Downloading $([IO.Path]::GetFileName($taskFile)) ($([math]::Round($taskPart.downloadSize.value/1GB,2)) GB)..."
        Invoke-WebRequest -Uri $taskPart.url -OutFile $taskFile -Resume -TimeoutSec 3600
    }
    if ((Get-FileHash -LiteralPath $taskFile -Algorithm MD5).Hash -ne $taskExpected) { throw 'Unity download integrity check failed.' }
    $taskSignature = Get-AuthenticodeSignature -LiteralPath $taskFile
    if ($taskSignature.Status -ne 'Valid' -or $taskSignature.SignerCertificate.Subject -notmatch 'Unity') { throw 'Unity installer signature check failed.' }
    Write-Output "Installing verified package: $([IO.Path]::GetFileName($taskFile))"
    $taskInstallerProcess = Start-Process -FilePath $taskFile -ArgumentList @('/S', "/D=$taskRoot") -WindowStyle Hidden -PassThru -Wait
    if ($taskInstallerProcess.ExitCode -ne 0) { throw "Installer failed ($($taskInstallerProcess.ExitCode))" }
}
$taskExe = Join-Path $taskRoot 'Editor\Unity.exe'
if (!(Test-Path -LiteralPath $taskExe)) { throw 'Unity Editor was not found after installation.' }
Get-Item -LiteralPath $taskExe | Format-List FullName, @{Name='Version';Expression={$_.VersionInfo.ProductVersion}}
Write-Output 'Unity Editor and Web Build Support installation finished.'
