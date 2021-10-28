Clear-Host
Write-Host 

$PipelineName = "Contoso Sales"

$fileName = $PSScriptRoot + "\Exports\Get-Pipeline-Operations.json"


$url = "pipelines"
$pipelinesResult = Invoke-PowerBIRestMethod -Url $url  -Method Get | ConvertFrom-Json
$pipelines = $pipelinesResult.value

$pipeline = $pipelines | Where-Object { $_.displayName -eq "$PipelineName" }

$pipelineId = $pipeline.id

# Get Pipeline Operations
$urlOperations = "pipelines/$pipelineId/operations"
$operationsJson = Invoke-PowerBIRestMethod -Url $urlOperations -Method Get

$operationsJson | Out-File $fileName