// <copyright file="StoredTenantInfo.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

using System.Text.Json.Serialization;

/// <summary>Durable tenant registry payload independent of the public response contract.</summary>
internal sealed record StoredTenantInfo(
    string Id,
    string DisplayName,
    TenantStatus Status,
    DateTimeOffset CreatedAt,
    // Story 25.4: mirror TenantInfo's WhenWritingNull omission so an embedding-unconfigured tenant's durable
    // registry row keeps the legacy shape (keys absent) instead of rewriting explicit nulls on every save.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? EmbeddingProvider = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? EmbeddingModel = null);
