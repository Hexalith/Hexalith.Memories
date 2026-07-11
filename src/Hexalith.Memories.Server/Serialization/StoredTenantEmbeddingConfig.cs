// <copyright file="StoredTenantEmbeddingConfig.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

/// <summary>Durable representation of a tenant embedding configuration.</summary>
internal sealed record StoredTenantEmbeddingConfig(
    string Provider,
    string Model,
    int Dimensions,
    int RateLimitPerMinute,
    string ApiSecretKeyName,
    bool ReindexRequired = false,
    string? BaseUrl = null,
    string AuthMode = "api-key",
    string? OidcTokenEndpoint = null,
    string? OidcClientId = null,
    string? OidcScope = null);
