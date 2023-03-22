using BH.DIS.Core.Events;
using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using BH.DIS.MessageStore;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace BH.DIS.Manager;
public interface IManagerClient
{
    /// <summary>
    /// Submit event to broker for publication.
    /// </summary>
    Task Publish(IEvent @event);

    /// <summary>
    /// Resolve failed event request by resubmitting/replacing it.
    /// </summary>
    /// <param name="errorResponse">ErrorResponse received from endpoint, representing the error that needs to be resolved.</param>
    /// <param name="eventTypeId">Event type that should be processed before resolving the error.</param>
    /// <param name="eventJson">Event data of that should be processed before resolving the error.</param>

    public Task Resubmit(MessageEntity errorResponse, string endpoint, string eventTypeId, string eventJson);

    /// <summary>
    /// Resolve failed event request by ignoring it.
    /// </summary>
    /// <param name="errorResponse">ErrorResponse received from endpoint, representing the error that needs to be resolved.</param>
    Task Skip(MessageEntity errorResponse, string endpoint, string eventTypeId);
}

public class ManagerClient : IManagerClient
{
    private readonly ISender _sender;
    private readonly ILogger _logger;

    public ManagerClient(ISender sender, ILogger logger = null)
    {
        _sender = sender;
        _logger = logger;
    }

    public Task Publish(IEvent @event)
    {
        _logger?.Verbose($"MANAGER PUBLISH EVENT: SessionId: {@event.GetSessionId()} EventtypeId: {@event.GetEventType().Id} EventName: {@event.GetEventType().Name} ");
        return _sender.Send(new Message
        {
            SessionId = @event.GetSessionId(),
            To = BH.DIS.Core.Messages.Constants.BrokerId,
            CorrelationId = Guid.NewGuid().ToString(),
            EventTypeId = @event.GetEventType().Id,
            MessageType = MessageType.EventRequest,
            EventId = Guid.NewGuid().ToString(),
            MessageId = Guid.NewGuid().ToString(),
            MessageContent = new MessageContent
            {
                EventContent = new EventContent
                {
                    EventTypeId = @event.GetEventType().Id,
                    EventJson = JsonConvert.SerializeObject(@event)
                }
            }
        });
    }

    public Task Resubmit(MessageEntity errorResponse, string endpoint, string eventTypeId, string eventJson)
    {
        _logger?.Verbose($"MANAGER RESUBMIT EVENT: EventId: {errorResponse.EventId} EventtypeId: {eventTypeId} EventJson: {eventJson} errorResponse: {errorResponse} ");
        return _sender.Send(new Message
        {
            CorrelationId = errorResponse.CorrelationId,
            EventId = errorResponse.EventId,
            SessionId = errorResponse.SessionId,
            To = endpoint,
            OriginatingMessageId = errorResponse.OriginatingMessageId ?? errorResponse.MessageId,
            ParentMessageId = errorResponse.MessageId,
            MessageType = MessageType.ResubmissionRequest,
            EventTypeId = eventTypeId,
            MessageContent = new MessageContent
            {
                EventContent = new EventContent
                {
                    EventTypeId = eventTypeId,
                    EventJson = eventJson
                }
            },
        });
    }

    public Task Skip(MessageEntity errorResponse, string endpoint, string eventTypeId)
    {
        _logger?.Verbose($"MANAGER SKIP EVENT: SessionId: {errorResponse.SessionId} EventId: {errorResponse.EventId} From: {errorResponse.To} ");
        return _sender.Send(new Message()
        {
            CorrelationId = errorResponse.MessageId,
            EventId = errorResponse.EventId,
            SessionId = errorResponse.SessionId,
            To = endpoint,
            MessageType = MessageType.SkipRequest,
            MessageContent = new MessageContent(),
            ParentMessageId = errorResponse.MessageId,
            EventTypeId = eventTypeId,
            OriginatingMessageId = errorResponse.OriginatingMessageId ?? errorResponse.MessageId
        });
    }
}