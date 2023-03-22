using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;


namespace BH.DIS.CosmosSubscriber;

public class CosmosFunction
{
    private const string DatabaseName = "MessageDatabase";
    private readonly HttpClient client;

    public CosmosFunction(HttpClient httpClient)
    {
        this.client = httpClient;
    }

    [FunctionName("Alice")]
    public async Task RunAlice([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "Alice",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on Alice");
        HandleInput(input, "Alice", log);
    }

    [FunctionName("Bob")]
    public async Task RunBob([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "Bob",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on Bob");
        HandleInput(input, "Bob", log);
    }

    [FunctionName("Charlie")]
    public async Task RunCharlie([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "Charlie",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on Charlie");
        HandleInput(input, "Charlie", log);
    }

    [FunctionName("FieldServiceEndpoint")]
    public async Task RunFieldServiceEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "FieldServiceEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on FieldServiceEndpoint");
        HandleInput(input, "FieldServiceEndpoint", log);
    }

    [FunctionName("FieldServiceLowEndpoint")]
    public async Task RunFieldServiceLowEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "FieldServiceLowEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on FieldServiceLowEndpoint");
        HandleInput(input, "FieldServiceLowEndpoint", log);
    }

    [FunctionName("FSADEndpoint")]
    public async Task RunFSADEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "FSADEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on FSADEndpoint");
        HandleInput(input, "FSADEndpoint", log);
    }

    [FunctionName("MascotStoreEndpoint")]
    public async Task RunMascotStoreEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "MascotStoreEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on MascotStoreEndpoint");
        HandleInput(input, "MascotStoreEndpoint", log);
    }

    [FunctionName("MitHrEndpoint")]
    public async Task RunMitHrEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "MitHrEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on MitHrEndpoint");
        HandleInput(input, "MitHrEndpoint", log);
    }

    [FunctionName("MitIndkoebEndpoint")]
    public async Task RunMitIndkoebEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "MitIndkoebEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on MitIndkoebEndpoint");
        HandleInput(input, "MitIndkoebEndpoint", log);
    }

    [FunctionName("NavisionEndpoint")]
    public async Task RunNavisionEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "NavisionEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on NavisionEndpoint");
        HandleInput(input, "NavisionEndpoint", log);
    }

    [FunctionName("SynergiLifeEndpoint")]
    public async Task RunSynergiLifeEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "SynergiLifeEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on SynergiLifeEndpoint");
        HandleInput(input, "SynergiLifeEndpoint", log);
    }

    [FunctionName("TracetoolEndpoint")]
    public async Task RunTracetoolEndpoint([CosmosDBTrigger(
            databaseName: DatabaseName,
            collectionName: "TracetoolEndpoint",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true
            )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on TracetoolEndpoint");
        HandleInput(input, "TracetoolEndpoint", log);
    }

    [FunctionName("IntegrationEndpoint")]
    public async Task RunIntegrationEndpoint([CosmosDBTrigger(
        databaseName: DatabaseName,
        collectionName: "IntegrationEndpoint",
        ConnectionStringSetting = "CosmosDBConnection",
        LeaseCollectionName = "leases",
        CreateLeaseCollectionIfNotExists = true
    )]IReadOnlyList<Document> input, ILogger log)
    {
        log.LogDebug($"CosmosSubscriber triggered on IntegrationEndpoint");
        HandleInput(input, "IntegrationEndpoint", log);
    }

    private async Task HandleInput(IReadOnlyList<Document> input, string endpoint, ILogger log)
    {
        if (input != null && input.Count > 0)
        {
            PublishToStoragehook(endpoint, log);
            PostToEventgrid(input, endpoint);
        }
    }

    private async Task PublishToStoragehook(string endpoint, ILogger log)
    {
        var baseUrl = Environment.GetEnvironmentVariable("StoragehookUrl");
        var url = $"{baseUrl}{endpoint}";

        try
        {
            var response = await client.PostAsync(url, null);
        }
        catch (Exception e)
        {
            log.LogError(e, $"Failed posting to {url}");
            throw;
        }
    }

    private async Task PostToEventgrid(IReadOnlyList<Document> input, string endpoint)
    {
        var eventDoc = input[0];
        if (eventDoc == null) return;

        var status = eventDoc.GetPropertyValue<string>("status");
        if (status != "Failed") return;

        var endpointUri = Environment.GetEnvironmentVariable("eventGridUrl");
        var accessKey = Environment.GetEnvironmentVariable("evenGridKey");

        EventGridPublisherClient client = new EventGridPublisherClient(
            new Uri(endpointUri),
            new AzureKeyCredential(accessKey));

        EventGridEvent egEvent = new EventGridEvent(endpoint, eventDoc.GetPropertyValue<string>("eventType"), "1.0", eventDoc.GetPropertyValue<string>("id"));

        //Send event
        await client.SendEventAsync(egEvent);
    }
}