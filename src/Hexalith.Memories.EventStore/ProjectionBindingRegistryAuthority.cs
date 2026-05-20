// <copyright file="ProjectionBindingRegistryAuthority.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Describes whether projection binding data is trustworthy enough to emit absence warnings.</summary>
public enum ProjectionBindingRegistryAuthority
{
    /// <summary>No provider exposed a projection binding registry for the selected tenant boundary.</summary>
    Unknown,

    /// <summary>The provider exposed advisory metadata that must not create configured-but-unbound warnings.</summary>
    NonAuthoritative,

    /// <summary>The provider exposed runtime-bound metadata that can prove configured routes lack bindings.</summary>
    Authoritative,

    /// <summary>The provider was configured but could not return a complete trustworthy snapshot.</summary>
    Unavailable,
}
