param topic string
param conAndSub string
param sbnamespace string

var casArray = split(conAndSub,'_')
var consumer = first(casArray)
var rule = last(casArray)

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' existing = {
  name: sbnamespace  
}

resource sbtopic 'Microsoft.ServiceBus/namespaces/topics@2022-01-01-preview' existing = {
  parent: serviceBus
  name: topic
}

resource sub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-01-01-preview' = {
  parent: sbtopic
  name: consumer
  properties:{
    enableBatchedOperations: true
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    forwardTo: consumer
  } 
}

resource subrule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2022-01-01-preview' =  {
  parent: sub
  name: rule
  properties: {
    filterType: 'SqlFilter'
    sqlFilter:{
      compatibilityLevel: 20
      sqlExpression: 'user.To = \'${rule}\''
    }
    action:{
      compatibilityLevel: 20
      sqlExpression: 'SET user.From = \'${topic}\'; SET user.EventId = newid(); SET user.To = \'${consumer}\';'
    }
  }
}
