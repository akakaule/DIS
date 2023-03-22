using BH.DIS.Core.Events;
using BH.DIS.Core.Logging;
using System.Threading.Tasks;

namespace BH.DIS.SDK.EventHandlers
{
    /// <summary>
    /// Defines a message handler.
    /// </summary>
    /// <typeparam name="T">The type of message to be handled.</typeparam>
    public interface IEventHandler<T> where T : IEvent
    {
        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <param name="logger">A logger.</param>
        /// <param name="context">The context of the currently handled message.</param>
        /// <remarks>
        /// This method will be called when a message arrives on at the endpoint and should contain
        /// the custom logic to execute when the message is received.
        /// </remarks>
        Task Handle(T message, ILogger logger, IEventHandlerContext context);
    }
}
