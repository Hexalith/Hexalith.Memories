// <copyright file="TenantEventRoute.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Resolved routing destination for a CloudEvent: which tenant owns it, which case receives it,
/// and the aggregate type extracted from the CloudEvents <c>type</c> value.</summary>
/// <param name="TenantId">The resolved tenant identifier.</param>
/// <param name="CaseId">The resolved case identifier.</param>
/// <param name="AggregateType">The aggregate type extracted from the CloudEvents <c>type</c> (see <see cref="AggregateTypeExtractor"/>).</param>
public sealed record TenantEventRoute(string TenantId, string CaseId, string AggregateType);
