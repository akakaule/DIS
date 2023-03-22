using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Messages;
using BH.DIS.Core.Messages.Exceptions;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.DIS.ServiceBus
{
    public class MessageContext : IMessageContext
    {
        private readonly IServiceBusMessage _sbMessage;
        private readonly IServiceBusSession _sbSession;
        private readonly bool _isDeferred;

        public MessageContext(IServiceBusMessage sbMessage, IServiceBusSession sbSession, bool isDeferred = false)
        {
            _sbMessage = sbMessage ?? throw new ArgumentNullException(nameof(sbMessage));
            _sbSession = sbSession ?? throw new ArgumentNullException(nameof(sbSession));

            _isDeferred = isDeferred;
        }

        public string From => GetUserProperty(UserPropertyName.From);

        public string OriginatingFrom
        {
            get { try { return GetUserProperty(UserPropertyName.OriginatingFrom); } catch { return null; } }
        }

        public string To => GetUserProperty(UserPropertyName.To);

        public string EventId => GetUserProperty(UserPropertyName.EventId);

        public string EventTypeId
        {
            get { try { return GetUserProperty(UserPropertyName.EventTypeId); } catch { return null; } }
        }

        public string DeadLetterReason
        {
            get { try { return GetUserProperty(UserPropertyName.DeadLetterReason); } catch { return null; } }
        }
        public string DeadLetterErrorDescription
        {
            get { try { return GetUserProperty(UserPropertyName.DeadLetterErrorDescription); } catch { return null; } }
        }

        public string OriginatingMessageId => GetUserProperty(UserPropertyName.OriginatingMessageId) ?? Constants.Self;

        public string ParentMessageId => GetUserProperty(UserPropertyName.ParentMessageId) ?? Constants.Self;

        public MessageType MessageType => GetMessageType();

        public string MessageId => _sbMessage.MessageId ?? throw new InvalidMessageException($"MessageId is not defined.");

        public string SessionId => _sbMessage.SessionId ?? throw new InvalidMessageException($"SessionId is not defined.");

        public string CorrelationId => _sbMessage.CorrelationId;

        public MessageContent MessageContent => GetContent() ?? throw new InvalidMessageException($"MessageContent is null.");

        public bool IsDeferred => _isDeferred;

        public DateTime EnqueuedTimeUtc => _sbMessage.EnqueuedTimeUtc;

        public int? RetryCount
        {
            get { try { return Int32.Parse(GetUserProperty(UserPropertyName.RetryCount)); } catch { return null; } }
        }


        /// <summary>
        /// We actually don't do anything when Abandon is called.
        /// The intention of abandoning a message is to make a retry attempt, and if we actually call IMessageSession.AbandonAsync, then the lock will be released and the message will be picked up again immediately.
        /// By doing nothing, the lock will expire before the message is picked up again.
        /// </summary>
        public Task Abandon(TransientException exception) => Task.CompletedTask;

        public async Task BlockSession()
        {
            SessionState state = await GetSessionState();
            state.BlockedByEventId = EventId;
            await UpdateSessionState(state);
        }

        public async Task UnblockSession()
        {
            SessionState state = await GetSessionState();
            state.BlockedByEventId = null;
            await UpdateSessionState(state);
        }

        public async Task Complete()
        {
            try
            {
                await _sbSession.CompleteAsync(_sbMessage);
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.SessionLockLost)
            {
                throw new TransientException("SessionLockLost exception.", e);
            }
            catch (ServiceBusException e) when (e.IsTransient)
            {
                throw new TransientException("ServiceBus SDK threw transient exception", e);
            }
        }

        public async Task DeadLetter(string reason, Exception exception = null)
        {
            try
            {
                await _sbSession.DeadLetterAsync(_sbMessage, reason, exception?.ToString());
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.SessionLockLost)
            {
                throw new TransientException("SessionLockLost exception.", e);
            }
            catch (ServiceBusException e) when (e.IsTransient)
            {
                throw new TransientException("ServiceBus SDK threw transient exception", e);
            }
        }

        public async Task Defer()
        {
            if (IsDeferred)
                throw new NotSupportedException("Is already deferred.");

            SessionState state = await GetSessionState();
            state.DeferredSequenceNumbers.Add(_sbMessage.SequenceNumber);
            await UpdateSessionState(state);

            try
            {
                await _sbSession.DeferAsync(_sbMessage);
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.SessionLockLost)
            {
                throw new TransientException("SessionLockLost exception.", e);
            }
            catch (ServiceBusException e) when (e.IsTransient)
            {
                throw new TransientException("ServiceBus SDK threw transient exception", e);
            }
        }

        public async Task DeferOnly()
        {
            if (IsDeferred)
                throw new NotSupportedException("Is already deferred.");

            try
            {
                await _sbSession.DeferAsync(_sbMessage);
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.SessionLockLost)
            {
                throw new TransientException("SessionLockLost exception.", e);
            }
            catch (ServiceBusException e) when (e.IsTransient)
            {
                throw new TransientException("ServiceBus SDK threw transient exception", e);
            }
        }

        public async Task<bool> IsSessionBlocked()
        {
            SessionState state = await GetSessionState();
            return !string.IsNullOrEmpty(state.BlockedByEventId)
                || state.DeferredSequenceNumbers.Any();
        }

        public async Task<bool> IsSessionBlockedByEventId()
        {
            SessionState state = await GetSessionState();
            return !string.IsNullOrEmpty(state.BlockedByEventId);
        }

        public async Task<bool> IsSessionBlockedByThis()
        {
            SessionState state = await GetSessionState();
            return !string.IsNullOrEmpty(state.BlockedByEventId) && state.BlockedByEventId.Equals(EventId, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> GetBlockedByEventId()
        {
            SessionState state = await GetSessionState();
            return state.BlockedByEventId;
        }

        public async Task<IMessageContext> ReceiveNextDeferred()
        {
            SessionState state = await GetSessionState();
            while (state.DeferredSequenceNumbers.Any())
            {
                long nextSequenceNumber = state.DeferredSequenceNumbers.First();

                try
                {
                    IServiceBusMessage deferred = await _sbSession.ReceiveDeferredMessageAsync(nextSequenceNumber);
                    if (deferred == null)
                    {
                        // Deferred message does not exist. 
                        // Update session state by removing the "null reference".
                        state.DeferredSequenceNumbers.RemoveRange(index: 0, count: 1);
                        await UpdateSessionState(state);

                        continue;
                    }
                    return new MessageContext(deferred, _sbSession, isDeferred: true);
                }
                catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.SessionLockLost)
                {
                    throw new TransientException("SessionLockLost exception.", e);
                }
                catch (ServiceBusException e) when (e.IsTransient)
                {
                    throw new TransientException("ServiceBus SDK threw transient exception", e);
                }
            }

            return null;
        }

        public async Task<IMessageContext> ReceiveNextDeferredWithPop()
        {
            SessionState state = await GetSessionState();
            while (state.DeferredSequenceNumbers.Any())
            {
                long nextSequenceNumber = state.DeferredSequenceNumbers.First();

                try
                {
                    IServiceBusMessage deferred = await _sbSession.ReceiveDeferredMessageAsync(nextSequenceNumber);
                    if (deferred == null)
                    {
                        // Deferred message does not exist. 
                        // Update session state by removing the "null reference".
                        state.DeferredSequenceNumbers.RemoveRange(index: 0, count: 1);
                        await UpdateSessionState(state);

                        continue;
                    }
                    else
                    {
                        state.DeferredSequenceNumbers.RemoveRange(index: 0, count: 1);
                        await UpdateSessionState(state);
                    }

                    return new MessageContext(deferred, _sbSession, isDeferred: true);
                }
                catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.SessionLockLost)
                {
                    throw new TransientException("SessionLockLost exception.", e);
                }
                catch (ServiceBusException e) when (e.IsTransient)
                {
                    throw new TransientException("ServiceBus SDK threw transient exception", e);
                }
            }

            return null;
        }

        private string GetUserProperty(UserPropertyName userPropertyName)
        {
            return _sbMessage.GetUserProperty(userPropertyName) ?? throw new InvalidMessageException($"Message.UserProperties[{userPropertyName}] is not defined.");
        }

        private MessageType GetMessageType()
        {
            var messageTypeString = GetUserProperty(UserPropertyName.MessageType);

            if (Enum.TryParse(messageTypeString, out MessageType messageType))
                return messageType;

            throw new InvalidMessageException($"Unable to parse MessageType from '{messageTypeString}'.");
        }

        private MessageContent GetContent() =>
            JsonConvert.DeserializeObject<MessageContent>(Encoding.UTF8.GetString(_sbMessage.Body));


        private async Task UpdateSessionState(SessionState sessionState)
        {
            try
            {
                await _sbSession.SetStateAsync(sessionState);
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.SessionLockLost)
            {
                throw new TransientException("SessionLockLost exception.", e);
            }
            catch (ServiceBusException e) when (e.IsTransient)
            {
                throw new TransientException("ServiceBus SDK threw transient exception", e);
            }
        }

        private async Task<SessionState> GetSessionState()
        {
            try
            {
                return await _sbSession.GetStateAsync();
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.SessionLockLost)
            {
                throw new TransientException("SessionLockLost exception.", e);
            }
            catch (ServiceBusException e) when (e.IsTransient)
            {
                throw new TransientException("ServiceBus SDK threw transient exception", e);
            }
        }
    }
}
