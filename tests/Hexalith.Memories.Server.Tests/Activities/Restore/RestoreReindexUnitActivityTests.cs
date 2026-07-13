// <copyright file="RestoreReindexUnitActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Restore;

using Hexalith.Memories.Server.Activities.Restore;

using Shouldly;

/// <summary>Tests restore re-index attribution compatibility.</summary>
[Trait("Category", "Unit")]
public sealed class RestoreReindexUnitActivityTests
{
    [Theory]
    [InlineData("google", "google", "gemini-embedding-001")]
    [InlineData("google:gemini-embedding-001", "google", "gemini-embedding-001")]
    [InlineData("GOOGLE:GEMINI-EMBEDDING-001", "google", "gemini-embedding-001")]
    [InlineData("ollama:qwen3-embedding:4b", "ollama", "qwen3-embedding:4b")]
    public void MatchesProviderAttribution_CompatibleStoredForms_ReturnTrue(
        string attribution,
        string provider,
        string model)
        => RestoreReindexUnitActivity.MatchesProviderAttribution(attribution, provider, model).ShouldBeTrue();

    [Theory]
    [InlineData("google:text-embedding-004", "google", "gemini-embedding-001")]
    [InlineData("ollama:qwen3-embedding:4b", "google", "gemini-embedding-001")]
    [InlineData("openai", "google", "gemini-embedding-001")]
    public void MatchesProviderAttribution_IncompatibleStoredForms_ReturnFalse(
        string attribution,
        string provider,
        string model)
        => RestoreReindexUnitActivity.MatchesProviderAttribution(attribution, provider, model).ShouldBeFalse();
}
