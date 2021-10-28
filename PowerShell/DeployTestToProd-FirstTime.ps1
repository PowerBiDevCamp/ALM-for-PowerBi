Clear-Host
Write-Host 

Write-Host

Write-Host "Deploy Test to Prod First Time"
Write-Host 

$PipelineName = "Contoso Sales"

$ProdWorkspaceName = "Contoso Sales"

$CapacityId = "AD3BDC40-9F0B-4936-912E-EEA555D3926C"

$pipelines = (Invoke-PowerBIRestMethod -Url "pipelines"  -Method Get | ConvertFrom-Json).value
$pipeline = $pipelines | Where-Object { $_.displayName -eq "$PipelineName" }

$pipelineId = $pipeline.id

# Get Dev Artifacts
$urlDeployDevToTest = "pipelines/$pipelineId/deployAll"

$body = @{ 
  sourceStageOrder = 1
  options = @{
     allowCreateArtifact = $TRUE
     allowOverwriteArtifact = $TRUE
     allowTakeOver = $TRUE
  }
  newWorkspace = @{
     name = $ProdWorkspaceName
     capacityId = $CapacityId
  }
} | ConvertTo-Json


$deployResult = Invoke-PowerBIRestMethod -Url $urlDeployDevToTest -Method Post -Body $body | ConvertFrom-Json

