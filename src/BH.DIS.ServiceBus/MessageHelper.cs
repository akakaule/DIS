using BH.DIS.Core.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BH.DIS.ServiceBus
{
    public static class MessageHelper
    {
        public static Azure.Messaging.ServiceBus.ServiceBusMessage ToServiceBusMessage(IMessage message, int messageEnqueueDelay = 0)
        {
            var result = new Azure.Messaging.ServiceBus.ServiceBusMessage();
            result.ApplicationProperties[UserPropertyName.To.ToString()] = message.To;
            result.ApplicationProperties[UserPropertyName.MessageType.ToString()] = message.MessageType.ToString();
            result.ApplicationProperties[UserPropertyName.EventId.ToString()] = message.EventId;
            result.ApplicationProperties[UserPropertyName.OriginatingMessageId.ToString()] = message.OriginatingMessageId ?? Constants.Self;
            result.ApplicationProperties[UserPropertyName.ParentMessageId.ToString()] = message.ParentMessageId ?? Constants.Self;
            result.ApplicationProperties[UserPropertyName.RetryCount.ToString()] = message.RetryCount ?? 0;
            result.ApplicationProperties[UserPropertyName.OriginatingFrom.ToString()] = message.OriginatingFrom ?? Constants.Self;
            result.ApplicationProperties[UserPropertyName.EventTypeId.ToString()] =  message.EventTypeId ?? message.MessageContent?.EventContent?.EventTypeId;

            result.ScheduledEnqueueTime = DateTime.UtcNow.AddMinutes(messageEnqueueDelay);
            var messageContentSerialized = JsonConvert.SerializeObject(message.MessageContent);
            result.Body = new BinaryData(Encoding.UTF8.GetBytes(messageContentSerialized));
            if (!string.IsNullOrWhiteSpace(message.MessageId))
                result.MessageId = message.MessageId;

            result.SessionId = message.SessionId;
            result.CorrelationId = message.CorrelationId;
            return result;
        }
    }
}
