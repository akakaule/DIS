using Azure.Messaging.ServiceBus.Administration;
using BH.DIS.Management.ServiceBus;
using Xunit;

namespace BH.DIS.Management.Tests
{
    public class ServiceBusManagementTest
    {
        const string ConnectionString = "";
        ServiceBusAdministrationClient client;

        public ServiceBusManagementTest()
        {
            client = new ServiceBusAdministrationClient(ConnectionString);
        }

        [Fact (Skip = "Skip test")]
        public void CreateSubscription()
        {

            var management = new ServiceBusManagement(client);
            management.CreateSubscription("bob", "bobtest").GetAwaiter().GetResult();

        }

        [Fact(Skip = "Skip test")]
        public void DeleteSubscription()
        {
            var management = new ServiceBusManagement(client);
            management.DeleteSubscription("bob", "bobtest").GetAwaiter().GetResult();

        }

        [Fact(Skip = "Skip test")]
        public void ClearEndpoint()
        {
            var serviceBusManagement = new ServiceBusManagement(client);

            var endpointManagement = new EndpointManagement(serviceBusManagement);

            endpointManagement.ClearEndpoint("bob").GetAwaiter().GetResult();

        }

        [Fact(Skip = "Skip test")]
        public void DisableEndpoint()
        {
            var serviceBusManagement = new ServiceBusManagement(client);

            var endpointManagement = new EndpointManagement(serviceBusManagement);

            endpointManagement.DisableEndpoint("bob").GetAwaiter().GetResult();

            Assert.False(endpointManagement.IsEndpointActive("bob").GetAwaiter().GetResult());
        }

        [Fact(Skip = "Skip test")]
        public void EnableEndpoint()
        {
            var serviceBusManagement = new ServiceBusManagement(client);

            var endpointManagement = new EndpointManagement(serviceBusManagement);

            endpointManagement.EnableEndpoint("bob").GetAwaiter().GetResult();

            Assert.True(endpointManagement.IsEndpointActive("bob").GetAwaiter().GetResult());
        }
    }
}
