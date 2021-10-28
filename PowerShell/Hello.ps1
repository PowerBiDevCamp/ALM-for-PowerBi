Clear-Host
Write-Host 

$PipelineName = "Contoso Sales"

# log into Azure AD as service principal
$tenantId = "2f23c5ea-5a75-41f6-922e-d3392313e61d"
$applictionId = "cb44fdd0-8330-41ea-bfd4-27525cbdbf67"
$applicationSecret = "OTA1ODUxNmEtNjJiYS00YjM5LWI3OTYtZDRmNjI5NDgxNTZh="

$SecuredApplicationSecret = ConvertTo-SecureString -String $applicationSecret -AsPlainText -Force
$credential = New-Object -TypeName System.Management.Automation.PSCredential `
                         -ArgumentList $applictionId, $SecuredApplicationSecret

$sp = Connect-PowerBIServiceAccount -ServicePrincipal -Tenant $tenantId -Credential $credential

$url = "pipelines"
$pipelinesResult = Invoke-PowerBIRestMethod -Url $url  -Method Get | ConvertFrom-Json
$pipelines = $pipelinesResult.value



$pipeline = $pipelines | Where-Object { $_.displayName -eq "$PipelineName" }

$pipelineId = $pipeline.id

$pipelineId

# Get Dev Artifacts
$urlOperations = "pipelines/$pipelineId/operations"
$operations = Invoke-PowerBIRestMethod -Url $urlOperations -Method Get | ConvertFrom-Json


$operations.value | Format-Table
