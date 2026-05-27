// <copyright file="MemoriesClientOptionsMutator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Execution;

using Hexalith.Memories.Client.Rest;

/// <summary>
/// Mutable singleton that backs the <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/> of
/// <see cref="MemoriesClientOptions"/>. Used by <see cref="CliCommandExecutor"/> to push the resolved
/// endpoint/token into the live client before calling the handler.
/// </summary>
public sealed class MemoriesClientOptionsMutator : CliCommandExecutor.IOptionsMutator
{
    /// <summary>Gets the live options instance.</summary>
    public MemoriesClientOptions Options { get; } = new();

    /// <inheritdoc />
    public void Apply(Uri endpoint, string? apiToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        Options.Endpoint = endpoint;
        Options.ApiToken = apiToken;
    }
}
