// <copyright file="EmbeddingProviderIdentifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Parsed persisted embedding provider identifier.</summary>
/// <param name="Provider">The embedding provider name.</param>
/// <param name="Model">The provider-specific embedding model identifier.</param>
internal sealed record EmbeddingProviderIdentifier(string Provider, string Model);
