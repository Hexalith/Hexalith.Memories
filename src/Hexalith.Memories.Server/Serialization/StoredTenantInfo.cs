// <copyright file="StoredTenantInfo.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

/// <summary>Durable tenant registry payload independent of the public response contract.</summary>
internal sealed record StoredTenantInfo(
    string Id,
    string DisplayName,
    TenantStatus Status,
    DateTimeOffset CreatedAt,
    string? EmbeddingProvider = null,
    string? EmbeddingModel = null);
