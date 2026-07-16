// <copyright file="PersistenceModelMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

using Hexalith.Memories.Server.Tenants;

/// <summary>Maps durable server representations to and from public contracts.</summary>
internal static class PersistenceModelMapper
{
    public static StoredTenantEmbeddingConfig ToStored(TenantEmbeddingConfig value)
        => new(
            value.Provider,
            value.Model,
            value.Dimensions,
            value.RateLimitPerMinute,
            value.ApiSecretKeyName,
            value.ReindexRequired,
            value.BaseUrl,
            value.AuthMode,
            value.OidcTokenEndpoint,
            value.OidcClientId,
            value.OidcScope);

    public static TenantEmbeddingConfig ToContract(StoredTenantEmbeddingConfig value)
        => new(
            value.Provider,
            value.Model,
            value.Dimensions,
            value.RateLimitPerMinute,
            value.ApiSecretKeyName,
            value.ReindexRequired,
            value.BaseUrl,
            value.AuthMode,
            value.OidcTokenEndpoint,
            value.OidcClientId,
            value.OidcScope);

    public static StoredFusionWeights ToStored(FusionWeights value)
        => new(value.SyntacticWeight, value.SemanticWeight, value.GraphWeight, value.NlWeight);

    public static FusionWeights ToContract(StoredFusionWeights value)
        => new()
        {
            SyntacticWeight = value.SyntacticWeight,
            SemanticWeight = value.SemanticWeight,
            GraphWeight = value.GraphWeight,
            NlWeight = value.NlWeight,
        };

    public static StoredTenantRegistryEntry ToStored(TenantRegistryEntry value)
        => new(ToStored(value.Tenant), value.WorkflowInstanceId, value.LastUpdated);

    public static TenantRegistryEntry ToContract(StoredTenantRegistryEntry value)
        => new(ToContract(value.Tenant), value.WorkflowInstanceId, value.LastUpdated);

    public static StoredTenantInfo ToStored(TenantInfo value)
        => new(value.Id, value.DisplayName, value.Status, value.CreatedAt, value.EmbeddingProvider, value.EmbeddingModel);

    public static TenantInfo ToContract(StoredTenantInfo value)
        => new(value.Id, value.DisplayName, value.Status, value.CreatedAt)
        {
            EmbeddingProvider = value.EmbeddingProvider,
            EmbeddingModel = value.EmbeddingModel,
        };

    public static StoredCaseMember ToStored(CaseMember value)
        => new(value.MemberId, value.MemberType, value.AddedAt);

    public static CaseMember ToContract(StoredCaseMember value)
        => new(value.MemberId, value.MemberType, value.AddedAt);

    public static StoredFailureDetails ToStored(FailureDetails value)
        => new(value.Stage, value.ErrorCode, value.RetryCount, value.ErrorMessage, value.LastRetryAt);

    public static FailureDetails ToContract(StoredFailureDetails value)
        => new(value.Stage, value.ErrorCode, value.RetryCount, value.ErrorMessage, value.LastRetryAt);

    public static StoredWorkflowPayloadReference ToStored(WorkflowPayloadReference value)
        => new(value.Id, value.Sha256Hash, value.ByteLength, value.ContentKind, value.TenantId, value.MemoryUnitId);

    public static WorkflowPayloadReference ToContract(StoredWorkflowPayloadReference value)
        => new(value.Id, value.Sha256Hash, value.ByteLength, value.ContentKind, value.TenantId, value.MemoryUnitId);

    public static Dictionary<string, StoredMetadataField> ToStored(IReadOnlyDictionary<string, MetadataField> value)
        => value.ToDictionary(
            static item => item.Key,
            static item => new StoredMetadataField(item.Value.Value, item.Value.Origin, item.Value.Confidence),
            StringComparer.Ordinal);

    public static Dictionary<string, MetadataField> ToContract(IReadOnlyDictionary<string, StoredMetadataField> value)
    {
        // Story 25.4: a corrupt/hand-edited durable payload can deserialize a metadata field as JSON null. The
        // previous dictionary-copy tolerated that without throwing; skip the null entry here so a corrupt field
        // degrades gracefully instead of raising an unhandled NullReferenceException on the persistence read path.
        Dictionary<string, MetadataField> result = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, StoredMetadataField> item in value)
        {
            StoredMetadataField? field = item.Value;
            if (field is null)
            {
                continue;
            }

            result[item.Key] = new MetadataField(field.Value, field.Origin, field.Confidence);
        }

        return result;
    }
}
