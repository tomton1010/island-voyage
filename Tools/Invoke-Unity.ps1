param([ValidateSet('Setup','ValidateRules','Windows','Web')][string]$Task='Setup')
$ErrorActionPreference='Stop'
$taskProject=Join-Path (Split-Path $PSScriptRoot -Parent) 'IslandVoyage'
$taskEditor='E:\Unity\Editors\6000.6.3f1\Editor\Unity.exe'
if(!(Test-Path -LiteralPath $taskEditor)){throw 'Unity 6000.6.3f1 is not installed in the expected directory.'}
$taskLogs=Join-Path $PSScriptRoot 'Logs'
New-Item -ItemType Directory -Path $taskLogs -Force | Out-Null
$taskLog=Join-Path $taskLogs ($Task+'.log')
$taskArguments=@('-batchmode','-nographics','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod',('VoyageBuild.'+$Task),'-logFile',('"'+$taskLog+'"'))
Write-Output "Unity $Task started. Log: $taskLog"
$taskProcess=Start-Process -FilePath $taskEditor -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -Wait
Write-Output "Unity $Task exit code: $($taskProcess.ExitCode)"
if($taskProcess.ExitCode -ne 0){throw "Unity $Task failed; inspect $taskLog"}
