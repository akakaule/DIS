
param solutionId string
param environment string = 'dev'
param webAppVersion string
param messageStoreStorageConnection string
param apiKey string
param appInsightsAppId string
param instrumentationKey string
param cosmosDbConnectionString string
param managerServiceBusConnection string
param locationParam string = 'westeurope'
//param sendGridApiKey string
//param sendGridEmail string
//param sendGridUseTemplateBool string
//param sendGridTemplateId string

//##############################################
// Define names Azure resource names
//##############################################
var resourceNamePostFix = substring(uniqueString(resourceGroup().id), 0, 4)

var location = locationParam

var sbNamespace = 'sb-${toLower(solutionId)}-${toLower(environment)}-${toLower(resourceNamePostFix)}'

var appServicePlanName = 'asp-${toLower(solutionId)}-${toLower(environment)}-management'

var messageStoreStorageAccountName = 'st${toLower(solutionId)}${toLower(environment)}msgstore${toLower(resourceNamePostFix)}'

var managementWebAppName = 'webapp-${toLower(solutionId)}-${toLower(environment)}-management-${toLower(resourceNamePostFix)}'

var alertsFunctionName = 'func-${toLower(solutionId)}-${toLower(environment)}-alerts-${toLower(resourceNamePostFix)}'

var webAppUrl = 'https://${managementWebAppName}.azurewebsites.net'

//##############################################
// Create Web App: Conflict Resolution Web App
//##############################################

var webappsettings = [
  {
    name: 'MessageStoreStorageAccount'
    value: messageStoreStorageAccountName
  }
  {
    name: 'MessageStoreStorageConnection'
    value: messageStoreStorageConnection
  }
  {
    name: 'AzureWebJobsServiceBus'
    value: managerServiceBusConnection
  }
  {
    name: 'ServiceBusNamespace'
    value: sbNamespace
  }
  {
    name: 'UnresolvedEventLimit'
    value: '50'
  }
  {
    name: 'AppInsights:ApiKey'
    value: apiKey
  }
  {
    name: 'Environment'
    value: environment
  }
  {
    name: 'AppInsights:ApplicationId'
    value: appInsightsAppId
  }
  {
    name: 'WebAppVersion'
    value: webAppVersion
  }
  {
    name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
    value: instrumentationKey
  }
  {
    name: 'CosmosConnection'
    value: cosmosDbConnectionString
  }
  //{
  //  name: 'ServiceNowPassword'
  //  value: serviceNowPassword
  //}
  //{
  //  name: 'ServiceNowUrl'
  //  value: serviceNowUrl
  //}
  //{
  //  name: 'ServiceNowUser'
  //  value: serviceNowUser
  //}
]

module webAppModule 'templates/webApp.bicep' = {
  name: 'webAppDeploy'
  params: {
    appName:managementWebAppName
    appServicePlanId:'/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Web/serverfarms/${appServicePlanName}'
    location:location
    alwaysOn: true
    settings:webappsettings
  }
}


//##############################################
//# Alerts: Create Function app 
//##############################################

var alertsFuntionSettings = [
    {
        name: 'CosmosConnection'
        value: cosmosDbConnectionString
    }
    {
        name: 'WebAppUrl'
        value: webAppUrl
    }
    {
        name: 'sendGridApiKey'
        value: 'placeholder'
    }
    {
        name: 'sendGridEmail'
        value: 'placeholder'
    }
    {
        name: 'sendGridTemplateId'
        value: 'placeholder'
    }
    {
        name: 'sendGridUseTemplateBool'
        value: 'placeholder'
    }
]

module azureFunctionModule 'templates/functionApp.bicep' = {
  name: 'functionAppDeploy'
  params:{
    appName: alertsFunctionName
    appServicePlanId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Web/serverfarms/${appServicePlanName}'
    location: location
    settings: alertsFuntionSettings
    storageConnectionString: messageStoreStorageConnection
    appInsightsInstrumentationKey: instrumentationKey    
  }
}
