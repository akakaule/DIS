using System.Threading.Tasks;

namespace BH.DIS.Core.Messages
{
    public interface IMessageHandler
    {
        Task Handle(IMessageContext messageContext);
    }
}
