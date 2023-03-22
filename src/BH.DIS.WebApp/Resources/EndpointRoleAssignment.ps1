$SERVICE_BUS_READER_ROLE = "4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0"
$SERVICE_BUS_SENDER_ROLE = "69a216fc-b8fb-44d8-bc22-1f3c2cd27a39"

function Set-Default-Resource-Group {
    param (
        [string]
        $resourceGroupName
    )
    $resourceGroup = az group show --name $resourceGroupName | ConvertFrom-Json
    $location = $resourceGroup.location

    az configure `
    --defaults `
        location=$location `
        group=$resourceGroupName
}


function Assign-ServiceBusSubscription {

  param ([string] $assigneeId, [string] $serviceBusNamespace, [string] $topic, [string] $subscription)

  $servicebusNamespaceId = (az servicebus namespace show `
  --name $serviceBusNamespace `
  | ConvertFrom-Json).id

  az role assignment create `
  --role $SERVICE_BUS_READER_ROLE `
  --assignee $assigneeId `
  --scope $servicebusNamespaceId/topics/$topic/subscriptions/$subscription `
  > $null 

  az role assignment create `
  --role $SERVICE_BUS_SENDER_ROLE `
  --assignee $assigneeId `
  --scope $servicebusNamespaceId/topics/$topic `
  > $null

  Write-Host "  Endpoint: $topic"
  Write-Host "  Assignee Id: $assigneeId"
}

az account set -s $subscription

Set-Default-Resource-Group -resourceGroupName $resourceGroupName