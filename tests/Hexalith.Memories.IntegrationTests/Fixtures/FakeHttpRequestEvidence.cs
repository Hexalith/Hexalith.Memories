// <copyright file="FakeHttpRequestEvidence.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>Sanitized request evidence captured by local HTTP fakes.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The request path.</param>
/// <param name="ClientId">The safe OIDC client identifier when present.</param>
/// <param name="Model">The embedding model when present.</param>
/// <param name="HasBearerToken">Whether an Authorization bearer header was present.</param>
/// <param name="HasClientSecret">Whether a client secret form field was present.</param>
public sealed record FakeHttpRequestEvidence(
    string Method,
    string Path,
    string? ClientId = null,
    string? Model = null,
    bool HasBearerToken = false,
    bool HasClientSecret = false);
