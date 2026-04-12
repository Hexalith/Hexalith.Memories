// <copyright file="AddCaseMemberInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input payload for adding a member to a case.</summary>
/// <param name="MemberId">The unique identifier of the member (user ID or role name).</param>
/// <param name="MemberType">Whether the member is a user or a role.</param>
public sealed record AddCaseMemberInput(
    string MemberId,
    CaseMemberType MemberType);
