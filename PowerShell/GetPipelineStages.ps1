Clear-Host
Write-Host 

$PipelineName = "Contoso Sales"

$fileName = $PSScriptRoot + "\Exports\Get-Pipeline-Stages.json"


$url = "pipelines"
$pipelinesResult = Invoke-PowerBIRestMethod -Url $url  -Method Get | ConvertFrom-Json
$pipelines = $pipelinesResult.value

$pipeline = $pipelines | Where-Object { $_.displayName -eq "$PipelineName" }

$pipelineId = $pipeline.id

# Get Pipeline Operations
$urlStages = "pipelines/$pipelineId/stages"
$StagesJson = Invoke-PowerBIRestMethod -Url $urlStages -Method Get

$StagesJson | Out-File $fileName