using Azure.Messaging.ServiceBus;
using BH.DIS.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using System;
using System.Threading.Tasks;

namespace BH.DIS.Resolver
{
    public class Functions
    {
        private readonly IServiceBusAdapter _serviceBusAdapter;

        public Functions(IServiceBusAdapter serviceBusAdapter)
        {
            _serviceBusAdapter = serviceBusAdapter ?? throw new ArgumentNullException(nameof(serviceBusAdapter));
        }

        [FunctionName("Resolver")]
        public async Task RunAsync([ServiceBusTrigger("%ResolverId%", "%ResolverId%", Connection = "AzureWebJobsServiceBus", IsSessionsEnabled = true)]
            ServiceBusReceivedMessage message,
            ServiceBusSessionMessageActions sessionActions,
            ServiceBusReceiveActions receiveActions)
        {
            await _serviceBusAdapter.Handle(message, sessionActions, receiveActions);
        }
    }
}
