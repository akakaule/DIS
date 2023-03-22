using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BH.DIS.MessageStore.States;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.DIS.MessageStore;

public interface IMessageStoreClient
{
    Task UploadMessage(MessageEntity message);
    Task UploadFailedEvent(string eventId, string messageId);
    Task UploadMessageAudit(string eventId, MessageAuditEntity auditEntity);
    Task<IEnumerable<MessageEntity>> DownloadFailedEvents(string endpointId);
    Task<IEnumerable<MessageAuditEntity>> DownloadMessageAuditsForEvent(string eventId);
    Task<MessageEntity> DownloadMessage(string eventId, string messageId);
    Task UploadPendingMessage(string eventId, string sessionId, string endpointId, object content);
    Task UploadDeferredMessage(string eventId, string sessionId, string endpointId, object content);
    Task UploadFailedMessage(string eventId, string sessionId, string endpointId, object content);
    void RemovePendingMessage(string eventId, string sessionId, string endpointId);
    void RemoveDeferredMessage(string eventId, string sessionId, string endpointId);
    void RemoveFailedMessage(string eventId, string sessionId, string endpointId);
    Task<EndpointState> DownloadEndpointState(string endpointId);
    Task<IEnumerable<EndpointState>> DownloadEndpointStates(IEnumerable<string> endpointIds);
    Task<EndpointState> DownloadEndpointStateSlim(string endpointId);
    Task<Dictionary<string, UnresolvedEvent>> DownloadEntireEndpointState(string endpointId);
    Task<UnresolvedEvent> GetPendingEvent(string endpointId, string eventId, string sessionId);
    Task<UnresolvedEvent> GetFailedEvent(string endpointId, string eventId, string sessionId);
    Task<UnresolvedEvent> GetDeferredEvent(string endpointId, string eventId, string sessionId);
    Task<MessageEntity> GetFailedMessage(string eventId, string endpoint);
    Task<MessageEntity> GetDeadletteredMessage(string eventId, string endpoint);
    Task<IEnumerable<MessageEntity>> GetEventHistory(string eventId);
}

public class MessageStoreClient : IMessageStoreClient
{
    private const string EndpointStatesContainerName = "endpoint-states";
    private const string PendingContainerSuffix = "pending";
    private const string DeferredContainerSuffix = "deferred";
    private const string FailedContainerSuffix = "failed";
    private const string FailedEventContainerPrefix = "failed-events-";
    private const string ResubmittedMessagesAuditContainer = "messages-audit";

    private readonly BlobServiceClient _blobServiceClient;

    public MessageStoreClient(string storageConnection)
    {
        _blobServiceClient = new BlobServiceClient(storageConnection);
    }

    public Task UploadMessage(MessageEntity message) =>
        UpsertBlob(containerId: message.EventId, blobId: message.MessageId, blobContent: message);

    public async Task UploadFailedEvent(string eventId, string messageId)
    {
        var message = await DownloadMessage(eventId, messageId);
        if (message != null)
            await UpsertBlob(containerId: $"{FailedEventContainerPrefix}{message.EndpointId.ToLower()}", blobId: eventId, blobContent: message);
    }

    public async Task UploadMessageAudit(string eventId, MessageAuditEntity auditEntity)
    {
        var audits = await DownloadMessageAuditsForEvent(eventId);
        audits = audits.Concat(new List<MessageAuditEntity>() { auditEntity });
        await UpsertBlob(containerId: ResubmittedMessagesAuditContainer, blobId: eventId, blobContent: audits);
    }

    public async Task<IEnumerable<MessageAuditEntity>> DownloadMessageAuditsForEvent(string eventId)
    {
        var audits = await DownloadBlob<IEnumerable<MessageAuditEntity>>(containerId: ResubmittedMessagesAuditContainer, blobId: eventId);
            
        return audits ?? new List<MessageAuditEntity>();
    }

    public async Task<IEnumerable<MessageEntity>> DownloadFailedEvents(string endpointId)
    {
        var failedEvents = await DownloadBlobs<MessageEntity>($"{FailedEventContainerPrefix}{endpointId.ToLower()}");
        return failedEvents;
    }

    public Task<MessageEntity> DownloadMessage(string eventId, string messageId) =>
        DownloadBlob<MessageEntity>(containerId: eventId, blobId: messageId);

