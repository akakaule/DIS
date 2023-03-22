param sbnamespace string
param topicname string
param subName string
param forwardTo string

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' existing = {
  name: sbnamespace  
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2022-01-01-preview' existing = {
  parent: serviceBus
  name: topicname
}

resource sub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-01-01-preview' = {
  parent: topic
  name: subName
  properties:{
    enableBatchedOperations: true
    requiresSession: false
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    forwardTo: forwardTo
  }  
}
