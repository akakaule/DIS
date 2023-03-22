param sbnamespace string
param topicname string
param subName string
param ruleName string
param filter string
param resolverId string
param continuationId string
param retryId string

//Mangler alt fra # Create Service Bus subscriptions in parallel fra deploy.servicebus.ps1
resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' existing = {
  name: sbnamespace  
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2022-01-01-preview' existing = {
  parent: serviceBus
  name: topicname
}


//Main subscription
resource endpointTopicSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-01-01-preview' = {
  parent: topic
  name: subName
  properties: {
    enableBatchedOperations: true
    requiresSession: true
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    forwardDeadLetteredMessagesTo: resolverId
  }
}

resource endpointTopicSubRule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-01-01-preview' ={
  parent: endpointTopicSub
  name: 'to-${subName}'
  properties:{
    filterType: 'SqlFilter'
    sqlFilter:{
      compatibilityLevel: 20
      sqlExpression: filter
    }
  }
}

//Forward-to-resolver
resource endpointTopicResolverSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-01-01-preview' = {
  parent: topic
  name: resolverId
  properties: {
    enableBatchedOperations: true
    requiresSession: false
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    forwardTo: resolverId
  }
}

resource endpointTopicResolverSubFromRule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-01-01-preview' ={
  parent: endpointTopicResolverSub
  name: 'from-${subName}'
  properties:{
    filterType: 'SqlFilter'
    sqlFilter:{
      compatibilityLevel: 20
      sqlExpression: 'user.To = \'${resolverId}\''
    }
    action: {
      compatibilityLevel: 20
      sqlExpression: 'SET user.From = \'${subName}\''
    }
  }
}

resource endpointTopicResolverSubToRule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-01-01-preview' ={
  parent: endpointTopicResolverSub
  name: 'to-${subName}'
  properties:{
    filterType: 'SqlFilter'
    sqlFilter:{
      compatibilityLevel: 20
      sqlExpression: 'user.To = \'${subName}\''
    } 
  }
}

//"Forward"-to-self subscription (continuation requests)
resource endpointTopiccontinuationSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-01-01-preview' = {
  parent: topic
  name: continuationId
  properties: {
    enableBatchedOperations: true
    requiresSession: false
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    forwardTo: subName
  }
}

resource endpointTopiccontinuationSubRule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-01-01-preview' ={
  parent: endpointTopiccontinuationSub
  name: 'continuation'
  properties:{
    filterType: 'SqlFilter'
    sqlFilter:{
      compatibilityLevel: 20
      sqlExpression: 'user.To = \'${continuationId}\''
    }
    action:{
      compatibilityLevel: 20
      sqlExpression: 'SET user.To = \'${subName}\'; SET user.From = \'${continuationId}\''
    }
  }
}


//"Forward"-to-self subscription (retry requests)
resource endpointTopicRetrySub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-01-01-preview' = {
  parent: topic
  name: retryId
  properties: {
    enableBatchedOperations: true
    requiresSession: false
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    forwardTo: subName
  }
}

resource endpointTopicRetrySubRule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-01-01-preview' ={
  parent: endpointTopicRetrySub
  name: 'retry'
  properties:{
    filterType: 'SqlFilter'
    sqlFilter:{
      compatibilityLevel: 20
      sqlExpression: 'user.To = \'${retryId}\''
    }
    action:{
      compatibilityLevel: 20
      sqlExpression: 'SET user.To = \'${subName}\'; SET user.From = \'${retryId}\''
    }
  }
}

