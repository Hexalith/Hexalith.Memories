// <copyright file="IngestionPayloadClaimCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Prepares ingestion workflow input by moving non-URL source bytes into the workflow payload store.</summary>
internal static class IngestionPayloadClaimCheck
{
    /// <summary>Returns input with non-URL bytes claim-checked when a payload store is available.</summary>
    public static async Task<IngestionInput> PrepareAsync(
        IWorkflowPayloadStore payloadStore,
        string memoryUnitId,
        IngestionInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payloadStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        ArgumentNullException.ThrowIfNull(input);

        IngestionIdentityGuard.Input(input, memoryUnitId);
        if (input.SourceType == SourceType.Url
            || input.PayloadReference is not null
            || input.ContentBytes is not { Length: > 0 } bytes)
        {
            return input;
        }

        WorkflowPayloadReference reference = await payloadStore
            .SaveAsync(
                input.TenantId,
                memoryUnitId,
                WorkflowPayloadKind.SourceBytes,
                bytes,
                idSuffix: "source",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return input with
        {
            ContentBytes = null,
            PayloadReference = reference,
        };
    }

    /// <summary>Derives the exact source identity before its save can succeed without acknowledgement.</summary>
    internal static WorkflowPayloadReference? GetSourceReference(string unit, IngestionInput input)
    {
        IngestionIdentityGuard.Input(input, unit);
        if (input.PayloadReference is not null) { return input.PayloadReference; }
        if (input.SourceType == SourceType.Url || input.ContentBytes is not { Length: > 0 } bytes) { return null; }
        string hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
        return new(unit + ":sourcebytes:" + hash + ":source", hash, bytes.LongLength, WorkflowPayloadKind.SourceBytes, input.TenantId, unit);
    }

    /// <summary>Gets the declared payload length for telemetry without exposing payload contents.</summary>
    public static long GetDeclaredPayloadLength(IngestionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.PayloadReference?.ByteLength ?? input.ContentBytes?.LongLength ?? 0L;
    }
}
