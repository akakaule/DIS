using BH.DIS.Core.Logging;
using System.Threading.Tasks;

namespace BH.DIS.Core.Messages
{
    public interface IEventContextHandler
    {
        Task Handle(IMessageContext context, ILogger logger);
    }
}