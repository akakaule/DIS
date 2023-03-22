using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BH.DIS.MessageStore;
using BH.DIS.WebApp.ManagementApi;
using Microsoft.AspNetCore.Http;
using BH.DIS.Core;
using BH.DIS.Core.Endpoints;
using Microsoft.Extensions.Configuration;
using BH.DIS.Endpoints.Demo;
using BH.DIS.Management.ServiceBus;
using System.Security.Claims;
using System.Text;
using System.Reflection;
using System.IO;
using BH.DIS.MessageStore.States;
using EndpointSubscription = BH.DIS.WebApp.ManagementApi.EndpointSubscription;
using System.Collections.Concurrent;
using TechnicalContact = BH.DIS.MessageStore.States.TechnicalContact;

namespace BH.DIS.WebApp.Controllers;

public class EndpointImplementation : IEndpointApiController
{
    private readonly IMessageStoreClient messageStoreClient;
    private readonly IPlatform platform;
    private readonly IConfiguration configuration;
    private readonly ICosmosDbClient cosmosClient;
    private readonly IServiceBusManagement serviceBusManagement;
    private const int InitialEvents = 40;
    private const int PagingEvents = 40;
    private readonly HttpContext _context;

    public EndpointImplementation(IMessageStoreClient messageStoreClient, IHttpContextAccessor contextAccessor,
        IPlatform platform, IConfiguration configuration, ICosmosDbClient cosmosClient,
        IServiceBusManagement serviceBusManagement)
    {
        this.messageStoreClient = messageStoreClient;
        this.platform = platform;
        this.configuration = configuration;
        this.cosmosClient = cosmosClient;
        this.serviceBusManagement = serviceBusManagement;
        _context = contextAccessor.HttpContext;
    }

    public async Task<ActionResult<IEnumerable<string>>> EndpointIdsAllAsync()
    {
        IEnumerable<IEndpoint> endpoints = platform.Endpoints;

        var endpointIds = platform.Endpoints
            .Where(endpoint => IsManagerOfEndpoint(endpoint.Id) && ShowEndpoint(endpoint.Id))
            .Select(e => e.Id);

        return new OkObjectResult(endpointIds);
    }

    public async Task<ActionResult<IEnumerable<EndpointStatusCount>>> EndpointstatusAllAsync()
    {
        var endpointIds = platform.Endpoints
            .Where(endpoint => IsManagerOfEndpoint(endpoint.Id) && ShowEndpoint(endpoint.Id))
            .Select(e => e.Id);

        var endpointStateCounts = new List<EndpointStateCount>();

        foreach (var endpointId in endpointIds)
        {
            endpointStateCounts.Add(await cosmosClient.DownloadEndpointStateCount(endpointId));
        }

        return new OkObjectResult(endpointStateCounts.Select(Mapper.EndpointStatusCountFromEndpointStateCount));
    }

    public async Task<ActionResult<IEnumerable<Event>>> EndpointstatusAsync(string endpointName)
    {
        var endpointIds = platform.Endpoints.Select(e => e.Id);
        var endpoints = await messageStoreClient.DownloadEndpointStates(endpointIds);
        var endpoint =
            endpoints.FirstOrDefault(e => e.EndpointId.Equals(endpointName, StringComparison.OrdinalIgnoreCase));
        if (endpoint != null)
        {
            var res = new Dictionary<string, Event>();
            foreach (var x in endpoint.EnrichedUnresolvedEvents)
            {
                var e = Mapper.EventFromMessageStoreEvent(x);
                res.Add(x.EventId, e);
            }

            return new OkObjectResult(res);
        }

        return new NotFoundObjectResult("Endpoint not found");
    }

    public async Task<ActionResult<EndpointStatus>> GetApiEndpointstatusStatusEndpointNameAsync(string endpointName)
    {
        var endpoint = await cosmosClient.DownloadEndpointStatePaging(endpointName, InitialEvents, "");
        return new OkObjectResult(Mapper.EndpointStatusFromEndpointState(endpoint));
    }

    public async Task<ActionResult<SessionStatus>> GetEndpointSessionStatusAsync(string endpointId,
        string sessionId)
    {
        var sessionState = await cosmosClient.DownloadEndpointSessionStateCount(endpointId, sessionId);
        return new OkObjectResult(Mapper.SessionStatusFromSessionState(sessionState));
    }

