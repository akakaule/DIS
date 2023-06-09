using System;
using System.Linq;
using BH.DIS.MessageStore;
using BH.DIS.WebApp.ManagementApi;
using BH.DIS.WebApp.Services.ApplicationInsights;
using BH.DIS.Core.Events;
using Event = BH.DIS.WebApp.ManagementApi.Event;
using BH.DIS.MessageStore.States;
using System.Collections;
using System.Collections.Generic;
using TechnicalContact = BH.DIS.WebApp.ManagementApi.TechnicalContact;
using Heartbeat = BH.DIS.WebApp.ManagementApi.Heartbeat;
using ResolutionStatus = BH.DIS.MessageStore.ResolutionStatus;

namespace BH.DIS.WebApp.Controllers;

public static class Mapper
{
    public static Event EventFromMessageStoreEvent(UnresolvedEvent @event)
    {
        var valid = Enum.TryParse(@event.ResolutionStatus.ToString(), true, out ResolutionStatus status);
        if (!valid)
        {
            throw new ArgumentOutOfRangeException(
                $"Value '{@event.ResolutionStatus}' is unknown for type of {nameof(Event.ResolutionStatus)}");
        }

        return new Event()
        {
            UpdatedAt = @event.UpdatedAt,
            EnqueuedTimeUtc = @event.EnqueuedTimeUtc,
            EventId = @event.EventId,
            SessionId = @event.SessionId,
            CorrelationId = @event.CorrelationId,
            ResolutionStatus = @event.ResolutionStatus.ToString(),
            EndpointRole = @event.EndpointRole.ToString(),
            EndpointId = @event.EndpointId,
            RetryCount = @event.RetryCount,
            RetryLimit = @event.RetryLimit,
            MessageType = @event.MessageType.ToString(),
            DeadLetterReason = @event.DeadLetterReason,
            DeadLetterErrorDescription = @event.DeadLetterErrorDescription,
            LastMessageId = @event.LastMessageId,
            OriginatingMessageId = @event.OriginatingMessageId,
            ParentMessageId = @event.ParentMessageId,
            Reason = @event.Reason,
            OriginatingFrom = @event.OriginatingFrom,
            EventTypeId = @event.EventTypeId,
            To = @event.To,
            From = @event.From,
            MessageContent = new MessageContent
            {
                ErrorContent = new ErrorContent
                {
                    ErrorText = @event.MessageContent?.ErrorContent?.ErrorText,
                    ErrorType = @event.MessageContent?.ErrorContent?.ErrorType,
                    ExceptionSource = @event.MessageContent?.ErrorContent?.ExceptionSource,
                    ExceptionStackTrace = @event.MessageContent?.ErrorContent?.ExceptionStackTrace,
                },
                EventContent = new EventContent
                {
                    EventJson = @event.MessageContent?.EventContent?.EventJson,
                    EventTypeId = @event.MessageContent?.EventContent?.EventTypeId,
                }
            }
        };
    }

    public static Message MessageFromMessageEntity(MessageEntity messageEntity)
    {
        var message = new Message
        {
            EventId = messageEntity.EventId,
            MessageId = messageEntity.MessageId,
            SessionId = messageEntity.SessionId,
            CorrelationId = messageEntity.CorrelationId,
            OriginatingMessageId = messageEntity.OriginatingMessageId,
            ParentMessageId = messageEntity.ParentMessageId,
            EndpointId = messageEntity.EndpointId,
            EndpointRole = messageEntity.EndpointRole == EndpointRole.Publisher
                ? MessageEndpointRole.Publisher
                : MessageEndpointRole.Subscriber,
            To = messageEntity.To,
            From = messageEntity.From,
            OriginatingFrom = messageEntity.OriginatingFrom,
            EnqueuedTimeUtc = messageEntity.EnqueuedTimeUtc,

            EventTypeId = messageEntity.MessageContent?.EventContent?.EventTypeId,
        };
        //Temporary fix, when messageType is not present
        try
        {
            message.MessageType = Enum.Parse<MessageType>(messageEntity.MessageType.ToString());
        }
        catch (Exception)
        {
            message.MessageType = MessageType.Unknown;
        }


        if (!string.IsNullOrEmpty(messageEntity.MessageContent?.EventContent?.EventJson))
            message.EventContent = messageEntity.MessageContent?.EventContent?.EventJson;

        if (messageEntity.MessageContent?.ErrorContent != null)
            message.ErrorContent = MessageErrorContentFromErroryContent(messageEntity.MessageContent?.ErrorContent);

        return message;
    }

    public static ManagementApi.EventType EventTypeFromIEventType(IEventType eventType)
    {
        return new ManagementApi.EventType()
        {
            Id = eventType.Id,
            Name = eventType.Name,
            Description = eventType.Description,
            Namespace = eventType.Namespace,
            Properties = eventType.Properties
                .Select(x => EventPropertyFromEventTypeProperty(x))
                .ToList()
        };
    }

