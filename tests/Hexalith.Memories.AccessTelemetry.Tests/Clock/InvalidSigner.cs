namespace Hexalith.Memories.AccessTelemetry.Tests.Clock;

using Hexalith.Memories.AccessTelemetry.Clock;

/// <summary>Produces signatures that fail verification against a valid public key.</summary>
internal sealed class InvalidSigner : IClockAttestationSigner
{
    /// <inheritdoc/>
    public string KeyEpoch => "test-epoch";

    /// <inheritdoc/>
    public byte[] Sign(ReadOnlySpan<byte> payload) => new byte[64];
}
