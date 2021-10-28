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
$operations = Invoke-PowerBIRestMethod -Url $urlOperations -Method Get | ConvertFrom-Json

foreach($operation in $operations.value){
  $operationId = $operation.Id
  $urlOperation = "pipelines/$pipelineId/operations/$operationId"
  $operation = Invoke-PowerBIRestMethod -Url $urlOperation -Method Get
  $operationDateTime = ([DateTime]($operation | ConvertFrom-Json).executionStartTime).toString("yyyy-MM-dd-HH-mm")
  $operationDateTime
  $fileName = $PSScriptRoot + "\Exports\Get-Operation-$operationDateTime.json"
  $operation | Out-File $fileName
  $fileName
}