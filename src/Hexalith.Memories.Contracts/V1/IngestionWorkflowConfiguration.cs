// <copyright file="IngestionWorkflowConfiguration.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Durable workflow configuration captured when an ingestion workflow is scheduled.</summary>
public sealed record IngestionWorkflowConfiguration
{
    /// <summary>Gets the captured activity retry configuration.</summary>
    public IngestionActivityRetryConfiguration Retry { get; init; } = new();

    /// <summary>Gets the captured natural-language workflow options.</summary>
    public NaturalLanguageWorkflowOptions NaturalLanguage { get; init; } = new();
}
