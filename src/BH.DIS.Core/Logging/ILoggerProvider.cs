using BH.DIS.Core.Messages;

namespace BH.DIS.Core.Logging
{
    public interface ILoggerProvider
    {
        ILogger GetContextualLogger(IMessageContext messageContext);

        ILogger GetContextualLogger(IMessage message);
        ILogger GetContextualLogger(string correlationId);
    }
}
