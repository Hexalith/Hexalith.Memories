// <copyright file="MemoriesPersistenceJsonSourceGenerationContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

using System.Text.Json.Serialization;

using Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Source-generated JSON metadata owned by the server persistence boundary.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BatchedGraphDeletionInput))]
[JsonSerializable(typeof(BatchedGraphDeletionResult))]
[JsonSerializable(typeof(CounterTransitionInput))]
[JsonSerializable(typeof(ExtractionInput))]
[JsonSerializable(typeof(ExtractionResult))]
[JsonSerializable(typeof(FailedUnitInput))]
[JsonSerializable(typeof(FetchUrlInput))]
[JsonSerializable(typeof(UrlFetchResult))]
[JsonSerializable(typeof(IndexInput))]
[JsonSerializable(typeof(IndexResult))]
[JsonSerializable(typeof(NaturalLanguageDescriptionInput))]
[JsonSerializable(typeof(NaturalLanguageDescriptionResult))]
[JsonSerializable(typeof(NaturalLanguageIndexInput))]
[JsonSerializable(typeof(QueueNaturalLanguageEmbeddingRetryInput))]
[JsonSerializable(typeof(FailedNaturalLanguageEmbeddingRecord))]
[JsonSerializable(typeof(NaturalLanguageEmbeddingRetryInput))]
[JsonSerializable(typeof(NaturalLanguageEmbeddingRetryResult))]
[JsonSerializable(typeof(StoredTenantEmbeddingConfig))]
[JsonSerializable(typeof(StoredFusionWeights))]
[JsonSerializable(typeof(StoredTenantInfo))]
[JsonSerializable(typeof(StoredTenantRegistryEntry))]
[JsonSerializable(typeof(StoredCaseMember))]
[JsonSerializable(typeof(StoredFailureDetails))]
[JsonSerializable(typeof(StoredWorkflowPayloadReference))]
[JsonSerializable(typeof(StoredMetadataField))]
[JsonSerializable(typeof(Dictionary<string, StoredMetadataField>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
internal sealed partial class MemoriesPersistenceJsonSourceGenerationContext : JsonSerializerContext;
