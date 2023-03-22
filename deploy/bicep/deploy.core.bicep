param solutionId string
param environment string = 'dev'
param locationParam string = 'westeurope'
param resolverId string
param uniqueDeploy string

//##############################################
// Define names Azure resource names
//##############################################
var resourceNamePostFix = substring(uniqueString(resourceGroup().id), 0, 4)

var location = locationParam

var sbNamespace = 'sb-${toLower(solutionId)}-${toLower(environment)}-${toLower(resourceNamePostFix)}'

var managementAppServicePlanName = 'asp-${toLower(solutionId)}-${toLower(environment)}-management'

var coreAppServicePlanName = 'asp-${toLower(solutionId)}-${toLower(environment)}-core'

var cosmosAccountName = 'cosmos-${toLower(solutionId)}-${toLower(environment)}-${toLower(resourceNamePostFix)}'

var cosmosDbName = 'MessageDatabase'

var cosmosFunctionAppName = 'func-${toLower(solutionId)}-${toLower(environment)}-cosmos-${toLower(resourceNamePostFix)}'

var resolverFunctionAppName = 'func-${toLower(solutionId)}-${toLower(environment)}-resolver-${toLower(resourceNamePostFix)}'

var eventPublisherFunctionAppName = 'func-${toLower(solutionId)}-${toLower(environment)}-event-${toLower(resourceNamePostFix)}'

var heartbeatFunctionAppname = 'func-${toLower(solutionId)}-${toLower(environment)}-heartbeat-${toLower(resourceNamePostFix)}'

var workloadWebjobName = 'webjob-${toLower(solutionId)}-${toLower(environment)}-workload-${toLower(resourceNamePostFix)}'

var messageStoreStorageAccountName = 'st${toLower(solutionId)}${toLower(environment)}msgstore${toLower(resourceNamePostFix)}'

var managementWebAppName = 'webapp-${toLower(solutionId)}-${toLower(environment)}-management-${toLower(resourceNamePostFix)}'

var appInsightsName = 'ai-${toLower(solutionId)}-${toLower(environment)}-global-tracelog'

var eventGridTopic = 'eg-${toLower(solutionId)}-${toLower(environment)}-topic'

var eventGridSubscription = 'eg-sub-${toLower(solutionId)}-${toLower(environment)}'

var queueName = 'eg-notify-queue'

var storageHookUrl = 'https://${managementWebAppName}.azurewebsites.net/api/storagehook/cosmos/'

//##############################################
//# Create Service Bus namespace
//##############################################

module serviceBusNamespace 'templates/servicebusNamespace.bicep' = {
  name : 'ServicebusNamespaceDeploy'
  params : {
    name: sbNamespace
    location: location
  }
}

//##############################################
//# Create Application Insights (for global trace log)
//##############################################

//TODO: MANGLER API KEY

module applicationinsights 'templates/applicationInsights.bicep' = {
  name: 'AppinsightsDeploy'
  params: {
    name: appInsightsName
    location: location
  }
}

//##############################################
//# Cosmos DB: Create Account and Database
//##############################################

module cosmosAccount 'templates/cosmosDB.bicep' = {
  name: 'cosmosDBDeploy'
  params: {
    name: cosmosAccountName
    dbname: cosmosDbName
    location: location
  }
}

//##############################################
//# Message Store: Create storage account
//##############################################

module storageaccount 'templates/storageaccount.bicep' = {
  name : 'StorageAccountDeploy'
  params : {
    name: messageStoreStorageAccountName
    location: location
    queueName: queueName
  }
}

//##############################################
//# Create App Service Plan for management app
//##############################################

module appserviceplan 'templates/appServicePlan.bicep' = {
  name: 'ManagementPlanDeploy'
  params: {
    name: managementAppServicePlanName
    skuName: 'S1'
    location: location
  }
}

//##############################################
//# Create App Service Plan for function apps
//##############################################

module functionappplan 'templates/functionAppPlan.bicep' = {
  name: 'functionAppplanDeploy'
  params: {
    name: coreAppServicePlanName
    skuName: 'EP1'
    location: location
  }
}


//##############################################
//# Resolver: Create Function app
//##############################################

var resolverappsettings = [
  {
    name: 'GlobalTraceLogInstrKey'
    value: applicationinsights.outputs.instrumentationKey
  }
  {
    name: 'ServiceBusNamespace'
    value: sbNamespace
  }
  {
    name: 'ResolverId'
    value: resolverId
  }
  {
    name: 'MessageStoreStorageAccount'
    value: messageStoreStorageAccountName
  }
  {
    name: 'CosmosConnection'
    value: cosmosAccount.outputs.connectionString
  }
  {
    name:'MessageStoreStorageConnection'
    value: storageaccount.outputs.connectionString
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: storageaccount.outputs.connectionString
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: '${toLower(resolverFunctionAppName)}${uniqueString(uniqueDeploy)}'
  }
  {
    name: 'AzureWebJobsServiceBus'
    value: serviceBusNamespace.outputs.SharedAccessKey
  }
]

