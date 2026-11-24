// <copyright file="QuickstartStepResultTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli.Quickstart;

using Shouldly;

public sealed class QuickstartStepResultTests
{
    [Theory]
    [InlineData(1.5, 2)]
    [InlineData(2_147_483_647.5, int.MaxValue)]
    [InlineData(-2_147_483_648.5, int.MinValue)]
    public void Serialize_DurationMs_UsesRoundedAndBoundedInt32(double milliseconds, int expected)
    {
        var result = new QuickstartStepResult(
            1, "Verifying prerequisites", QuickstartStepStatus.Ok, TimeSpan.FromMilliseconds(milliseconds),
            "Ready", Suggestion: null, ErrorCode: null);

        using JsonDocument payload = JsonSerializer.SerializeToDocument(result);

        payload.RootElement.GetProperty("durationMs").GetInt32().ShouldBe(expected);
    }
}
