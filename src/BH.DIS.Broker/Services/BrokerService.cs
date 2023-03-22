using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using System.Threading.Tasks;

namespace BH.DIS.Broker.Services
{
    public class BrokerService : IEventContextHandler
    {
        private readonly ISender _sender;

        public BrokerService(ISender sender)
        {
            _sender = sender;
        }

        public async Task Handle(IMessageContext context, ILogger logger)
        {
            await ForwardMessage(context);
        }

        private Task ForwardMessage(IMessageContext context)
        {
            return _sender.Send(new Core.Messages.Message
            {
                CorrelationId = context.CorrelationId,
                EventId = context.EventId,
                To = context.MessageContent.EventContent.EventTypeId,
                SessionId = context.SessionId,
                MessageId = context.EventId,
                OriginatingFrom = context.From,

                MessageType = MessageType.EventRequest,
                MessageContent = new MessageContent
                {
                    EventContent = context.MessageContent.EventContent
                }
            });
        }
    }
}
