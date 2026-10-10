param([ValidateSet('Day','Sailing','Night','Rain','Fog','Storm','Migration','Swell','Luminous','Fishing','Cottage','Offshore')][string]$Scene='Day',[switch]$ShowHud)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=Join-Path $taskRoot 'IslandVoyage\Builds\Windows\IslandVoyage.exe'
$taskPictures=Join-Path $taskRoot 'Screenshots'
New-Item -ItemType Directory -Path $taskPictures -Force | Out-Null
$taskImage=Join-Path $taskPictures ($Scene+'.png')
$taskLog=Join-Path $PSScriptRoot ('Logs\Preview-'+$Scene+'.log')
$taskArguments=@('--capture',('"'+$taskImage+'"'),'--preview-quit','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',('"'+$taskLog+'"'))
if(!$ShowHud){$taskArguments+='--scenic'}
if($Scene -eq 'Night'){$taskArguments+=@('--hour','21')}
if($Scene -eq 'Sailing'){$taskArguments+='--sailing'}
if($Scene -eq 'Rain'){$taskArguments+=@('--weather','Rain')}
if($Scene -eq 'Fog'){$taskArguments+=@('--weather','Fog')}
if($Scene -eq 'Storm'){$taskArguments+=@('--weather','Thunderstorm','--sailing')}
if($Scene -eq 'Migration'){$taskArguments+=@('--event','Migration')}
if($Scene -eq 'Swell'){$taskArguments+=@('--event','RogueSwell')}
if($Scene -eq 'Luminous'){$taskArguments+=@('--event','LuminousSea','--hour','21','--sailing')}
if($Scene -eq 'Fishing'){$taskArguments+=@('--fishing-demo','--capture-delay','12')}
if($Scene -eq 'Cottage'){$taskArguments+=@('--boat-position','35,-13','--view-yaw','-20','--view-elevation','18')}
if($Scene -eq 'Offshore'){$taskArguments+=@('--boat-position','-210,170','--view-elevation','12','--view-distance','24')}
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -Wait
if($taskProcess.ExitCode -ne 0){throw "Preview failed: $($taskProcess.ExitCode)"}
$taskLines=Get-Content -LiteralPath $taskLog
if($taskLines -match 'Exception|ERROR:|shader is not supported'){throw "Preview log contains an error: $taskLog"}
if(!($taskLines -match 'VOYAGE_CAPTURE_PASSED')){throw "Preview capture was not confirmed: $taskLog"}
Write-Output "Preview saved: $taskImage"
