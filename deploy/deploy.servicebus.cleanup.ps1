Param (
	[Parameter(Mandatory=$True)]
    [string]$solutionId,
    [Parameter(Mandatory=$True)]
    [string]$environment,
	[Parameter(Mandatory=$True)]
    [string]$resourceGroupName
)


$ErrorActionPreference = "Stop"

##############################################
# Import utility functions
##############################################
. ".\deploy.utilities.ps1"
. ".\deploy.servicebus.ps1"

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

##############################################
# Get Integration Platform configuration
##############################################
#Add-Type -Path "BH.DIS\BH.DIS.dll"

#    Add-Type -Path "BH.DIS\System.ComponentModel.Annotations.dll"
#    Add-Type -Path "BH.DIS\Newtonsoft.Json.dll"

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


$platform = New-Object -TypeName BH.DIS.BunkerPlatform

$nameOfTo = [BH.DIS.Core.Messages.UserPropertyName]::To.ToString()
$nameOfFrom = [BH.DIS.Core.Messages.UserPropertyName]::From.ToString()
$nameOfEventId = [BH.DIS.Core.Messages.UserPropertyName]::EventId.ToString()

##############################################
# Define names Azure resource names
##############################################
$sbNamespace = "sb-{0}-{1}" -f $solutionId, $environment

$region="westeurope"

##############################################
# Install Azure CLI extensions
##############################################

Try {
    Add-Extension -name application-insights
}
Catch {
    if($_.Exception.Message -eq "WARNING: Extension 'application-insights' is already installed.")
    {
        Write-Host "Extension 'application-insights' is already installed."
    }
    else
    {
        throw
    }
}

##############################################
# Delete Obsolete Topics
##############################################

$obsoleteTopics = New-Object System.Collections.ArrayList

$topics = New-Object System.Collections.ArrayList

$topics.Add($resolverId) > $null
$topics.Add($brokerId) > $null
$topics.Add($managerId) > $null
$topics.Add($eventId) > $null

foreach ($endpoint in $platform.Endpoints) {
    $topics.Add($endpoint.Id) > $null
}

$existingTopics = az servicebus topic list `
    --namespace-name $sbNamespace `
    --resource-group $resourceGroupName
    | ConvertFrom-Json

foreach($existingTopic in $existingTopics){
    If($topics -notcontains $existingTopic.name){
        $obsoleteTopics.Add($existingTopic.name) > $null
    }
}

foreach($obsoleteTopic in $obsoleteTopics){
    Write-Host 'Deleting topic '$obsoleteTopic
    #az servicebus topic delete `
    #    --namespace-name $sbNamespace `
    #    --resource-group $resourceGroupName `
    #    --name $obsoleteTopic
}

Write-Host 'Done Deleting obsolete Topics'

##############################################
# Delete Obsolete Subscriptions on endpointTopics
##############################################

