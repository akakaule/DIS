param sbnamespace string
param endpoints array

module mainfwdSubscriptions 'servicebusMainEndpointForwardSubscriptionsIterate.bicep' = [for eventsAsString in endpoints: {
  name: 'mfw${indexOf(endpoints,eventsAsString)}iteration'
  params: {
    sbnamespace: sbnamespace
    eventsAsString: eventsAsString
  }
}]
