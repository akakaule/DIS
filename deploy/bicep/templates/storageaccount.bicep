param name string
param location string = resourceGroup().location
param queueName string

resource storageaccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: name
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
  }
}

resource queue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-09-01' = {
  name:'${storageaccount.name}/default/${queueName}'  
  properties:{
    metadata: {}
  }
}

output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${name};AccountKey=${listKeys(storageaccount.id, storageaccount.apiVersion).keys[0].value};EndpointSuffix=core.windows.net'
output queueId string = queue.id
output storageId string = storageaccount.id