    public async Task<EndpointState> DownloadEndpointState(string endpointId)
    {
        var deferredResults = (await DownloadBlobList(GetDeferredEndpointContainerName(endpointId))).ToList();
        var pendingResults = (await DownloadBlobList(GetPendingEndpointContainerName(endpointId))).ToList();
        var failedResults = (await DownloadBlobList(GetFailedEndpointContainerName(endpointId))).ToList();

        var eventConcat = deferredResults.Concat(pendingResults).Concat(failedResults).ToList();

        return new EndpointState
        {
            EventTime = eventConcat.Max(b => b.Properties.LastModified)?.DateTime ?? DateTime.Now,
            EndpointId = endpointId,
            PendingEvents = pendingResults.Select(b => b.Name),
            DeferredEvents = deferredResults.Select(b => b.Name),
            FailedEvents = failedResults.Select(b => b.Name)
        };
    }

    public async Task<EndpointState> DownloadEndpointStateSlim(string endpointId)
    {
        var segmentSize = 10;
        var deferredResults = await DownloadBlobListPage(GetDeferredEndpointContainerName(endpointId), null, segmentSize);
        var pendingResults = await DownloadBlobListPage(GetPendingEndpointContainerName(endpointId), null, segmentSize);
        var failedResults = await DownloadBlobListPage(GetFailedEndpointContainerName(endpointId), null, segmentSize);

        var eventConcat = deferredResults.Blobs.Concat(pendingResults.Blobs.Concat(failedResults.Blobs)).ToList();
            
        return new EndpointState
        {
            EventTime = eventConcat.Max(b => b.Properties.LastModified)?.DateTime ?? DateTime.Now,
            EndpointId = endpointId,
            PendingEvents = pendingResults.Blobs.Select(b => b.Name),
            DeferredEvents = deferredResults.Blobs.Select(b => b.Name),
            FailedEvents = failedResults.Blobs.Select(b => b.Name)
        };
    }

    public async Task<Dictionary<string, UnresolvedEvent>> DownloadEntireEndpointState(string endpointId)
    {
        var endpointState = await DownloadEndpointState(endpointId);
        var containerName = GetDeferredEndpointContainerName(endpointId);

        var deferredUnresolvedEvents = await DownloadUnresolvedEvents(endpointState.DeferredEvents, containerName);
        var pendingUnresolvedEvents = await DownloadUnresolvedEvents(endpointState.PendingEvents, containerName);
        var failedUnresolvedEvents = await DownloadUnresolvedEvents(endpointState.FailedEvents, containerName);
            
        var allEvents = deferredUnresolvedEvents.Concat(pendingUnresolvedEvents).Concat(failedUnresolvedEvents);

        var allEventResults = allEvents.Where(e => !string.IsNullOrEmpty(e?.EventId)).Select(e => e).ToList();

        if (allEventResults.Count < 1)
        {
            return new Dictionary<string, UnresolvedEvent>();
        }

        Dictionary<string, UnresolvedEvent> unresolvedEventsDict = allEventResults
            .OrderBy(e => e.EventId)
            .ToDictionary(g => g.EventId, g => g);

        return unresolvedEventsDict;
    }

    private async Task<IEnumerable<UnresolvedEvent>> DownloadUnresolvedEvents(IEnumerable<string> events, string containerName)
    {
        var result = new List<UnresolvedEvent>();
        foreach (var blobId in events)
        {
            var blob = await DownloadBlob<UnresolvedEvent>(containerName, blobId);
            result.Add(blob);
        }

        return result;
    }
        
    public async Task<IEnumerable<EndpointState>> DownloadEndpointStates(IEnumerable<string> endpointIds)
    {
        var result = new List<EndpointState>();

        foreach (var endpointId in endpointIds)
        {
            var endpointState = await DownloadEndpointState(endpointId);
            result.Add(endpointState);
        }

        return result;
    }

