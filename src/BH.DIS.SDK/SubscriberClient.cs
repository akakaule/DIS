using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Events;
using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using BH.DIS.SDK.EventHandlers;
using BH.DIS.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;
using System;
using System.Threading.Tasks;

namespace BH.DIS.SDK
{
    public class SubscriberClient : ISubscriberClient
    {
        private readonly IServiceBusAdapter _serviceBusAdapter;
        private readonly EventHandlerProvider _eventHandlerProvider;

        public SubscriberClient(ServiceBusClient client, string endpoint, ILoggerProvider loggerProvider)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrEmpty(endpoint)) throw new ArgumentException(nameof(endpoint));

            var serviceBusSender = client.CreateSender(endpoint);

            ISender sender = new Sender(serviceBusSender);
            IResponseService responseService = new ResponseService(sender);

            _eventHandlerProvider = new EventHandlerProvider();

            IMessageHandler strictMessageHandler = new StrictMessageHandler(_eventHandlerProvider, responseService, loggerProvider);

            _serviceBusAdapter = new ServiceBusAdapter(strictMessageHandler);
        }

        public Task Handle(ServiceBusReceivedMessage message, ServiceBusSessionMessageActions sessionActions, ServiceBusReceiveActions receiveActions) =>
            _serviceBusAdapter.Handle(message, sessionActions, receiveActions);

        public Task Handle(ServiceBusReceivedMessage message, ServiceBusSessionReceiver sessionReceiver) =>
            _serviceBusAdapter.Handle(message, sessionReceiver);

        public void RegisterHandler<T_Event>(Func<IEventHandler<T_Event>> eventHandlerFactory)
            where T_Event : IEvent
        {
            _eventHandlerProvider.RegisterHandler(eventHandlerFactory);
        }
    }
}
