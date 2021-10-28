Clear-Host
Write-Host 

Write-Host

Write-Host "Deploy Dev to Test"
Write-Host 

$PipelineName = "Contoso Sales"

$pipelines = (Invoke-PowerBIRestMethod -Url "pipelines"  -Method Get | ConvertFrom-Json).value
$pipeline = $pipelines | Where-Object { $_.displayName -eq "$PipelineName" }

$pipelineId = $pipeline.id

# Get Dev Artifacts
$urlDeployDevToTest = "pipelines/$pipelineId/deployAll"

$body = @{ 
  sourceStageOrder = 0
  options = @{
     allowCreateArtifact = $TRUE
     allowOverwriteArtifact = $TRUE
     allowTakeOver = $TRUE
  }
} | ConvertTo-Json


$deployResult = Invoke-PowerBIRestMethod -Url $urlDeployDevToTest -Method Post -Body $body | ConvertFrom-Json

