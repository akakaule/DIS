$ErrorActionPreference = "Stop"

##############################################
# Get Integration Platform configuration
##############################################

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
$eventId = [BH.DIS.Core.Messages.Constants]::EventId

echo "##vso[task.setvariable variable=broker_id;isOutput=true]$($brokerId.ToLower())"
echo "##vso[task.setvariable variable=resolver_Id;isOutput=true]$($resolverId.ToLower())"
echo "##vso[task.setvariable variable=event_id;isOutput=true]$($eventId.ToLower())"
echo "##vso[task.setvariable variable=manager_id;isOutput=true]$($managerId.ToLower())"

$platform = New-Object -TypeName BH.DIS.BunkerPlatform

$endpoints = @{}

foreach ($endpoint in $platform.Endpoints) {
    $endpointId = $endpoint.Id.ToLower()
    if($endpoint.EventTypesConsumed.Count -ne 0) {
        $eventTypes = @($endpoint.EventTypesConsumed)
        $counter = 0
        foreach ($eventType in $eventTypes) {
            $eventTypes[$counter] = $eventType.Id.ToLower()
            $counter = $counter + 1
        }

        if($eventTypes -eq 1) {
            $endpoints[$endpointId] = ,$eventTypes
        }
        else {
            $endpoints[$endpointId] = $eventTypes
        }
    }
}

$counter = 0
$endpointIds = @($platform.Endpoints.Id)
foreach ($endpointId in $endpointIds) {
    $endpointIds[$counter] = $endpointId.ToLower()
    $counter = $counter + 1
}

$json = ($endpoints | ConvertTo-Json -Compress)

Write-Host $endpointIds
Write-Host $json

echo "##vso[task.setvariable variable=platform_eventtypes_json;isOutput=true]$json"
echo "##vso[task.setvariable variable=platform_endpoints;isOutput=true]$endpointIds"