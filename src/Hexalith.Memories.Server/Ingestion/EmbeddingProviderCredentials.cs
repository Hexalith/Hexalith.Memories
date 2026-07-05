// <copyright file="EmbeddingProviderCredentials.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Resolved credentials for a single embedding provider HTTP exchange.</summary>
/// <param name="PrimaryValue">The value applied to the outgoing request's authentication header — the Google API key or the Ollama bearer token.</param>
/// <param name="SensitiveValues">Every secret-like value that must be redacted from provider error bodies for this attempt (e.g., the DAPR secret and the acquired token).</param>
internal sealed record EmbeddingProviderCredentials(string PrimaryValue, IReadOnlyList<string?> SensitiveValues);
