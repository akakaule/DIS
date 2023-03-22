using BH.DIS.Core.Messages.Exceptions;
using System;
using System.Threading.Tasks;

namespace BH.DIS.Core.Messages
{
    public interface IReceivedMessage : IMessage
    {
        DateTime EnqueuedTimeUtc { get; }
        string From { get; }
        string MessageId { get; }

        string EventTypeId { get; }
        string DeadLetterReason { get; }
        string DeadLetterErrorDescription { get; }
    }

    public interface IMessageContext : IReceivedMessage
    {
        bool IsDeferred { get; }
        Task Complete();

        Task Abandon(TransientException exception);

        Task DeadLetter(string reason, Exception exception = null);

        Task Defer();

        Task DeferOnly();

        Task<IMessageContext> ReceiveNextDeferred();
        Task<IMessageContext> ReceiveNextDeferredWithPop();
        Task BlockSession();
        Task UnblockSession();
        Task<bool> IsSessionBlocked();
        Task<bool> IsSessionBlockedByThis();
        Task<bool> IsSessionBlockedByEventId();
        Task<string> GetBlockedByEventId();
    }
}