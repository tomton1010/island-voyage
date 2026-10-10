$ErrorActionPreference='Stop'
$taskUrl='https://mirror.nju.edu.cn/blender/release/Blender5.2/blender-5.2.2-windows-x64.zip'
$taskSize=[long]404453484
$taskExpectedHash='3849d17a682cba006075aaa3f3597ecb5c9c30ec31035b2e092c53e40679b535'
$taskDownloads=Join-Path $PSScriptRoot 'Downloads'
$taskParts=Join-Path $taskDownloads 'Blender-Parts'
$taskArchive=Join-Path $taskDownloads 'blender-5.2.2-windows-x64.zip'
New-Item -ItemType Directory -Path $taskParts -Force | Out-Null
$taskChunks=8
$taskChunkSize=[long][math]::Ceiling($taskSize/$taskChunks)
$taskTransfers=@()
for($taskIndex=0;$taskIndex -lt $taskChunks;$taskIndex++){
    $taskStart=$taskIndex*$taskChunkSize
    $taskEnd=[math]::Min($taskSize-1,$taskStart+$taskChunkSize-1)
    $taskPart=Join-Path $taskParts ('part-{0:00}.bin' -f $taskIndex)
    $taskExpectedSize=$taskEnd-$taskStart+1
    $taskProcess=$null
    if(!(Test-Path -LiteralPath $taskPart) -or (Get-Item -LiteralPath $taskPart).Length -ne $taskExpectedSize){
        $taskArguments=@('--noproxy','*','--location','--fail','--silent','--show-error','--retry','2','--connect-timeout','20','--max-time','900','--range',"$taskStart-$taskEnd",'--output',('"'+$taskPart+'"'),$taskUrl)
        $taskProcess=Start-Process -FilePath 'curl.exe' -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskParts "out-$taskIndex.log") -RedirectStandardError (Join-Path $taskParts "error-$taskIndex.log")
    }
    $taskTransfers+=@{process=$taskProcess;path=$taskPart;expected=$taskExpectedSize}
}
$taskLastProgress=-1
while(@($taskTransfers | Where-Object {$_.process -and !$_.process.HasExited}).Count -gt 0){
    $taskReceived=[long]0
    foreach($taskTransfer in $taskTransfers){if(Test-Path -LiteralPath $taskTransfer.path){$taskReceived+=(Get-Item -LiteralPath $taskTransfer.path).Length}}
    $taskProgress=[int][math]::Floor($taskReceived/$taskSize*100)
    if($taskProgress -ge $taskLastProgress+5){Write-Output "Blender download: $taskProgress% ($([math]::Round($taskReceived/1MB,1)) MiB)";$taskLastProgress=$taskProgress}
    Start-Sleep -Seconds 3
}
foreach($taskTransfer in $taskTransfers){
    if(($taskTransfer.process -and $taskTransfer.process.ExitCode -ne 0) -or !(Test-Path -LiteralPath $taskTransfer.path) -or (Get-Item -LiteralPath $taskTransfer.path).Length -ne $taskTransfer.expected){throw "Blender range download failed: $($taskTransfer.path)"}
}
$taskCombined=Join-Path $taskDownloads 'blender-5.2.2-assembled.zip'
$taskOutput=[IO.File]::Open($taskCombined,[IO.FileMode]::Create)
try {
    foreach($taskTransfer in $taskTransfers){$taskInput=[IO.File]::OpenRead($taskTransfer.path);try{$taskInput.CopyTo($taskOutput)}finally{$taskInput.Dispose()}}
} finally {$taskOutput.Dispose()}
$taskHash=(Get-FileHash -LiteralPath $taskCombined -Algorithm SHA256).Hash.ToLowerInvariant()
if($taskHash -ne $taskExpectedHash){throw 'Mirror download does not match official SHA-256; it will not be installed.'}
if(Test-Path -LiteralPath $taskArchive){throw 'An archive already exists; no file was replaced.'}
Move-Item -LiteralPath $taskCombined -Destination $taskArchive
Write-Output "BLENDER_DOWNLOAD_VERIFIED: $taskHash"
