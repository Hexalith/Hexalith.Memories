// <copyright file="EmbeddingProviderTestMode.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>Embedding provider runtime mode selected by the Aspire integration fixture.</summary>
public enum EmbeddingProviderTestMode
{
    /// <summary>Use the default deterministic fake embedding path.</summary>
    GoogleFake,

    /// <summary>Disable fake embeddings so tests can exercise Ollama OIDC dispatch through local fakes.</summary>
    OllamaOidcFake,
}
