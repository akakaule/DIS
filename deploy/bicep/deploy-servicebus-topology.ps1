Param (
	[Parameter(Mandatory=$True)]
    [string]$solutionId,
    [Parameter(Mandatory=$True)]
    [string]$environment,
	[Parameter(Mandatory=$True)]
    [string]$resourceGroupName
)

$CONTRIBUTOR_ROLE = "b24988ac-6180-42a0-ab88-20f7382dd24c"
$READER_ROLE = "acdd72a7-3385-48ef-bd42-f606fba81ae7"

$ErrorActionPreference = "Stop"

##############################################
# Import utility functions
##############################################
. ".\deploy-utilities.ps1"
. ".\deploy-servicebus.ps1"

##############################################
# Remove non-alpha-numeric characters
##############################################
$solutionId = $solutionId.ToLower() -replace "\W"
$environment = $environment.ToLower() -replace "\W"

##############################################
# Set default resource group and location
##############################################
Set-Default-Resource-Group -resourceGroupName $resourceGroupName

# Enable Microsoft.EventGrid in the subscription if it isn't already registered
az provider register --namespace Microsoft.EventGrid

try
{
    Add-Type -Path "BH.DIS\BH.DIS.dll"
}
catch [System.Reflection.ReflectionTypeLoadException]
{
    Write-Host "Could not load BH.DIS.dll"
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Green
    Write-Host "StackTrace: $($_.Exception.StackTrace)" -ForegroundColor Yellow
    Write-Host "LoaderExceptions: $($_.Exception.LoaderExceptions)" -ForegroundColor Cyan
    throw
}

try
{
    Add-Type -Path "BH.DIS\BH.DIS.Core.dll"
}
catch [System.Reflection.ReflectionTypeLoadException]
{
    Write-Host "Could not load BH.DIS.Core.dll"
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Green
    Write-Host "StackTrace: $($_.Exception.StackTrace)" -ForegroundColor Yellow
    Write-Host "LoaderExceptions: $($_.Exception.LoaderExceptions)" -ForegroundColor Cyan
    throw
}

$brokerId = [BH.DIS.Core.Messages.Constants]::BrokerId
$resolverId = [BH.DIS.Core.Messages.Constants]::ResolverId
$managerId = [BH.DIS.Core.Messages.Constants]::ManagerId
$continuationId = [BH.DIS.Core.Messages.Constants]::ContinuationId
$eventId = [BH.DIS.Core.Messages.Constants]::EventId
$retryId = [BH.DIS.Core.Messages.Constants]::RetryId


$platform = New-Object -TypeName BH.DIS.PlatformConfiguration

$nameOfTo = [BH.DIS.Core.Messages.UserPropertyName]::To.ToString()
$nameOfFrom = [BH.DIS.Core.Messages.UserPropertyName]::From.ToString()
$nameOfEventId = [BH.DIS.Core.Messages.UserPropertyName]::EventId.ToString()

##############################################
# Define names Azure resource names
##############################################

##############################################
# Scope the functions for use inside parallel loops
##############################################

$csbtDef = ${function:Create-ServiceBusTopic}.ToString()
$csbsDef = ${function:Create-ServiceBusSubscription}.ToString()
$csbsrDef = ${function:Create-ServiceBusSubscriptionRule}.ToString()
$csbfsDef = ${function:Create-ServiceBusForwardSubscription}.ToString()

##############################################
# Create Service Bus topics
##############################################

$topics = New-Object System.Collections.ArrayList
foreach ($endpoint in $platform.Endpoints) {
    $topics.Add($endpoint.Id)
}

# Forward-to-eventtype subscriptions
$producingEvents = New-Object System.Collections.ArrayList

foreach ($_ in $platform.Endpoints) {   
    $endpointId = $_.Id
    if($_.EventTypesConsumed.Count -ne 0){
        $endpointAndEvents = $endpointId
        foreach ($eventType in $_.EventTypesConsumed) {
            $eventTypeId = $eventType.Id
            $endpointAndEvents += "-$eventTypeId"
        }
        $producingEvents.Add($endpointAndEvents)
    }
}

#Write-Host 'Created Subscriptions'
#Write-Host ''
##############################################
# Endpoints: Create Service Bus subscriptions
##############################################
$consumingEvents = New-Object System.Collections.ArrayList

foreach ($ep in $platform.Endpoints) {
    $eventsProduced = $ep.EventTypesProduced
    $endpointString = $ep.Id
    $DoesEndpointProduceEvents = $false
    foreach ($eT in $eventsProduced){
        $DoesEndpointProduceEvents = $true
        # Get consuming endpoints    
        $consumingEndpoints = New-Object System.Collections.ArrayList
        $TypeId = $eT.Id
        foreach ($endpoint in $platform.Endpoints) {
            # Find endpoints that consumes eventtype
            if ( $endpoint.EventTypesConsumed -contains $eT )
            {
              $consumingEndpoints.Add($endpoint.Id)
            }                
        }
        # Iterate consuming endpoints and add to string
        foreach($ce in $consumingEndpoints){
            $endpointString += "-$ce"+"_$TypeId"
        }
        #$endpointString += "_$TypeId"        
    }
    #If endpoint produces events add to list
    if($DoesEndpointProduceEvents -eq $True) {
        $consumingEvents.Add($endpointString)
    }
}

#Write-Host 'Created Endpoint Subscriptions'
#Write-Host ''
##############################################
# Manager: Create Service Bus subscriptions
##############################################

#Write-Host 'Created Manager Subscriptions'
#Write-Host ''

Write-Host $topics
Write-Host ''
Write-Host $producingEvents
Write-Host ''
Write-Host $consumingEvents

$output = az deployment group create `
  --resource-group $resourceGroupName `
  --template-file bicep/deploy-servicebus-topology.bicep `
  --parameters `
    solutionId=$solutionId `
    environment=$environment `
    resolverId=$resolverId `
    managerId=$managerId `
    eventId=$eventId `
    retryId=$retryId `
    continuationId=$continuationId `
    endpoints=$topics `
    producingEvents=$producingEvents `
    consumingEvents=$consumingEvents

Throw-WhenError -output $output