// <copyright file="RedisPlaceholder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Redis;

/// <summary>Compatibility constants retained for consumers of the published Redis package.</summary>
/// <remarks>
/// New code should configure backend endpoints directly. This type may be removed only in an owned
/// breaking major release after downstream consumers have migrated.
/// </remarks>
public static class RedisPlaceholder
{
    /// <summary>The historical default Redis port.</summary>
    public const string DefaultRedisPort = "6379";

    /// <summary>The historical default FalkorDB port.</summary>
    public const string DefaultFalkorDbPort = "6380";
}
