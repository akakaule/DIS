using BH.DIS.Core.Events;
using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BH.DIS.SDK.EventHandlers
{

    public class EventJsonHandler<T_Event> : IEventJsonHandler
        where T_Event : IEvent
    {
        public EventJsonHandler(IEventHandler<T_Event> eventHandler)
        {
            _eventHandler = eventHandler;
        }

        private readonly IEventHandler<T_Event> _eventHandler;

        public Task Handle(IMessageContext context, ILogger logger)
        {
            var @event = JsonConvert.DeserializeObject<T_Event>(context.MessageContent.EventContent.EventJson);
            var eventHandlercontext = new EventHandlerContext { CorrelationId = context.CorrelationId, EventId = context.EventId, EventType = context.MessageContent.EventContent.EventTypeId };
            return _eventHandler.Handle(@event, logger, eventHandlercontext);
        }
    }
}
