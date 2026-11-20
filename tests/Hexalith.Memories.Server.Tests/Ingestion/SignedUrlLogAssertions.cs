// <copyright file="SignedUrlLogAssertions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using Shouldly;

internal static class SignedUrlLogAssertions
{
    internal const string PathSecret = "synthetic-private-path";
    internal const string QuerySecret = "synthetic-query-token";
    internal const string Url = "https://203.0.113.10/" + PathSecret + "/document.md?sig=" + QuerySecret;

    internal static void ShouldProtectSource(this IReadOnlyList<AuditLogCapture> logs)
    {
        logs.ShouldNotBeEmpty();
        foreach (AuditLogCapture log in logs)
        {
            AssertSafe(log.Message);
            AssertSafe(log.Exception?.ToString() ?? string.Empty);
            if (log.AuditEvent is { } audit)
            {
                AssertSafe(JsonSerializer.Serialize(audit, MemoriesJsonContext.Options));
            }
            foreach (var field in log.Fields)
            {
                AssertSafe(field.ToString());
            }
        }
    }

    private static void AssertSafe(string value)
    {
        value.ShouldNotContain(PathSecret);
        value.ShouldNotContain(QuerySecret);
    }
}
