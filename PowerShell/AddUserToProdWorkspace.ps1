Clear-Host
Write-Host 

$userEmail = "tedp@powerbidevcamp.net"

$workspace = Get-PowerBIGroup -Name "Contoso Sales"

Add-PowerBIWorkspaceUser -Id $workspace.Id -UserEmailAddress $userEmail -AccessRight Admin
