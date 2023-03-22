using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using BH.DIS.Core.Messages.Exceptions;
using BH.DIS.MessageStore;
using System;
using System.Threading.Tasks;

namespace BH.DIS.Broker.Services
{
    public class ResolverService : IMessageHandler
    {
        private readonly IMessageStoreClient _messageStoreClient;
        private readonly ICosmosDbClient _cosmosClient;
        private readonly ILoggerProvider _loggerProvider;

        public ResolverService(ILoggerProvider loggerProvider, IMessageStoreClient messageStoreClient, ICosmosDbClient cosmosClient)
        {
            _loggerProvider = loggerProvider;
            _messageStoreClient = messageStoreClient;
            _cosmosClient = cosmosClient;
        }

        public async Task Handle(IMessageContext messageContext)
        {
            ILogger logger = _loggerProvider.GetContextualLogger(messageContext);
            logger.Verbose($"Resolver: Handle {messageContext.MessageContent.EventContent?.EventTypeId} EventId:{messageContext.EventId}, MessageId:{messageContext.MessageId}, SessionId:{messageContext.SessionId}");

            try
            {
                MessageEntity messageEntity = await CreateMessageEntity(messageContext, logger);

                await _messageStoreClient.UploadMessage(messageEntity);

                var status = await UpdateState(messageEntity);

                if (status != ResolutionStatus.TooManyRequests)
                {
                    logger.Information("Resolver: Updated Endpoint EndpointId:{EndpointId}, Status:{Status}, EventId:{EventId}, MessageId:{MessageId}, SessionId:{SessionId}",
                        messageEntity.EndpointId, status, messageEntity.EventId, messageContext.MessageId, messageEntity.SessionId);
                    await messageContext.Complete();
                }
                else
                {
                    logger.Information("Resolver: Too many CosmosDB requests will reprocess message. EndpointId:{EndpointId}, EventId:{EventId}",
                        messageEntity.EndpointId, messageEntity.EventId);
                }
            }
            catch (TransientException transientException)
            {
                logger.Error(transientException, $"Resolver: Transient exception EventId:{messageContext?.EventId}");
            }
            catch (Exception unexpectedException)
            {
                logger.Error(unexpectedException, $"Resolver: Failed to handle message, add to DeadLetter. EventId:{messageContext?.EventId}");
                await messageContext.DeadLetter("Failed to handle message.", unexpectedException);
            }
        }

        private async Task<MessageEntity> CreateMessageEntity(IReceivedMessage message, ILogger logger)
        {
            var endpointRole = EndpointRole.Subscriber;

            string endpointId;
            if (message.MessageType == MessageType.RetryRequest)
            {
                var messageAudit = new MessageAuditEntity() { AuditorName = Constants.ManagerId, AuditTimestamp = DateTime.Now, AuditType = MessageAuditType.Retry };
                await _messageStoreClient.UploadMessageAudit(message.EventId, messageAudit);
            }

            if (message.From.Equals(Constants.BrokerId, StringComparison.OrdinalIgnoreCase))
            {
                endpointId = message.To;
            }
            else if (message.MessageType == MessageType.EventRequest || message.MessageType == MessageType.ContinuationRequest ||
                message.MessageType == MessageType.RetryRequest || message.MessageType == MessageType.ResubmissionRequest ||
                message.MessageType == MessageType.SkipRequest)
            {
                endpointId = message.To;
            }
            else
            {
                endpointId = message.From;
            }

            if (message.From.Equals(Constants.BrokerId, StringComparison.OrdinalIgnoreCase) && message.MessageType == MessageType.ErrorResponse)
            {
                endpointRole = EndpointRole.Publisher;
                endpointId = message.OriginatingFrom;
            }

            return new MessageEntity
            {
                EventId = message.EventId,
                MessageId = message.MessageId,
                OriginatingMessageId = message.OriginatingMessageId,
                ParentMessageId = message.ParentMessageId,
                From = message.From,
                To = message.To,
                SessionId = message.SessionId,
                CorrelationId = message.CorrelationId,
                EnqueuedTimeUtc = message.EnqueuedTimeUtc,
                MessageContent = message.MessageContent,
                MessageType = message.MessageType,
                EndpointId = endpointId,
                EndpointRole = endpointRole,
                DeadLetterErrorDescription = message.DeadLetterErrorDescription,
                DeadLetterReason = message.DeadLetterReason,
                EventTypeId = message.EventTypeId ?? message?.MessageContent?.EventContent?.EventTypeId,
            };
        }

