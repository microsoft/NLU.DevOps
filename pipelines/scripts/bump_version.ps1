$version = $args[0]
Write-Host "Set version: $version"

$pathToJson = "extensions\tasks\NLUCleanV0\task.json"
$a = Get-Content $pathToJson | ConvertFrom-Json
$a.version.patch = $version
ConvertTo-Json $a | set-content $pathToJson

$pathToJson = "extensions\tasks\NLUTestV0\task.json"
$a = Get-Content $pathToJson | ConvertFrom-Json
$a.version.patch = $version
ConvertTo-Json $a | set-content $pathToJson

$pathToJson = "extensions\tasks\NLUTrainV0\task.json"
$a = Get-Content $pathToJson | ConvertFrom-Json
$a.version.patch = $version
ConvertTo-Json $a | set-content $pathToJson