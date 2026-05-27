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
        ValidateContentBytesForSourceType(input.ContentBytes, input.SourceType, input.SourceUri);
        ValidateMetadata(input.Metadata);
    }

    private static void ValidateContentBytesForSourceType(byte[]? contentBytes, SourceType sourceType, string sourceUri)
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

        // Inline ingestion types (File, Event, Command, Projection, Discussion, Annotation)
        // carry payload bytes directly through extraction/indexing.
        if (contentBytes is null)
        {
            throw new ArgumentException($"ContentBytes is required for SourceType={sourceType}.");
        }

        if (contentBytes.Length == 0)
        {
            throw new ArgumentException($"ContentBytes must not be empty for SourceType={sourceType}.");
        }

        if (contentBytes.Length > MaxContentBytes)
        {
            throw new ArgumentException($"ContentBytes must not exceed {MaxContentBytes} bytes (1 MB).");
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