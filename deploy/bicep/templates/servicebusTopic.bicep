param name string
param topics array

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' existing = {
  name: name  
}

resource servicebusTopics 'Microsoft.ServiceBus/namespaces/topics@2022-01-01-preview' = [for topic in topics: {
  parent: serviceBus
  name: topic
  properties:{
    maxSizeInMegabytes: 5120
  }
}]

var listKeysEndpoint = '${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey'
output SharedAccessKey string = 'Endpoint=sb://${serviceBus.name}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=${listKeys(listKeysEndpoint, serviceBus.apiVersion).primaryKey}'
