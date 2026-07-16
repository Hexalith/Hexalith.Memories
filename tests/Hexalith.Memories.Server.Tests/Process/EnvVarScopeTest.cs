// <copyright file="EnvVarScopeTest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Process;

using Hexalith.Memories.TestHelpers.Process;

using Shouldly;

/// <summary>
/// Regression coverage for the shared process-wide env-var scope helper used by Story 8.4's
/// telemetry and Aspire fixtures.
/// </summary>
public sealed class EnvVarScopeTest
{
    [Fact]
    public async Task SameLogicalAsyncFlowReentryShouldFailFastAcrossThreadHop()
    {
        string name = $"HEXALITH_ENV_SCOPE_{Guid.NewGuid():N}";
        using EnvVarScope outer = EnvVarScope.Set(name, "outer");

        Exception? ex = await Task.Run(() =>
        {
            try
            {
                using EnvVarScope inner = EnvVarScope.Set(name, "inner");
                return null;
            }
            catch (Exception caught)
            {
                return caught;
            }
        }).WaitAsync(TimeSpan.FromSeconds(5));

        ex.ShouldNotBeNull();
        ex.ShouldBeOfType<InvalidOperationException>();
        ex.Message.ShouldContain("logical async flow");
    }

    [Fact]
    public void NameComparisonShouldMatchCurrentOperatingSystemBehavior()
    {
        string upper = $"HEXALITH_ENV_SCOPE_{Guid.NewGuid():N}";
        string lower = upper.ToLowerInvariant();

        using EnvVarScope outer = EnvVarScope.Set(upper, "outer");
        if (OperatingSystem.IsWindows())
        {
            Should.Throw<InvalidOperationException>(() => EnvVarScope.Set(lower, "inner"));
        }
        else
        {
            Should.NotThrow(() =>
            {
                using EnvVarScope inner = EnvVarScope.Set(lower, "inner");
            });
        }
    }
}