foreach($endpoint in $platform.Endpoints){
    $obsoleteforwardSubscriptions = New-Object System.Collections.ArrayList

    $existingSubscriptions = az servicebus topic subscription list `
    --namespace-name $sbNamespace `
    --resource-group $resourceGroupName `
    --topic-name $endpoint.Id
    | ConvertFrom-Json

    $forwardSubscriptions = New-Object System.Collections.ArrayList
    $forwardSubscriptions.Add($brokerId) > $null
    $forwardSubscriptions.Add($ContinuationId) > $null
    $forwardSubscriptions.Add($resolverId) > $null
    $forwardSubscriptions.Add($retryId) > $null
    $forwardSubscriptions.Add($endpoint.Id) > $null

    foreach($forwardSub in $existingSubscriptions){
        if($forwardSub -ne $null){
            if($forwardSubscriptions -notcontains $forwardSub.name){
                $obsoleteforwardSubscriptions.Add($forwardSub.name) > $null
            }
        }        
    }

    #Delete obsolete subscriptions on endpointTopics
    foreach($fwsub in $obsoleteforwardSubscriptions){
        Write-Host 'Deleting subscription' $fwsub 'on' $endpoint.Id
        az servicebus topic subscription delete  `
            --namespace-name $sbNamespace `
            --resource-group $resourceGroupName `
            --topic-name $endpoint.Id `
            --name $fwsub
    }
}

Write-Host 'Done Deleting obsolete endpoint subscriptions'

##############################################
# Delete Obsolete Subscriptions on systemTopics
##############################################
#Manager
$managerExistingSubscriptions = az servicebus topic subscription list `
    --namespace-name $sbNamespace `
    --resource-group $resourceGroupName `
    --topic-name $managerId
    | ConvertFrom-Json


$manSubs = New-Object System.Collections.ArrayList
$manSubs.Add($brokerId) > $null
foreach($endpoint in $platform.Endpoints){
    $manSubs.Add($endpoint.Id) > $null
}

foreach($manExSub in $managerExistingSubscriptions){
    If($manSubs -notcontains $manExSub.name){
        Write-Host 'Deleting manager-topic subscription'$manExSub.name
        az servicebus topic subscription delete  `
            --namespace-name $sbNamespace `
            --resource-group $resourceGroupName `
            --topic-name $managerId `
            --name $manExSub.name
    }
}

#Broker
$brokerExistingSubscriptions = az servicebus topic subscription list `
    --namespace-name $sbNamespace `
    --resource-group $resourceGroupName `
    --topic-name $brokerId
    | ConvertFrom-Json

$brokerSubs = New-Object System.Collections.ArrayList
$brokerSubs.Add($brokerId) > $null
$brokerSubs.Add($resolverId) > $null
foreach($endpoint in $platform.Endpoints){
    $brokerSubs.Add($endpoint.Id) > $null
}

foreach($brokerExSub in $brokerExistingSubscriptions){
    If($brokerSubs -notcontains $brokerExSub.name){
        Write-Host 'Deleting broker-topic subscription' $brokerExSub.name
        az servicebus topic subscription delete  `
            --namespace-name $sbNamespace `
            --resource-group $resourceGroupName `
            --topic-name $brokerId `
            --name $brokerExSub.name
    }
}

Write-Host 'Done Deleting obsolete Subscriptions'

##############################################
# Delete Obsolete Subscriptions Rules on EndpointSubscriptions
##############################################

foreach($endpoint in $platform.Endpoints){
    #Get all subscription rules on topics named after endpoints
    $topicSubscriptionRules = az servicebus topic subscription rule list `
        --namespace-name $sbNamespace `
        --resource-group $resourceGroupName `
        --topic-name $endpoint.Id `
        --subscription-name $endpoint.Id
        | ConvertFrom-Json

    $obsoleteEndpointSups = New-Object System.Collections.ArrayList
    foreach($subRule in $topicSubscriptionRules){
        $subRuleName = $endpoint.id
        If($subRule.name -ne "to-$subRuleName"){
            Write-Host 'EndpointSubRule' $subRule.name 'not equal '"to-$subRuleName" 
            $obsoleteEndpointSups.Add($subRule.name) > $null
            az servicebus topic subscription rule delete  `
            --namespace-name $sbNamespace `
            --resource-group $resourceGroupName `
            --topic-name $endpoint.Id `
            --subscription-name $endpoint.Id `
            --name $subRule.name
        }        
    }

    $brokerSubscriptionRules = az servicebus topic subscription rule list `
        --namespace-name $sbNamespace `
        --resource-group $resourceGroupName `
        --topic-name $endpoint.Id `
        --subscription-name $brokerId
        | ConvertFrom-Json

    foreach($brokerSubRule in $brokerSubscriptionRules){
        $subRuleName = $endpoint.Id
        If($brokerSubRule.name -ne "from-$subRuleName"){
            Write-Host 'brokerSubRule ' $brokerSubRule.name 'not equal '"from-$subRuleName" 
            az servicebus topic subscription rule delete  `
            --namespace-name $sbNamespace `
            --resource-group $resourceGroupName `
            --topic-name $endpoint.Id `
            --subscription-name $brokerId `
            --name $$brokerSubRule.name
        }        
    }

    $contSubscriptionRules = az servicebus topic subscription rule list `
        --namespace-name $sbNamespace `
        --resource-group $resourceGroupName `
        --topic-name $endpoint.Id `
        --subscription-name $continuationId
        | ConvertFrom-Json

    foreach($contSubRule in $contSubscriptionRules){
        If($contSubRule.name -ne "$continuationId"){
            Write-Host 'continuationSubRule ' $contSubRule.name 'not equal '"$continuationId"         
            az servicebus topic subscription rule delete  `
            --namespace-name $sbNamespace `
            --resource-group $resourceGroupName `
            --topic-name $endpoint.Id `
            --subscription-name $continuationId `
            --name $contSubRule.name
        }        
    }

    $resolverSubscriptionRules = az servicebus topic subscription rule list `
        --namespace-name $sbNamespace `
        --resource-group $resourceGroupName `
        --topic-name $endpoint.Id `
        --subscription-name $resolverId
        | ConvertFrom-Json

    foreach($resSubRule in $resolverSubscriptionRules){
        $subRuleName = $endpoint.Id
        If($resSubRule.name -ne "from-$subRuleName"){
            If($resSubRule.name -ne "to-$subRuleName"){
                Write-Host 'ResolverSubRule' $resSubRule.name 'not equal '"to-$subRuleName" 'or' "from-$subRuleName" 
                az servicebus topic subscription rule delete  `
                --namespace-name $sbNamespace `
                --resource-group $resourceGroupName `
                --topic-name $endpoint.Id `
                --subscription-name $resolverId `
                --name $resSubRule.name
            }
        }
    }



    $retrySubscriptionRules = az servicebus topic subscription rule list `
        --namespace-name $sbNamespace `
        --resource-group $resourceGroupName `
        --topic-name $endpoint.Id `
        --subscription-name $retryId
        | ConvertFrom-Json

    foreach($retrySubRule in $retrySubscriptionRules){
        If($retrySubRule.name -ne $retryId){
            Write-Host 'retrytSubRule ' $retrySubRule.name 'not equal'"$retryId" 
            az servicebus topic subscription rule delete  `
            --namespace-name $sbNamespace `
            --resource-group $resourceGroupName `
            --topic-name $endpoint.Id `
            --subscription-name $retryId `
            --name $retrySubRule.name
        }        
    }
}