    private static EventTypeProperty EventPropertyFromEventTypeProperty(IProperty property)
    {
        return new EventTypeProperty()
        {
            Name = property.Name,
            Description = property.Description,
            IsRequired = property.IsRequired,
            TypeFullName = property.TypeFullName,
            TypeName = property.TypeName
        };
    }

    internal static MessageAudit MessageAuditFromMessageAuditEntity(MessageAuditEntity audit)
    {
        return new MessageAudit()
        {
            AuditorName = audit.AuditorName,
            AuditTimestamp = audit.AuditTimestamp,
            AuditType = Enum.Parse<MessageAuditAuditType>(audit.AuditType.ToString())
        };
    }

    internal static MessageAuditEntity MessageAudityEntityFromMessageAudit(MessageAudit audit)
    {
        return new MessageAuditEntity()
        {
            AuditorName = audit.AuditorName,
            AuditTimestamp = audit.AuditTimestamp,
            AuditType = Enum.Parse<MessageAuditType>(audit.AuditType.ToString())
        };
    }

    internal static SessionStatus SessionStatusFromSessionState(SessionStateCount sessionState)
    {
        return new SessionStatus
        {
            SessionId = sessionState.SessionId,
            DeferredEvents = sessionState.DeferredEvents.ToList(),
            PendingEvents = sessionState.PendingEvents.ToList()
        };
    }

    internal static MessageErrorContent MessageErrorContentFromErroryContent(MessageEntity message)
    {
        return new MessageErrorContent
        {
            ErrorText = message.MessageType == Core.Messages.MessageType.UnsupportedResponse
                ? "This message is unsupported by the Subscriber"
                : "Deadlettered Event",
            ErrorType = message.MessageType.ToString(),
            ExceptionSource = !string.IsNullOrEmpty(message.DeadLetterErrorDescription)
                ? message.DeadLetterErrorDescription
                : "Mismatching nuget versions. Subscriber does not have the event created from the Publisher",
            ExceptionStackTrace = !string.IsNullOrEmpty(message.DeadLetterReason)
                ? message.DeadLetterReason
                : "In order to fix this issue: upgrade BH.EIP nuget, to the same version as the Publisher"
        };
    }

    private static MessageErrorContent MessageErrorContentFromErroryContent(Core.Messages.ErrorContent errorContent)
    {
        return new MessageErrorContent
        {
            ErrorText = errorContent.ErrorText ?? "",
            ErrorType = errorContent.ErrorType ?? "",
            ExceptionSource = !string.IsNullOrEmpty(errorContent.ExceptionSource)
                ? errorContent.ExceptionSource
                : "",
            ExceptionStackTrace = !string.IsNullOrEmpty(errorContent.ExceptionStackTrace)
                ? errorContent.ExceptionStackTrace
                : ""
        };
    }

    internal static EndpointStatusCount EndpointStatusCountFromEndpointStateCount(EndpointStateCount state)
    {
        return new EndpointStatusCount
        {
            EndpointId = state.EndpointId,
            EventTime = state.EventTime,
            DeferredCount = state.DeferredCount,
            PendingCount = state.PendingCount + state.UnsupportedCount,
            UnsupportedCount = state.UnsupportedCount,
            DeadletterCount = state.DeadletterCount,
            FailedCount = state.FailedCount + state.DeadletterCount
        };
    }

    internal static EventLogEntry EventLogEntryFromLogEntry(LogEntry log)
    {
        return new EventLogEntry
        {
            EventId = log.EventId,
            MessageId = log.MessageId,
            SessionId = log.SessionId,
            CorrelationId = log.CorrelationId,
            To = log.To,
            From = log.From,
            Text = log.Text,
            EventType = log.EventType,
            IsDeferred = log.IsDeferred,
            MessageType = log.MessageType,
            Payload = log.Payload,
            SeverityLevel = (EventLogEntrySeverityLevel)log.SeverityLevel,
            TimeStamp = log.Timestamp
        };
    }

    public static EndpointStatus EndpointStatusFromEndpointState(EndpointState state)
    {
        var events = new UnresolvedEvents();
        foreach (var e in state.EnrichedUnresolvedEvents)
        {
            events.Add(EventFromMessageStoreEvent(e));
        }

        return new EndpointStatus
        {
            EndpointId = state.EndpointId,
            EventTime = state.EventTime,
            DeferredEvents = state.DeferredEvents.ToList(),
            FailedEvents = state.FailedEvents.ToList(),
            PendingEvents = state.PendingEvents.ToList(),
            EnrichedUnresolvedEvents = events,
            UnsupportedEvents = state.UnsupportedEvents.ToList(),
            DeadletteredEvents = state.DeadletteredEvents.ToList(),
            ContinuationToken = String.IsNullOrEmpty(state.ContinuationToken) ? "" : state.ContinuationToken
        };
    }

