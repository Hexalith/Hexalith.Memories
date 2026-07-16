// <copyright file="CaseMember.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Represents a member associated with a case.</summary>
/// <param name="MemberId">The unique identifier of the member (user ID or role name).</param>
/// <param name="MemberType">Whether the member is a user or a role.</param>
/// <param name="AddedAt">The timestamp when the member was added to the case.</param>
public sealed record CaseMember(
    string MemberId,
    CaseMemberType MemberType,
    DateTimeOffset AddedAt);
