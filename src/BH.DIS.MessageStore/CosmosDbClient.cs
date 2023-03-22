using BH.DIS.Core.Logging;
using BH.DIS.MessageStore.States;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BH.DIS.MessageStore;

public class EventFilter
{
    public string? EndPointId { get; set; }

    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
    public DateTime? EnqueuedAtFrom { get; set; }
    public DateTime? EnqueuedAtTo { get; set; }

    public string? EventId { get; set; }
    public List<string>? EventTypeId { get; set; }
    public string? SessionId { get; set; }
    public string? To { get; set; }
    public string? From { get; set; }
    public List<string>? ResolutionStatus { get; set; }
    public string? Payload { get; set; }
}

public interface ICosmosDbClient
{
    Task<bool> UploadPendingMessage(string eventId, string sessionId, string endpointId, UnresolvedEvent content);
    Task<bool> UploadDeferredMessage(string eventId, string sessionId, string endpointId, UnresolvedEvent content);
    Task<bool> UploadFailedMessage(string eventId, string sessionId, string endpointId, UnresolvedEvent content);

    Task<bool> UploadDeadletteredMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content);

    Task<bool> UploadUnsupportedMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content);

    public Task<bool> UploadSkippedMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content);

    Task<bool> UploadCompletedMessage(string eventId, string sessionId, string endpointId, UnresolvedEvent content);

    Task<SearchResponse> GetEventsByFilter(EventFilter filter, string continuationToken, int maxSearchItemsCount);
    Task<UnresolvedEvent> GetPendingEvent(string endpointId, string eventId, string sessionId);
    Task<UnresolvedEvent> GetFailedEvent(string endpointId, string eventId, string sessionId);
    Task<UnresolvedEvent> GetDeferredEvent(string endpointId, string eventId, string sessionId);
    Task<UnresolvedEvent> GetDeadletteredEvent(string endpointId, string eventId, string sessionId);
    Task<UnresolvedEvent> GetUnsupportedEvent(string endpointId, string eventId, string sessionId);
    Task<IEnumerable<UnresolvedEvent>> GetCompletedEventsOnEndpoint(string endpointId);
    Task<UnresolvedEvent> GetEvent(string endpointId, string eventId);
    Task<UnresolvedEvent> GetEventById(string endpointId, string eventId);

    Task<bool> RemoveMessage(string eventId, string sessionId, string endpointId);

    Task<SessionStateCount> DownloadEndpointSessionStateCount(string endpointId, string sessionId);
    Task<EndpointStateCount> DownloadEndpointStateCount(string endpointId);
    Task<EndpointState> DownloadEndpointStatePaging(string endpointId, int pageSize, string continuationToken);

    Task<IEnumerable<BlockedMessageEvent>> GetBlockedEventsOnSession(string endpointId, string sessionId);
    Task<IEnumerable<UnresolvedEvent>> GetPendingEventsOnSession(string endpointId);
    Task<IEnumerable<BlockedMessageEvent>> GetInvalidEventsOnSession(string endpointId);

    Task<EndpointSubscription> SubscribeToEndpointNotification(string endpointId, string mail, string type,
        string author, string url, List<string> eventTypes, string payload, int frequency);

    Task<IEnumerable<EndpointSubscription>> GetSubscriptionsOnEndpoint(string endpointId);

    Task<IEnumerable<EndpointSubscription>> GetSubscriptionsOnEndpointWithEventtype(string endpoint,
        string eventtypes, string payload, string errorText);

    Task<string> GetEndpointErrorList(string endpointId);
    Task<bool> UpdateSubscription(EndpointSubscription subscription);
    Task<bool> UnsubscribeById(string endpointId, string mail);
    Task<bool> DeleteSubscription(string subscriptionId);
    Task<bool> UnsubscribeByMail(string endpointId, string mail);

    Task<bool> PurgeMessages(string endpointId, string sessionId);
    Task<bool> PurgeMessages(string endpointId);

    Task<EndpointMetadata> GetEndpointMetadata(string endpointId);
    Task<List<EndpointMetadata>> GetMetadatas();
    Task<List<EndpointMetadata>?> GetMetadatas(IEnumerable<string> endpointIds);
    Task<List<EndpointMetadata>> GetMetadatasWithEnabledHeartbeat();
    Task<bool> SetEndpointMetadata(EndpointMetadata endpointMetadata);
    Task EnableHeartbeatOnEndpoint(string endpointId, bool enable);

    Task<bool> SetHeartbeat(Heartbeat heartbeat, string endpointId);
}

