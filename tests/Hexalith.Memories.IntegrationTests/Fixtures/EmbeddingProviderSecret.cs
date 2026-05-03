// <copyright file="EmbeddingProviderSecret.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>Secret-store entry used by provider-specific Aspire integration tests.</summary>
/// <param name="Name">The DAPR secret name referenced by tenant embedding configuration.</param>
/// <param name="Value">The secret value written to the local test secret store.</param>
public sealed record EmbeddingProviderSecret(string Name, string Value);
