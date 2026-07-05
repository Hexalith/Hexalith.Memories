// <copyright file="NaturalLanguageWorkflowOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Durable natural-language ingestion options captured for workflow replay.</summary>
public sealed record NaturalLanguageWorkflowOptions
{
    /// <summary>Gets a value indicating whether the generated description is persisted into memory metadata.</summary>
    public bool PersistInMetadata { get; init; }
}
