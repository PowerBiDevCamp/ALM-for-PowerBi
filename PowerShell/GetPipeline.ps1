Clear-Host
Write-Host 

$PipelineName = "Contoso Sales"

$url = "pipelines"
$pipelinesResult = Invoke-PowerBIRestMethod -Url $url  -Method Get | ConvertFrom-Json
$pipelines = $pipelinesResult.value

$pipeline = $pipelines | Where-Object { $_.displayName -eq "$PipelineName" }

$pipelineId = $pipeline.id

$pipelineId