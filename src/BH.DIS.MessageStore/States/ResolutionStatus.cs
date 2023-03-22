namespace BH.DIS.MessageStore;

public enum ResolutionStatus
{
    Pending,
    Deferred,
    Failed,
    TooManyRequests,
    DeadLettered,
    Unsupported,
    Published,
    Completed,
    Skipped,
}