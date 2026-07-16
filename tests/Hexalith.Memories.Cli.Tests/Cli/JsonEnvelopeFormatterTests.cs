// <copyright file="JsonEnvelopeFormatterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;

using Shouldly;

/// <summary>Regression coverage for the consolidated JSON success-envelope formatter.</summary>
public sealed class JsonEnvelopeFormatterTests
{
    [Fact]
    public void FixedCommand_Write_EmitsExactSchemaAndDataShape()
    {
        var data = new ConfigShowData(
            Endpoint: "https://srv.example.com/",
            ResolvedBy: "EnvironmentVariableConfigurationSource",
            TokenConfigured: true);
        using var writer = new StringWriter() { NewLine = "\n" };

        var formatter = new JsonEnvelopeFormatter<ConfigShowData>(ConfigShowCommand.CommandName);
        formatter.Write(data, writer);

        formatter.Format.ShouldBe(OutputFormat.Json);
        writer.ToString().ShouldBe(
            "{\n"
            + "  \"schemaVersion\": 1,\n"
            + "  \"command\": \"config show\",\n"
            + "  \"data\": {\n"
            + "    \"endpoint\": \"https://srv.example.com/\",\n"
            + "    \"resolvedBy\": \"EnvironmentVariableConfigurationSource\",\n"
            + "    \"tokenConfigured\": true\n"
            + "  }\n"
            + "}\n");
    }

    [Theory]
    [InlineData("repair", ConsistencyRepairCommand.CommandName)]
    [InlineData("verify", ConsistencyVerifyCommand.CommandName)]
    [InlineData("unexpected", ConsistencyVerifyCommand.CommandName)]
    public void SelectorCommand_Write_UsesPayloadValueOrVerifyFallback(string kind, string expectedCommand)
    {
        var receipt = new ConsistencyCommandReceipt(
            "acme",
            "consistency-acme-1",
            kind,
            new Uri("https://srv.example.com/api/v1/tenants/acme/consistency/consistency-acme-1"));
        using var writer = new StringWriter() { NewLine = "\n" };
        var formatter = new JsonEnvelopeFormatter<ConsistencyCommandReceipt>(value =>
            value.Kind == "repair"
                ? ConsistencyRepairCommand.CommandName
                : ConsistencyVerifyCommand.CommandName);

        formatter.Write(receipt, writer);

        using JsonDocument document = JsonDocument.Parse(writer.ToString());
        document.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("command").GetString().ShouldBe(expectedCommand);
        document.RootElement.GetProperty("data").GetProperty("kind").GetString().ShouldBe(kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void FixedCommand_BlankCommand_ThrowsArgumentException(string command)
    {
        Should.Throw<ArgumentException>(() => new JsonEnvelopeFormatter<ConfigShowData>(command));
    }

    [Fact]
    public void FixedCommand_NullCommand_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new JsonEnvelopeFormatter<ConfigShowData>((string)null!));
    }

    [Fact]
    public void SelectorCommand_NullSelector_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => new JsonEnvelopeFormatter<ConfigShowData>((Func<ConfigShowData, string>)null!));
    }

    [Fact]
    public void Write_NullPayload_ThrowsArgumentNullException()
    {
        var formatter = new JsonEnvelopeFormatter<ConfigShowData>(ConfigShowCommand.CommandName);
        using var writer = new StringWriter();

        Should.Throw<ArgumentNullException>(() => formatter.Write(null!, writer));
    }

    [Fact]
    public void Write_NullWriter_ThrowsArgumentNullException()
    {
        var formatter = new JsonEnvelopeFormatter<ConfigShowData>(ConfigShowCommand.CommandName);
        var data = new ConfigShowData("https://srv.example.com/", "test", TokenConfigured: false);

        Should.Throw<ArgumentNullException>(() => formatter.Write(data, null!));
    }
}