    public async Task<ActionResult<EndpointStatus>> PostApiEndpointStatusEndpointNameTokenAsync(
        ContinuationToken body, string endpointName)
    {
        var endpoint = await cosmosClient.DownloadEndpointStatePaging(endpointName, PagingEvents, body.Token);
        return new OkObjectResult(Mapper.EndpointStatusFromEndpointState(endpoint));
    }

    private bool ShowEndpoint(string endpointId)
    {
        if (!configuration.GetValue<string>("Environment").Equals("dev", StringComparison.OrdinalIgnoreCase) &&
            !configuration.GetValue<string>("Environment").Equals("sbdev", StringComparison.OrdinalIgnoreCase))
        {
            var filterList = new List<string> { new Alice().Name, new Bob().Name, new Charlie().Name };

            return !filterList.Contains(endpointId, StringComparer.OrdinalIgnoreCase);
        }

        return true;
    }

    private bool IsManagerOfEndpoint(string endpointId)
    {
        // TODO
        return true;
    }

    public async Task<ActionResult<IEnumerable<EndpointStatusCount>>> GetEndpointStatusCountAllAsync()
    {
        var endpointIds = platform.Endpoints
            .Where(endpoint => IsManagerOfEndpoint(endpoint.Id) && ShowEndpoint(endpoint.Id))
            .Select(e => e.Id);

        var endpointStateCounts = new List<EndpointStateCount>();

        foreach (var endpointId in endpointIds)
        {
            endpointStateCounts.Add(await cosmosClient.DownloadEndpointStateCount(endpointId));
        }

        return new OkObjectResult(endpointStateCounts.Select(Mapper.EndpointStatusCountFromEndpointStateCount));
    }

    public async Task<ActionResult<IEnumerable<EndpointStatusCount>>> PostApiEndpointStatusCountAsync(IEnumerable<string> body)
    {
        var result = new ConcurrentBag<EndpointStatusCount>();
        var endpointIds = body as string[] ?? body.ToArray();

        var options = new ParallelOptions { MaxDegreeOfParallelism = -1 };

        var par = Parallel.ForEachAsync(endpointIds, options, async (endpointId, token) =>
        {
            var endpointStateCount = await cosmosClient.DownloadEndpointStateCount(endpointId);
            result.Add(Mapper.EndpointStatusCountFromEndpointStateCount(endpointStateCount));
        });

        par.Wait();
        return new OkObjectResult(result.ToList());
    }


    public async Task<ActionResult<IEnumerable<Event>>> GetEndpointStatusIdAsync(string endpointName)
    {
        var endpoints = await messageStoreClient.DownloadEndpointStates(new string[] { endpointName });
        var endpoint =
            endpoints.FirstOrDefault(e => e.EndpointId.Equals(endpointName, StringComparison.OrdinalIgnoreCase));
        if (endpoint != null)
        {

            var res = new Dictionary<string, Event>();
            foreach (var x in endpoint.EnrichedUnresolvedEvents)
            {
                var e = Mapper.EventFromMessageStoreEvent(x);
                res.Add(x.EventId, e);
            }


            return new OkObjectResult(res);
        }

        return new NotFoundObjectResult("Endpoint not found");
    }

    public async Task<ActionResult<IEnumerable<string>>> GetEndpointsAllAsync()
    {
        var endpointIds = platform.Endpoints
            .Where(endpoint => IsManagerOfEndpoint(endpoint.Id) && ShowEndpoint(endpoint.Id))
            .Select(e => e.Id);

        return new OkObjectResult(endpointIds);
    }

    public async Task<ActionResult<EndpointStatusCount>> GetEndpointStatusCountIdAsync(string endpointName)
    {
        var state = await cosmosClient.DownloadEndpointStateCount(endpointName);
        var result = Mapper.EndpointStatusCountFromEndpointStateCount(state);

        return new OkObjectResult(result);
    }

    public async Task<ActionResult<EndpointStatus>> PostEndpointStatusIdTokenAsync(ContinuationToken body,
        string endpointName)
    {
        var endpoint = await cosmosClient.DownloadEndpointStatePaging(endpointName, PagingEvents, body.Token);
        return new OkObjectResult(Mapper.EndpointStatusFromEndpointState(endpoint));
    }

