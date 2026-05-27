// <copyright file="ExportedTenantConfig.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Tenant-scope configuration snapshot included in a tenant export envelope (Story 8.3).
/// Composes <see cref="TenantConfigurationView"/> with tenant-registry metadata. No secret values
/// are present — <see cref="TenantEmbeddingConfig.ApiSecretKeyName"/> is a secret-store identifier,
/// not the secret itself (Story 5.5 security posture).
/// </summary>
/// <param name="Configuration">Composed operator-facing tenant configuration.</param>
/// <param name="Status">Tenant lifecycle status at export time.</param>
/// <param name="CreatedAt">When the tenant was registered.</param>
/// <param name="LastUpdated">When the tenant record was last updated.</param>
public sealed record ExportedTenantConfig(
    TenantConfigurationView Configuration,
    TenantStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdated);
