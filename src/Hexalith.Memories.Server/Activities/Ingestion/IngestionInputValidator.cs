// <copyright file="IngestionInputValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;

/// <summary>Central validation rules for ingestion requests and workflow input.</summary>
internal static class IngestionInputValidator
{
    internal const int MaxContentBytes = 1024 * 1024;

    /// <summary>Validates an ingestion payload and throws <see cref="ArgumentException"/> when invalid.</summary>
    /// <param name="input">The ingestion payload to validate.</param>
    public static void Validate(IngestionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidateTenantId(input.TenantId);
        ValidateRequired(input.CaseId, nameof(input.CaseId));
        ValidateRequired(input.SourceUri, nameof(input.SourceUri));
        ValidateRequired(input.ContentType, nameof(input.ContentType));
        ValidateRequired(input.IngestedBy, nameof(input.IngestedBy));
        ValidateSourceType(input.SourceType);
        ValidateContentBytesForSourceType(input.ContentBytes, input.PayloadReference, input.SourceType, input.SourceUri, input.TenantId);
        ValidateMetadata(input.Metadata);
    }

    private static void ValidateContentBytesForSourceType(
        byte[]? contentBytes,
        WorkflowPayloadReference? payloadReference,
        SourceType sourceType,
        string sourceUri,
        string tenantId)
    {
        if (sourceType == SourceType.Url)
        {
            if (contentBytes is { Length: > 0 })
            {
                throw new ArgumentException("ContentBytes must be null for SourceType=Url; the server fetches the URL body.");
            }

            if (!Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? parsed)
                || (parsed.Scheme is not "http" and not "https"))
            {
                throw new ArgumentException("SourceUri must be an absolute http(s) URL when SourceType=Url.");
            }

            return;
        }

        bool hasReference = payloadReference is not null;
        if (contentBytes is { Length: 0 } && !hasReference)
        {
            throw new ArgumentException($"ContentBytes must not be empty for SourceType={sourceType}.");
        }

        bool hasInlineBytes = contentBytes is { Length: > 0 };
        if (!hasInlineBytes && !hasReference)
        {
            throw new ArgumentException($"ContentBytes or PayloadReference is required for SourceType={sourceType}.");
        }

        if (contentBytes is { Length: > MaxContentBytes })
        {
            throw new ArgumentException($"ContentBytes must not exceed {MaxContentBytes} bytes (1 MB).");
        }

        if (payloadReference is not null)
        {
            if (!string.Equals(payloadReference.TenantId, tenantId, StringComparison.Ordinal))
            {
                throw new ArgumentException("PayloadReference tenant scope must match TenantId.");
            }

            if (payloadReference.ContentKind != WorkflowPayloadKind.SourceBytes)
            {
                throw new ArgumentException("PayloadReference must reference source bytes for non-URL ingestion.");
            }

            if (payloadReference.ByteLength <= 0
                || string.IsNullOrWhiteSpace(payloadReference.Id)
                || string.IsNullOrWhiteSpace(payloadReference.Sha256Hash)
                || string.IsNullOrWhiteSpace(payloadReference.MemoryUnitId))
            {
                throw new ArgumentException("PayloadReference must include id, sha256Hash, byteLength, and memoryUnitId.");
            }
        }
    }

    private static void ValidateMetadata(IReadOnlyDictionary<string, MetadataField> metadata)
    {
        foreach ((string key, MetadataField field) in metadata)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Metadata keys must not be empty or whitespace.");
            }

            if (float.IsNaN(field.Confidence) ||
                float.IsInfinity(field.Confidence) ||
                field.Confidence < 0f ||
                field.Confidence > 1f)
            {
                throw new ArgumentException($"Metadata field '{key}' confidence must be between 0.0 and 1.0.");
            }
        }
    }

    private static void ValidateRequired(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} is required.");
        }
    }

    private static void ValidateSourceType(SourceType sourceType)
    {
        if (!Enum.IsDefined(sourceType))
        {
            throw new ArgumentException("SourceType must be a defined enum value.");
        }
    }

    private static void ValidateTenantId(string? tenantId)
    {
        ValidateRequired(tenantId, nameof(IngestionInput.TenantId));

        try
        {
            TenantIdGuard.Validate(tenantId!);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException("TenantId contains invalid characters. Only alphanumeric and hyphens are allowed.");
        }
    }
}
