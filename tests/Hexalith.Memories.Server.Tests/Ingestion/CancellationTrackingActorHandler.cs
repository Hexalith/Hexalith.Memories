// <copyright file="CancellationTrackingActorHandler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

/// <summary>Holds the Dapr HTTP request open until the request or test token cancels it.</summary>
/// <param name="testToken">A bounded test cleanup token.</param>
internal sealed class CancellationTrackingActorHandler(CancellationToken testToken) : HttpMessageHandler
{
    private readonly TaskCompletionSource<string> _requestPath = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the path captured when the client dispatches the actor request.</summary>
    internal Task<string> RequestPath => _requestPath.Task;

    /// <summary>Gets a value indicating whether the actor request token was cancelled.</summary>
    internal bool RequestCancelled { get; private set; }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requestPath.SetResult(request.RequestUri!.AbsolutePath);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, testToken);
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            RequestCancelled = cancellationToken.IsCancellationRequested;
            throw;
        }

        throw new InvalidOperationException("The blocking actor request must end by cancellation.");
    }
}
