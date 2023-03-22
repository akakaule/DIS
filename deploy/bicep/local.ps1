###########################################################################
#
# Use this script to test locally
#
###########################################################################
Write-Host "Initialize local deployment" -ForegroundColor Blue

az logout
az login --use-device-code
az account set --subscription "55de24f0-5fa8-4507-b205-f1662c43e441"

$environment = "sbdev"
$solutionId = "bhdis"
$resourceGroupName ="DIS-D-Platform-Sandbox"

& '.\deploy-servicebus.ps1' -solutionId $solutionId -environment $environment -resourceGroupName $resourceGroupName