using BH.DIS.Core;
using BH.DIS.Manager;
using BH.DIS.MessageStore;
using BH.DIS.WebApp.ManagementApi;
using BH.DIS.WebApp.Services.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EventFilter = BH.DIS.WebApp.ManagementApi.EventFilter;

namespace BH.DIS.WebApp.Controllers.ApiContract
{
    namespace BH.DIS.WebApp.ManagementApi
    {
        [AllowAnonymous]
        public partial class EventApiController : Controller { }
    }
    public class EventImplementation : IEventApiController
    {
        private readonly IMessageStoreClient _messageStoreClient;
        private readonly IPlatform _platform;
        private readonly ILogger<EventImplementation> _logger;
        private readonly ICosmosDbClient _cosmosClient;
        private readonly IManagerClient _managerClient;
        private readonly HttpContext _context;
        private readonly IApplicationInsightsService _applicationInsightsService;
        private readonly IConfiguration _configuration;

        public EventImplementation(IMessageStoreClient messageStoreClient, IHttpContextAccessor contextAccessor, IApplicationInsightsService applicationInsightsService, IPlatform platform, IManagerClient managerClient, ILogger<EventImplementation> logger, ICosmosDbClient cosmosClient, IConfiguration config)
        {
            _messageStoreClient = messageStoreClient;
            _platform = platform;
            _logger = logger;
            _cosmosClient = cosmosClient;
            _managerClient = managerClient;
            _context = contextAccessor.HttpContext;
            _applicationInsightsService = applicationInsightsService;
            _configuration = config;
        }
        public async Task<ActionResult<Message>> GetEventIdsAsync(string eventId, string messageId)
        {
            try
            {
                var messageEntity = await _messageStoreClient.DownloadMessage(eventId, messageId);
                if (messageEntity != null)
                {
                    var message = Mapper.MessageFromMessageEntity(messageEntity);
                    return message;
                }
                return new NotFoundObjectResult("Event Message not found");
            }
            catch (Exception e)
            {
                _logger.LogWarning("Event Message not found. EventId: {EventId}, MessageId: {MessageId}, Ex: {Exception}", eventId, messageId, e.Message);
                return new NotFoundObjectResult("Event Message not found");
            }
        }

        public async Task<ActionResult<Event>> GetUnresolvedFailedEventIdAsync(string endpointId, string eventId, string sessionId)
        {
            try
            {
                var unresolvedEvent = await _cosmosClient.GetFailedEvent(endpointId, eventId, sessionId);
                return Mapper.EventFromMessageStoreEvent(unresolvedEvent);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Unresolved failed not found. EndpointId: {EndpointId}, EventId: {EventId}, SessionId: {SessionId}, Ex: {Exception}", endpointId, eventId, sessionId, e.Message);
                return new NotFoundObjectResult("Unresolved failed not found");
            }
        }

