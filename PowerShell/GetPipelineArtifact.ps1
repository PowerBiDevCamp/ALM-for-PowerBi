Clear-Host
Write-Host 

$PipelineName = "Contoso Sales"

$url = "pipelines"
$pipelinesResult = Invoke-PowerBIRestMethod -Url $url  -Method Get | ConvertFrom-Json
$pipelines = $pipelinesResult.value

$pipeline = $pipelines | Where-Object { $_.displayName -eq "$PipelineName" }

$pipelineId = $pipeline.id

# Get Pipeline Artifacts in Dev Stage (stage=0)
$urlartifacts= "pipelines/$pipelineId/stages/0/artifacts"
$artifactsJson = Invoke-PowerBIRestMethod -Url $urlartifacts -Method Get
$fileName = $PSScriptRoot + "\Exports\Get-Pipeline-Artifacts-Stage0-Dev.json"
$artifactsJson | Out-File $fileName 

# Get Pipeline Artifacts in Test Stage (stage=1)
$urlartifacts= "pipelines/$pipelineId/stages/1/artifacts"
$artifactsJson = Invoke-PowerBIRestMethod -Url $urlartifacts -Method Get
$fileName = $PSScriptRoot + "\Exports\Get-Pipeline-Artifacts-Stage1-Test.json"
$artifactsJson | Out-File $fileName 

# Get Pipeline Artifacts in Production Stage (stage=2)
$urlartifacts= "pipelines/$pipelineId/stages/2/artifacts"
$artifactsJson = Invoke-PowerBIRestMethod -Url $urlartifacts -Method Get
$fileName = $PSScriptRoot + "\Exports\Get-Pipeline-Artifacts-Stage2-Production.json"
$artifactsJson | Out-File $fileName 

