// <copyright file="ResolvedConfigPipelineTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Configuration;

using Shouldly;

public class ResolvedConfigPipelineTests
{
    private static readonly Uri FlagEndpoint = new("https://flag.example.com/");
    private static readonly Uri EnvEndpoint = new("https://env.example.com/");
    private static readonly Uri FileEndpoint = new("https://file.example.com/");
    private static readonly Uri DefaultEndpoint = DefaultConfigurationSource.DefaultEndpoint;

    [Fact]
    public void Resolve_FlagWinsOverEverythingElse()
    {
        var pipeline = new ResolvedConfigPipeline(
        [
            new StubSource(nameof(FlagConfigurationSource), FlagEndpoint, "flagtoken"),
            new StubSource(nameof(EnvironmentVariableConfigurationSource), EnvEndpoint, "envtoken"),
            new StubSource(nameof(FileConfigurationSource), FileEndpoint, "filetoken"),
            new DefaultConfigurationSource(),
        ]);

        ResolvedConfig resolved = pipeline.Resolve();

        resolved.Endpoint.ShouldBe(FlagEndpoint);
        resolved.ApiToken.ShouldBe("flagtoken");
        resolved.ResolvedBy.ShouldBe(nameof(FlagConfigurationSource));
    }

    [Fact]
    public void Resolve_EnvWinsOverFileWhenFlagAbsent()
    {
        var pipeline = new ResolvedConfigPipeline(
        [
            new StubSource(nameof(FlagConfigurationSource), null, null),
            new StubSource(nameof(EnvironmentVariableConfigurationSource), EnvEndpoint, "envtoken"),
            new StubSource(nameof(FileConfigurationSource), FileEndpoint, "filetoken"),
            new DefaultConfigurationSource(),
        ]);

        ResolvedConfig resolved = pipeline.Resolve();

        resolved.Endpoint.ShouldBe(EnvEndpoint);
        resolved.ApiToken.ShouldBe("envtoken");
        resolved.ResolvedBy.ShouldBe(nameof(EnvironmentVariableConfigurationSource));
    }

    [Fact]
    public void Resolve_FileWinsOverDefaultWhenFlagAndEnvAbsent()
    {
        var pipeline = new ResolvedConfigPipeline(
        [
            new StubSource(nameof(FlagConfigurationSource), null, null),
            new StubSource(nameof(EnvironmentVariableConfigurationSource), null, null),
            new StubSource(nameof(FileConfigurationSource), FileEndpoint, "filetoken"),
            new DefaultConfigurationSource(),
        ]);

        ResolvedConfig resolved = pipeline.Resolve();

        resolved.Endpoint.ShouldBe(FileEndpoint);
        resolved.ApiToken.ShouldBe("filetoken");
        resolved.ResolvedBy.ShouldBe(nameof(FileConfigurationSource));
    }

    [Fact]
    public void Resolve_FallsThroughToDefaultWhenAllPriorTiersEmpty()
    {
        var pipeline = new ResolvedConfigPipeline(
        [
            new StubSource(nameof(FlagConfigurationSource), null, null),
            new StubSource(nameof(EnvironmentVariableConfigurationSource), null, null),
            new StubSource(nameof(FileConfigurationSource), null, null),
            new DefaultConfigurationSource(),
        ]);

        ResolvedConfig resolved = pipeline.Resolve();

        resolved.Endpoint.ShouldBe(DefaultEndpoint);
        resolved.ApiToken.ShouldBeNull();
        resolved.ResolvedBy.ShouldBe(nameof(DefaultConfigurationSource));
    }

    [Fact]
    public void Resolve_TokenIndependentFromEndpoint_EnvSuppliesTokenWhenFlagOnlyOverridesEndpoint()
    {
        // Flag provides endpoint only; env provides token only.
        var pipeline = new ResolvedConfigPipeline(
        [
            new StubSource(nameof(FlagConfigurationSource), FlagEndpoint, apiToken: null),
            new StubSource(nameof(EnvironmentVariableConfigurationSource), endpoint: null, "envtoken"),
            new DefaultConfigurationSource(),
        ]);

        ResolvedConfig resolved = pipeline.Resolve();

        resolved.Endpoint.ShouldBe(FlagEndpoint);
        resolved.ApiToken.ShouldBe("envtoken");
        resolved.ResolvedBy.ShouldBe(nameof(FlagConfigurationSource));
    }

    private sealed class StubSource : IConfigurationSource
    {
        private readonly Uri? _endpoint;
        private readonly string? _apiToken;

        public StubSource(string name, Uri? endpoint, string? apiToken)
        {
            SourceName = name;
            _endpoint = endpoint;
            _apiToken = apiToken;
        }

        public string SourceName { get; }

        public bool TryResolve(out Uri? endpoint, out string? apiToken)
        {
            endpoint = _endpoint;
            apiToken = _apiToken;
            return endpoint is not null || apiToken is not null;
        }
    }
}
