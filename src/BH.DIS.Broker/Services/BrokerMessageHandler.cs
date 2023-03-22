using Azure.Messaging.ServiceBus;
using BH.DIS.Core;
using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using BH.DIS.Core.Messages.Exceptions;
using System;

using System.Threading.Tasks;

namespace BH.DIS.Broker.Services
{
    public class BrokerMessageHandler : MessageHandler
    {
        private readonly IEventContextHandler _eventContextHandler;
        private readonly IResponseService _responseService;

        public BrokerMessageHandler(IEventContextHandler eventContextHandler, IResponseService responseService, ILoggerProvider loggerProvider, IServiceProvider sp, IPlatform platform) : base(loggerProvider)
        {
            _eventContextHandler = eventContextHandler;
            _responseService = responseService;
        }

        public override async Task HandleEventRequest(IMessageContext messageContext, ILogger logger)
        {
            try
            {
                logger.Verbose($"Broker: Handle {messageContext.MessageContent.EventContent?.EventTypeId} EventId:{messageContext.EventId}, MessageId:{messageContext.MessageId}, SessionId:{messageContext.SessionId}");

                await HandleEventContent(messageContext, logger);
                await CompleteMessage(messageContext);

                logger.Information("Broker: Successfully processed {EventTypeId} EventId:{EventId}, MessageId:{MessageId}, SessionId:{SessionId}",
                    messageContext.MessageContent.EventContent?.EventTypeId, messageContext.EventId, messageContext.MessageId, messageContext.SessionId);
            }
            catch (EventContextHandlerException ex)
            {
                logger.Error(ex, "Broker: Failed processed {EventTypeId} EventId:{EventId}, MessageId:{MessageId}, SessionId:{SessionId}",
                    messageContext.MessageContent.EventContent?.EventTypeId, messageContext.EventId, messageContext.MessageId, messageContext.SessionId);
                await SendErrorResponse(messageContext, ex);
                await CompleteMessage(messageContext);
            }
        }

        private Task CompleteMessage(IMessageContext messageContext) =>
            messageContext.Complete();

        private Task SendErrorResponse(IMessageContext messageContext, Exception exception) =>
            _responseService.SendErrorResponse(messageContext, exception);

        private async Task HandleEventContent(IMessageContext messageContext, ILogger logger)
        {
            try
            {
                await _eventContextHandler.Handle(messageContext, logger);
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.ServiceBusy)
            {
                throw new TransientException("ServiceBus SDK threw exception.", e);
            }
            catch (ServiceBusException e) when (e.IsTransient)
            {
                throw new TransientException("ServiceBus SDK threw exception.", e);
            }
            catch (TransientException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.Error(exception, $"Exception EventId:{messageContext.EventId}, MessageId:{messageContext.MessageId}, SessionId:{messageContext.SessionId}");
                throw new EventContextHandlerException(exception)
                {
                    Source = exception.Source
                };
            }
        }
    }
}