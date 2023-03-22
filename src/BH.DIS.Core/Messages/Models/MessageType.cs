namespace BH.DIS.Core.Messages
{
    public enum MessageType
    {
        Unknown,

        EventRequest,

        ErrorResponse,
        ResolutionResponse,
        DeferralResponse,
        SkipResponse,

        ResubmissionRequest,
        SkipRequest,
        RetryRequest,

        ContinuationRequest,
        UnsupportedResponse,

        HeartbeatResponse
    }
}
