// <copyright file="MissingCaseCreationService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Placeholder case-creation adapter used until the host supplies a concrete implementation.</summary>
internal sealed class MissingCaseCreationService : ICaseCreationService
{
    public Task<string> CreateCaseAsync(string tenantId, string caseName, CancellationToken cancellationToken)
        => Task.FromException<string>(new InvalidOperationException(
            "EventStore integration requires a concrete ICaseCreationService. "
            + "Register one by calling AddMemoriesEventStoreIntegration(..., builder => builder.AddCaseCreationService<TImplementation>())."));
}