public class CosmosDbClient : ICosmosDbClient
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger _logger;
    private const string DatabaseId = "MessageDatabase";

    private const string PendingStatus = "Pending";
    private const string FailedStatus = "Failed";
    private const string DeferredStatus = "Deferred";
    private const string DLQStatus = "DeadLettered";
    private const string UnsupportedStatus = "Unsupported";
    private const string CompletedStatus = "Completed";
    private const string SkippedStatus = "Skipped";
    private const string PublishedStatus = "Published";
    private const int Maxheartbeats = 10;

    private const string PublisherRole = "Publisher";
    private const string SubscriberRole = "Subscriber";
    private const string SubscriptionsContainer = "subscriptions";

    //Has to be atleast 1 higher than rows showed in table 
    private const int MaxSearchItemsCount = 100;

    public CosmosDbClient(CosmosClient cosmosClient, ILogger logger = null)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
    }


    public async Task<EndpointStateCount> DownloadEndpointStateCount(string endpointId)
    {
        var container = await GetEndpointContainer(endpointId);
        const string sqlQuery =
            "SELECT COUNT(1) AS EventCount, c.status FROM c WHERE (NOT IS_DEFINED(c.deleted) or c.deleted != true) GROUP BY c.status";
        var queryDefinition = new QueryDefinition(sqlQuery);

        var result = container.GetItemQueryIterator<StatusQueryResult>(queryDefinition);
        var resultDict = new Dictionary<string, int>();
        while (result.HasMoreResults)
        {
            var currentResultSet = await result.ReadNextAsync();
            foreach (var queryResult in currentResultSet)
            {
                resultDict.Add(queryResult.Status, queryResult.EventCount);
            }
        }

        return new EndpointStateCount
        {
            EndpointId = endpointId,
            EventTime = DateTime.Now,
            DeferredCount = resultDict.ContainsKey(DeferredStatus) ? resultDict[DeferredStatus] : 0,
            PendingCount = resultDict.ContainsKey(PendingStatus) ? resultDict[PendingStatus] : 0,
            FailedCount = resultDict.ContainsKey(FailedStatus) ? resultDict[FailedStatus] : 0,
            DeadletterCount = resultDict.ContainsKey(DLQStatus) ? resultDict[DLQStatus] : 0,
            UnsupportedCount = resultDict.ContainsKey(UnsupportedStatus) ? resultDict[UnsupportedStatus] : 0,
        };
    }

    public async Task<SessionStateCount> DownloadEndpointSessionStateCount(string endpointId, string sessionId)
    {
        var container = await GetEndpointContainer(endpointId);
        var sqlQuery =
            $"SELECT c.id, c.status FROM c where c.status IN ('{PendingStatus}', '{DeferredStatus}') AND c.sessionId = '{sessionId}' AND (NOT IS_DEFINED(c.deleted) or c.deleted != true)";
        var queryDefinition = new QueryDefinition(sqlQuery);
        var result = container.GetItemQueryIterator<SessionCountQueryResult>(queryDefinition);

        var sessionResults = new List<SessionCountQueryResult>();

        while (result.HasMoreResults)
        {
            var currentResultSet = await result.ReadNextAsync();
            foreach (var queryResult in currentResultSet)
            {
                sessionResults.Add(queryResult);
            }
        }

        return new SessionStateCount
        {
            SessionId = sessionId,
            DeferredEvents = sessionResults
                .Where(se => se.Status.Equals(DeferredStatus, StringComparison.OrdinalIgnoreCase))
                .Select(se => se.Id),
            PendingEvents = sessionResults
                .Where(se => se.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase))
                .Select(se => se.Id)
        };
    }

    public async Task<EndpointState> DownloadEndpointStatePaging(string endpointId, int pageSize,
        string continuationToken)
    {
        var container = await GetEndpointContainer(endpointId);

        var requestOptions = new QueryRequestOptions
        {
            MaxItemCount = pageSize
        };

        try
        {
            FeedIterator<EventDbo> result = container.GetItemLinqQueryable<EventDbo>(
                    true,
                    String.IsNullOrEmpty(continuationToken) ? null : continuationToken,
                    requestOptions)
                .Where(e => e.Status.Equals(FailedStatus, StringComparison.OrdinalIgnoreCase)
                            || e.Status.Equals(DLQStatus, StringComparison.OrdinalIgnoreCase)
                            || e.Status.Equals(UnsupportedStatus, StringComparison.OrdinalIgnoreCase))
                .Where(e => !e.Deleted.HasValue || !e.Deleted.Value)
                .OrderByDescending(e => e.Event.UpdatedAt)
                .ToFeedIterator();

            var pendingEvents = new List<string>();
            var failedEvents = new List<string>();
            var deferredEvents = new List<string>();
            var deadletteredEvents = new List<string>();
            var unsupportedEvents = new List<string>();
            var token = "";
            if (result.HasMoreResults)
            {
                var feed = await result.ReadNextAsync();
                token = feed.ContinuationToken;
                foreach (var eventDbo in feed)
                {
                    var status = eventDbo.Status;
                    switch (status)
                    {
                        case FailedStatus:
                            failedEvents.Add(eventDbo.Id);
                            break;
                        case PendingStatus:
                            pendingEvents.Add(eventDbo.Id);
                            break;
                        case DeferredStatus:
                            deferredEvents.Add(eventDbo.Id);
                            break;
                        case DLQStatus:
                            deadletteredEvents.Add(eventDbo.Id);
                            break;
                        case UnsupportedStatus:
                            unsupportedEvents.Add(eventDbo.Id);
                            break;
                        default:
                            break;
                    }
                }
            }

            var endpointState = new EndpointState
            {
                EndpointId = endpointId,
                DeferredEvents = deferredEvents,
                PendingEvents = pendingEvents,
                FailedEvents = failedEvents,
                DeadletteredEvents = deadletteredEvents,
                UnsupportedEvents = unsupportedEvents,
                ContinuationToken = token
            };

            return endpointState;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task<bool> UploadDeferredMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content) =>
        UploadMessage(eventId, sessionId, endpointId, content, DeferredStatus);

    public Task<bool> UploadFailedMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content) =>
        UploadMessage(eventId, sessionId, endpointId, content, FailedStatus);

    public Task<bool> UploadPendingMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content) =>
        UploadMessage(eventId, sessionId, endpointId, content, PendingStatus);

    public Task<bool> UploadDeadletteredMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent contet) =>
        UploadMessage(eventId, sessionId, endpointId, contet, DLQStatus);

    public Task<bool> UploadUnsupportedMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content) =>
        UploadMessage(eventId, sessionId, endpointId, content, UnsupportedStatus);

    public Task<bool> UploadSkippedMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content) =>
        UploadCompletedMessage(eventId, sessionId, endpointId, content, SkippedStatus);

    public Task<bool> UploadCompletedMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content) =>
        UploadCompletedMessage(eventId, sessionId, endpointId, content, CompletedStatus);

    private async Task<bool> UploadCompletedMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content, string status)
    {
        var container = await GetEndpointContainer(endpointId);
        //var cosmosEvent = await GetPendingEvent(endpointId, eventId, sessionId);
        //cosmosEvent.ResolutionStatus = resolutionStatus;
        var eventDbo = new EventDbo
        {
            Id = $"{eventId}_{sessionId}",
            Event = content,
            SessionId = sessionId,
            Status = status,
            EventType = content.EventTypeId,
            Deleted = true,
            TimeToLive = 60 * 60 * 24 * 30 // 30 days TTL
        };

        try
        {
            var response = await container.UpsertItemAsync(eventDbo, new PartitionKey(eventDbo.Id));
            _logger?.Verbose(
                $"COSMOS UPSERT-RESPONSE: EventId: {eventId}, SessionId: {sessionId}, HttpStatusCode: {response.StatusCode}, Status : {CompletedStatus}");
            return true;
        }
        catch (CosmosException e)
        {
            _logger?.Error(e,
                $"COSMOS UPSERT-ERROR: EventId: {eventId}, SessionId: {sessionId}, Status : {CompletedStatus}");

            if (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new RequestLimitException();
            }

            throw;
        }
    }

    public async Task<bool> RemoveMessage(string eventId, string sessionId, string endpointId)
    {
        var container = await GetEndpointContainer(endpointId);
        var id = $"{eventId}_{sessionId}";

        try
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.id =\"{id}\"";
            var result = container.GetItemQueryIterator<EventDbo>(sqlQuery);

            if (result.HasMoreResults)
            {
                var eventDbo = await result.ReadNextAsync();
                if (eventDbo.Any())
                {
                    var updateEvent = eventDbo.First();
                    updateEvent.Deleted = true;
                    updateEvent.TimeToLive = 60; // 1 Minute
                    var response = await container.UpsertItemAsync<EventDbo>(updateEvent);
                    _logger?.Verbose(
                        $"COSMOS REMOVE-MESSAGE: EventId: {eventId}, SessionId: {sessionId}, HttpStatusCode: {response.StatusCode}");
                    return true;
                }
            }

            return false;
        }
        catch (CosmosException e)
        {
            _logger?.Error(e,
                $"COSMOS REMOVE-MESSAGE: EventId: {eventId}, SessionId: {sessionId}, HttpStatusCode: {e.StatusCode}");

            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            if (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new RequestLimitException();
            }

            throw;
        }
    }

    public async Task<bool> PurgeMessages(string endpointId, string sessionId)
    {
        try
        {
            var container = await GetEndpointContainer(endpointId);
            var sqlQuery = $"SELECT * FROM c WHERE c.sessionId =\"{sessionId}\"";
            var result = container.GetItemQueryIterator<EventDbo>(sqlQuery);

            _logger?.Information(
                $"COSMOS PURGE: Deleted all messages on endpoint {endpointId} in session {sessionId}");
            while (result.HasMoreResults)
            {
                var eventDbo = await result.ReadNextAsync();
                if (eventDbo.Any())
                {
                    foreach (var queryResult in eventDbo)
                        await container.DeleteItemAsync<EventDbo>(queryResult.Id, new PartitionKey(queryResult.Id));
                }
            }

            return true;
        }
        catch (Exception e)
        {
            _logger?.Error(e,
                $"COSMOS PURGE: Couldn't delete all messages on endpoint {endpointId} in session {sessionId}");
            return false;
        }
    }

    public async Task<bool> PurgeMessages(string endpointId)
    {
        try
        {
            var container = await GetEndpointContainer(endpointId);

            await container.DeleteContainerAsync();
            _logger?.Information($"COSMOS PURGE: Deleted all messages on endpoint {endpointId}");

            return true;
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"COSMOS PURGE: Couldn't delete all messages on endpoint {endpointId}");
            return false;
        }
    }

    public Task<UnresolvedEvent> GetPendingEvent(string endpointId, string eventId, string sessionId) =>
        GetEvent(endpointId, eventId, sessionId, PendingStatus);

    public Task<UnresolvedEvent> GetFailedEvent(string endpointId, string eventId, string sessionId) =>
        GetEvent(endpointId, eventId, sessionId, FailedStatus);

    public Task<UnresolvedEvent> GetDeferredEvent(string endpointId, string eventId, string sessionId) =>
        GetEvent(endpointId, eventId, sessionId, DeferredStatus);

    public Task<UnresolvedEvent> GetDeadletteredEvent(string endpointId, string eventId, string sessionId) =>
        GetEvent(endpointId, eventId, sessionId, DLQStatus);

    public Task<UnresolvedEvent> GetUnsupportedEvent(string endpointId, string eventId, string sessionId) =>
        GetEvent(endpointId, eventId, sessionId, UnsupportedStatus);


    public async Task<UnresolvedEvent> GetEvent(string endpointId, string eventId)
    {
        var container = await GetEndpointContainer(endpointId);
        var sqlQuery = $"SELECT * FROM c WHERE c.event.EventId = \"{eventId}\"";
        var result = container.GetItemQueryIterator<EventDbo>(sqlQuery);

        if (result.HasMoreResults)
        {
            var eventDbo = await result.ReadNextAsync();
            if (eventDbo.Any())
            {
                return eventDbo.First().Event;
            }
        }

        return null;
    }


    private async Task<UnresolvedEvent> GetEvent(string endpointId, string eventId, string sessionId, string status)
    {
        var container = await GetEndpointContainer(endpointId);
        var sqlQuery =
            $"SELECT * FROM c WHERE c.id =\"{eventId}_{sessionId}\" AND c.status = '{status}' AND (NOT IS_DEFINED(c.deleted) or c.deleted != true)";
        var result = container.GetItemQueryIterator<EventDbo>(sqlQuery);

        if (result.HasMoreResults)
        {
            var eventDbo = await result.ReadNextAsync();
            if (eventDbo.Any())
            {
                return eventDbo.First().Event;
            }
        }

        return null;
    }

    public async Task<UnresolvedEvent> GetEventById(string endpointId, string id)
    {
        var container = await GetEndpointContainer(endpointId);
        try
        {
            var rel = await container.ReadItemAsync<EventDbo>(id, new PartitionKey(id), new ItemRequestOptions() { });
            return rel.Resource?.Event;
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw;
        }
    }

    public async Task<SearchResponse> GetEventsByFilter(EventFilter filter, string continuationToken,
        int maxSearchItemsCount)
    {
        var container = await GetEndpointContainer(filter.EndPointId);
        var requestOptions = new QueryRequestOptions
            { MaxItemCount = maxSearchItemsCount != 0 ? maxSearchItemsCount : MaxSearchItemsCount };
        var queryable = container
            .GetItemLinqQueryable<EventDbo>(true,
                String.IsNullOrEmpty(continuationToken) ? null : continuationToken,
                requestOptions);
        var query = queryable.Where(x => true);

        // Datetimes
        if (filter.UpdatedAtFrom != null)
            query = query
                .Where(x => x.Event.UpdatedAt >= filter.UpdatedAtFrom);

        if (filter.UpdatedAtTo != null)
            query = query
                .Where(x => x.Event.UpdatedAt <= filter.UpdatedAtTo);

        if (filter.EnqueuedAtFrom != null)
            query = query
                .Where(x => x.Event.EnqueuedTimeUtc >= filter.EnqueuedAtFrom);

        if (filter.EnqueuedAtTo != null)
            query = query
                .Where(x => x.Event.EnqueuedTimeUtc <= filter.EnqueuedAtTo);

        // Strings
        if (filter.EventTypeId != null && filter.EventTypeId.Any())
            query = query
                .Where(x => filter.EventTypeId.Contains(x.EventType));


        if (filter.EndPointId != null)
            query = query
                .Where(x => x.Event.EndpointId.Contains(filter.EndPointId));

        if (filter.EventId != null)
            query = query
                .Where(x => x.Id.Contains(filter.EventId));

        if (filter.SessionId != null)
            query = query
                .Where(x => x.SessionId.Contains(filter.SessionId));

        if (filter.To != null)
            query = query
                .Where(x => x.Event.To.Contains(filter.To));

        if (filter.From != null)
            query = query
                .Where(x => x.Event.From.Contains(filter.From));

        if (filter.ResolutionStatus != null && filter.ResolutionStatus.Any())
            query = query
                .Where(x => filter.ResolutionStatus.Contains(x.Status));


        if (filter.Payload != null)
            query = query
                .Where(x => x.Event.MessageContent.EventContent.EventJson.Contains(filter.Payload));

        var result = query.OrderByDescending(e => e.Event.UpdatedAt).ToFeedIterator();
        var events = new List<UnresolvedEvent>();
        var token = "";
        while (result.HasMoreResults && events.Count <= MaxSearchItemsCount)
        {
            var eventDbo = await result.ReadNextAsync();
            token = eventDbo.ContinuationToken;
            foreach (var queryResult in eventDbo)
            {
                events.Add(queryResult.Event);
            }

            if (eventDbo.Count > 0)
            {
                return new SearchResponse { Events = events, ContinuationToken = token };
            }
        }

        return new SearchResponse { Events = events, ContinuationToken = token };
    }

    public async Task<IEnumerable<UnresolvedEvent>> GetCompletedEventsOnEndpoint(string endpointId)
    {
        var container = await GetEndpointContainer(endpointId);
        var sqlQuery = $"SELECT * FROM c WHERE c.status ='{CompletedStatus}'";
        var result = container.GetItemQueryIterator<EventDbo>(sqlQuery, null, new QueryRequestOptions { });
        var unresolvedEvents = new List<UnresolvedEvent>();

        while (result.HasMoreResults)
        {
            var eventDbo = await result.ReadNextAsync();
            foreach (var queryResult in eventDbo)
            {
                unresolvedEvents.Add(queryResult.Event);
            }
        }

        return unresolvedEvents;
    }

    public async Task<IEnumerable<BlockedMessageEvent>> GetBlockedEventsOnSession(string endpointId,
        string sessionId)
    {
        var container = await GetEndpointContainer(endpointId);
        var sqlQuery =
            $"SELECT * FROM c WHERE c.sessionId =\"{sessionId}\" AND c.status IN ('{PendingStatus}', '{DeferredStatus}') AND (NOT IS_DEFINED(c.deleted) or c.deleted != true)";
        var result = container.GetItemQueryIterator<EventDbo>(sqlQuery, null, new QueryRequestOptions { });
        var blockedMessageEvents = new List<BlockedMessageEvent>();

        while (result.HasMoreResults)
        {
            var eventDbo = await result.ReadNextAsync();
            foreach (var queryResult in eventDbo)
            {
                blockedMessageEvents.Add(new BlockedMessageEvent
                {
                    EventId = queryResult.Event.EventId,
                    OriginatingId =
                        queryResult.Event.OriginatingMessageId.Equals("self", StringComparison.OrdinalIgnoreCase)
                            ? queryResult.Event.LastMessageId
                            : queryResult.Event.OriginatingMessageId,
                    Status = queryResult.Status
                });
            }
        }

        return blockedMessageEvents;
    }

    public async Task<IEnumerable<UnresolvedEvent>> GetPendingEventsOnSession(string endpointId)
    {
        var container = await GetEndpointContainer(endpointId);
        var blockedMessageEvents = new List<UnresolvedEvent>();
        try
        {
            FeedIterator<EventDbo> queryResult = container.GetItemLinqQueryable<EventDbo>(true, null)
                .Where(e => e.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase))
                .Where(e => !e.Deleted.HasValue || !e.Deleted.Value)
                .OrderByDescending(e => e.Event.UpdatedAt).ToFeedIterator();
            while (queryResult.HasMoreResults)
            {
                var eventDbo = await queryResult.ReadNextAsync();
                foreach (var pendingEvent in eventDbo)
                {
                    blockedMessageEvents.Add(pendingEvent.Event);
                }
            }
        }
        catch
        {
            return null;
        }

        return blockedMessageEvents;
    }

    public async Task<IEnumerable<BlockedMessageEvent>> GetInvalidEventsOnSession(string endpointId)
    {
        var container = await GetEndpointContainer(endpointId);
        var sqlQuery =
            $"SELECT * FROM c WHERE c.event.EndpointRole = '{PublisherRole}' AND (NOT IS_DEFINED(c.deleted) or c.deleted != true)";
        var result = container.GetItemQueryIterator<EventDbo>(sqlQuery);
        var invalidMessageEvents = new List<BlockedMessageEvent>();

        while (result.HasMoreResults)
        {
            var eventDbo = await result.ReadNextAsync();
            foreach (var queryResult in eventDbo)
            {
                invalidMessageEvents.Add(new BlockedMessageEvent
                {
                    EventId = queryResult.Event.EventId,
                    OriginatingId =
                        queryResult.Event.OriginatingMessageId.Equals("self", StringComparison.OrdinalIgnoreCase)
                            ? queryResult.Event.LastMessageId
                            : queryResult.Event.OriginatingMessageId,
                    Status = queryResult.Status
                });
            }
        }

        return invalidMessageEvents;
    }

    public async Task<EndpointSubscription> SubscribeToEndpointNotification(string endpointId, string mail,
        string type, string author, string url, List<string> eventTypes, string payload, int frequency)
    {
        var formattedType = string.Equals(type, "mail", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(type, "teams", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(type, "mail;teams", StringComparison.OrdinalIgnoreCase)
            ? type.ToLower()
            : throw new Exception($"Invalid type.{type} valid: mail or teams ");

        if (!ValidateEmail(mail)) throw new Exception($"Invalid email: {mail}");

        var subscriptionContainer = await GetEndpointContainer(SubscriptionsContainer);
        var subscription = new EndpointSubscription
        {
            Mail = mail,
            Url = url,
            Type = formattedType,
            EndpointId = endpointId,
            AuthorId = author,
            Id = Guid.NewGuid().ToString(),
            EventTypes = eventTypes,
            Payload = payload,
            Frequency = frequency
        };

        //Add author here
        var response = await subscriptionContainer.UpsertItemAsync(subscription);

        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
        {
            _logger?.Verbose(
                $"COSMOS SUBSCRIPTION: endpointId: {subscription.EndpointId}, SubscriptionId: {subscription.Id}, HttpStatusCode: {response.StatusCode}");
            return subscription;
        }

        _logger?.Error(
            $"COSMOS SUBSCRIPTION ERROR: endpointId: {subscription.EndpointId}, SubscriptionId: {subscription.Id}, HttpStatusCode: {response.StatusCode}");
        return null; //Return error?
    }

    public async Task<IEnumerable<EndpointSubscription>> GetSubscriptionsOnEndpoint(string endpointId)
    {
        var subscriptions = new List<EndpointSubscription>();
        var db = _cosmosClient.GetDatabase(DatabaseId);
        Container subscriptionContainer = await db.CreateContainerIfNotExistsAsync(SubscriptionsContainer, "/id");

        var sqlQuery = $"SELECT * FROM c WHERE c.endpointId='{endpointId}'";
        var result = subscriptionContainer.GetItemQueryIterator<EndpointSubscription>(sqlQuery);

        while (result.HasMoreResults)
        {
            var subDbo = await result.ReadNextAsync();
            foreach (var queryResult in subDbo)
            {
                subscriptions.Add(queryResult);
            }
        }

        return subscriptions;
    }

    public async Task<IEnumerable<EndpointSubscription>> GetSubscriptionsOnEndpointWithEventtype(string endpointId,
        string eventType, string payload, string errorText)
    {
        var subscriptions = new List<EndpointSubscription>();
        var db = _cosmosClient.GetDatabase(DatabaseId);
        Container subscriptionContainer = await db.CreateContainerIfNotExistsAsync(SubscriptionsContainer, "/id");

        var sqlQuery = $"SELECT * FROM c WHERE c.endpointId='{endpointId}'";
        //Is defined, not defined, null or empty?
        if (!String.IsNullOrEmpty(eventType))
            sqlQuery +=
                $" AND (" +
                $"ARRAY_CONTAINS(c.eventTypes,'{eventType}') OR " +
                $"ARRAY_LENGTH(c.eventTypes) = 0 OR " +
                $"c.eventTypes = null OR " +
                $"c.eventTypes = '' OR " +
                $"(NOT IS_DEFINED(c.eventTypes))" +
                $")";
        if (!String.IsNullOrEmpty(payload))
            sqlQuery +=
                $" AND (" +
                $"CONTAINS('{payload}',c.payload) OR " +
                $"c.payload = null OR c.payload = '' OR " +
                $"(NOT IS_DEFINED(c.payload))";
        if (!String.IsNullOrEmpty(errorText))
            sqlQuery +=
                $" OR CONTAINS('{errorText}',c.payload)";

        // Correct parenthesis termination
        if (!String.IsNullOrEmpty(payload))
            sqlQuery += $")";

        var result = subscriptionContainer.GetItemQueryIterator<EndpointSubscription>(sqlQuery);

        while (result.HasMoreResults)
        {
            var subDbo = await result.ReadNextAsync();
            foreach (var queryResult in subDbo)
            {
                subscriptions.Add(queryResult);
            }
        }

        return subscriptions;
    }

    public async Task<bool> DeleteSubscription(string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId)) return false;

        var subscriptionContainer = await GetEndpointContainer(SubscriptionsContainer);

        try
        {
            var response = await subscriptionContainer.DeleteItemAsync<SubscriptionDbo>(subscriptionId, new PartitionKey(subscriptionId));
            _logger?.Verbose(
                $"COSMOS REMOVE-SUBSCRIPTION: SubscriptionId: {subscriptionId}, HttpStatusCode: {response.StatusCode}");
            return true;
        }
        catch (Exception e)
        {
            _logger?.Error(
                $"COSMOS REMOVE-SUBSCRIPTION: SubscriptionId: {subscriptionId}, Exception: {e.Message}");
            return false;
        }
    }
    public async Task<bool> UnsubscribeById(string endpointId, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        var subscriptionContainer = await GetEndpointContainer(SubscriptionsContainer);

        try
        {
            var response = await subscriptionContainer.DeleteItemAsync<SubscriptionDbo>(id, new PartitionKey(id));
            _logger?.Verbose(
                $"COSMOS REMOVE-SUBSCRIPTION: endpointId: {endpointId}, SubscriptionId: {id}, HttpStatusCode: {response.StatusCode}");
            return true;
        }
        catch (Exception e)
        {
            _logger?.Error(
                $"COSMOS REMOVE-SUBSCRIPTION: endpointId: {endpointId}, SubscriptionId: {id}, Exception: {e.Message}");
            return false;
        }
    }

    public async Task<bool> UnsubscribeByMail(string endpointId, string mail)
    {
        if (string.IsNullOrWhiteSpace(mail)) return false;

        var subs = await GetSubscriptionsOnEndpoint(endpointId);
        var mySubscription =
            subs.FirstOrDefault(x => string.Equals(mail, x.Mail, StringComparison.OrdinalIgnoreCase));
        if (mySubscription != null)
        {
            return await UnsubscribeById(endpointId, mySubscription.Id);
        }

        return false;
    }

    public async Task<bool> UpdateSubscription(EndpointSubscription subscription)
    {
        subscription.ErrorList = await GetEndpointErrorList(subscription.EndpointId);
        subscription.NotifiedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        try
        {
            var subscriberContainer = await GetEndpointContainer(SubscriptionsContainer);
            await subscriberContainer.UpsertItemAsync(subscription);
            return true;
        }
        catch (Exception e)
        {
            _logger?.Error(e,
                $"COSMOS UPDATE-SUBSCRIPTION: Endpoint: {subscription.EndpointId}, SubscriptionId: {subscription.Id}, Exception: {e.Message}");
            return false;
        }
    }

    public async Task<string> GetEndpointErrorList(string endpointId)
    {
        var container = await GetEndpointContainer(endpointId);
        var sqlQuery =
            $"SELECT * FROM c WHERE c.status IN ('{FailedStatus}', '{DeferredStatus}') AND (NOT IS_DEFINED(c.deleted) or c.deleted != true)";

        var result = container.GetItemQueryIterator<EventDbo>(sqlQuery);
        var failedAndDefferedlist = "";
        while (result.HasMoreResults)
        {
            var message = await result.ReadNextAsync();
            foreach (var queryResult in message)
            {
                failedAndDefferedlist += $"{queryResult.Id};";
            }
        }

        return failedAndDefferedlist;
    }

    private async Task<Container> GetEndpointContainer(string endpointId)
    {
        var db = _cosmosClient.GetDatabase(DatabaseId);
        var container = await db.CreateContainerIfNotExistsAsync(new ContainerProperties
        {
            Id = endpointId,
            PartitionKeyPath = "/id",
            DefaultTimeToLive = -1,
        }, ThroughputProperties.CreateAutoscaleThroughput(1000));
        return container;
    }


    private async Task<bool> UploadMessage(string eventId, string sessionId, string endpointId,
        UnresolvedEvent content, string status)
    {
        var container = await GetEndpointContainer(endpointId);

        var eventDbo = new EventDbo
        {
            Id = $"{eventId}_{sessionId}",
            Event = content,
            SessionId = sessionId,
            Status = status,
            EventType = content.EventTypeId,
            Deleted = false,
            TimeToLive = -1 // TTL Disabled
        };

        try
        {
            var response = await container.UpsertItemAsync(eventDbo, new PartitionKey(eventDbo.Id));
            _logger?.Verbose(
                $"COSMOS UPSERT-RESPONSE: EventId: {eventId}, SessionId: {sessionId}, HttpStatusCode: {response.StatusCode}, Status : {status}");
            return true;
        }
        catch (CosmosException e)
        {
            _logger?.Error(e,
                $"COSMOS UPSERT-ERROR: EventId: {eventId}, SessionId: {sessionId}, Status : {status}, HttpStatusCode: {e.StatusCode}");

            if (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new RequestLimitException();
            }

            throw;
        }
    }

    private static bool ValidateEmail(string mail)
    {
        var regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
        var match = regex.Match(mail);
        return match.Success;
    }

    public async Task<EndpointMetadata> GetEndpointMetadata(string endpointId)
    {
        var container = await GetEndpointContainer("Metadata");
        try
        {
            var rel = await container.ReadItemAsync<EndpointMetadata>(endpointId, new PartitionKey(endpointId));
            return rel.Resource;
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw;
        }
    }

    public async Task<List<EndpointMetadata>>? GetMetadatas(IEnumerable<string> endpointIds)
    {
        var container = await GetEndpointContainer("Metadata");
        try
        {
            var rel = await container.ReadManyItemsAsync<EndpointMetadata>(endpointIds.Select(x => (x, new PartitionKey(x))).ToList());
            return rel.Any() ? rel.Resource.ToList() : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<EndpointMetadata>> GetMetadatas()
    {
        var sqlQuery = $"SELECT * FROM c";
        var metadatas = await GetMetadatasByFilter(sqlQuery);

        return metadatas;
    }

    public async Task<List<EndpointMetadata>> GetMetadatasWithEnabledHeartbeat()
    {
        var sqlQuery = $"SELECT * FROM c WHERE c.IsHeartbeatEnabled = true";
        var metadatas = await GetMetadatasByFilter(sqlQuery);

        return metadatas;
    }

    private async Task<List<EndpointMetadata>> GetMetadatasByFilter(string sqlQuery)
    {
        var container = await GetEndpointContainer("Metadata");
        var result = container.GetItemQueryIterator<EndpointMetadata>(sqlQuery);
        var metadatas = new List<EndpointMetadata>();

        while (result.HasMoreResults)
        {
            var subDbo = await result.ReadNextAsync();
            foreach (var queryResult in subDbo)
            {
                metadatas.Add(queryResult);
            }
        }
        return metadatas;
    }

    public async Task EnableHeartbeatOnEndpoint(string endpointId, bool enable)
    {
        var container = await GetEndpointContainer("Metadata");
        List<PatchOperation> patchOperations = new List<PatchOperation>()
        {
            PatchOperation.Replace("/IsHeartbeatEnabled", enable),
        };

        var item = await container.PatchItemAsync<EndpointMetadata>(endpointId, new PartitionKey(endpointId), patchOperations);

    }

    public async Task<bool> SetEndpointMetadata(EndpointMetadata endpointMetadata)
    {
        var container = await GetEndpointContainer("Metadata");

        try
        {
            var response =
                await container.UpsertItemAsync(endpointMetadata, new PartitionKey(endpointMetadata.EndpointId));
            _logger?.Verbose(
                $"COSMOS UPSERT-RESPONSE: Metadata upsert. Id: {endpointMetadata.EndpointId}, HttpStatusCode: {response.StatusCode}");
            return true;
        }
        catch (CosmosException e)
        {
            _logger?.Error(e,
                $"COSMOS UPSERT-ERROR: Metadata upsert. Id: {endpointMetadata.EndpointId}, HttpStatusCode: {e.StatusCode}");

            if (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new RequestLimitException();
            }

            throw;
        }
    }

    public async Task<bool> SetHeartbeat(Heartbeat heartbeat, string endpointId)
    {
        var container = await GetEndpointContainer("Metadata");

        try
        {
            var metadata = await GetEndpointMetadata(endpointId);
            metadata.EndpointHeartbeatStatus = heartbeat.EndpointHeartbeatStatus;

            // Check if heartbeat exists
            if (metadata.Heartbeats == null)
                metadata.Heartbeats = new List<Heartbeat>();
            // Check if id exists
            var existingHeartbeat = metadata.Heartbeats.FirstOrDefault(h => h.MessageId == heartbeat.MessageId);
            if (existingHeartbeat != null)
            {
                existingHeartbeat.ReceivedTime = heartbeat.ReceivedTime;
                existingHeartbeat.EndTime = heartbeat.EndTime;
                existingHeartbeat.EndpointHeartbeatStatus = heartbeat.EndpointHeartbeatStatus;
            }
            else
            {
                if (metadata.Heartbeats.Count >= Maxheartbeats)
                {
                    metadata.Heartbeats = metadata.Heartbeats.OrderBy(h => h.StartTime).Skip(1).ToList();
                    metadata.Heartbeats.Add(heartbeat);
                }
                else
                {
                    metadata.Heartbeats.Add(heartbeat);
                }
            }

            var response =
                await container.UpsertItemAsync(metadata, new PartitionKey(endpointId));
            _logger?.Verbose(
                $"COSMOS UPSERT-RESPONSE: Metadata upsert. Id: {endpointId}, HttpStatusCode: {response.StatusCode}");
            return true;
        }
        catch (CosmosException e)
        {
            _logger?.Error(e,
                $"COSMOS UPSERT-ERROR: Metadata upsert. Id: {endpointId}, HttpStatusCode: {e.StatusCode}");

            if (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new RequestLimitException();
            }

            throw;
        }
    }

    class StatusQueryResult
    {
        public int EventCount { get; set; }

        [JsonProperty(PropertyName = "Status")]
        public string Status { get; set; }
    }

    class SessionCountQueryResult
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "Status")]
        public string Status { get; set; }
    }

    class EventDbo
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "eventType")]
        public string EventType { get; set; }

        [JsonProperty(PropertyName = "sessionId")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "event")] public UnresolvedEvent Event { get; set; }

        [JsonProperty(PropertyName = "deleted")]
        public bool? Deleted { get; set; }

        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLive { get; set; }
    }

    class SubscriptionDbo
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }
        [JsonProperty(PropertyName = "type")] public string Type { get; set; }

        [JsonProperty(PropertyName = "severity")]
        public string Severity { get; set; }

        [JsonProperty(PropertyName = "mail")] public string Mail { get; set; }

        [JsonProperty(PropertyName = "endpointId")]
        public string EndpointId { get; set; }
    }
}