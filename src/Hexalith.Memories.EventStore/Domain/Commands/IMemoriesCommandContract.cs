// <copyright file="IMemoriesCommandContract.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Commands;

/// <summary>Defines the routing metadata required to submit a Memories command to EventStore.</summary>
public interface IMemoriesCommandContract
{
    /// <summary>Gets the command type discriminator.</summary>
    static abstract string CommandType { get; }

    /// <summary>Gets the owning domain name.</summary>
    static abstract string Domain { get; }

    /// <summary>Gets the target aggregate identifier.</summary>
    string AggregateId { get; }
}
