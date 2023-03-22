using BH.DIS.Core.Messages;
using BH.DIS.Core.Logging;
using System.Threading.Tasks;

namespace BH.DIS.SDK.EventHandlers
{
    public interface IEventJsonHandler
    {
        Task Handle(IMessageContext context, ILogger logger);
    }
}
