// <copyright file="MemoriesClientOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

/// <summary>Options controlling how <see cref="MemoriesClient"/> talks to the Memories Server.</summary>
public sealed class MemoriesClientOptions
{
    /// <summary>The base URI of the Memories Server (e.g. <c>http://127.0.0.1:5000/</c>).</summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Optional API token. Prefer <c>HEXALITH_MEMORIES_API_TOKEN</c> environment variable over the
    /// <c>--token</c> CLI flag — argv is visible in shell history and process listings.
    /// </summary>
    public string? ApiToken { get; set; }
}