        public async Task<ActionResult<IEnumerable<MessageAudit>>> GetMessageAuditsEventIdAsync(string eventId)
        {
            var audits = await _messageStoreClient.DownloadMessageAuditsForEvent(eventId);
            if (audits != null)
            {
                return audits.Reverse().Select(Mapper.MessageAuditFromMessageAuditEntity).ToList();
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> PostMessageAuditAsync(MessageAudit body, string eventId)
        {
            try
            {
                var audit = Mapper.MessageAudityEntityFromMessageAudit(body);
                await _messageStoreClient.UploadMessageAudit(eventId, audit);
                return new OkResult();
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to PostMessageAudit: {ExceptionMessage}", e.Message);
                return new BadRequestResult();
            }
        }

        public async Task<IActionResult> PostResubmitEventIdsAsync(string eventId, string messageId)
        {
            _logger.LogInformation($"Resubmit message. EventId:{eventId}, MessageId:{messageId}");

            string eventTypeId;
            string endpoint;
            MessageEntity errorResponse = await _messageStoreClient.DownloadMessage(eventId, messageId);
            if (errorResponse == null)
            {
                _logger.LogWarning($"Could not resubmit message. Message not found. EventId: {eventId}, MessageId: {messageId}");
                return new BadRequestResult();
            }

            eventTypeId = errorResponse.EventTypeId;
            if (string.IsNullOrEmpty(eventTypeId))
            {
                MessageEntity origMessage = await _messageStoreClient.DownloadMessage(eventId, errorResponse.OriginatingMessageId);
                eventTypeId = origMessage.EventTypeId;
            }

            if (errorResponse.OriginatingMessageId.Equals("self"))
            {
                endpoint = errorResponse.To;
            }
            else
            {
                endpoint = errorResponse.From;
            }

            var messageAuditEntity = GetMessageAuditEntity(MessageAuditType.Resubmit);
            var eventJson = errorResponse.MessageContent.EventContent.EventJson;

            if (!IsManagerOfEndpoint(endpoint))
                throw new UnauthorizedAccessException($"User is unauthorized to manage endpoint '{endpoint}'.");

            await _managerClient.Resubmit(errorResponse, endpoint, eventTypeId, eventJson);
            await UploadFailedEvent(eventId, messageId);
            await _messageStoreClient.UploadMessageAudit(eventId, messageAuditEntity);
            return new OkResult();
        }

        public async Task<IActionResult> PostSkipEventIdsAsync(string eventId, string messageId)
        {
            _logger.LogInformation($"Skip message. EventId:{eventId}, MessageId:{messageId}");

            string eventTypeId;
            string endpoint;
            MessageEntity errorResponse = await _messageStoreClient.DownloadMessage(eventId, messageId);
            if (errorResponse == null)
            {
                _logger.LogWarning($"Could not skip message. Message not found. EventId: {eventId}, MessageId: {messageId}");
                return new BadRequestResult();
            }

            eventTypeId = errorResponse.EventTypeId;
            if (string.IsNullOrEmpty(eventTypeId))
            {
                MessageEntity origMessage = await _messageStoreClient.DownloadMessage(eventId, errorResponse.OriginatingMessageId);
                eventTypeId = origMessage.EventTypeId;
            }

            if (errorResponse.OriginatingMessageId.Equals("self"))
            {
                endpoint = errorResponse.To;
            }
            else
            {
                endpoint = errorResponse.From;
            }

            var messageAuditEntity = GetMessageAuditEntity(MessageAuditType.Skip);

            if (!IsManagerOfEndpoint(endpoint))
                throw new UnauthorizedAccessException($"User is unauthorized to manage endpoint '{endpoint}'.");

            await _managerClient.Skip(errorResponse, endpoint, eventTypeId);
            await _messageStoreClient.UploadMessageAudit(eventId, messageAuditEntity);
            await UploadFailedEvent(eventId, messageId);

            return new OkResult();
        }
        public async Task<ActionResult<Event>> GetEventIdAsync(string eventId, string endpointId)
        {
            try
            {
                var unresolvedEvent = await _cosmosClient.GetEvent(endpointId, eventId);
                if (unresolvedEvent == null) return new BadRequestResult();
                return Mapper.EventFromMessageStoreEvent(unresolvedEvent);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Event not found. EndpointId: {endpointId}, EventId: {eventId}, Ex: {e.Message}");
                return new NotFoundObjectResult("Event not found");
            }
        }

        public async Task<ActionResult<EventDetails>> GetEventDetailsIdAsync(string id, string endpoint)
        {
            var eventDetails = new EventDetails();

            try
            {
                var failedMsg = await _messageStoreClient.GetFailedMessage(id, endpoint);
                if (failedMsg != null)
                {
                    eventDetails.FailedMessage = Mapper.MessageFromMessageEntity(failedMsg);

                    var downloadedMsg = await _messageStoreClient.DownloadMessage(eventDetails.FailedMessage.EventId,
                        eventDetails.FailedMessage.OriginatingMessageId);
                    if (downloadedMsg != null)
                    {
                        eventDetails.OriginatingMessage = Mapper.MessageFromMessageEntity(downloadedMsg);
                    }

                    return eventDetails;
                }

                var msg = await _messageStoreClient.GetDeadletteredMessage(id, endpoint);

                if (msg.MessageType == Core.Messages.MessageType.ResolutionResponse)
                {
                    var completedMsg = await _messageStoreClient.DownloadMessage(msg.EventId, msg.OriginatingMessageId);
                    if (completedMsg != null)
                    {
                        eventDetails.FailedMessage = Mapper.MessageFromMessageEntity(completedMsg);
                    }
                    return eventDetails;
                }

                eventDetails.FailedMessage = Mapper.MessageFromMessageEntity(msg);

                eventDetails.FailedMessage.ErrorContent = Mapper.MessageErrorContentFromErroryContent(msg);
            }
            catch (Exception e)
            {
                _logger.LogWarning("GetEventDetailsIdAsync: {Exception}", e.Message);
            }

            return eventDetails;
        }

        public async Task<ActionResult<IEnumerable<EventLogEntry>>> GetEventDetailsLogsIdAsync(string id, string endpoint)
        {
            var logs = new List<EventLogEntry>();
            try
            {
                logs = (await _applicationInsightsService.GetLogs(id))
                    .Where(l => l.To.Equals(endpoint, StringComparison.OrdinalIgnoreCase) || l.From.Equals(endpoint, StringComparison.OrdinalIgnoreCase))
                    .Select(Mapper.EventLogEntryFromLogEntry)
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogWarning("GetEventDetailsLogsIdAsync: {Exception}", e.Message);
            }
            return logs;
        }

        public async Task<ActionResult<IEnumerable<Message>>> GetEventDetailsHistoryIdAsync(string id, string endpoint)
        {
            var histories = new List<Message>();
            try
            {
                histories = (await _messageStoreClient.GetEventHistory(id))
                    .Where(x => x.EndpointId.Equals(endpoint, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => a.EnqueuedTimeUtc)
                    .Select(Mapper.MessageFromMessageEntity)
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogWarning("GetEventDetailsHistoryIdAsync: {Exception}", e.Message);
            }
            return histories;
        }

        public async Task<IActionResult> PostComposeNewEventAsync(ResubmitWithChanges body)
        {
            var eventType = _platform.EventTypes.FirstOrDefault(x => x.Id.Equals(body.EventTypeId, StringComparison.OrdinalIgnoreCase));
            var producingEndpoint = _platform.GetProducers(eventType).FirstOrDefault();
            if (producingEndpoint == null)
            {
                return new BadRequestObjectResult("Could not find any producers for the given event");
            }
            if (eventType == null)
            {
                return new BadRequestObjectResult("Could not find event type: " + body.EventTypeId);
            }

            if (string.IsNullOrWhiteSpace(body.EventContent))
            {
                return new BadRequestObjectResult("Event content is empty" + body.EventContent);
            }

            ICollection<ValidationResult> validationResults = null;
            var type = eventType.GetEventClassType();
            try
            {
                var @event = (Core.Events.IEvent)JsonConvert.DeserializeObject(Convert.ToString(body.EventContent), type);
                var validationResult = @event.TryValidate();
                if (validationResult.IsValid)
                {
                    object data = new
                    {
                        TopicName = producingEndpoint.Id,
                        EventType = eventType.Id,
                        EventMessage = body.EventContent,
                        SessionId = @event.GetSessionId(),
                        CorrelationId = Guid.NewGuid().ToString()
                    };
                    var json = JsonConvert.SerializeObject(data);
                    var stringContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
                    var client = new HttpClient();
                    var response = await client.PostAsync(
                        new Uri(_configuration.GetValue<string>("EventPublisherUri")), stringContent);

                    if (!response.IsSuccessStatusCode)
                        return new BadRequestObjectResult($"Could not publish the event on the Event Publisher");

                    return new OkResult();
                }
                string errorMessage = validationResult.ValidationResults.Select(x => x.ErrorMessage).Aggregate("", (current, next) => current + ", " + next);
                return new BadRequestObjectResult($"\"Validation failed. Event does not fullfill the scheme '{type.Name}' Error: '{errorMessage}'\"");
            }
            catch (JsonReaderException e)
            {
                return new BadRequestObjectResult($"\"Could not parse the value '{e.Path}'\"");
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult("Could not compose event.");
            }
        }

        public async Task<IActionResult> PostResubmitWithChangesEventIdsAsync(ResubmitWithChanges body, string eventId, string messageId)
        {
            _logger.LogInformation($"Resubmit message with changes. EventId:{eventId}, MessageId:{messageId}, Body:{JsonConvert.SerializeObject(body)}");
            string endpoint;

            MessageEntity errorResponse = await _messageStoreClient.DownloadMessage(eventId, messageId);
            if (errorResponse == null)
            {
                _logger.LogWarning($"Could not resubmit message. Message not found. EventId: {eventId}, MessageId: {messageId}");
                return new BadRequestResult();
            }

            // If error response message is a result of forwarding a deadlettered message.
            if (errorResponse.OriginatingMessageId.Equals("self"))
            {
                endpoint = errorResponse.To;
            }
            else
            {
                endpoint = errorResponse.From;
            }

            string eventTypeId = body.EventTypeId;
            if (string.IsNullOrEmpty(body.EventTypeId))
            {
                eventTypeId = errorResponse.EventTypeId;

                if (string.IsNullOrEmpty(eventTypeId))
                {
                    MessageEntity origMessage = await _messageStoreClient.DownloadMessage(eventId, errorResponse.OriginatingMessageId);
                    eventTypeId = origMessage.EventTypeId;
                }
            }

            var messageAuditEntity = GetMessageAuditEntity(MessageAuditType.ResubmitWithChanges);

            if (!IsManagerOfEndpoint(endpoint))
                throw new UnauthorizedAccessException($"User is unauthorized to manage endpoint '{endpoint}'.");

            await _managerClient.Resubmit(errorResponse, endpoint, eventTypeId, body.EventContent);
            await _messageStoreClient.UploadMessageAudit(eventId, messageAuditEntity);

            return new OkResult();
        }

        // Somehow design this as a reusable function shared between implementations
        public async Task UploadFailedEvent(string eventId, string messageId)
        {
            await _messageStoreClient.UploadFailedEvent(eventId, messageId);
        }

        public async Task<ActionResult<IEnumerable<BlockedEvent>>> GetEventBlockedIdAsync(string sessionId, string endpointId)
        {
            var blockedEvents = (await _cosmosClient.GetBlockedEventsOnSession(endpointId, sessionId))
                    .Select(Mapper.BlockedEventFromBlockedMessageEvent)
                    .ToList();

            return blockedEvents;
        }

        public async Task<ActionResult<IEnumerable<Event>>> GetEventPendingIdAsync(string endpointId)
        {
            var events = (await _cosmosClient.GetPendingEventsOnSession(endpointId))
                .Select(Mapper.EventFromMessageStoreEvent)
                .ToList();

            return events;
        }

        public async Task<IActionResult> DeleteEventInvalidIdAsync(string endpointId, string eventId, string sessionId)
        {
            var result = await _cosmosClient.RemoveMessage(eventId, sessionId, endpointId);
            return result ? new OkResult() : null;
        }

        // Somehow design this as a reusable function shared between implementations
        private bool IsManagerOfEndpoint(string endpointId)
        {
            // TODO
            return true;
        }

        // Somehow design this as a reusable function shared between implementations
        private MessageAuditEntity GetMessageAuditEntity(MessageAuditType type)
        {
            var name = _context.User.FindFirst(c => c.Type.Equals("name", StringComparison.OrdinalIgnoreCase))?.Value;

            if (string.IsNullOrEmpty(name))
                name = _context.User.FindFirst(ClaimTypes.Name).Value;

            return new MessageAuditEntity { AuditorName = name, AuditTimestamp = DateTime.Now, AuditType = type };
        }

        public async Task<ActionResult<Event>> GetEventUnsupportedEndpointIdEventIdAsync(string endpointId, string eventId, string sessionId)
        {
            var result = await _cosmosClient.GetUnsupportedEvent(endpointId, eventId, sessionId);
            return Mapper.EventFromMessageStoreEvent(result);
        }

        public async Task<ActionResult<Event>> GetEventDeadletterEndpointIdEventIdAsync(string endpointId, string eventId, string sessionId)
        {
            var result = await _cosmosClient.GetDeadletteredEvent(endpointId, eventId, sessionId);
            return Mapper.EventFromMessageStoreEvent(result);
        }

        public async Task<ActionResult<SearchResponse>> PostApiEventEndpointIdGetByFilterAsync(SearchRequest body, string endpointId)
        {
            var reponse = await _cosmosClient.GetEventsByFilter(Mapper.MapFilter(body.EventFilter), body.ContinuationToken, body.MaxSearchItemsCount);
            return new SearchResponse
            {
                Events = reponse.Events
                .Select(Mapper.EventFromMessageStoreEvent)
                .ToList(),
                ContinuationToken = reponse.ContinuationToken
            };
        }


    }
}
