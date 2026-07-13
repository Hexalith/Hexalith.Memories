// <copyright file="GraphQueryBuilderRestoreEdgeTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Graph;

using System;
using System.Collections.Generic;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Shouldly;

/// <summary>Story 26.2 (AC3) — Docker-free coverage for restoring an edge with its full audit trail.</summary>
public class GraphQueryBuilderRestoreEdgeTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildRestoreEdge_ReconstructsIdentityFromSourceTargetType_NotEdgeId()
    {
        GraphQueryBuilder builder = new();

        (string query, IDictionary<string, object> parameters) = builder.BuildRestoreEdge(
            "mu-1",
            "mu-2",
            EdgeType.CausedBy,
            0.9f,
            EdgeOrigin.Inferred,
            CreatedAt,
            verifiedBy: "reviewer-1",
            previousConfidence: 0.5f);

        // Identity is (source, target, type) — the graph-instance edge id is never a parameter.
        query.ShouldContain("MERGE (s)-[r:CAUSED_BY]->(t)");
        parameters["sourceId"].ShouldBe("mu-1");
        parameters["targetId"].ShouldBe("mu-2");
        parameters.ShouldNotContainKey("id");
        parameters.ShouldNotContainKey("edgeId");

        // Full audit trail restored literally.
        parameters["confidence"].ShouldBe(0.9f);
        parameters["origin"].ShouldBe("inferred");
        parameters["verifiedBy"].ShouldBe("reviewer-1");
        parameters["previousConfidence"].ShouldBe(0.5f);
        parameters.ShouldContainKey("createdAt");
        query.ShouldContain("r.verifiedBy = $verifiedBy");
        query.ShouldContain("r.previousConfidence = $previousConfidence");
    }

    [Fact]
    public void BuildRestoreEdge_WithoutPromotionAudit_OmitsAuditFields()
    {
        GraphQueryBuilder builder = new();

        (string query, IDictionary<string, object> parameters) = builder.BuildRestoreEdge(
            "mu-1",
            "mu-2",
            EdgeType.References,
            0.5f,
            EdgeOrigin.Explicit,
            CreatedAt,
            verifiedBy: null,
            previousConfidence: null);

        query.ShouldNotContain("verifiedBy");
        query.ShouldNotContain("previousConfidence");
        parameters.ShouldNotContainKey("verifiedBy");
        parameters.ShouldNotContainKey("previousConfidence");
        query.ShouldContain("MERGE (s)-[r:REFERENCES]->(t)");
    }

    [Fact]
    public void BuildRestoreEdge_ConfidenceOutOfRange_Throws()
        => Should.Throw<ArgumentOutOfRangeException>(() => new GraphQueryBuilder().BuildRestoreEdge(
            "mu-1",
            "mu-2",
            EdgeType.CausedBy,
            1.5f,
            EdgeOrigin.Inferred,
            CreatedAt,
            verifiedBy: null,
            previousConfidence: null));

    [Fact]
    public void BuildRestoreEdge_BlankSource_Throws()
        => Should.Throw<ArgumentException>(() => new GraphQueryBuilder().BuildRestoreEdge(
            " ",
            "mu-2",
            EdgeType.CausedBy,
            0.9f,
            EdgeOrigin.Inferred,
            CreatedAt,
            verifiedBy: null,
            previousConfidence: null));
}
