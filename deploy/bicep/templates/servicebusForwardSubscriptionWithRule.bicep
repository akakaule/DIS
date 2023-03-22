param sbnamespace string
param topicname string
param subName string
param forwardTo string
param ruleName string
param filter string
param action string

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

resource subrule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-01-01-preview' = {
  parent: sub
  name: ruleName
  properties: {
    filterType: 'SqlFilter'
    sqlFilter:{
      compatibilityLevel: 20
      sqlExpression: filter
    }
    action:{
      compatibilityLevel: 20
      sqlExpression: action
    }
  }
}

