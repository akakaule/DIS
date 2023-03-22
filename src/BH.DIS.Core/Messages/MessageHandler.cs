using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages.Exceptions;
using System;
using System.Threading.Tasks;

namespace BH.DIS.Core.Messages
{
    public class MessageHandler : IMessageHandler
    {
        private readonly ILoggerProvider _loggerProvider;

        public MessageHandler(ILoggerProvider loggerProvider)
        {
            _loggerProvider = loggerProvider;
        }

        public async Task Handle(IMessageContext messageContext)
        {
            ILogger logger = _loggerProvider.GetContextualLogger(messageContext);

            try
            {
                await HandleByMessageType(messageContext, logger);
            }
            catch (TransientException transientException)
            {
                logger.Error(transientException?.InnerException, $"Transient Error. Failed to handle message. EventId:{messageContext?.EventId}, MessageId:{messageContext.MessageId}, SessionId:{messageContext.SessionId}");

                try
                {
                    await messageContext.Abandon(transientException);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to abandon message. EventId:{messageContext?.EventId}, MessageId:{messageContext.MessageId}, SessionId:{messageContext.SessionId}");
                }
            }
            catch (SessionBlockedException)
            {
            }
            catch (EventContextHandlerException)
            {
            }
            catch (Exception unexpectedException)
            {
                try
                {
                    logger.Error(unexpectedException, $"Unexpected Error. Failed to handle message. EventId:{messageContext?.EventId}, MessageId:{messageContext.MessageId}, SessionId:{messageContext.SessionId}");
                    await messageContext.DeadLetter("Failed to handle message.", unexpectedException);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to deadletter message. EventId:{messageContext?.EventId}, MessageId:{messageContext.MessageId}, SessionId:{messageContext.SessionId}");
                }
            }
        }

        private Task HandleByMessageType(IMessageContext messageContext, ILogger logger)
        {
            switch (messageContext.MessageType)
            {
                case MessageType.EventRequest:
                case MessageType.UnsupportedResponse:
                    return HandleEventRequest(messageContext, logger);

                case MessageType.ContinuationRequest:
                    return HandleContinuationRequest(messageContext, logger);

                case MessageType.SkipRequest:
                    return HandleSkipRequest(messageContext, logger);

                case MessageType.ResubmissionRequest:
                    return HandleResubmissionRequest(messageContext, logger);

                case MessageType.RetryRequest:
                    return HandleRetryRequest(messageContext, logger);

                case MessageType.ErrorResponse:
                    return HandleErrorResponse(messageContext, logger);

                case MessageType.ResolutionResponse:
                    return HandleResolutionResponse(messageContext, logger);

                case MessageType.DeferralResponse:
                    return HandleDeferralResponse(messageContext, logger);

                default:
                    return HandleDefault(messageContext, logger);
            }
        }

        public virtual Task HandleDefault(IMessageContext messageContext, ILogger logger) =>
            throw new UnsupportedMessageTypeException(messageContext.MessageType);

        public virtual Task HandleDeferralResponse(IMessageContext messageContext, ILogger logger) =>
            HandleDefault(messageContext, logger);

        public virtual Task HandleResolutionResponse(IMessageContext messageContext, ILogger logger) =>
            HandleDefault(messageContext, logger);

        public virtual Task HandleErrorResponse(IMessageContext messageContext, ILogger logger) =>
            HandleDefault(messageContext, logger);

        public virtual Task HandleResubmissionRequest(IMessageContext messageContext, ILogger logger) =>
            HandleDefault(messageContext, logger);

        public virtual Task HandleRetryRequest(IMessageContext messageContext, ILogger logger) =>
            HandleDefault(messageContext, logger);

        public virtual Task HandleSkipRequest(IMessageContext messageContext, ILogger logger) =>
            HandleDefault(messageContext, logger);

        public virtual Task HandleContinuationRequest(IMessageContext messageContext, ILogger logger) =>
            HandleDefault(messageContext, logger);

        public virtual Task HandleEventRequest(IMessageContext messageContext, ILogger logger) =>
            HandleDefault(messageContext, logger);
    }
}
