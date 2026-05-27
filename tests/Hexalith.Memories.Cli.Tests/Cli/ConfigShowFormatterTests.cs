// <copyright file="ConfigShowFormatterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;

using Shouldly;

public sealed class ConfigShowFormatterTests
{
    [Fact]
    public void Json_EmitsSchemaVersion1EnvelopeWithoutTokenField()
    {
        var data = new ConfigShowData(
            Endpoint: "https://srv.example.com/",
            ResolvedBy: "EnvironmentVariableConfigurationSource",
            TokenConfigured: true);
        using var writer = new StringWriter() { NewLine = "\n" };

        new ConfigShowJsonFormatter().Write(data, writer);
        using JsonDocument doc = JsonDocument.Parse(writer.ToString());

        doc.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("command").GetString().ShouldBe("config show");
        JsonElement payload = doc.RootElement.GetProperty("data");
        payload.GetProperty("endpoint").GetString().ShouldBe("https://srv.example.com/");
        payload.GetProperty("resolvedBy").GetString().ShouldBe("EnvironmentVariableConfigurationSource");
        payload.GetProperty("tokenConfigured").GetBoolean().ShouldBeTrue();
        payload.TryGetProperty("apiToken", out _).ShouldBeFalse();
    }

    [Fact]
    public void Table_EmitsThreeRows()
    {
        var data = new ConfigShowData(
            Endpoint: "http://127.0.0.1:5000/",
            ResolvedBy: "DefaultConfigurationSource",
            TokenConfigured: false);
        using var writer = new StringWriter() { NewLine = "\n" };

        new ConfigShowTableFormatter().Write(data, writer);
        string[] lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Length.ShouldBe(5);
        lines[0].ShouldContain("KEY");
        lines[0].ShouldContain("VALUE");
        lines[2].ShouldStartWith("endpoint");
        lines[3].ShouldStartWith("resolvedBy");
        lines[4].ShouldStartWith("tokenConfigured");
    }
}
