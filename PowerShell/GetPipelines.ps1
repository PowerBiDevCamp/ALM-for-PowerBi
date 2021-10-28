Clear-Host
Write-Host 

$fileName = $PSScriptRoot + "\Exports\Get-Pipelines.json"


$url = "pipelines"
$pipelinesJson = Invoke-PowerBIRestMethod -Url $url  -Method Get 

$pipelinesJson | Out-File $fileName