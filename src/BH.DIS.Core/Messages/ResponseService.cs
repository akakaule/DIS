using System;
using System.Threading.Tasks;
using BH.DIS.Core.Events;
using Newtonsoft.Json;

namespace BH.DIS.Core.Messages
{

    public class ResponseService : IResponseService
    {
        private readonly ISender _sender;

        public ResponseService(ISender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        public async Task SendResolutionResponse(IMessageContext messageContext)
        {
            IMessage response = CreateResponse(messageContext, MessageType.ResolutionResponse, responseContent: new MessageContent());
            await _sender.Send(response);
        }

        public async Task SendSkipResponse(IMessageContext messageContext)
        {
            IMessage response = CreateResponse(messageContext, MessageType.SkipResponse, responseContent: new MessageContent());
            await _sender.Send(response);
        }

        public async Task SendErrorResponse(IMessageContext messageContext, Exception exception)
        {
            IMessage response = CreateResponse(messageContext, MessageType.ErrorResponse, CreateErrorContent(exception, messageContext));
            await _sender.Send(response);
        }

        public async Task SendDeferralResponse(IMessageContext messageContext, SessionBlockedException exception)
        {
            IMessage response = CreateResponse(messageContext, MessageType.DeferralResponse, CreateErrorContent(exception, messageContext));
            await _sender.Send(response);
        }

        public async Task SendRetryResponse(IMessageContext messageContext, int messageDelayMinutes)
        {
            IMessage response = CreateRetryResponse(messageContext, MessageType.RetryRequest, responseContent: messageContext.MessageContent);
            await _sender.Send(response, messageDelayMinutes);
        }

        public async Task SendContinuationRequestToSelf(IMessageContext deferredMessageContext)
        {
            await _sender.Send(new Message()
            {
                To = Constants.ContinuationId,
                CorrelationId = deferredMessageContext.MessageId,
                SessionId = deferredMessageContext.SessionId,
                EventId = deferredMessageContext.EventId,
                OriginatingMessageId = !deferredMessageContext.OriginatingMessageId.Equals(Constants.Self, StringComparison.OrdinalIgnoreCase) ? deferredMessageContext.OriginatingMessageId : deferredMessageContext.MessageId,
                ParentMessageId = deferredMessageContext.MessageId,
                EventTypeId = deferredMessageContext.EventTypeId,
                MessageType = MessageType.ContinuationRequest,
                MessageContent = new MessageContent(),
            });
        }

        public async Task SendHeartbeatResponseToSelf(IMessageContext messageContext)
        {
            var content = JsonConvert.DeserializeObject<Heartbeat>(messageContext.MessageContent.EventContent.EventJson);
            content.ForwardReceivedTime = DateTime.Now;
            content.BackwardSendTime = DateTime.Now;
            content.Endpoint = messageContext.To;

            var newMessageContent = new MessageContent()
            {
                ErrorContent = messageContext.MessageContent.ErrorContent,
                EventContent = new EventContent
                {
                    EventTypeId = messageContext.EventTypeId,
                    EventJson = JsonConvert.SerializeObject(content)
                }
            };

            await _sender.Send(new Message()
            {
                To = Constants.HeartbeatId,
                CorrelationId = messageContext.MessageId,
                SessionId = messageContext.SessionId,
                EventId = messageContext.EventId,
                OriginatingMessageId = !messageContext.OriginatingMessageId.Equals(Constants.Self, StringComparison.OrdinalIgnoreCase) ? messageContext.OriginatingMessageId : messageContext.MessageId,
                ParentMessageId = messageContext.MessageId,
                RetryCount = messageContext.RetryCount ?? null,
                OriginatingFrom = messageContext.From,
                EventTypeId = messageContext.EventTypeId,
                MessageType = MessageType.HeartbeatResponse,
                MessageContent = newMessageContent,
            });
        }

        public async Task SendUnsupportedResponse(IMessageContext messageContext)
        {
            IMessage response = CreateResponse(messageContext, MessageType.UnsupportedResponse, responseContent: messageContext.MessageContent);
            await _sender.Send(response);
        }

        private IMessage CreateResponse(IMessageContext messageContext, MessageType responseType, MessageContent responseContent) =>
            new Message()
            {
                To = Constants.ResolverId,
                CorrelationId = messageContext.MessageId,
                SessionId = messageContext.SessionId,
                EventId = messageContext.EventId,
                OriginatingMessageId = !messageContext.OriginatingMessageId.Equals(Constants.Self, StringComparison.OrdinalIgnoreCase) ? messageContext.OriginatingMessageId : messageContext.MessageId,
                ParentMessageId = messageContext.MessageId,
                RetryCount = messageContext.RetryCount ?? null,
                OriginatingFrom = messageContext.From,
                EventTypeId = messageContext.EventTypeId,
                MessageType = responseType,
                MessageContent = responseContent,
            };


        private IMessage CreateRetryResponse(IMessageContext messageContext, MessageType responseType, MessageContent responseContent) =>
            new Message()
            {
                To = Constants.RetryId,
                CorrelationId = messageContext.MessageId,
                SessionId = messageContext.SessionId,
                EventId = messageContext.EventId,
                OriginatingMessageId = !messageContext.OriginatingMessageId.Equals(Constants.Self, StringComparison.OrdinalIgnoreCase) ? messageContext.OriginatingMessageId : messageContext.MessageId,
                ParentMessageId = messageContext.MessageId,
                RetryCount = messageContext.RetryCount.HasValue ? messageContext.RetryCount + 1 : 1,
                OriginatingFrom = messageContext.From,
                EventTypeId = messageContext.EventTypeId,
                MessageType = MessageType.RetryRequest,
                MessageContent = responseContent,
            };


        private static MessageContent CreateErrorContent(Exception exception, IMessageContext messageContext) =>
            new MessageContent()
            {
                ErrorContent = new ErrorContent()
                {
                    ErrorText = exception.Message,
                    ErrorType = exception.GetType().FullName,
                    ExceptionStackTrace = $"{exception?.InnerException?.ToString()} {exception}",
                    ExceptionSource = exception.Source,
                },
                EventContent = messageContext.MessageContent.EventContent
            };
    }
}
