// <copyright file="ErrorCatalogTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Errors;

using Shouldly;

/// <summary>
/// Tests the <see cref="ErrorMessageCatalog"/> covers the per-code translations expected by Story 7.3
/// AC #3, #6, #7, #8 for representative current server and synthetic codes.
/// </summary>
public sealed class ErrorCatalogTests
{
    public static IEnumerable<object[]> DomainCodes =>
    [
        ["TENANT_NOT_FOUND", 1, "memories tenant list"],
        ["CASE_NOT_FOUND", 1, "memories tenant list"],
        ["CASE_MISMATCH", 1, "failed-units"],
        ["MEMORY_UNIT_NOT_FOUND", 1, "memories search query"],
        ["MEMORY_UNIT_NOT_INDEXED", 1, "Retry"],
        ["INVALID_INPUT", 1, "memories search query --help"],
        ["INVALID_REQUEST", 1, "memories --help"],
        ["INVALID_AXIS", 1, "syntactic, semantic, nl, graph, hybrid"],
        ["INVALID_TENANT_ID", 1, "memories tenant list"],
        ["INVALID_CONFIG", 1, "Fix the configuration"],
        ["TENANT_DELETING", 1, "memories tenant list"],
        ["TENANT_PROVISIONING", 1, "memories tenant list"],
        ["TENANT_FAILED", 1, "memories tenant list"],
        ["TENANT_UNAVAILABLE", 1, "memories tenant list"],
        ["RATE_LIMIT_EXCEEDED", 1, "rate-limit window"],
        ["MEMBER_NOT_FOUND", 1, "REST API"],
        ["MEMBER_LIMIT_EXCEEDED", 1, "Remove existing members"],
        ["INVALID_CASE_ID", 1, "REST API"],
        ["EDGE_NOT_FOUND", 1, "REST API"],
        ["BATCH_NOT_FOUND", 1, "REST API"],
        ["RE_INGESTION_IN_PROGRESS", 1, "Retry"],
        ["IMPORT_SCHEMA_VERSION_UNSUPPORTED", 1, "Re-export"],
        ["IMPORT_SCOPE_MISMATCH", 1, "import route"],
        ["IMPORT_TENANT_MISMATCH", 1, "same tenant"],
        ["IMPORT_CASE_MISMATCH", 1, "same case"],
        ["IMPORT_TOO_LARGE", 1, "case-by-case"],
        ["IMPORT_ABORTED", 1, "Retry"],
        ["IMPORT_EMPTY", 1, "export envelope"],
        ["IMPORT_MANIFEST_UNREADABLE", 1, "export envelope"],
        ["RESTORE_STATUS_NOT_FOUND", 1, "instance id"],
        ["RESTORE_TARGET_BUSY", 1, "active restore"],
        ["RESTORE_TARGET_NOT_CLEAN", 1, "empty tenant"],
        ["TENANT_UPDATE_CONFLICT", 1, "memories tenant list"],
    ];

    public static IEnumerable<object[]> PlumbingCodes =>
    [
        ["DAPR_UNAVAILABLE", 2],
        ["EMBEDDING_UNAVAILABLE", 2],
        ["BACKEND_UNAVAILABLE", 2],
        ["GRAPH_UNAVAILABLE", 2],
        ["ALL_BACKENDS_UNAVAILABLE", 2],
        ["GRAPH_TIMEOUT", 2],
        ["BATCH_TRACKING_UNAVAILABLE", 2],
        ["BATCH_SCHEDULING_FAILED", 2],
        ["CONNECTION_REFUSED", 2],
        ["REQUEST_TIMEOUT", 2],
        ["TLS_ERROR", 2],
        ["INVALID_ENDPOINT", 2],
        ["UNEXPECTED_ERROR", 2],
        ["HTTP_400", 2],
        ["HTTP_401", 2],
        ["HTTP_403", 2],
        ["HTTP_404", 2],
        ["HTTP_409", 2],
        ["HTTP_500", 2],
        ["HTTP_502", 2],
        ["HTTP_503", 2],
        ["HTTP_504", 2],
    ];

    [Theory]
    [MemberData(nameof(DomainCodes))]
    public void Catalog_DomainCode_ExitsWithOneAndIncludesActionableSuggestion(
        string code,
        int expectedExitCode,
        string suggestionSubstring)
    {
        ErrorTranslation translation = ErrorMessageCatalog.Resolve(code);
        translation.ExitCode.ShouldBe(expectedExitCode);
        translation.CliSuggestion.ShouldNotBeNullOrWhiteSpace();
        translation.CliSuggestion.ShouldContain(suggestionSubstring);
    }

    [Theory]
    [MemberData(nameof(PlumbingCodes))]
    public void Catalog_PlumbingCode_ExitsWithTwo(string code, int expectedExitCode)
    {
        ErrorTranslation translation = ErrorMessageCatalog.Resolve(code);
        translation.ExitCode.ShouldBe(expectedExitCode);
        translation.CliSuggestion.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Resolve_UnknownCode_DefaultsToDomainExitOneWithVerboseHint()
    {
        ErrorTranslation translation = ErrorMessageCatalog.Resolve("SOME_FUTURE_CODE_NOT_IN_CATALOG");
        translation.ExitCode.ShouldBe(1);
        translation.CliMessage.ShouldBeNull();
        translation.CliSuggestion.ShouldBe(ErrorMessageCatalog.UnknownCodeSuggestion);
    }

    [Fact]
    public void Resolve_NullOrEmpty_DoesNotThrowAndDefaultsToOne()
    {
        ErrorMessageCatalog.Resolve(null).ExitCode.ShouldBe(1);
        ErrorMessageCatalog.Resolve(string.Empty).ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Catalog_GraphAndBackendUnavailable_ShareBothFatalAndDegradedContextWording()
    {
        // Story 7.3 Task 5.3 wording-context invariant: *_UNAVAILABLE text must read sensibly in BOTH
        // HTTP 503 fatal case AND per-axis-degraded context. Here we assert the wording does NOT make
        // fatal-only claims (which would mislead the per-axis warning surface).
        ErrorMessageCatalog.Resolve("GRAPH_UNAVAILABLE").CliSuggestion!
            .ShouldNotContain("all graph queries will fail", Case.Insensitive);
        ErrorMessageCatalog.Resolve("BACKEND_UNAVAILABLE").CliSuggestion!
            .ShouldNotContain("all queries will fail", Case.Insensitive);
    }
}
