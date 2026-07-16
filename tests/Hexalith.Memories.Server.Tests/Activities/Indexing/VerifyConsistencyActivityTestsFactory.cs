// <copyright file="VerifyConsistencyActivityTestsFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using NSubstitute;

using StackExchange.Redis;

/// <summary>
/// Shared fake factory for FalkorDB multiplexers used across Story 8.2 activity tests.
/// Emits responses in the FalkorDB compact wire format that <c>NFalkorDB.ResultSet</c>
/// iterates into <c>Record</c> instances.
/// </summary>
internal static class VerifyConsistencyActivityTestsFactory
{
    // FalkorDB compact-format type codes.
    private const long ScalarTypeInteger = 3;
    private const long ScalarTypeString = 2;

    /// <summary>
    /// Creates a FalkorDB multiplexer whose <c>ExecuteAsync</c> returns the supplied
    /// memory unit IDs as single-column string records.
    /// </summary>
    public static IConnectionMultiplexer CreateFalkorMultiplexer(IReadOnlyList<string> graphIds)
        => CreateFalkorMultiplexer(BuildStringIdRows(graphIds));

    /// <summary>
    /// Creates a FalkorDB multiplexer whose <c>ExecuteAsync</c> returns a fixed response.
    /// </summary>
    public static IConnectionMultiplexer CreateFalkorMultiplexer(RedisResult fixedResponse)
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer mux = Substitute.For<IConnectionMultiplexer>();
        mux.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(fixedResponse);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(fixedResponse);

        return mux;
    }

    /// <summary>
    /// Builds a FalkorDB scalar-result response (one record, one field).
    /// Matches the wire format for <c>MATCH (n) RETURN count(n)</c>.
    /// </summary>
    public static RedisResult BuildScalarResponse(long value)
        => RedisResult.Create(new RedisResult[]
        {
            BuildHeaders(1, "result"),
            RedisResult.Create(new RedisResult[]
            {
                BuildRecord(
                [
                    BuildScalarField(ScalarTypeInteger, (RedisValue)value),
                ]),
            }),
            BuildStatsRow(),
        });

    /// <summary>
    /// Builds a response containing multiple records, each with one string-typed column
    /// (e.g. <c>MATCH (m:MemoryUnit) RETURN m.id</c> or
    /// <c>MATCH (m:MemoryUnit {id: $id}) RETURN m.id</c>).
    /// </summary>
    public static RedisResult BuildStringIdRows(IReadOnlyList<string> ids)
    {
        RedisResult[] rows = ids
            .Select(id => BuildRecord(
            [
                BuildScalarField(ScalarTypeString, new RedisValue(id)),
            ]))
            .ToArray();

        return RedisResult.Create(new RedisResult[]
        {
            BuildHeaders(1, "memoryUnitId"),
            RedisResult.Create(rows),
            BuildStatsRow(),
        });
    }

    /// <summary>
    /// Builds a response containing a single three-integer record (matches
    /// <c>BuildCountMemoryUnitEdges</c> shape: <c>outgoing, incoming, caseEdges</c>).
    /// </summary>
    public static RedisResult BuildEdgeCountsResponse(long outgoing, long incoming, long caseEdges)
        => RedisResult.Create(new RedisResult[]
        {
            BuildHeaders(3, "outgoing", "incoming", "caseEdges"),
            RedisResult.Create(new RedisResult[]
            {
                BuildRecord(
                [
                    BuildScalarField(ScalarTypeInteger, (RedisValue)outgoing),
                    BuildScalarField(ScalarTypeInteger, (RedisValue)incoming),
                    BuildScalarField(ScalarTypeInteger, (RedisValue)caseEdges),
                ]),
            }),
            BuildStatsRow(),
        });

    private static RedisResult BuildHeaders(int columnCount, params string[] names)
    {
        RedisResult[] headers = new RedisResult[Math.Max(columnCount, names.Length)];
        for (int i = 0; i < headers.Length; i++)
        {
            string name = i < names.Length ? names[i] : $"col_{i}";
            headers[i] = RedisResult.Create(new RedisResult[]
            {
                RedisResult.Create((RedisValue)1),
                RedisResult.Create(new RedisValue(name)),
            });
        }

        return RedisResult.Create(headers);
    }

    private static RedisResult BuildRecord(RedisResult[] fields)
        => RedisResult.Create(fields);

    private static RedisResult BuildScalarField(long typeCode, RedisValue value)
        => RedisResult.Create(new RedisResult[]
        {
            RedisResult.Create((RedisValue)typeCode),
            RedisResult.Create(value),
        });

    private static RedisResult BuildStatsRow()
        => RedisResult.Create(new RedisResult[]
        {
            RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
        });
}
