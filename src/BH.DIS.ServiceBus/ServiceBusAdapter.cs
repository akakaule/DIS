using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Messages;
using Microsoft.Azure.WebJobs.ServiceBus;
using System;
using System.Threading.Tasks;

namespace BH.DIS.ServiceBus
{
    public interface IServiceBusAdapter
    {
        Task Handle(ServiceBusReceivedMessage message, ServiceBusSessionMessageActions sessionActions, ServiceBusReceiveActions receiveActions);

        Task Handle(ServiceBusReceivedMessage message, ServiceBusSessionReceiver sessionReceiver);
    }

    public class ServiceBusAdapter : IServiceBusAdapter
    {
        private readonly IMessageHandler _messageHandler;

        public ServiceBusAdapter(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        public async Task Handle(ServiceBusReceivedMessage message, ServiceBusSessionMessageActions sessionActions, ServiceBusReceiveActions receiveActions)
        {
            var messageWrapper = new ServiceBusMessage(message);
            var sessionWrapper = new ServiceBusSession(sessionActions, receiveActions);
            var messageContext = new MessageContext(messageWrapper, sessionWrapper);

            await _messageHandler.Handle(messageContext);
        }

        public async Task Handle(ServiceBusReceivedMessage message, ServiceBusSessionReceiver sessionReceiver)
        {
            var messageWrapper = new ServiceBusMessage(message);
            var sessionWrapper = new ServiceBusSession(sessionReceiver);
            var messageContext = new MessageContext(messageWrapper, sessionWrapper);

            await _messageHandler.Handle(messageContext);
        }
    }
}
