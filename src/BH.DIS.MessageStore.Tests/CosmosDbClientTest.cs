using BH.DIS.SDK.Logging;
using Microsoft.Azure.Cosmos;
using Serilog;
using System;
using Xunit;

namespace BH.DIS.MessageStore.Tests
{
    public class CosmosDbClientTest
    {
        const string ConnectionString = "";
        ICosmosDbClient client;


        public CosmosDbClientTest()
        {
            var cosmosClient = new CosmosClient(ConnectionString);
            client = new CosmosDbClient(cosmosClient, new SerilogAdapter(CreateLogger()));
        }
        private ILogger CreateLogger()
        {
            return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();
        }

        [Fact]
        public async void DownloadEndpointStateCount()
        {
            string endpoint = "bob";

            var result = await client.DownloadEndpointStateCount(endpoint);

            Console.WriteLine($"Failed:{result.FailedCount}, Deferred:{result.DeferredCount}, Pending:{result.PendingCount}, Deadletter:{result.DeadletterCount}");
        }

        [Fact]
        public async void DownloadEndpointSessionStateCount()
        {
            string endpoint = "bob";
            string sessionId = "String3";

            var result = await client.DownloadEndpointSessionStateCount(endpoint, sessionId);

            Console.WriteLine($"Pending:{result.PendingEvents}, Deferred:{result.DeferredEvents}");
        }
    }
}
