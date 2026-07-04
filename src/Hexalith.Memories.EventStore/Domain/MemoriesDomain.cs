// <copyright file="MemoriesDomain.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain;

/// <summary>Defines the EventStore domain names owned by Hexalith.Memories.</summary>
public static class MemoriesDomain
{
    /// <summary>Case aggregate domain.</summary>
    public const string Cases = "memories-cases";

    /// <summary>Memory unit aggregate domain.</summary>
    public const string MemoryUnits = "memories-memory-units";

    /// <summary>Tenant lifecycle aggregate domain.</summary>
    public const string Tenants = "memories-tenants";
}
