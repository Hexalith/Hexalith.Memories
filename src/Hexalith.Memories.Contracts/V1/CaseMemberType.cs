// <copyright file="CaseMemberType.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Identifies whether a case member is an individual user or a role.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<CaseMemberType>))]
public enum CaseMemberType
{
    /// <summary>An individual user identity.</summary>
    User,

    /// <summary>A role-based identity granting access to all users in the role.</summary>
    Role,
}
