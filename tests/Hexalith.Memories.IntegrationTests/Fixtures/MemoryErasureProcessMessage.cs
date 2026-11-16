namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Text.Json;

/// <summary>A bounded private request or response, authenticated for one worker incarnation.</summary>
/// <param name="Sequence">The monotonically increasing request sequence.</param>
/// <param name="Operation">The requested operation or response disposition.</param>
/// <param name="Payload">The operation data, retained only on the private pipe.</param>
/// <param name="Signature">The session HMAC over the complete request identity.</param>
internal sealed record MemoryErasureProcessMessage(long Sequence, string Operation, JsonElement Payload, string Signature);
