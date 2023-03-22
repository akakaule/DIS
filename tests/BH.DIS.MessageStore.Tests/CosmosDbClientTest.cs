using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using BH.DIS.MessageStore.States;

namespace BH.DIS.MessageStore.Tests
{
    public class CosmosDbClientTest
    {
        const string ConnectionString = "";
        CosmosDbClient client;

        public CosmosDbClientTest()
        {
            var cosmosClient = new CosmosClient(ConnectionString);
            client = new CosmosDbClient(cosmosClient);
        }

        [Fact]
        public async Task GetEventsByFilter_SessionId_ReturnsEvents()
        {
            string endpoint = "Bob";
            string sessionId = "1";

            string continuationToken = string.Empty;
            int maxSearchItemsCount = 2;
            int resultCount = 0;
            var events = new List<UnresolvedEvent>();
            SearchResponse searchResponse;

            do
            {
                searchResponse = await client.GetEventsByFilter(new EventFilter() { EndPointId = endpoint, SessionId = sessionId }, continuationToken, maxSearchItemsCount);
                if (searchResponse.Events.Any())
                {
                    events.AddRange(events);
                }
                continuationToken = searchResponse.ContinuationToken;

            }
            while (continuationToken != null);

            Assert.NotEmpty(searchResponse.Events);
        }
    }
}