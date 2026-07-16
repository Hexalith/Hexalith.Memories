// <copyright file="CaseCreationServiceAdapter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Cases;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Server-side adapter implementing <see cref="ICaseCreationService"/> over the existing
/// <see cref="CaseService"/>. Scoped so that each subscription invocation gets its own DB/actor
/// context consistent with how the HTTP endpoints resolve <see cref="CaseService"/> (ADR 9.1-D).</summary>
internal sealed class CaseCreationServiceAdapter : ICaseCreationService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CaseCreationServiceAdapter(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scopeFactory = scopeFactory;
    }

    public async Task<string> CreateCaseAsync(string tenantId, string caseName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseName);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        CaseService caseService = scope.ServiceProvider.GetRequiredService<CaseService>();
        Case created = await caseService
            .CreateCaseAsync(new CreateCaseInput(tenantId, caseName, Description: null), cancellationToken)
            .ConfigureAwait(false);
        return created.Id;
    }
}
