using BH.DIS.Core.Events;
using BH.DIS.SDK.EventHandlers;
using BH.DIS.ServiceBus;
using System;

namespace BH.DIS.SDK
{
    public interface ISubscriberClient : IServiceBusAdapter 
    {
        void RegisterHandler<T_Event>(Func<IEventHandler<T_Event>> eventHandlerFactory) where T_Event : IEvent;
    }
}
