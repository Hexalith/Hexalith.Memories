namespace Hexalith.Memories.Server.NaturalLanguage;

/// <summary>Signals an atomic rejection of stale or erased retry publication without retaining content.</summary>
internal sealed class RetryPublicationRejectedException : InvalidOperationException
{
    /// <summary>Initializes the content-free rejection.</summary>
    internal RetryPublicationRejectedException() : base("retry-payload-retired") { }
}
