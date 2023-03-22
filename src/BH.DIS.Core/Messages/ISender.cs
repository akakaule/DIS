using System.Collections.Generic;
using System.Threading.Tasks;

namespace BH.DIS.Core.Messages
{
    public interface ISender
    {
        Task Send(IMessage message, int messageEnqueueDelay = 0);
        Task Send(IEnumerable<IMessage> messages, int messageEnqueueDelay = 0);
    }
}
