using BH.DIS.Core.Events;
using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using BH.DIS.Core.Messages.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BH.DIS.SDK.EventHandlers
{
    public class EventHandlerProvider : IEventContextHandler
    {
        private readonly IDictionary<string, Func<IEventJsonHandler>> _handlerBuilders;

        public EventHandlerProvider()
        {
            _handlerBuilders = new Dictionary<string, Func<IEventJsonHandler>>();
        }

        /// <summary>
        /// Abstract override, handles <see cref="IEventContext"/> regardless of when/how/why the event message was sent.
        /// </summary>
        public async Task Handle(IMessageContext context, ILogger logger)
        {
            // Get handler from factory.
            var handler = GetHandler(context.MessageContent.EventContent.EventTypeId);

            // Invoke handler.
            await handler.Handle(context, logger);
        }

        public void RegisterHandler<T_Event>(Func<IEventHandler<T_Event>> eventHandlerFactory)
            where T_Event : IEvent
        {
            IEventJsonHandler buildEventJsonHandler()
            {
                // Build event handler, and adapt it to handle json.
                IEventHandler<T_Event> eventHandler = eventHandlerFactory.Invoke();
                return new EventJsonHandler<T_Event>(eventHandler);
            }

            var eventTypeId = new EventType<T_Event>().Id;
            _handlerBuilders[eventTypeId] = buildEventJsonHandler;
        }

        private IEventJsonHandler GetHandler(string eventTypeId)
        {
            if (!_handlerBuilders.ContainsKey(eventTypeId))
                throw new EventHandlerNotFoundException($"Event handler not registered for Event type {eventTypeId}");

            // Build and return event json handler.
            return _handlerBuilders[eventTypeId].Invoke();
        }
    }
}
