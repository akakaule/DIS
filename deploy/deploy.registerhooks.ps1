Param (
	[Parameter(Mandatory=$True)]
    [string]$solutionId,
    [Parameter(Mandatory=$True)]
    [string]$environment,
	[Parameter(Mandatory=$True)]
    [string]$resourceGroupName,
    [Parameter(Mandatory=$True)]
    [string]$tenantId,
    [Parameter(Mandatory=$True)]
    [string]$clientId
)

$ErrorActionPreference = "Stop"

##############################################
# Import utility functions
##############################################
. ".\deploy.utilities.ps1"

##############################################
# Remove non-alpha-numeric characters
##############################################
$solutionId = $solutionId.ToLower() -replace "\W"
$environment = $environment.ToLower() -replace "\W"

##############################################
# Set default resource group and location
##############################################
Set-Default-Resource-Group -resourceGroupName $resourceGroupName

##############################################
# Define names Azure resource names
##############################################
$messageStoreStorageAccountName = "st{0}{1}msgstore" -f $solutionId, $environment

$managementWebAppName = "webapp-{0}-{1}-management" -f $solutionId, $environment

# Register event subscription to message store for the management app 
$storageid=$(az storage account show --name $messageStoreStorageAccountName --query id --output tsv)
$endpoint="https://$($managementWebAppName).azurewebsites.net/api/storagehook?code=code"

az eventgrid event-subscription create `
  --source-resource-id $storageid `
  --name "esb-messagestore-update-es" `
  --endpoint $endpoint #`
  #--endpoint-type webhook `
  #--azure-active-directory-tenant-id $tenantId `
  #--azure-active-directory-application-id-or-uri $clientId