#System topic subscription
#broker
foreach($endpoint in $platform.Endpoints){
    If($endpoint.EventTypesConsumed.Count -ne 0){
        $brokerTopicEndpointSubscriptionRules = az servicebus topic subscription rule list `
            --namespace-name $sbNamespace `
            --resource-group $resourceGroupName `
            --topic-name $brokerId `
            --subscription-name $endpoint.Id
            | ConvertFrom-Json
    
        $eventTypesConsumed = New-Object System.Collections.ArrayList
        $endpointId = $endpoint.Id
        $eventTypesConsumed.Add("to-$endpointId") > $null
        foreach($event in $endpoint.EventTypesConsumed){
            $eventTypesConsumed.Add($event.Id) > $null
        }
    

        foreach($brokerEndpointrule in $brokerTopicEndpointSubscriptionRules){
            $endpointId = $endpoint.Id
            If($eventTypesConsumed -notcontains $brokerEndpointrule.name){
                Write-Host 'broker topic endpoint' $endpointId 'sub: '$brokerEndpointrule.name 'not in EventTypesConsumed'
                az servicebus topic subscription rule delete  `
                    --namespace-name $sbNamespace `
                    --resource-group $resourceGroupName `
                    --topic-name $brokerId `
                    --subscription-name $endpoint.Id `
                    --name $brokerEndpointrule.name
            }
        }
    }
}

#manager
foreach($endpoint in $platform.Endpoints){
    $managerTopicEndpointSubscriptionRules = az servicebus topic subscription rule list `
        --namespace-name $sbNamespace `
        --resource-group $resourceGroupName `
        --topic-name $managerId `
        --subscription-name $endpoint.Id
        | ConvertFrom-Json

    foreach($managerEndpointrule in $managerTopicEndpointSubscriptionRules){
        $endpointId = $endpoint.Id
        If($managerEndpointrule.name -ne "to-$endpointId"){
            Write-Host 'manager topic endpoint sub: '$managerEndpointrule.name 'not equal' "to-$endpointId"
            az servicebus topic subscription rule delete  `
                --namespace-name $sbNamespace `
                --resource-group $resourceGroupName `
                --topic-name $managerId `
                --subscription-name $endpoint.Id `
                --name $managerEndpointrule.name
        }
    }
}

