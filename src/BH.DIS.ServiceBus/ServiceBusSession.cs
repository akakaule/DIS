using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Messages;
using Microsoft.Azure.WebJobs.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace BH.DIS.ServiceBus
{
    public interface IServiceBusSession
    {
        Task CompleteAsync(IServiceBusMessage message);
        Task DeadLetterAsync(IServiceBusMessage message, string reason, string v);
        Task DeferAsync(IServiceBusMessage message);
        Task<IServiceBusMessage> ReceiveDeferredMessageAsync(long nextSequenceNumber);
        Task SetStateAsync(SessionState sessionState);
        Task<SessionState> GetStateAsync();
    }

    public class ServiceBusSession : IServiceBusSession
    {
        private readonly ServiceBusSessionMessageActions _sessionActions;
        private readonly ServiceBusReceiveActions _receiveActions;
        private readonly ServiceBusSessionReceiver _sessionReceiver;

        public ServiceBusSession(ServiceBusSessionMessageActions sessionActions, ServiceBusReceiveActions receiveActions)
        {
            _sessionActions = sessionActions ?? throw new ArgumentNullException(nameof(sessionActions));
            _receiveActions = receiveActions ?? throw new ArgumentNullException(nameof(receiveActions));
        }

        public ServiceBusSession(ServiceBusSessionReceiver sessionReceiver)
        {
            _sessionReceiver = sessionReceiver ?? throw new ArgumentNullException(nameof(sessionReceiver));
        }

        public Task CompleteAsync(IServiceBusMessage message)
        {
            if (_sessionActions != null)
            {
                return _sessionActions.CompleteMessageAsync(message.Message);
            }

            return _sessionReceiver.CompleteMessageAsync(message.Message);
        }

        public Task DeadLetterAsync(IServiceBusMessage message, string deadLetterReason, string deadLetterErrorDescription)
        {
            if (_sessionActions != null)
            {
                return _sessionActions.DeadLetterMessageAsync(message.Message, deadLetterReason, deadLetterErrorDescription);
            }

            return _sessionReceiver.DeadLetterMessageAsync(message.Message, deadLetterReason, deadLetterErrorDescription);
        }

        public Task DeferAsync(IServiceBusMessage message)
        {
            if (_sessionActions != null)
            {
                return _sessionActions.DeferMessageAsync(message.Message);
            }

            return _sessionReceiver.DeferMessageAsync(message.Message);
        }

        public async Task<SessionState> GetStateAsync()
        {
            System.BinaryData sessionData;
            if (_sessionActions != null)
            {
                sessionData = await _sessionActions.GetSessionStateAsync();
            }
            else
            {
                sessionData = await _sessionReceiver.GetSessionStateAsync();
            }

            if (sessionData == null)
                return new SessionState();

            return sessionData.ToObjectFromJson<SessionState>();
        }

        public async Task SetStateAsync(SessionState sessionState)
        {
            System.BinaryData sessionData = null;
            
            if (!sessionState.IsEmpty())
            {
                string json = JsonConvert.SerializeObject(sessionState);
                sessionData = new BinaryData(Encoding.UTF8.GetBytes(json));
            }

            if (_sessionActions != null)
            {
                await _sessionActions.SetSessionStateAsync(sessionData);
            }
            else
            {
                await _sessionReceiver.SetSessionStateAsync(sessionData);
            }
        }

        public async Task<IServiceBusMessage> ReceiveDeferredMessageAsync(long nextSequenceNumber)
        {
            try
            {
                if (_receiveActions != null)
                {
                    var deferredMessages = await _receiveActions.ReceiveDeferredMessagesAsync(new long[] { nextSequenceNumber });
                    var deferredMessage = deferredMessages.Count > 0 ? deferredMessages[0] : null;
                    if (deferredMessage == null)
                    {
                        return null;
                    }

                    return new ServiceBusMessage(deferredMessage);
                }
                else
                {
                    var deferredMessages = await _sessionReceiver.ReceiveDeferredMessagesAsync(new long[] { nextSequenceNumber });
                    var deferredMessage = deferredMessages.Count > 0 ? deferredMessages[0] : null;

                    if (deferredMessage == null)
                        return null;

                    return new ServiceBusMessage(deferredMessage);
                }
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound)
            {
                throw new ServiceBusException(isTransient: true, "Failed to retrieve deferred message. It might be locked by another process.");
            }
        }
    }
}