        private UnresolvedEvent CreateUnresolvedEvent(MessageEntity message)
        {
            return new UnresolvedEvent
            {
                UpdatedAt = DateTime.UtcNow,
                EnqueuedTimeUtc = message.EnqueuedTimeUtc,

                EventId = message.EventId,
                SessionId = message.SessionId,
                CorrelationId = message.CorrelationId,

                ResolutionStatus = GetResultingStatus(message),
                EndpointRole = message.EndpointRole,
                EndpointId = message.EndpointId,
                RetryCount = message.RetryCount,
                RetryLimit = message.RetryLimit,
                MessageType = message.MessageType,
                DeadLetterReason = message.DeadLetterReason,
                DeadLetterErrorDescription = message.DeadLetterErrorDescription,

                LastMessageId = message.MessageId,
                OriginatingMessageId = message.OriginatingMessageId,
                ParentMessageId = message.ParentMessageId,
                Reason = message.DeadLetterErrorDescription,
                OriginatingFrom = message.OriginatingFrom,

                EventTypeId = message.EventTypeId,
                To = message.To,
                From = message.From,
                MessageContent = message.MessageContent,
            };
        }

        private async Task<ResolutionStatus> UpdateState(MessageEntity message)
        {
            ResolutionStatus status = GetResultingStatus(message);
            UnresolvedEvent unresolvedEvent = CreateUnresolvedEvent(message);

            // For backward compatibility for clients not having updated to latest SDK that ensures that EventTypeId also is sent in the response message
            // Can be removed when all clients have been updated.
            if (string.IsNullOrWhiteSpace(unresolvedEvent.EventTypeId) && status == ResolutionStatus.Completed)
            {
                var pendingEvent = await _messageStoreClient.DownloadMessage(message.EventId, message.OriginatingMessageId);
                if (pendingEvent != null)
                {
                    unresolvedEvent.EventTypeId = pendingEvent.EventTypeId;
                }
            }

            try
            {
                switch (status)
                {
                    case ResolutionStatus.Completed:
                        await _cosmosClient.UploadCompletedMessage(message.EventId, message.SessionId, message.EndpointId, unresolvedEvent);
                        break;
                    case ResolutionStatus.Skipped:
                        await _cosmosClient.UploadSkippedMessage(message.EventId, message.SessionId, message.EndpointId, unresolvedEvent);
                        break;
                    case ResolutionStatus.Failed:
                        await _cosmosClient.UploadFailedMessage(message.EventId, message.SessionId, message.EndpointId, unresolvedEvent);
                        break;
                    case ResolutionStatus.Deferred:
                        await _cosmosClient.UploadDeferredMessage(message.EventId, message.SessionId, message.EndpointId, unresolvedEvent);
                        break;
                    case ResolutionStatus.Pending:
                        await _cosmosClient.UploadPendingMessage(message.EventId, message.SessionId, message.EndpointId, unresolvedEvent);
                        break;
                    case ResolutionStatus.DeadLettered:
                        await _cosmosClient.UploadDeadletteredMessage(message.EventId, message.SessionId, message.EndpointId, unresolvedEvent);
                        break;
                    case ResolutionStatus.Unsupported:
                        await _cosmosClient.UploadUnsupportedMessage(message.EventId, message.SessionId, message.EndpointId, unresolvedEvent);
                        break;

                }
            }
            catch (RequestLimitException)
            {
                status = ResolutionStatus.TooManyRequests;
            }

            return status;
        }

        private ResolutionStatus GetResultingStatus(MessageEntity message)
        {
            if (message.DeadLetterErrorDescription != null)
            {
                return ResolutionStatus.DeadLettered;
            }

            switch (message.MessageType)
            {
                case MessageType.EventRequest:
                case MessageType.ResubmissionRequest:
                case MessageType.RetryRequest:
                case MessageType.SkipRequest:
                case MessageType.ContinuationRequest:
                    return ResolutionStatus.Pending;

                case MessageType.ErrorResponse:
                    return ResolutionStatus.Failed;

                case MessageType.ResolutionResponse:
                    return ResolutionStatus.Completed;

                case MessageType.DeferralResponse:
                    return ResolutionStatus.Deferred;

                case MessageType.SkipResponse:
                    return ResolutionStatus.Skipped;

                case MessageType.UnsupportedResponse:
                    return ResolutionStatus.Unsupported;

                default:
                    throw new ArgumentException($"Unexpected {nameof(MessageType)}", nameof(message.MessageType));
            }
        }
    }
}
