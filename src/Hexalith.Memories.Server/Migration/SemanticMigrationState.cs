// <copyright file="SemanticMigrationState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Provider, model, and dimension metadata persisted on a semantic vector hash.</summary>
/// <param name="Provider">The embedding provider.</param>
/// <param name="Model">The embedding model.</param>
/// <param name="Dimensions">The embedding dimension count.</param>
public sealed record SemanticMigrationState(string? Provider, string? Model, int? Dimensions);
