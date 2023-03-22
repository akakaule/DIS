param name string
param subname string
param location string = resourceGroup().location
param queueId string
param queueName string
param ttl int = 604800

resource eventgridTopic 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: name
  location:location
}

resource eventgridSubscription 'Microsoft.EventGrid/topics/eventSubscriptions@2022-06-15' = {
  name: subname
  parent: eventgridTopic  
  properties: {
    destination:{
      endpointType: 'StorageQueue'
      properties:{
        queueMessageTimeToLiveInSeconds: ttl
        queueName: queueName
        resourceId: queueId
      }
    }    
  }
}

output eventGridKey string = eventgridTopic.listKeys().key1
output eventGridUrl string = eventgridTopic.properties.endpoint
