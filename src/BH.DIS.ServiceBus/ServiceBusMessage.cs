﻿using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Messages;
using System;

namespace BH.DIS.ServiceBus
{
    public interface IServiceBusMessage
    {
        string GetUserProperty(UserPropertyName name);
        byte[] Body { get; }
        string LockToken { get; }
        string SessionId { get; }
        string MessageId { get; }
        string CorrelationId { get; }
        int DeliveryCount { get; }
        long SequenceNumber { get; }
        DateTime EnqueuedTimeUtc { get; }
        internal ServiceBusReceivedMessage Message { get; }
    }

    public class ServiceBusMessage : IServiceBusMessage
    {
        private readonly ServiceBusReceivedMessage _message;

        public ServiceBusMessage(ServiceBusReceivedMessage message)
        {
            _message = message;
        }


        public string LockToken => _message.LockToken;

        public string SessionId => _message.SessionId;

        public byte[] Body => _message.Body.ToArray();

        public string MessageId => _message.MessageId;

        public string CorrelationId => _message.CorrelationId;

        public int DeliveryCount => _message.DeliveryCount;

        public long SequenceNumber => _message.SequenceNumber;

        public DateTime EnqueuedTimeUtc => _message.EnqueuedTime.UtcDateTime;

        ServiceBusReceivedMessage IServiceBusMessage.Message => _message;

        public string GetUserProperty(UserPropertyName name)
        {
            string key = name.ToString();
            if (!_message.ApplicationProperties.ContainsKey(key))
                return null;

            return _message.ApplicationProperties[key]?.ToString();
        }
    }
}
