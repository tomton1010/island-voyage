param([string]$File)
$ErrorActionPreference='Stop'
$taskEnvironment=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'BlenderEnvironment.json') -Raw | ConvertFrom-Json
if(!(Test-Path -LiteralPath $taskEnvironment.executable)){throw 'Blender is not installed at the recorded location.'}
if($File){
    $taskFile=(Resolve-Path -LiteralPath $File).Path
    Start-Process -FilePath $taskEnvironment.executable -ArgumentList ('"'+$taskFile+'"')
} else {
    Start-Process -FilePath $taskEnvironment.executable
}
