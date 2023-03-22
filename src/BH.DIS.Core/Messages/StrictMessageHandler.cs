using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages.Exceptions;
using System;
using System.Threading.Tasks;

namespace BH.DIS.Core.Messages
{
    public class StrictMessageHandler : MessageHandler
    {
        private readonly IEventContextHandler _eventContextHandler;
        private readonly IResponseService _responseService;

        public StrictMessageHandler(IEventContextHandler eventContextHandler, IResponseService responseService, ILoggerProvider loggerProvider) : base(loggerProvider)
        {
            _eventContextHandler = eventContextHandler;
            _responseService = responseService;
        }

        public override async Task HandleEventRequest(IMessageContext messageContext, ILogger logger)
        {
            try
            {
                LogInfoWithMessageMetaData(logger, messageContext, "Handle");

                if (messageContext.EventTypeId == "Heartbeat" && messageContext.MessageType == MessageType.EventRequest)
                {
                    await _responseService.SendHeartbeatResponseToSelf(messageContext);
                    await SendResolutionResponse(messageContext);
                }
                else
                {
                    await VerifySessionIsNotBlocked(messageContext);
                    await HandleEventContent(messageContext, logger);
                    await SendResolutionResponse(messageContext);
                }
                await CompleteMessage(messageContext);
                LogInfoWithMessageMetaData(logger, messageContext, "Successfully processed");
            }
            catch (EventHandlerNotFoundException exception)
            {
                LogErrorWithMessageMetaData(logger, messageContext, "Failed to handle event", exception);
                await SendUnsupportedResponse(messageContext);
                await CompleteMessage(messageContext);
            }
            catch (SessionBlockedException exception)
            {
                LogErrorWithMessageMetaData(logger, messageContext, "Failed to handle event", exception);
                // Session is blocked (by failed event request, or by deferred event requests).
                await SendDeferralResponse(messageContext, exception);
                await DeferMessage(messageContext);
                throw;
            }
            catch (EventContextHandlerException exception)
            {
                LogErrorWithMessageMetaData(logger, messageContext, "Failed to handle event", exception);
                // Event handler threw (non-transient) exception.
                await SendErrorResponse(messageContext, exception);
                await BlockSession(messageContext);
                await CompleteMessage(messageContext);
                await CheckForRetry(messageContext, exception);
                throw;
            }
        }


        public override async Task HandleRetryRequest(IMessageContext messageContext, ILogger logger)
        {
            try
            {
                LogInfoWithMessageMetaData(logger, messageContext, "Handle (RetryRequest)");
                await VerifySessionIsBlockedByThis(messageContext);
                await HandleEventContent(messageContext, logger);
                await UnblockSession(messageContext);
                await ContinueWithAnyDeferredMessages(messageContext, logger);
                await SendResolutionResponse(messageContext);
                await CompleteMessage(messageContext);
                LogInfoWithMessageMetaData(logger, messageContext, "Successfully processed (RetryRequest)");
            }
            catch (SessionBlockedException exception)
            {
                LogErrorWithMessageMetaData(logger, messageContext, "Failed to handle event (RetryRequest)", exception);
                // Session is not blocked by this, so the resubmitted event must have already been resolved.
                await SendResolutionResponse(messageContext);
                await CompleteMessage(messageContext);
            }
            catch (EventContextHandlerException exception)
            {
                LogErrorWithMessageMetaData(logger, messageContext, "Failed to handle event (RetryRequest)", exception);
                // Event handler threw (non-transient) exception.
                await SendErrorResponse(messageContext, exception);
                await CompleteMessage(messageContext);
                await CheckForRetry(messageContext, exception);
            }
        }

