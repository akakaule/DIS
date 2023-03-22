// See https://aka.ms/new-console-template for more information
using Azure.Messaging.ServiceBus.Administration;
using BH.DIS;
using BH.DIS.Core.Messages;
using BH.DIS.Management.ServiceBus;

//https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/servicebus/Azure.ResourceManager.ServiceBus Måske?

if (string.IsNullOrEmpty(args[0])) { Console.WriteLine("ConnectionString not set"); throw new ArgumentNullException(); }
string connectionString = args[0];

ServiceBusAdministrationClient client = new ServiceBusAdministrationClient(connectionString);
var management = new ServiceBusManagement(client);
var platform = new PlatformConfiguration();

//Get existing topics
var topicPages = client.GetTopicsAsync();
List<TopicProperties> topics = new List<TopicProperties>();

await foreach (var page in topicPages.AsPages())
{
    topics.AddRange(page.Values);    
}

var managerSubscriptionPages = client.GetSubscriptionsAsync(Constants.ManagerId);
List<SubscriptionProperties> ManagerSubscriptions = new List<SubscriptionProperties>();

await foreach (var page in managerSubscriptionPages.AsPages())
{
    ManagerSubscriptions.AddRange(page.Values);
}

try
{
    //System topics
    Console.WriteLine("Creating system topics, subscriptions and rules...");
    if (topics.FirstOrDefault(e => e.Name == Constants.EventId) != null) 
        management.CreateTopic(Constants.EventId).GetAwaiter().GetResult();
    if (topics.FirstOrDefault(e => e.Name == Constants.ResolverId) != null) 
        management.CreateTopic(Constants.ResolverId).GetAwaiter().GetResult();
    if (topics.FirstOrDefault(e => e.Name == Constants.ResolverId) != null) 
        management.CreateTopic(Constants.ManagerId).GetAwaiter().GetResult();

    //System topic subscriptions & rules
    management.CreateSubscription(Constants.EventId, Constants.EventId).GetAwaiter().GetResult();
    management.CreateSubscription(Constants.ResolverId, Constants.ResolverId).GetAwaiter().GetResult();
    Console.WriteLine("Created system topics, subscriptions and rules successfully.");

}
catch (Exception)
{
    Console.WriteLine("Error creating system topology");
    throw;
}


Console.WriteLine("Creating endpoint topology:");
platform.Endpoints.ToList().ForEach(async endpoint =>
{
    List<SubscriptionProperties> subscriptions = new List<SubscriptionProperties>();
    
    Console.WriteLine($"Endpoint interation: {endpoint.Name}");
    try
    {        
        //Create endpoint Topic
        if (!topics.Any(e => e.Name == endpoint.Name)) management.CreateTopic(endpoint.Name).GetAwaiter().GetResult();

        //Get existing subscriptions on topic
        var subscriptionPages = client.GetSubscriptionsAsync(endpoint.Name);

        await foreach (var page in subscriptionPages.AsPages())
        {
            subscriptions.AddRange(page.Values);
        }

        //Create manager Subscription & Rule
        var managerSub = ManagerSubscriptions.FirstOrDefault(s => s.SubscriptionName == endpoint.Name);
        if (managerSub == null || managerSub.ForwardTo != endpoint.Name)
        {
            await management.CreateForwardSubscription(Constants.ManagerId, endpoint.Name, endpoint.Name);
            await management.CreateCustomRule(Constants.ManagerId, endpoint.Name, $"to-{endpoint.Name}", $"user.To = '{endpoint.Name}'", $"SET user.From = '{Constants.ManagerId}'");
        }
    }
    catch (Exception)
    {
        Console.WriteLine("Error creating endpoint topic or subscription");
        throw;
    }

    endpoint.EventTypesProduced.ToList().ForEach(eventType =>
    {
        //Get consumers of events
        var endpointsConsumingEvent = platform.Endpoints.Where(ep => ep.EventTypesConsumed.Contains(eventType));

        //Create subscriptions for each consuming endpoint
        endpointsConsumingEvent.ToList().ForEach(ep =>
        {
            try
            {
                //Create new subscription with forward
                var endpointSub = subscriptions.FirstOrDefault(s => s.SubscriptionName == ep.Name);
                if (endpointSub == null || endpointSub.ForwardTo != ep.Name)
                {
                    management.CreateForwardSubscription(endpoint.Name, ep.Name, ep.Name).GetAwaiter().GetResult();
                    Console.WriteLine($"Created subscription: {ep.Name} on topic: {endpoint.Name}");

                    //Create subscripionRule
                    management.CreateEventTypeRule(endpoint.Name, ep.Name, eventType.Name, eventType.Name).GetAwaiter().GetResult();
                    Console.WriteLine($"Created subscriptionRule: {eventType.Name}");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error creating subscription and Rule");
                throw;
            }
        });
    });

    //Create standard subscriptions
    try
    {
        //Continuation
        var continuationSub = subscriptions.FirstOrDefault(s => s.SubscriptionName == Constants.ContinuationId);
        if(continuationSub == null || continuationSub.ForwardTo != endpoint.Name)
        {
            await management.CreateForwardSubscription(endpoint.Name, Constants.ContinuationId, endpoint.Name);
            await management.CreateCustomRule(endpoint.Name, Constants.ContinuationId, Constants.ContinuationId, $"user.To='{Constants.ContinuationId}'", $"SET user.To = '{endpoint.Name}'; SET user.From = '{Constants.ContinuationId}'");
        }

        //Resolver
        var resolverSub = subscriptions.FirstOrDefault(s => s.SubscriptionName == Constants.ResolverId);
        if(resolverSub == null || resolverSub.ForwardTo != Constants.ResolverId)
        {
            await management.CreateForwardSubscription(endpoint.Name, Constants.ResolverId, Constants.ResolverId);
            await management.CreateCustomRule(endpoint.Name, Constants.ResolverId, $"from-{endpoint.Name}", $"user.To='{Constants.ResolverId}'", $"SET user.From = '{endpoint.Name}'");
            await management.CreateCustomRule(endpoint.Name, Constants.ResolverId, $"to-{endpoint.Name}", $"user.To='{endpoint.Name}'", "");
        }

        //Retry
        var retrySub = subscriptions.FirstOrDefault(s => s.SubscriptionName == Constants.RetryId);
        if(retrySub == null || retrySub.ForwardTo != endpoint.Name)
        {
            await management.CreateForwardSubscription(endpoint.Name, Constants.RetryId, endpoint.Name);
            await management.CreateCustomRule(endpoint.Name, Constants.RetryId, Constants.RetryId, $"user.To = '{Constants.RetryId}'", $"SET user.To = '{endpoint.Name}'; SET user.From = '{Constants.RetryId}'");
        }

    }
    catch (Exception)
    {
        Console.WriteLine("Error creating standard subscription and Rules on endpoint topics");
        throw;
    }
});

Console.WriteLine("Successfully created topology");
