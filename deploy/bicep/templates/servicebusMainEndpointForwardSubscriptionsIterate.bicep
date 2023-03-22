param sbnamespace string
param eventsAsString string

var endpointsAndConsumers = split(eventsAsString,'-')
var endpoint = first(endpointsAndConsumers)
var consumersAndEvents = skip(endpointsAndConsumers,1)


module mainFwdToConsumers 'servicebusMainEndpointForwardSubscriptionsCreater.bicep' = [for cae in consumersAndEvents: {
  name: 'mainFwdToCon${indexOf(consumersAndEvents, cae)}${endpoint}'
  params: {
    topic: endpoint
    conAndSub: cae
    sbnamespace: sbnamespace
  }
}]

