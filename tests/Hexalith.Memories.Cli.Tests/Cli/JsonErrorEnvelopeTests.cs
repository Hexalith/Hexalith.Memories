// <copyright file="JsonErrorEnvelopeTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;
using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>
/// Story 7.3 Task 7.6 — JSON error envelope shape (AC #4). Asserts
/// <c>{ schemaVersion, command, error }</c> with <c>data</c> absent when an error is emitted.
/// </summary>
public sealed class JsonErrorEnvelopeTests
{
    [Fact]
    public void Write_ErrorEnvelope_OmitsDataField()
    {
        using var writer = new StringWriter();
        var error = new CliErrorPayload("TENANT_NOT_FOUND", "Tenant 'acme' does not exist.", "Run 'memories tenant list'.");

        JsonErrorEnvelopeWriter.Write<IReadOnlyList<TenantSummary>>(writer, "tenant list", error);

        using JsonDocument doc = JsonDocument.Parse(writer.ToString());
        doc.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("command").GetString().ShouldBe("tenant list");
        doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse();

        JsonElement err = doc.RootElement.GetProperty("error");
        err.GetProperty("code").GetString().ShouldBe("TENANT_NOT_FOUND");
        err.GetProperty("message").GetString().ShouldBe("Tenant 'acme' does not exist.");
        err.GetProperty("suggestion").GetString().ShouldBe("Run 'memories tenant list'.");
    }

    [Fact]
    public void WriteForCommand_UnknownCommand_FallsBackToDefaultEnvelopeShape()
    {
        using var writer = new StringWriter();
        var error = new CliErrorPayload("UNEXPECTED_ERROR", "boom", "Run with --verbose.");

        JsonErrorEnvelopeWriter.WriteForCommand(writer, CliCommandExecutor.RootCommandName, error);

        using JsonDocument doc = JsonDocument.Parse(writer.ToString());
        doc.RootElement.GetProperty("command").GetString().ShouldBe("memories");
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().ShouldBe("UNEXPECTED_ERROR");
        doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse();
    }

    [Fact]
    public void WriteForCommand_EveryKnownCommand_EmitsParseableEnvelope()
    {
        foreach (string name in new[] { "tenant list", "config show", "search query", "search inspect" })
        {
            using var writer = new StringWriter();
            var error = new CliErrorPayload("INVALID_INPUT", "bad input", "run --help");
            JsonErrorEnvelopeWriter.WriteForCommand(writer, name, error);

            using JsonDocument doc = JsonDocument.Parse(writer.ToString());
            doc.RootElement.GetProperty("command").GetString().ShouldBe(name);
            doc.RootElement.GetProperty("error").GetProperty("code").GetString().ShouldBe("INVALID_INPUT");
            doc.RootElement.TryGetProperty("data", out _).ShouldBeFalse(
                $"{name}: data slot must be suppressed when Error is non-null");
        }
    }

    [Fact]
    public void SuccessEnvelope_StillEmitsDataAndOmitsErrorSlot()
    {
        // Regression guard: adding Error to the envelope must not churn the existing success shape.
        using var writer = new StringWriter();
        var payload = (IReadOnlyList<TenantSummary>)[new TenantSummary
        {
            Id = "t-1",
            DisplayName = "Tenant One",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            IndexSizes = new TenantIndexSizes(null, null, null),
            IndexStatus = new TenantIndexStatus(IndexHealth.Unknown, IndexHealth.Unknown, IndexHealth.Unknown),
            ReindexRequired = false,
        }];

        new JsonEnvelopeFormatter<IReadOnlyList<TenantSummary>>(TenantListCommand.CommandName).Write(payload, writer);

        using JsonDocument doc = JsonDocument.Parse(writer.ToString());
        doc.RootElement.TryGetProperty("error", out _).ShouldBeFalse();
        doc.RootElement.GetProperty("data").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public void ForError_DataIsNullErrorIsSet()
    {
        var error = new CliErrorPayload("CODE", "msg", "sug");
        CliOutputEnvelope<IReadOnlyList<TenantSummary>> envelope =
            CliOutputEnvelope<IReadOnlyList<TenantSummary>>.ForError("tenant list", error);

        envelope.Data.ShouldBeNull();
        envelope.Error.ShouldBe(error);
        envelope.SchemaVersion.ShouldBe(CliOutputEnvelope<IReadOnlyList<TenantSummary>>.CurrentSchemaVersion);
    }

    [Fact]
    public void Constructor_BothDataAndErrorNull_Throws()
    {
        Should.Throw<ArgumentException>(
            () => new CliOutputEnvelope<IReadOnlyList<TenantSummary>>(1, "tenant list", data: null, error: null));
    }

    [Fact]
    public void Constructor_BothDataAndErrorSet_Throws()
    {
        IReadOnlyList<TenantSummary> data = Array.Empty<TenantSummary>();
        var error = new CliErrorPayload("CODE", "msg", "sug");

        Should.Throw<ArgumentException>(
            () => new CliOutputEnvelope<IReadOnlyList<TenantSummary>>(1, "tenant list", data, error));
    }
}
