// <copyright file="ImportedCase.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using Hexalith.Memories.Contracts.V1;

/// <summary>A case object from the export envelope: the <see cref="Case"/> record plus its members array.</summary>
/// <param name="Case">The case record (its <c>members</c> array is carried separately).</param>
/// <param name="Members">The case membership entries.</param>
internal sealed record ImportedCase(Case Case, IReadOnlyList<CaseMember> Members);
