// <copyright file="StartupValidationHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Hosting;

using Hexalith.Memories.Mcp.Authentication;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>Forces startup validation of security-critical MCP options.</summary>
internal sealed class StartupValidationHostedService(IOptions<MemoriesMcpAuthenticationOptions> options) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = options.Value;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
