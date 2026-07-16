// <copyright file="DefaultProjectionBindingProviderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.EventStore;

using Shouldly;

/// <summary>Story 16.1 — guards the default provider's non-noisy contract so deployments without
/// projection-binding registry data never get configured-but-unbound warnings by default.</summary>
public sealed class DefaultProjectionBindingProviderTests
{
    [Fact]
    public async Task GetBindingsAsync_ReturnsUnknownAuthority_ForSelectedTenant()
    {
        DefaultProjectionBindingProvider provider = new();

        ProjectionBindingSnapshot snapshot = await provider.GetBindingsAsync("acme", CancellationToken.None);

        snapshot.TenantId.ShouldBe("acme");
        snapshot.Authority.ShouldBe(ProjectionBindingRegistryAuthority.Unknown);
        snapshot.Bindings.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetBindingsAsync_NullTenantId_Throws()
    {
        DefaultProjectionBindingProvider provider = new();

        await Should.ThrowAsync<ArgumentException>(
            () => provider.GetBindingsAsync(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetBindingsAsync_WhitespaceTenantId_Throws()
    {
        DefaultProjectionBindingProvider provider = new();

        await Should.ThrowAsync<ArgumentException>(
            () => provider.GetBindingsAsync("   ", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetBindingsAsync_CancelledToken_Throws()
    {
        DefaultProjectionBindingProvider provider = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.GetBindingsAsync("acme", cts.Token).AsTask());
    }
}
