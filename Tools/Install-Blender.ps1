param([string]$InstallRoot='E:\Blender')
$ErrorActionPreference='Stop'
$taskVersion='5.2.2'
$taskArchiveName="blender-$taskVersion-windows-x64.zip"
$taskExpectedHash='3849d17a682cba006075aaa3f3597ecb5c9c30ec31035b2e092c53e40679b535'
$taskDownloads=Join-Path $PSScriptRoot 'Downloads'
$taskArchive=Join-Path $taskDownloads $taskArchiveName
$taskDestination=Join-Path $InstallRoot "blender-$taskVersion-windows-x64"
$taskExecutable=Join-Path $taskDestination 'blender.exe'
New-Item -ItemType Directory -Path $taskDownloads -Force | Out-Null
if(!(Test-Path -LiteralPath $taskArchive)){
    Write-Output "Downloading official Blender $taskVersion LTS..."
    & curl.exe --fail --location --retry 2 --connect-timeout 20 --max-time 1200 --output $taskArchive "https://download.blender.org/release/Blender5.2/$taskArchiveName"
    if($LASTEXITCODE -ne 0){throw 'Official Blender download failed.'}
}
$taskHash=(Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256).Hash.ToLowerInvariant()
if($taskHash -ne $taskExpectedHash){throw 'Blender ZIP does not match the official SHA-256. No downloaded software has been executed.'}
Write-Output "BLENDER_ARCHIVE_VERIFIED: $taskHash"
if(!(Test-Path -LiteralPath $taskExecutable)){
    if(Test-Path -LiteralPath $taskDestination){throw "An existing incomplete destination requires inspection: $taskDestination"}
    New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $taskZip=[IO.Compression.ZipFile]::OpenRead($taskArchive)
    try {
        $taskAllowedRoot=[IO.Path]::GetFullPath($taskDestination)+[IO.Path]::DirectorySeparatorChar
        foreach($taskEntry in $taskZip.Entries){
            $taskEntryPath=[IO.Path]::GetFullPath((Join-Path $InstallRoot $taskEntry.FullName))
            if($taskEntryPath -ne $taskAllowedRoot.TrimEnd('\') -and !$taskEntryPath.StartsWith($taskAllowedRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'ZIP entry is outside the expected Blender directory.'}
        }
    } finally {$taskZip.Dispose()}
    Write-Output "Extracting Blender to $taskDestination..."
    [IO.Compression.ZipFile]::ExtractToDirectory($taskArchive,$InstallRoot)
}
if(!(Test-Path -LiteralPath $taskExecutable)){throw 'Blender executable missing after extraction.'}
$taskSignature=Get-AuthenticodeSignature -LiteralPath $taskExecutable
Write-Output "BLENDER_SIGNATURE: $($taskSignature.Status) $($taskSignature.SignerCertificate.Subject)"
& $taskExecutable --version
if($LASTEXITCODE -ne 0){throw 'Blender version check failed.'}
$taskShell=New-Object -ComObject WScript.Shell
foreach($taskShortcutRoot in @([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('Programs'))){
    $taskShortcutPath=Join-Path $taskShortcutRoot 'Blender 5.2 LTS.lnk'
    if(!(Test-Path -LiteralPath $taskShortcutPath)){
        $taskShortcut=$taskShell.CreateShortcut($taskShortcutPath)
        $taskShortcut.TargetPath=$taskExecutable
        $taskShortcut.WorkingDirectory=$taskDestination
        $taskShortcut.IconLocation="$taskExecutable,0"
        $taskShortcut.Description='Official Blender 5.2 LTS — Island Voyage modeling environment'
        $taskShortcut.Save()
    }
}
[ordered]@{version=$taskVersion;executable=$taskExecutable;archive=$taskArchive;sha256=$taskHash;source="https://download.blender.org/release/Blender5.2/$taskArchiveName";distribution='Official portable Windows x64';installed=(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'BlenderEnvironment.json') -Encoding utf8
Write-Output "BLENDER_INSTALLED: $taskExecutable"
