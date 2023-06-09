﻿using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Events;
using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using BH.DIS.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BH.DIS.SDK;

public class PublisherClient : IPublisherClient
{
    private readonly ISender _sender;
    private readonly ILoggerProvider _loggerProvider;

    public PublisherClient(ServiceBusClient client, string endpoint) : this(client, endpoint, null)
    {
    }

    public PublisherClient(ServiceBusClient client, string endpoint, ILoggerProvider loggerProvider)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (string.IsNullOrEmpty(endpoint)) throw new ArgumentException(nameof(endpoint));
        _loggerProvider = loggerProvider;

        var serviceBusSender = client.CreateSender(endpoint);
        _sender = new Sender(serviceBusSender);
    }

    public async Task Publish(IEvent @event)
    {
        await Publish(@event, @event.GetSessionId(), Guid.NewGuid().ToString());
    }

    public async Task Publish(IEvent @event, string sessionId, string correlationId)
    {
        var eventType = @event.GetEventType();
        var messagePayload = JsonConvert.SerializeObject(@event);
        var messageId = $"{eventType.Id}-{messagePayload.GetHashCode()}";

        await Publish(@event, sessionId, correlationId, messageId);
    }

    public async Task Publish(IEvent @event, string sessionId, string correlationId, string messageId)
    {
        var message = GetMessage(@event, correlationId, messageId, sessionId);
        var logger = _loggerProvider?.GetContextualLogger(message);

        try
        {
            await _sender.Send(message);
            logger?.Information("Publisher Successfully published {EventType} event on ServiceBus. MessageId: {MessageId}, CorrelationId: {CorrelationId}", @event.GetEventType().Id, message.MessageId, correlationId);
        }
        catch (Exception ex)
        {
            logger?.Error(ex, "Failed to publish {EventType} event on ServiceBus", @event.GetEventType().Id);
            throw;
        }
    }

    /// <summary>
    /// Pre-release - use with care!
    /// Publish multiple messages at once. Make sure batch size enforced by Azure Service Bus is taken into account.
    /// </summary>
    /// <param name="events">List of events you want to publish. Make sure to make them before publishing</param>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    public async Task PublishBatch(IEnumerable<IEvent> events, string correlationId = null)
    {
        if (correlationId == null)
        {
            correlationId = Guid.NewGuid().ToString();
        }

        var messages = events.Select(@event => GetMessage(@event, correlationId)).ToList();

        var logger = _loggerProvider?.GetContextualLogger(correlationId);

        try
        {
            await _sender.Send(messages);
            logger?.Information("Publisher Successfully published batch of {Count} events on ServiceBus. CorrelationId: {CorrelationId}", messages.Count(), correlationId);
        }
        catch (Exception ex)
        {
            logger?.Error(ex, $"Failed to publish batch of events on ServiceBus");
            throw;
        }
    }

    /// <summary>
    /// Use to get batch of maximum possible size supported by Azure Service Bus
    /// </summary>
    /// <param name="events">Events you want to split into multiple batches</param>
    /// <returns>Batches of events</returns>
    public IEnumerable<IEnumerable<IEvent>> GetBatches(List<IEvent> events)
    {
        return GetBatchesStatic(events);
    }

    /// <summary>
    /// Use to get batch of maximum possible size supported by Azure Service Bus
    /// </summary>
    /// <param name="events">Events you want to split into multiple batches</param>
    /// <returns>Batches of events</returns>
    public static IEnumerable<IEnumerable<IEvent>> GetBatchesStatic(List<IEvent> events)
    {
        // There is a 256 KB limit per message sent on Azure Service Bus.
        // We will divide it into messages block lower or equal to 256 KB.
        // Maximum message size: 256 KB for Standard tier, 1 MB for Premium tier.
        // Maximum header size: 64 KB.
        // Our user properties are pretty significant, so we'll leave half the size for that as well.
        // https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas
        var maxBatchSize = 256000 - 64000 - 128000;
        do
        {
            var pageSize = 0L;
            var page = events.TakeWhile(@event =>
            {
                var messageToPublish = MessageHelper.ToServiceBusMessage(GetMessageStatic(@event));
                if (pageSize + messageToPublish.Body.ToArray().Length > maxBatchSize)
                    return false;

                pageSize += messageToPublish.Body.ToArray().Length;
                return true;
            }).ToList();

            events.RemoveRange(0, page.Count);
            yield return page;
        } while (events.Any());
    }

    private IMessage GetMessage(IEvent @event, string correlationId = null, string messageId = null, string sessionId = null)
    {
        return GetMessageStatic(@event, correlationId, messageId, sessionId);
    }

    private static IMessage GetMessageStatic(IEvent @event, string correlationId = null, string messageId = null, string sessionId = null)
    {
        @event.Validate();

        var eventType = @event.GetEventType().Id;
        var messagePayload = JsonConvert.SerializeObject(@event);
        messageId ??= $"{eventType}-{messagePayload.GetHashCode()}";
        sessionId ??= @event.GetSessionId();
        correlationId ??= Guid.NewGuid().ToString();
        var message = new Message()
        {
            To = eventType,
            EventTypeId = eventType,
            SessionId = sessionId,
            CorrelationId = correlationId,
            MessageId = messageId,
            RetryCount = 0,
            MessageType = MessageType.EventRequest,
            MessageContent = new MessageContent
            {
                EventContent = new EventContent
                {
                    EventTypeId = eventType,
                    EventJson = messagePayload
                }
            }
        };
        return message;
    }
}