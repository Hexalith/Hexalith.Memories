// <copyright file="EmbeddingMigrationLease.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Identifies the owner-locked blue/green migration run.</summary>
/// <param name="OwnerId">The owner id that must hold the Redis lock for mutable operations.</param>
/// <param name="Version">The deterministic version id used for staging and previous targets.</param>
public sealed record EmbeddingMigrationLease(string OwnerId, string Version);
