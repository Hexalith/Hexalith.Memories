// <copyright file="OpenBaoCapturedRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net.Http;

/// <summary>Captures the non-network representation of an OpenBao HTTP request for contract assertions.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The request path.</param>
/// <param name="Token">The test token header, when present.</param>
/// <param name="Body">The JSON request body.</param>
internal sealed record OpenBaoCapturedRequest(HttpMethod Method, string Path, string? Token, string Body);
