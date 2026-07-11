// <copyright file="StoredCaseMember.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

/// <summary>Durable representation of a case membership.</summary>
internal sealed record StoredCaseMember(
    string MemberId,
    CaseMemberType MemberType,
    DateTimeOffset AddedAt);
