using System;
using System.Threading.Tasks;

namespace BH.DIS.Core.Messages
{
    public interface IResponseService
    {
        Task SendResolutionResponse(IMessageContext messageContext);
        Task SendSkipResponse(IMessageContext messageContext);
        Task SendErrorResponse(IMessageContext messageContext, Exception exception);
        Task SendDeferralResponse(IMessageContext messageContext, SessionBlockedException exception);
        Task SendContinuationRequestToSelf(IMessageContext deferredMessageContext);
        Task SendRetryResponse(IMessageContext messageContext, int messageDelayMinutes);
        Task SendUnsupportedResponse(IMessageContext messageContext);
        Task SendHeartbeatResponseToSelf(IMessageContext messageContext);
    }
}
