$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskExe=Join-Path $taskRoot 'IslandVoyage\Builds\Windows\IslandVoyage.exe'
if(!(Test-Path -LiteralPath $taskExe)){throw 'Build the Windows game first.'}
$taskLog=Join-Path $PSScriptRoot 'Logs\Runtime.log'
$taskPictures=Join-Path $taskRoot 'Screenshots'
New-Item -ItemType Directory -Path $taskPictures -Force | Out-Null
$taskImage=Join-Path $taskPictures 'IslandVoyage.png'
$taskArguments=@('--self-test','--capture',('"'+$taskImage+'"'),'-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',('"'+$taskLog+'"'))
Write-Output 'Running the built game and scene checks...'
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -Wait
if($taskProcess.ExitCode -ne 0){throw "Runtime tests failed ($($taskProcess.ExitCode)); see $taskLog"}
$taskLines=Get-Content -LiteralPath $taskLog
if(!($taskLines -match 'VOYAGE_RUNTIME_TESTS_PASSED')){throw 'Runtime validation did not report success.'}
if(!($taskLines -match 'VOYAGE_FISHING_TESTS_PASSED')){throw 'Fishing validation did not report success.'}
if(!($taskLines -match 'VOYAGE_ENVIRONMENT_TESTS_PASSED')){throw 'Environment validation did not report success.'}
if($taskLines -match 'NullReferenceException|MissingReferenceException|Shader error|shader is not supported|ArgumentException|InvalidOperationException|ERROR:'){throw 'Runtime log contains an error.'}
$taskLines | Select-String 'VOYAGE_RUNTIME_TESTS_PASSED|VOYAGE_FISHING_DETAIL|VOYAGE_FISHING_TESTS_PASSED|VOYAGE_ENVIRONMENT_TESTS_PASSED'
if(!(Test-Path -LiteralPath $taskImage)){throw 'Runtime screenshot was not produced.'}
Write-Output "Screenshot: $taskImage"
