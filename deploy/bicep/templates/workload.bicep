param appName string
param appServicePlanId string
param location string = resourceGroup().location
param alwaysOn bool = true

resource webApplication 'Microsoft.Web/sites@2022-03-01' = {
  name: appName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  tags: {
    'hidden-related:${resourceGroup().id}/providers/Microsoft.Web/serverfarms/appServicePlan': 'Resource'
  }
  properties: {    
    serverFarmId: appServicePlanId
    siteConfig: {
      alwaysOn: alwaysOn
    }   
  }
}

output name string = webApplication.name
output identity string = webApplication.identity.principalId
