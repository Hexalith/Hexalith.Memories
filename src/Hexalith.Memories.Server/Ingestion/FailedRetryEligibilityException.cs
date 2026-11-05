namespace Hexalith.Memories.Server.Ingestion;

/// <summary>An exact failed-attempt proof or admission conflict that can be retried after restoring its claimed failed record.</summary>
/// <param name="reason">The nonsecret eligibility refusal reason.</param>
internal sealed class FailedRetryEligibilityException(string reason) : InvalidOperationException(reason);
