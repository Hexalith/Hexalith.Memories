// <copyright file="HandlerMismatchReportSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 9.3 — round-trip and camelCase verification for mismatch-report contract types.</summary>
public class HandlerMismatchReportSerializationTests
{
    [Fact]
    public void HandlerMismatchReport_ShouldRoundTripThroughMemoriesJsonContext()
    {
        HandlerMismatchReport report = new()
        {
            TenantId = "acme",
            AsOf = "2026-04-24T12:00:00+00:00",
            WindowHours = 24,
            Summary = new HandlerMismatchReportSummary
            {
                RoutesConfigured = 3,
                ObservationsChecked = 12,
            },
            Mismatches = new List<HandlerMismatch>
            {
                new()
                {
                    Category = HandlerMismatchCategory.VersionMismatch,
                    Severity = HandlerMismatchSeverity.Warning,
                    Subject = "ClaimSubmitted",
                    Context = "Stem 'ClaimSubmitted' observed with 2 concurrent versions.",
                    Suggestion = "Multiple versions of 'ClaimSubmitted' observed.",
                },
            },
        };

        string json = JsonSerializer.Serialize(report, MemoriesJsonContext.Options);
        json.ShouldContain("\"category\":\"versionMismatch\"");
        json.ShouldContain("\"severity\":\"warning\"");
        json.ShouldContain("\"routesConfigured\":3");

        HandlerMismatchReport? roundTripped = JsonSerializer.Deserialize<HandlerMismatchReport>(
            json, MemoriesJsonContext.Options);
        roundTripped.ShouldNotBeNull();
        roundTripped!.Mismatches.Count.ShouldBe(1);
        roundTripped.Mismatches[0].Category.ShouldBe(HandlerMismatchCategory.VersionMismatch);
        roundTripped.Mismatches[0].Severity.ShouldBe(HandlerMismatchSeverity.Warning);
        roundTripped.Summary.RoutesConfigured.ShouldBe(3);
        roundTripped.HasWarnings.ShouldBeTrue();
        roundTripped.HasInfo.ShouldBeFalse();
    }

    [Fact]
    public void EmptyMismatchReport_HasWarningsAndHasInfo_ShouldBeFalse()
    {
        HandlerMismatchReport empty = new()
        {
            TenantId = "acme",
            AsOf = "2026-04-24T12:00:00+00:00",
            WindowHours = 24,
            Mismatches = new List<HandlerMismatch>(),
            Summary = new HandlerMismatchReportSummary { RoutesConfigured = 1, ObservationsChecked = 0 },
        };

        empty.HasWarnings.ShouldBeFalse();
        empty.HasInfo.ShouldBeFalse();
    }

    [Theory]
    [InlineData(HandlerMismatchCategory.UnhandledEventType, "\"unhandledEventType\"")]
    [InlineData(HandlerMismatchCategory.StaleHandler, "\"staleHandler\"")]
    [InlineData(HandlerMismatchCategory.VersionMismatch, "\"versionMismatch\"")]
    public void HandlerMismatchCategory_ShouldRoundTripAsCamelCaseString(
        HandlerMismatchCategory value, string expectedJson)
    {
        string json = JsonSerializer.Serialize(value, MemoriesJsonContext.Options);
        json.ShouldBe(expectedJson);

        HandlerMismatchCategory deserialized = JsonSerializer.Deserialize<HandlerMismatchCategory>(
            json, MemoriesJsonContext.Options);
        deserialized.ShouldBe(value);
    }

    [Theory]
    [InlineData(HandlerMismatchSeverity.Info, "\"info\"")]
    [InlineData(HandlerMismatchSeverity.Warning, "\"warning\"")]
    public void HandlerMismatchSeverity_ShouldRoundTripAsCamelCaseString(
        HandlerMismatchSeverity value, string expectedJson)
    {
        string json = JsonSerializer.Serialize(value, MemoriesJsonContext.Options);
        json.ShouldBe(expectedJson);

        HandlerMismatchSeverity deserialized = JsonSerializer.Deserialize<HandlerMismatchSeverity>(
            json, MemoriesJsonContext.Options);
        deserialized.ShouldBe(value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void HandlerMismatchCategory_ShouldRejectIntegerTokens(string json)
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<HandlerMismatchCategory>(
            json, MemoriesJsonContext.Options));
    }
}
