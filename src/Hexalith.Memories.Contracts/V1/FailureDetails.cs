namespace Hexalith.Memories.Contracts.V1;

/// <summary>Captures failure context when a memory unit enters the failed state.</summary>
public sealed record FailureDetails(
    string Stage,
    string ErrorCode,
    int RetryCount,
    string? ErrorMessage = null,
    DateTimeOffset? LastRetryAt = null);
