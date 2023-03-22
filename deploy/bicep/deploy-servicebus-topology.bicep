param solutionId string
param environment string

param resolverId string
param managerId string
param continuationId string
param eventId string
param retryId string

param endpoints string
param producingEvents string
param consumingEvents string

//##############################################
// Define names Azure resource names
//##############################################

var endpointsArray = split(endpoints,' ')
var consumingEndpoints = split(producingEvents, ' ')
var producingEndpoints = split(consumingEvents,' ')

var resourceNamePostFix = substring(uniqueString(resourceGroup().id), 0, 4)
var sbNamespace = 'sb-${toLower(solutionId)}-${toLower(environment)}-${toLower(resourceNamePostFix)}'

//##############################################
//# Create Service Bus namespace
//##############################################

module serviceBusTopics 'templates/servicebusTopic.bicep' = {
  name : 'ServicebusNamespaceDeploy'
  params : {
    name: sbNamespace
    topics: concat(endpointsArray,[resolverId, managerId, eventId])    
  }
}

//##############################################
//# Resolver & EventId : Create Service Bus subscription
//##############################################

module resolversub 'templates/servicebusSubscription.bicep' = {
  name: 'resolverSubDeploy'
  dependsOn: [serviceBusTopics]
  params: {
    sbnamespace: sbNamespace
    subName: resolverId
    topicname: resolverId
  }
}

module eventsub 'templates/servicebusSubscription.bicep' = {
  name: 'eventSubDeploy'
  dependsOn: [serviceBusTopics]
  params: {
    sbnamespace: sbNamespace
    subName: eventId
    topicname: eventId
  }
}

//##############################################
//# Endpoints: Create Service Bus subscriptions
//##############################################

module mainEndpointSubscriptions 'templates/servicebusMainEndpointSubscriptions.bicep' = [for endpoint in endpointsArray: {
  name: 'main${endpoint}SubscriptionsDeploy'
  dependsOn: [
    serviceBusTopics
  ]
  params: {
    sbnamespace: sbNamespace
    subName: endpoint
    topicname: endpoint
    ruleName: 'to-${endpoint}'
    filter: 'user.To = \'${endpoint}\''
    resolverId: resolverId
    continuationId: continuationId
    retryId: retryId
  }
}]

//Forward-from-eventtype-to-endpoint subscriptions
module mainEndpointForwardSubscriptions 'templates/servicebusMainEndpointForwardSubscriptions.bicep' ={
  dependsOn: [
    mainEndpointSubscriptions
  ]
  name: 'mainEndpointForwardSubscriptionsDeploy'
  params: {
    sbnamespace: sbNamespace
    endpoints: producingEndpoints
  }
}


//##############################################
//# Manager: Create Service Bus subscriptions
//##############################################

//Forward-to-endpoint subscriptions

module managerTopicForwardSubs 'templates/servicebusForwardFromManager.bicep' = {
  dependsOn: [serviceBusTopics]
  name: 'managerTopicForwardSubs'
  params: {
    sbnamespace: sbNamespace
    topicname: managerId
    endpoints: endpointsArray
    managerId: managerId
  }
}
