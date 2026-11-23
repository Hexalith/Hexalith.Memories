// <copyright file="ProjectionBindingExpectation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Handlers;

/// <summary>Coverage expected for one configured route and event name.</summary>
/// <param name="SourcePrefix">The configured route source prefix.</param>
/// <param name="SourceKey">The normalized route source prefix.</param>
/// <param name="AggregateKey">The normalized aggregate name.</param>
/// <param name="EventKey">The normalized event name or the wildcard for an unobserved aggregate.</param>
/// <param name="IsUnobserved">Whether the aggregate has no events to compare against the pattern.</param>
/// <param name="ComparisonKey">The tenant-scoped key shown in diagnostics.</param>
internal sealed record ProjectionBindingExpectation(
    string SourcePrefix,
    string SourceKey,
    string AggregateKey,
    string EventKey,
    bool IsUnobserved,
    string ComparisonKey);
