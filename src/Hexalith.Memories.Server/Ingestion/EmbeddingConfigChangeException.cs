// <copyright file="EmbeddingConfigChangeException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Thrown when an embedding configuration change requires a full reindex but forceReindex was not specified.</summary>
public class EmbeddingConfigChangeException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingConfigChangeException"/> class.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="currentConfig">The current embedding configuration.</param>
    /// <param name="proposedConfig">The proposed embedding configuration.</param>
    /// <param name="affectedFields">The fields that changed and require reindex.</param>
    public EmbeddingConfigChangeException(
        string tenantId,
        TenantEmbeddingConfig currentConfig,
        TenantEmbeddingConfig proposedConfig,
        string[] affectedFields)
        : base($"Embedding configuration change for tenant '{tenantId}' requires a full reindex. " +
               $"Affected fields: {string.Join(", ", affectedFields)}. " +
               "Existing vectors are incompatible with the new configuration. " +
               "Set forceReindex=true to acknowledge and proceed.")
    {
        TenantId = tenantId;
        CurrentConfig = currentConfig;
        ProposedConfig = proposedConfig;
        AffectedFields = affectedFields;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingConfigChangeException"/> class.</summary>
    public EmbeddingConfigChangeException()
        : base()
    {
        TenantId = string.Empty;
        AffectedFields = [];
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingConfigChangeException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EmbeddingConfigChangeException(string message, Exception innerException)
        : base(message, innerException)
    {
        TenantId = string.Empty;
        AffectedFields = [];
    }

    /// <summary>Gets the fields that changed and require reindex.</summary>
    public string[] AffectedFields { get; }

    /// <summary>Gets the current embedding configuration.</summary>
    public TenantEmbeddingConfig? CurrentConfig { get; }

    /// <summary>Gets the proposed embedding configuration.</summary>
    public TenantEmbeddingConfig? ProposedConfig { get; }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; }
}