    public async Task<ActionResult<SessionStatus>> GetEndpointSessionIdAsync(string endpointId, string sessionId)
    {
        var sessionState = await cosmosClient.DownloadEndpointSessionStateCount(endpointId, sessionId);
        return new OkObjectResult(Mapper.SessionStatusFromSessionState(sessionState));
    }

    public async Task<IActionResult> PostEndpointPurgeAsync(string endpointName)
    {
        var isManagementUser = IsUserInSecurityGroup("EIP_Management");

        // Validate environment
        var env = configuration.GetValue<string>("Environment");
        if (!isManagementUser && (env.Equals("prod", StringComparison.OrdinalIgnoreCase) ||
                                  env.Equals("stag", StringComparison.OrdinalIgnoreCase)))
            return new NotFoundObjectResult("Endpoint cannot be purged in Production and Staging environments");

        // Purge endpoint
        var isPurged = await cosmosClient.PurgeMessages(endpointName);

        var endpointManagement = new EndpointManagement(serviceBusManagement);
        await endpointManagement.ClearEndpoint(endpointName);

        if (isPurged)
            return new OkObjectResult($"{endpointName} is purged");

        return new NotFoundObjectResult($"{endpointName} couldn't be found");
    }

    private bool IsUserInSecurityGroup(string securityGrp)
    {
        var userClaims = _context.User.Identities.First().Claims;
        return userClaims.Any(c => c.Value == securityGrp);
    }

    public async Task<IActionResult> PostEndpointSubscribeAsync(EndpointSubscription body, string endpointId)
    {
        var subscriptionStatus = await cosmosClient.SubscribeToEndpointNotification(endpointId, body.Mail,
            body.Type, GetCurrentUsersMail(), body.Url, body.EventTypes, body.Payload, body.Frequency);
        return new OkObjectResult(subscriptionStatus);
    }

    public async Task<IActionResult> DeleteEndpointSubscribeAsync(SubscriptionAuthor obj, string endpointId)
    {
        var success = await cosmosClient.DeleteSubscription(obj.Id);

        if (success)
            return new OkResult();

        return new BadRequestResult();
    }

    async Task<ActionResult<IEnumerable<EndpointSubscription>>> IEndpointApiController.GetEndpointSubscribeAsync(
        string endpointId)
    {
        var subscriptions = await cosmosClient.GetSubscriptionsOnEndpoint(endpointId);
        return new OkObjectResult(Mapper.SubscriptionsFromEndpointsubscriptions(subscriptions));
    }

    private string GetCurrentUsersMail()
    {
        var name = _context.User.Identities.FirstOrDefault()?.Name;

        if (string.IsNullOrEmpty(name))
            name = _context.User.FindFirst(ClaimTypes.Name).Value;

        return name;
    }

    public async Task<ActionResult<string>> GetEndpointRoleAssignmentScriptAsync(string endpointId)
    {
        var builder = new StringBuilder();

        var subscriptionId = configuration.GetSection("ServiceBusManagement:SubscriptionId").Value;
        var environment = configuration.GetSection("Environment").Value;
        var resourceGroupName = configuration.GetSection("ServiceBusManagement:ResourceGroupName").Value;
        var serviceBusNamespace = configuration.GetSection("ServiceBusNamespace").Value;
        var endpoint =
            platform.Endpoints.Single(ep => ep.Id.Equals(endpointId, StringComparison.OrdinalIgnoreCase));

        var principals = endpoint.RoleAssignments
            .Where(x => x.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.PrincipalId).ToList();
        if (principals.Any())
        {
            await using Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("BH.EIP.WebApp.Resources.EndpointRoleAssignment.ps1");
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                builder.AppendLine($"$subscription =\"{subscriptionId}\" \n");
                builder.AppendLine($"$resourceGroupName = \"{resourceGroupName}\" \n");
                builder.Append(await reader.ReadToEndAsync());
                builder.AppendLine();
                foreach (var principal in principals)
                {
                    builder.AppendLine(
                        $"Assign-ServiceBusSubscription -assigneeId \"{principal}\" -serviceBusNamespace \"{serviceBusNamespace}\" -topic \"{endpoint.Id}\" -subscription \"{endpoint.Id}\" \n");
                }
            }
        }

