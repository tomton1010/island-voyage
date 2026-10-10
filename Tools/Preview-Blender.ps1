param([switch]$SkipCapture)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskFolder=Join-Path $taskRoot 'Screenshots\Blender'
if(!$SkipCapture){
    $taskExe=Join-Path $taskRoot 'IslandVoyage\Builds\Windows\IslandVoyage.exe'
    $taskLog=Join-Path $PSScriptRoot 'Logs\Blender-Gallery.log'
    $taskApp=Start-Process -FilePath $taskExe -ArgumentList @('--asset-gallery',('"'+$taskFolder+'"'),'-screen-width','1200','-screen-height','800','-screen-fullscreen','0','-logFile',('"'+$taskLog+'"')) -WindowStyle Hidden -PassThru -Wait
    if($taskApp.ExitCode -ne 0){throw 'Blender asset capture failed.'}
    $taskLines=Get-Content -LiteralPath $taskLog
    if(!($taskLines -match 'BLENDER_GALLERY_CAPTURE_PASSED') -or $taskLines -match 'Exception|ERROR:|shader is not supported'){throw 'Blender capture validation failed.'}
}
$taskEncoder=Get-Command ffmpeg -ErrorAction SilentlyContinue
if(!$taskEncoder){Write-Output "Model PNG previews saved in $taskFolder; ffmpeg is unavailable for animation conversion.";return}
foreach($taskName in @('Dolphin','Resident0')){
    $taskFrames=Join-Path $taskFolder ($taskName+'Frames\%03d.png')
    $taskGif=Join-Path $taskFolder ($taskName+'.gif')
    $taskVideo=Join-Path $taskFolder ($taskName+'.mp4')
    # Both Swim and Walk last 36 frames at 30 fps. Export one complete seamless cycle.
    & $taskEncoder.Source -y -loglevel error -framerate 30 -i $taskFrames -t 1.2 -vf 'fps=15,scale=720:-1:flags=lanczos,split[s0][s1];[s0]palettegen=stats_mode=diff[p];[s1][p]paletteuse=dither=sierra2_4a' -loop 0 $taskGif
    if($LASTEXITCODE -ne 0){throw "GIF conversion failed: $taskName"}
    & $taskEncoder.Source -y -loglevel error -framerate 30 -i $taskFrames -t 1.2 -vf 'scale=1200:-2' -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart $taskVideo
    if($LASTEXITCODE -ne 0){throw "Video conversion failed: $taskName"}
    Write-Output "Unity skin animation preview: $taskGif"
}