    public static BlockedEvent BlockedEventFromBlockedMessageEvent(BlockedMessageEvent blockedMessageEvent)
    {
        return new BlockedEvent
        {
            EventId = blockedMessageEvent.EventId,
            OriginatingId = blockedMessageEvent.OriginatingId,
            Status = blockedMessageEvent.Status
        };
    }

    public static PendingEvent PendingEventFromBlockedMessageEvent(BlockedMessageEvent pendingMessageEvent)
    {
        return new PendingEvent
        {
            EventId = pendingMessageEvent.EventId,
            OriginatingId = pendingMessageEvent.OriginatingId,
            Status = pendingMessageEvent.Status
        };
    }

    public static List<ManagementApi.EndpointSubscription> SubscriptionsFromEndpointsubscriptions(
        IEnumerable<MessageStore.States.EndpointSubscription> endpointSubscriptions)
    {
        var returnList = new List<ManagementApi.EndpointSubscription>();
        foreach (var sub in endpointSubscriptions)
        {
            returnList.Add(new ManagementApi.EndpointSubscription()
            {
                Type = sub.Type,
                NotificationSeverity = sub.NotificationSeverity,
                Mail = sub.Mail,
                AuthorId = sub.AuthorId,
                Url = sub.Url,
                EventTypes = sub.EventTypes,
                Payload = sub.Payload,
                Frequency = sub.Frequency,
                Id = sub.Id,
            });
        }

        return returnList;
    }

    public static MessageStore.EventFilter MapFilter(ManagementApi.EventFilter filter)
    {
        List<string>? status = null;
        if (filter.ResolutionStatus != null)
        {
            status = new List<string>();
            filter.ResolutionStatus.ForEach(rs => status.Add(rs.ToString()));
        }


        return new MessageStore.EventFilter
        {
            EndPointId = filter.EndpointId,
            EventId = filter.EventId,
            EventTypeId = filter.EventTypeId,
            SessionId = filter.SessionId,
            ResolutionStatus = status,
            EnqueuedAtFrom = filter.EnqueuedAtFrom,
            EnqueuedAtTo = filter.EnqueuedAtTo,
            UpdatedAtFrom = filter.UpdateAtFrom,
            UpdatedAtTo = filter.UpdatedAtTo,

            Payload = filter.Payload,
        };
    }

    public static Metadata MetadataFromEndpointMetadata(EndpointMetadata endpointMetadata)
    {
        var technicalContacts = new List<TechnicalContact>();
        if (endpointMetadata.TechnicalContacts != null)
        {
            technicalContacts = endpointMetadata.TechnicalContacts
                .Select(technicalContact => new TechnicalContact()
                    { Name = technicalContact.Name, Email = technicalContact.Email }).ToList();
        }

        var heartbeats = new List<Heartbeat>();
        if (endpointMetadata.Heartbeats != null)
        {
            heartbeats = endpointMetadata.Heartbeats.Select(hb => new Heartbeat()
            {
                Id = hb.MessageId,
                StartTime = hb.StartTime,
                ReceivedTime = hb.ReceivedTime,
                EndTime = hb.EndTime,
                EndpointHeartbeatStatus = hb.EndpointHeartbeatStatus.ToString()
            }).ToList();
        }


        return new Metadata
        {
            Id = endpointMetadata.EndpointId,
            EndpointOwner = endpointMetadata.EndpointOwner,
            EndpointOwnerTeam = endpointMetadata.EndpointOwnerTeam,
            EndpointOwnerEmail = endpointMetadata.EndpointOwnerEmail,
            TechnicalContacts = technicalContacts,
            EndpointHeartbeatStatus = endpointMetadata.EndpointHeartbeatStatus.ToString(),
            HeartBeats = heartbeats,
            IsHeartbeatEnabled = endpointMetadata.IsHeartbeatEnabled,

        };
    }

    public static IEnumerable<MetadataShort> MetadataShortFromList(List<EndpointMetadata> heartbeats)
    {
        return heartbeats.Select(metadata => new MetadataShort()
        {
            EndpointId = metadata.EndpointId,
            HeartbeatStatus = metadata.EndpointHeartbeatStatus.ToString(),
            SubscriptionStatus = metadata.SubscriptionStatus != null && metadata.SubscriptionStatus.Value ? "active" : "disabled",
            IsHeartbeatEnabled = metadata.IsHeartbeatEnabled,
        });
    }

    public static MetadataShort MetadataShortFromMetadata(EndpointMetadata metadata)
    {
        return new MetadataShort()
        {
            EndpointId = metadata.EndpointId,
            HeartbeatStatus = metadata.EndpointHeartbeatStatus.ToString(),
            SubscriptionStatus = metadata.SubscriptionStatus != null && metadata.SubscriptionStatus.Value ? "active" : "disabled",
            IsHeartbeatEnabled = metadata.IsHeartbeatEnabled,
        };
    }
}