    private async Task<BlobsEventPaging> DownloadBlobListPage(string containerId, string continuationToken, int segmentSize)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerId);
        if (!await container.ExistsAsync())
            return null;

        var blobs = new List<BlobItem>();
        var resultSegment = container.GetBlobs()
            .AsPages(continuationToken, segmentSize);

        var page = resultSegment.FirstOrDefault();
        continuationToken = page.ContinuationToken;
        //foreach (BlobItem blobItem in page.Values)
        //{
        //    blobs.Add(blobItem);
        //}

        return new BlobsEventPaging
        {
            Blobs = blobs,
            ContinuationToken = continuationToken
        };
    }

    private async Task<IEnumerable<BlobItem>> DownloadBlobList(string containerId)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerId);
        if (!await container.ExistsAsync())
            return Enumerable.Empty<BlobItem>();

        var blobs = container.GetBlobs();
        return blobs.ToList();
    }

    private async Task<IEnumerable<T>> DownloadBlobs<T>(string containerId)
    {
        var blobs = await DownloadBlobList(containerId);
        var tcol = new List<T>();
        foreach (var b in blobs)
        {
            var t = await DownloadBlob<T>(containerId, b.Name);
            tcol.Add(t);
        }
        return tcol;
    }

    private async Task<T> DownloadBlob<T>(string containerId, string blobId)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerId);
        if (!await container.ExistsAsync())
            return default(T);

        var blob = container.GetBlobClient(blobId);
        if (!await blob.ExistsAsync())
            return default(T);

        var response = await blob.DownloadAsync();

        var reader = new StreamReader(response.Value.Content, Encoding.UTF8);
        string json = reader.ReadToEnd();
        return JsonConvert.DeserializeObject<T>(json);
    }

    private void DeleteBlobIfExists(string containerId, string blobId)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerId);
        container.DeleteBlobIfExists(blobId);
    }

    private async Task UpsertBlob(string containerId, string blobId, object blobContent)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerId);
        await container.CreateIfNotExistsAsync();

        var blob = container.GetBlobClient(blobId);

        string json = JsonConvert.SerializeObject(blobContent);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        await blob.UploadAsync(new MemoryStream(bytes), overwrite: true);
    }

    public async Task<MessageEntity> GetFailedMessage(string eventId, string endpointId)
    {
        var blobs = await DownloadBlobs<MessageEntity>(eventId);
        var failedMessageEntity = blobs.Where(me => me.MessageContent.ErrorContent != null)
            .Where(me => me.EndpointId.Equals(endpointId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(me => me.EnqueuedTimeUtc)
            .LastOrDefault();
        return failedMessageEntity;
    }

    public async Task<MessageEntity> GetDeadletteredMessage(string eventId, string endpointId)
    {
        var blobs = await DownloadBlobs<MessageEntity>(eventId);
        return blobs
            .Where(e => e.EndpointId.Equals(endpointId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.EnqueuedTimeUtc)
            .LastOrDefault();
    }

    public Task UploadPendingMessage(string eventId, string sessionId, string endpointId, object content) =>
        UpsertBlob(containerId: GetPendingEndpointContainerName(endpointId), blobId: $"{eventId}_{sessionId}", blobContent: content);


    public Task UploadDeferredMessage(string eventId, string sessionId, string endpointId, object content) =>
        UpsertBlob(containerId: GetDeferredEndpointContainerName(endpointId), blobId: $"{eventId}_{sessionId}", blobContent: content);

    public Task UploadFailedMessage(string eventId, string sessionId, string endpointId, object content) =>
        UpsertBlob(containerId: GetFailedEndpointContainerName(endpointId), blobId: $"{eventId}_{sessionId}", blobContent: content);

    public Task<UnresolvedEvent> GetPendingEvent(string endpointId, string eventId, string sessionId) =>
        DownloadBlob<UnresolvedEvent>(GetPendingEndpointContainerName(endpointId), $"{eventId}_{sessionId}");

    public Task<UnresolvedEvent> GetFailedEvent(string endpointId, string eventId, string sessionId) =>
        DownloadBlob<UnresolvedEvent>(GetFailedEndpointContainerName(endpointId), $"{eventId}_{sessionId}");

    public Task<UnresolvedEvent> GetDeferredEvent(string endpointId, string eventId, string sessionId) =>
        DownloadBlob<UnresolvedEvent>(GetDeferredEndpointContainerName(endpointId), $"{eventId}_{sessionId}");

    public void RemovePendingMessage(string eventId, string sessionId, string endpointId) =>
        DeleteBlobIfExists(GetPendingEndpointContainerName(endpointId), $"{eventId}_{sessionId}");

    public void RemoveDeferredMessage(string eventId, string sessionId, string endpointId) =>
        DeleteBlobIfExists(GetDeferredEndpointContainerName(endpointId), $"{eventId}_{sessionId}");

    public void RemoveFailedMessage(string eventId, string sessionId, string endpointId) =>
        DeleteBlobIfExists(GetFailedEndpointContainerName(endpointId), $"{eventId}_{sessionId}");

    private string GetPendingEndpointContainerName(string endpointId)
    {
        return $"{endpointId.ToLower()}-{PendingContainerSuffix}";
    }

    private string GetDeferredEndpointContainerName(string endpointId)
    {
        return $"{endpointId.ToLower()}-{DeferredContainerSuffix}";
    }

    private string GetFailedEndpointContainerName(string endpointId)
    {
        return $"{endpointId.ToLower()}-{FailedContainerSuffix}";
    }

    public async Task<IEnumerable<MessageEntity>> GetEventHistory(string eventId)
    {
        var blobs = await DownloadBlobs<MessageEntity>(eventId);
        var eventHistoryMessageEntities = blobs;
        return eventHistoryMessageEntities;
    }
}