// <copyright file="RedisPlaceholder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Redis;

/// <summary>Compatibility constants retained for consumers of the published Redis package.</summary>
/// <remarks>
/// New code should configure backend endpoints directly. This type may be removed only in an owned
/// breaking major release after downstream consumers have migrated.
/// <para>spec-infrastructure-dependency-abstraction (F9, Decision D30): the port constants below are
/// confirmed unreferenced by any connection code in this repository (they open no connection and are a
/// compile-time compat surface only). Scheduled for removal on the next owned breaking major once no
/// external consumer depends on them; tracked as deferred cleanup, not an infrastructure leak.</para>
/// </remarks>
public static class RedisPlaceholder
{
    /// <summary>The historical default Redis port.</summary>
    public const string DefaultRedisPort = "6379";

    /// <summary>The historical default FalkorDB port.</summary>
    public const string DefaultFalkorDbPort = "6380";
}
