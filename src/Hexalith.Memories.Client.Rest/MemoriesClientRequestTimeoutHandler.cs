// <copyright file="MemoriesClientRequestTimeoutHandler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.Options;

/// <summary>Selects the ordinary or long-running import timeout for each typed-client request.</summary>
internal sealed class MemoriesClientRequestTimeoutHandler(IOptions<MemoriesClientOptions> options) : DelegatingHandler
{
    /// <summary>Marks requests that require the configured long-running import timeout.</summary>
    internal static readonly HttpRequestOptionsKey<bool> UseImportTimeout = new("Hexalith.Memories.UseImportTimeout");

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        TimeSpan timeout = request.Options.TryGetValue(UseImportTimeout, out bool useImportTimeout) && useImportTimeout
            ? options.Value.ImportTimeout
            : MemoriesClientServiceCollectionExtensions.DefaultTimeout;
        if (timeout != Timeout.InfiniteTimeSpan && timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"The configured Memories request timeout must be positive or infinite; received '{timeout}'.");
        }

        if (timeout == Timeout.InfiniteTimeSpan)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            return await base.SendAsync(request, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new TaskCanceledException(
                $"The Memories request timed out after {timeout}.",
                exception,
                timeoutSource.Token);
        }
    }
}
