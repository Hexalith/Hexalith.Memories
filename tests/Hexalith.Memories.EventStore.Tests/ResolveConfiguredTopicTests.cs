// <copyright file="ResolveConfiguredTopicTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Configuration;

using Shouldly;

/// <summary>review P7 — env-var-through-IConfiguration visibility for the EventStore topic override.</summary>
[Collection("EnvironmentVariableSerialized")]
public sealed class ResolveConfiguredTopicTests
{
    [Fact]
    public void ResolveConfiguredTopic_FromEnvironmentVariablesProvider_SurfacesAppHostInjectedValue()
    {
        using var scope = new EnvScope((EventIngestionController.TopicEnvVar, "  memories-events-env  "));
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        EventStoreIntegrationServiceCollectionExtensions.ResolveConfiguredTopic(configuration)
            .ShouldBe("memories-events-env");
    }

    [Fact]
    public void ResolveConfiguredTopic_WithoutEnvironmentVariablesProvider_MissesProcessEnv()
    {
        using var scope = new EnvScope((EventIngestionController.TopicEnvVar, "memories-events-missed"));
        IConfiguration configuration = new ConfigurationBuilder().Build();

        EventStoreIntegrationServiceCollectionExtensions.ResolveConfiguredTopic(configuration)
            .ShouldBeNull();
    }

    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previous = new(StringComparer.Ordinal);

        public EnvScope(params (string Key, string? Value)[] values)
        {
            foreach ((string key, string? value) in values)
            {
                _previous[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach ((string key, string? prior) in _previous)
            {
                Environment.SetEnvironmentVariable(key, prior);
            }
        }
    }
}

[CollectionDefinition("EnvironmentVariableSerialized", DisableParallelization = true)]
public sealed class EnvironmentVariableSerializedCollection
{
}
