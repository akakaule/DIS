Param (
	[Parameter(Mandatory=$True)]
    [string]$solutionId,
    [Parameter(Mandatory=$True)]
    [string]$environment,
	[Parameter(Mandatory=$True)]
    [string]$resourceGroupName
)

##############################################
# Import utility functions
##############################################
. ".\deploy-utilities.ps1"
. ".\deploy-servicebus.ps1"

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

$sbNamespace = "sb-{0}-{1}" -f $solutionId, $environment

$platform = New-Object -TypeName BH.DIS.BunkerPlatform

foreach ($endpoint in $platform.Endpoints) {
    foreach ($roleAssignment in $endpoint.RoleAssignments) {        
        if ($roleAssignment.Environment -eq $Environment)
        {
            Write-Host "AssigneeId:" $roleAssignment.PrincipalId
            Assign-ServiceBusTopicSender -assigneeId $roleAssignment.PrincipalId -serviceBusNamespace $sbNamespace -topic $endpoint.Id -resourceGroupName $resourceGroupName
            Assign-ServiceBusSubscriptionReader -assigneeId $roleAssignment.PrincipalId -serviceBusNamespace $sbNamespace -topic $endpoint.Id -subscription $endpoint.Id -resourceGroupName $resourceGroupName
        }
    }
}