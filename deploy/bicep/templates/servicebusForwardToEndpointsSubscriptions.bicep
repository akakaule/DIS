param sbnamespace string
param topicname string
param endpoints array

module fwdToEndpointsSubs 'servicebusForwardSubscriptionWithMultipleRules.bicep' = [for endpoint in endpoints: {
  name: 'fwdTo${first(split(endpoint,'-'))}'
  params: {
    sbnamespace: sbnamespace
    topicname: topicname
    eventsAsString: endpoint
  }
}]