module resolverFunction 'templates/functionApp.bicep' = {
  name: 'resolverDeploy'
  params: {
    appName: resolverFunctionAppName
    appInsightsInstrumentationKey: applicationinsights.outputs.instrumentationKey
    appServicePlanId: functionappplan.outputs.id
    functionAppVersion: '4'
    storageConnectionString: storageaccount.outputs.connectionString
    location: location
    settings: resolverappsettings
  }

}

//##############################################
//# EventPublisher: Create Function app 
//##############################################

var eventpublisherappsettings = [
  {
    name: 'GlobalTraceLogInstrKey'
    value: applicationinsights.outputs.instrumentationKey
  }
  {
    name: 'ServiceBusConnection'
    value: serviceBusNamespace.outputs.SharedAccessKey
  }
  {
    name:'MessageStoreStorageConnection'
    value: storageaccount.outputs.connectionString
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: storageaccount.outputs.connectionString
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: '${toLower(eventPublisherFunctionAppName)}${uniqueString(uniqueDeploy)}'
  }
]

module eventpublisher 'templates/functionApp.bicep' = {
  name: 'eventpublisherDeploy'
  params: {
    appName: eventPublisherFunctionAppName
    appInsightsInstrumentationKey: applicationinsights.outputs.instrumentationKey
    appServicePlanId: functionappplan.outputs.id
    functionAppVersion: '4'
    storageConnectionString: storageaccount.outputs.connectionString
    location: location
    settings: eventpublisherappsettings
  }
}

//##############################################
//# Heartbeat: Create Function app 
//##############################################

var heartbeatappsettings = [
  {
    name: 'GlobalTraceLogInstrKey'
    value: applicationinsights.outputs.instrumentationKey
  }
  {
    name: 'AzureWebJobsServiceBus'
    value: serviceBusNamespace.outputs.SharedAccessKey
  }
  {
    name: 'CosmosConnectionString'
    value: cosmosAccount.outputs.connectionString
  }
  {
    name: 'StoragehookUrl'
    value: storageHookUrl
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: storageaccount.outputs.connectionString
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: '${toLower(heartbeatFunctionAppname)}${uniqueString(uniqueDeploy)}'
  }
]

module heartbeat 'templates/functionApp.bicep' = {
  name: 'heartbeatDeploy'
  params: {
    appName: heartbeatFunctionAppname
    appInsightsInstrumentationKey: applicationinsights.outputs.instrumentationKey
    appServicePlanId: functionappplan.outputs.id
    functionAppVersion: '4'
    storageConnectionString: storageaccount.outputs.connectionString
    location: location
    settings: heartbeatappsettings
  }
}

//##############################################
//# CosmosSubscriber: Create Function app 
//##############################################

var cosmossubscriberappsettings = [
  {
    name: 'GlobalTraceLogInstrKey'
    value: applicationinsights.outputs.instrumentationKey
  }
  {
    name: 'AppInsights:ApplicationId'
    value: applicationinsights.outputs.appId
  }
  {
    name:'CosmosDBConnection'
    value: cosmosAccount.outputs.connectionString
  }
  {
    name:'StoragehookUrl'
    value: storageHookUrl
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: storageaccount.outputs.connectionString
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: '${toLower(cosmosFunctionAppName)}${uniqueString(uniqueDeploy)}'
  }
  {
    name: 'evenGridKey'
    value: eventGrid.outputs.eventGridKey
  }
  {
    name: 'eventGridUrl'
    value: eventGrid.outputs.eventGridUrl
  }
]

module cosmossubscriber 'templates/functionApp.bicep' = {
  name: 'cosmosSubscriberDeploy'
  dependsOn: [
    eventGrid
  ]
  params: {
    appName: cosmosFunctionAppName
    appInsightsInstrumentationKey: applicationinsights.outputs.instrumentationKey
    appServicePlanId: functionappplan.outputs.id
    functionAppVersion: '4'
    storageConnectionString: storageaccount.outputs.connectionString
    location: location
    settings: cosmossubscriberappsettings
  }
}

//##############################################
//# Create EventGrid
//##############################################

module eventGrid 'templates/eventgrid.bicep' = {
  name: 'eventGridDeploy'
  dependsOn: [
    storageaccount
  ]
  params: {
    name: eventGridTopic
    subname: eventGridSubscription
    location: location
    queueId: storageaccount.outputs.storageId
    queueName: queueName
  }
}
