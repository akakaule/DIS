param sbnamespace string
param topicname string
param eventsAsString string

var endpointAndEvents = split(eventsAsString,'-')
var endpoint = first(endpointAndEvents)
var events = skip(endpointAndEvents,1)


resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' existing = {
  name: sbnamespace  
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2022-01-01-preview' existing = {
  parent: serviceBus
  name: topicname
}

resource sub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-01-01-preview' = {
  parent: topic
  name: endpoint
  properties:{
    enableBatchedOperations: true
    requiresSession: false
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    forwardTo: endpoint
  }  
}

resource subrule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-01-01-preview' = [for event in events: {
  parent: sub
  name: event
  properties: {
    filterType: 'SqlFilter'
    sqlFilter:{
      compatibilityLevel: 20
      sqlExpression: 'user.To = \'${event}\''
    }
    action:{
      compatibilityLevel: 20
      sqlExpression: 'SET user.To = \'${endpoint}\'; SET user.From = \'${topicname}\''
    }
  }
}]
