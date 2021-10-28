Clear-Host
Write-Host 

Write-Host

Write-Host "Deploy Dev to Test - First Time"
Write-Host 

$PipelineName = "Contoso Sales"

$TestWorkspaceName = "Contoso Sales Test"

$CapacityId = "AD3BDC40-9F0B-4936-912E-EEA555D3926C"

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
  newWorkspace = @{
     name = $TestWorkspaceName
     capacityId = $CapacityId
  }
} | ConvertTo-Json


$deployResult = Invoke-PowerBIRestMethod -Url $urlDeployDevToTest -Method Post -Body $body | ConvertFrom-Json

