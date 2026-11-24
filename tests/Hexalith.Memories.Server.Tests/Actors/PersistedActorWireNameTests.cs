// <copyright file="PersistedActorWireNameTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Actors;

using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;

using Shouldly;

public sealed class PersistedActorWireNameTests
{
    [Theory]
    [InlineData(typeof(CorpusStatistics), "AverageDocumentLength", "DocumentCount", "LastRefreshedAt")]
    [InlineData(typeof(RateLimitState), "CeilingPerMinute", "Remaining", "WindowStart")]
    [InlineData(typeof(CaseIngestionCounts), "Embedding", "Extracting", "Indexing", "Queued")]
    public void PersistedState_UsesExplicitExistingDataMemberNames(Type stateType, params string[] expectedNames)
    {
        string[] wireNames = stateType.GetProperties()
            .Select(property => property.GetCustomAttribute<DataMemberAttribute>())
            .OfType<DataMemberAttribute>()
            .Select(member => member.Name.ShouldNotBeNull())
            .Order(StringComparer.Ordinal)
            .ToArray();

        wireNames.ShouldBe(expectedNames);
    }

    [Theory]
    [InlineData(typeof(CorpusStatistics), """{"documentCount":42,"averageDocumentLength":2.5,"lastRefreshedAt":"2026-11-24T00:00:00+00:00"}""")]
    [InlineData(typeof(RateLimitState), """{"remaining":3,"windowStart":"2026-11-24T00:00:00Z","ceilingPerMinute":7}""")]
    [InlineData(typeof(CaseIngestionCounts), """{"queued":1,"extracting":2,"embedding":3,"indexing":4}""")]
    [InlineData(typeof(CaseIngestionCounterState), """{"queued":1,"extracting":2,"embedding":3,"indexing":4,"lastTransitionId":"start","appliedTransitionSequences":{"workflow":1},"appliedTransitionWorkflowOrder":["workflow"]}""")]
    public void ActorJson_RoundTripsExistingWireMembers(Type stateType, string legacyJson)
    {
        object state = JsonSerializer.Deserialize(legacyJson, stateType, JsonSerializerOptions.Web).ShouldNotBeNull();
        string currentJson = JsonSerializer.Serialize(state, stateType, JsonSerializerOptions.Web);

        JsonNode.DeepEquals(JsonNode.Parse(legacyJson), JsonNode.Parse(currentJson)).ShouldBeTrue();
        stateType.GetProperties()
            .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>().ShouldNotBeNull().Name)
            .Order(StringComparer.Ordinal)
            .ShouldBe(JsonNode.Parse(legacyJson).ShouldNotBeNull().AsObject().Select(property => property.Key).Order(StringComparer.Ordinal));
    }
}