        return new OkObjectResult(builder.ToString());
    }

    public async Task<IActionResult> PostEndpointSubscriptionstatusAsync(string body, string endpointId)
    {
        var endpointManagement = new EndpointManagement(serviceBusManagement);
        switch (body)
        {
            case "enable":
                {
                    await endpointManagement.EnableEndpoint(endpointId);
                    var metadata = await cosmosClient.GetEndpointMetadata(endpointId);
                    metadata.SubscriptionStatus = true;
                    await cosmosClient.SetEndpointMetadata(metadata);
                    if (await endpointManagement.IsEndpointActive(endpointId))
                        return new OkObjectResult($"{endpointId} is active");
                    break;
                }
            case "disable":
                {
                    await endpointManagement.DisableEndpoint(endpointId);
                    var metadata = await cosmosClient.GetEndpointMetadata(endpointId);
                    metadata.SubscriptionStatus = false;
                    await cosmosClient.SetEndpointMetadata(metadata);
                    if (!await endpointManagement.IsEndpointActive(endpointId))
                        return new OkObjectResult($"{endpointId} is disable");
                    break;
                }
        }

        return new NotFoundObjectResult($"{endpointId} status not set");
    }

    public async Task<ActionResult<string>> GetEndpointSubscriptionstatusAsync(string endpointId)
    {
        var endpointManagement = new EndpointManagement(serviceBusManagement);
        if (await endpointManagement.IsEndpointActive(endpointId))
        {
            return new OkObjectResult($"active");
        }

        return new OkObjectResult($"disabled");
    }

    public async Task<IActionResult> EndpointEnableHeartbeatAsync(bool? body, string endpointId)
    {
        if (!body.HasValue)
            return new BadRequestResult();

        var endpointManagement = new EndpointManagement(serviceBusManagement);
        await endpointManagement.EnableHeartbeatOnEndpoint(endpointId, body.Value);
        await cosmosClient.EnableHeartbeatOnEndpoint(endpointId, body.Value);

        return new OkObjectResult(body);
    }

    public async Task<ActionResult<Metadata>> GetMetadataEndpointAsync(string endpointId)
    {
        var metadata = await cosmosClient.GetEndpointMetadata(endpointId);
        if (metadata == null)
            return new NotFoundObjectResult($"Metadata for {endpointId} not found");
        if (metadata.SubscriptionStatus == null)
        {
            await SetSubscriptionStatusMetadata(metadata);
        }

        return new OkObjectResult(Mapper.MetadataFromEndpointMetadata(metadata));
    }




    public async Task<ActionResult<IEnumerable<MetadataShort>>> PostApiMetadatashortAsync(IEnumerable<string> body)
    {
        var endpointIds = body.ToList();
        var metadataList = await cosmosClient.GetMetadatas(endpointIds) ?? endpointIds.Select(x => new EndpointMetadata { EndpointId = x }).ToList();

        foreach (var s in endpointIds.Where(s => !metadataList.Exists(m => m.EndpointId.Equals(s, StringComparison.OrdinalIgnoreCase))))
        {
            metadataList.Add(new EndpointMetadata { EndpointId = s });
        }

        foreach (var endpointMetadata in metadataList.Where(endpointMetadata => endpointMetadata.SubscriptionStatus == null))
        {
            await SetSubscriptionStatusMetadata(endpointMetadata);
        }

        return new OkObjectResult(Mapper.MetadataShortFromList(metadataList));
    }


    public async Task<IActionResult> PostMetadataEndpointAsync(Metadata body, string endpointId)
    {
        var technicalContacts = body.TechnicalContacts
            .Select(technicalContact => new TechnicalContact()
            { Name = technicalContact.Name, Email = technicalContact.Email }).ToList();


        var metadataStatus = await cosmosClient.SetEndpointMetadata(
            new EndpointMetadata
            {
                EndpointOwner = body.EndpointOwner,
                EndpointId = body.Id,
                EndpointOwnerTeam = body.EndpointOwnerTeam,
                EndpointOwnerEmail = body.EndpointOwnerEmail,
                TechnicalContacts = technicalContacts,
            });
        return new OkObjectResult(metadataStatus);
    }

    private async Task SetSubscriptionStatusMetadata(EndpointMetadata metadata)
    {
        var endpointManagement = new EndpointManagement(serviceBusManagement);
        if (await endpointManagement.IsEndpointActive(metadata.EndpointId))
        {
            metadata.SubscriptionStatus = true;
        }
        else
        {
            metadata.SubscriptionStatus = false;
        }

        await cosmosClient.SetEndpointMetadata(metadata);
    }
}