        public override async Task HandleResubmissionRequest(IMessageContext messageContext, ILogger logger)
        {
            try
            {
                LogInfoWithMessageMetaData(logger, messageContext, "Handle (Resubmission)");
                AuthorizeManagerRequest(messageContext);
                await HandleEventContent(messageContext, logger);
                if (await messageContext.IsSessionBlockedByThis())
                    await UnblockSession(messageContext);
                await ContinueWithAnyDeferredMessages(messageContext, logger);
                await SendResolutionResponse(messageContext);
                await CompleteMessage(messageContext);
                LogInfoWithMessageMetaData(logger, messageContext, "Successfully processed (Resubmission)");
            }
            catch (EventHandlerNotFoundException exception)
            {
                LogErrorWithMessageMetaData(logger, messageContext, "Failed to handle event (Resubmission)", exception);
                await SendUnsupportedResponse(messageContext);
                await CompleteMessage(messageContext);
            }
            catch (EventContextHandlerException exception)
            {
                LogErrorWithMessageMetaData(logger, messageContext, "Failed to handle event (Resubmission)", exception);
                // Event handler threw (non-transient) exception.
                await SendErrorResponse(messageContext, exception);
                await CompleteMessage(messageContext);
            }
        }

        public override async Task HandleSkipRequest(IMessageContext messageContext, ILogger logger)
        {
            try
            {
                LogInfoWithMessageMetaData(logger, messageContext, "Handle (Skip)");
                AuthorizeManagerRequest(messageContext);
                await VerifySessionIsBlockedByThis(messageContext);
                await UnblockSession(messageContext);
                await ContinueWithAnyDeferredMessages(messageContext, logger);
                await SendSkipResponse(messageContext);
                await CompleteMessage(messageContext);
            }
            catch (SessionBlockedException)
            {
                // Session is not blocked by this, so the resubmitted event must have already been resolved.
                await SendSkipResponse(messageContext);
                await CompleteMessage(messageContext);
            }

            LogInfoWithMessageMetaData(logger, messageContext, "Successfully processed (Skip)");
        }

        public override async Task HandleContinuationRequest(IMessageContext messageContext, ILogger logger)
        {
            try
            {
                LogInfoWithMessageMetaData(logger, messageContext, "Handle (Continuation)");

                AuthorizeContinuationRequest(messageContext);
                IMessageContext deferredMessageContext = await ReceiveNextDeferredAndVerifyEventId(messageContext, true);
                await HandleEventRequest(deferredMessageContext, logger);
                await ContinueWithAnyDeferredMessages(messageContext, logger);
                await CompleteMessage(messageContext);
            }
            catch (EventContextHandlerException)
            {
                await CompleteMessage(messageContext);
            }
            catch (NextDeferredException)
            {
                // Either: 1) There is no next deferred event request,
                // or 2) EventId of continuation request does not match EventId of next deferred event request.
                // Either way, the requested continuation must have already been resolved.
                await CompleteMessage(messageContext);
            }

            LogInfoWithMessageMetaData(logger, messageContext, "Successfully processed (Continuation)");
        }

        private Task CompleteMessage(IMessageContext messageContext) =>
            messageContext.Complete();

        private Task DeferMessage(IMessageContext messageContext) =>
            messageContext.Defer();

        private Task SendResolutionResponse(IMessageContext messageContext) =>
            _responseService.SendResolutionResponse(messageContext);

        private Task SendSkipResponse(IMessageContext messageContext) =>
            _responseService.SendSkipResponse(messageContext);

        private Task SendErrorResponse(IMessageContext messageContext, EventContextHandlerException exception) =>
            _responseService.SendErrorResponse(messageContext, exception);

        private Task SendDeferralResponse(IMessageContext messageContext, SessionBlockedException exception) =>
            _responseService.SendDeferralResponse(messageContext, exception);

        private Task BlockSession(IMessageContext messageContext) =>
            messageContext.BlockSession();

        private Task UnblockSession(IMessageContext messageContext) =>
            messageContext.UnblockSession();

        private Task SendRetryResponse(IMessageContext messageContext, int messageDelayMinutes) =>
            _responseService.SendRetryResponse(messageContext, messageDelayMinutes);

        private Task SendUnsupportedResponse(IMessageContext messageContext) =>
            _responseService.SendUnsupportedResponse(messageContext);

