param sbnamespace string
param topicname string
param endpoints array
param managerId string

module managerSubs 'servicebusForwardSubscriptionWithRule.bicep' = [for endpoint in endpoints: {
  name: 'managerSub${endpoint}deploy'
  params:{
    sbnamespace: sbnamespace
    topicname: topicname
    subName: endpoint
    ruleName: 'to-${endpoint}'
    filter: 'user.To = \'${endpoint}\''
    action: 'SET user.From = \'${managerId}\''
    forwardTo: endpoint
  }
}]
