using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Events;
using BH.DIS.Core.Logging;
using BH.DIS.EventPublisher.Models;
using BH.DIS.SDK;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BH.DIS.EventPublisher.Services
{
    public class EventPublisherService : IEventPublisherService
    {
        ILoggerProvider loggerProvider;
        readonly string serviceBusConnection;

        public EventPublisherService(ILoggerProvider loggerProvider, IConfiguration configuration)
        {
            this.loggerProvider = loggerProvider;
            serviceBusConnection = configuration.GetValue<string>("ServiceBusConnection");
        }

        public async Task Handle(EventPublisherModel eventModel)
        {
            var eventType = new PlatformConfiguration().Endpoints
                .FirstOrDefault(x => x.Id.Equals(eventModel.TopicName, StringComparison.OrdinalIgnoreCase))
                ?.EventTypesProduced
                ?.Where(x => x.Id.Equals(eventModel.EventType, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            var type = eventType?.GetEventClassType();
            var @event = (IEvent)JsonConvert.DeserializeObject(Convert.ToString(eventModel.EventMessage), type);

            PublisherClient publisherClient = CreatePublisherClient(eventModel);

            var correlationId = eventModel.CorrelationId ?? Guid.NewGuid().ToString();
            var sessionId = eventModel.SessionId ?? @event.GetSessionId();

            await publisherClient.Publish(@event, sessionId, correlationId);
        }

        private PublisherClient CreatePublisherClient(EventPublisherModel eventModel)
        {
            ServiceBusClient serviceBusClient = new ServiceBusClient(serviceBusConnection);
            return new PublisherClient(serviceBusClient, eventModel.TopicName, loggerProvider);
        }
    }
}