        private async Task<IMessageContext> ReceiveNextDeferredAndVerifyEventId(IMessageContext messageContext, bool removeFromQueue = false)
        {
            IMessageContext nextDeferred;
            if (removeFromQueue)
            {
                nextDeferred = await messageContext.ReceiveNextDeferredWithPop();
            }
            else
            {
                nextDeferred = await messageContext.ReceiveNextDeferred();
            }

            if (!messageContext.EventId.Equals(nextDeferred?.EventId, StringComparison.OrdinalIgnoreCase))
                throw new NextDeferredException($"Unable to continue with {messageContext.EventId}, because it is not the next deferred event request in this session.");
            return nextDeferred;
        }

        private void AuthorizeManagerRequest(IMessageContext messageContext)
        {
            if (!messageContext.From.Equals(Constants.ManagerId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"Only {Constants.ManagerId} is authorized to send {messageContext.MessageType} messages.");
        }

        private void AuthorizeContinuationRequest(IMessageContext messageContext)
        {
            if (!messageContext.From.Equals(Constants.ContinuationId, StringComparison.OrdinalIgnoreCase) &&
                !messageContext.From.Equals(Constants.ManagerId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"{messageContext.From} is not authorized to send {MessageType.ContinuationRequest} messages to this messaging entity.");
        }

        private async Task VerifySessionIsBlockedByThis(IMessageContext messageContext)
        {
            if (!await messageContext.IsSessionBlockedByThis())
            {
                var blockedBy = await messageContext.GetBlockedByEventId();
                throw new SessionBlockedException($"Session {messageContext.SessionId} is blocked by {blockedBy}");
            }
        }

        private async Task VerifySessionIsNotBlocked(IMessageContext messageContext)
        {
            var blockedBy = await messageContext.GetBlockedByEventId();
            if (!string.IsNullOrEmpty(blockedBy))
                throw new SessionBlockedException($"Session {messageContext.SessionId} is blocked by {blockedBy}");
        }

        private async Task HandleEventContent(IMessageContext context, ILogger logger)
        {
            try
            {
                await _eventContextHandler.Handle(context, logger);
            }
            catch (TransientException)
            {
                throw;
            }
            catch (EventHandlerNotFoundException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new EventContextHandlerException(exception)
                {
                    Source = exception.Source
                };
            }
        }

        private async Task ContinueWithAnyDeferredMessages(IMessageContext messageContext, ILogger logger)
        {
            var next = await messageContext.ReceiveNextDeferred();
            if (next != null)
            {
                await _responseService.SendContinuationRequestToSelf(next);
                LogInfoWithMessageMetaData(logger, messageContext, "Send ContinuationRequest");
            }
        }

        private async Task CheckForRetry(IMessageContext messageContext, EventContextHandlerException exception)
        {
            var retryDefinition = RetryDefinitions.GetRetryDefinition(messageContext.MessageContent.EventContent.EventTypeId,
                $"{exception?.InnerException} {exception}", messageContext.To);
            if (retryDefinition != null && messageContext.RetryCount != null && messageContext.RetryCount < retryDefinition.RetryCount)
            {
                await SendRetryResponse(messageContext, retryDefinition.RetryDelay);
            }
        }

        private void LogInfoWithMessageMetaData(ILogger logger, IMessageContext messageContext, string prefixMessage)
        {
            var logMetaData = "EventTypeId: {EventTypeId}, EventId:{EventId}, MessageId:{MessageId}, SessionId:{SessionId}";

            logger.Information($"{prefixMessage} {logMetaData}",
                messageContext.MessageContent?.EventContent?.EventTypeId, messageContext.EventId, messageContext.MessageId, messageContext.SessionId);
        }

        private void LogErrorWithMessageMetaData(ILogger logger, IMessageContext messageContext, string prefixMessage, Exception exception)
        {
            var logMetaData = "EventTypeId: {EventTypeId}, EventId:{EventId}, MessageId:{MessageId}, SessionId:{SessionId}";

            logger.Error(exception, $"{prefixMessage} {logMetaData}",
                messageContext.MessageContent?.EventContent?.EventTypeId, messageContext.EventId, messageContext.MessageId, messageContext.SessionId);
        }
    }
}