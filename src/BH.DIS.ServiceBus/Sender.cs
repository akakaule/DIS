using BH.DIS.Core.Messages;
using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BH.DIS.ServiceBus
{

    public abstract class SenderBase : ISender
    {
        private readonly ServiceBusSender _serviceBusSender;

        public SenderBase(ServiceBusSender serviceBusSender)
        {
            _serviceBusSender = serviceBusSender ?? throw new ArgumentNullException(nameof(serviceBusSender)); ;
        }

        public string TopicName => _serviceBusSender.EntityPath;

        public Task Send(IMessage message, int messageEnqueueDelay = 0) =>
             _serviceBusSender.SendMessageAsync(MessageHelper.ToServiceBusMessage(message, messageEnqueueDelay));

        public Task Send(IEnumerable<IMessage> messages, int messageEnqueueDelay = 0) =>
            _serviceBusSender.SendMessagesAsync(messages.Select(message => MessageHelper.ToServiceBusMessage(message, messageEnqueueDelay)).ToList());
    }

    public class Sender : SenderBase
    {
        public Sender(ServiceBusSender serviceBusSender) : base(serviceBusSender) { }
    }

    public class SenderManager : SenderBase
    {
        public SenderManager(ServiceBusSender serviceBusSender) : base(serviceBusSender) { }
    }
}
