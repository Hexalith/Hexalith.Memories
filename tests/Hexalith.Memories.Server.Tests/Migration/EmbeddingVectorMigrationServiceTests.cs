// <copyright file="EmbeddingVectorMigrationServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Migration;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;

using Shouldly;

/// <summary>Focused unit coverage for Story 13.6 embedding vector migration orchestration.</summary>
public sealed class EmbeddingVectorMigrationServiceTests
{
    [Theory]
    [InlineData("provider failed: AKIAEXAMPLE123456789 reset key", "AKIAEXAMPLE123456789")]
    [InlineData("temporary creds ASIA0123456789ABCDEF rotated", "ASIA0123456789ABCDEF")]
    public void RedactorShouldMaskAwsAccessKeyIds(string input, string secret)
    {
        string output = EmbeddingMigrationRedactor.Redact(input);

        output.ShouldNotContain(secret);
        output.ShouldContain("[redacted-aws-key]");
    }

    [Fact]
    public void RedactorShouldMaskRawJwtWithoutBearerPrefix()
    {
        // Realistic-shape JWT body so the test would have caught a JWT regex that only matched
        // when prefixed with "Bearer ".
        const string rawJwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ1c2VyIn0.signaturePartXYZ";
        string output = EmbeddingMigrationRedactor.Redact($"upstream returned token {rawJwt} for request");

        output.ShouldNotContain(rawJwt);
        output.ShouldContain("[redacted-jwt]");
    }

    [Theory]
    [InlineData("Basic")]
    [InlineData("basic")]
    [InlineData("BASIC")]
    [InlineData("bAsIc")]
    public void RedactorShouldMaskHttpBasicAuthorizationValues(string scheme)
    {
        // The literal value used by the embedded credential.
        const string basicValue = "dXNlcjpwYXNzd29yZA==";
        string output = EmbeddingMigrationRedactor.Redact($"Authorization: {scheme} {basicValue} forwarded.");

        output.ShouldNotContain(basicValue);
        output.ShouldContain("[redacted]");
    }

    [Theory]
    [InlineData("client_secret named memories-embedding-client-secret was not found", "memories-embedding-client-secret")]
    [InlineData("ApiSecretKeyName memories-embedding-client-secret missing", "memories-embedding-client-secret")]
    [InlineData("the secret 'memories-embedding-client-secret' could not be resolved", "memories-embedding-client-secret")]
    public void RedactorShouldPreserveNameOnlySecretReferences(string input, string benignName)
    {
        string output = EmbeddingMigrationRedactor.Redact(input);

        // Benign secret-name references must remain operator-visible per Story 14.4 AC #2.
        output.ShouldContain(benignName);
        output.ShouldNotContain("[redacted]");
    }

    [Fact]
    public void RedactorShouldKeepExistingGoogleAndBearerAndSecretFieldRedactions()
    {
        const string google = "AIzaFakeKeyExampleValue123";
        const string bearer = "eyJfake.bearer.body";
        const string secretValue = "super-secret-client-secret";
        string input = $"google={google} Authorization: Bearer {bearer} client_secret={secretValue}";

        string output = EmbeddingMigrationRedactor.Redact(input);

        output.ShouldNotContain(google);
        output.ShouldNotContain(bearer);
        output.ShouldNotContain(secretValue);
        output.ShouldContain("AIza[redacted]");
        output.ShouldContain("Bearer [redacted]");
        output.ShouldContain("client_secret=[redacted]");
    }

    [Fact]
    public void RedactorShouldMaskJsonEscapedSecretFields()
    {
        const string secretValue = "super-secret-json-value";
        string input = $"event payload {{\\\"client_secret\\\":\\\"{secretValue}\\\"}}";

        string output = EmbeddingMigrationRedactor.Redact(input);

        output.ShouldNotContain(secretValue);
        output.ShouldContain("[redacted]");
    }

    [Fact]
    public void RedactorShouldNotLeakSecretsAcrossTruncationBoundary()
    {
        // Build a >MaxMessageLength input whose AWS key sits within the first 512 chars and a JWT
        // straddling the boundary. The redactor must apply masks on the full input *before*
        // truncation so neither secret survives in the truncated output.
        string awsKey = "AKIA0123456789ABCDEF";
        string jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJib3VuZGFyeSJ9.bbbbbbbbbbbbbbbb";
        string preFiller = new string('p', 200);
        string straddleFiller = new string('s', 280); // AWS key at ~210, JWT at ~510 (straddles 512)
        string suffixFiller = new string('q', 600);
        string input = $"{preFiller} {awsKey} {straddleFiller} {jwt} {suffixFiller}";
        input.Length.ShouldBeGreaterThan(512);

        string output = EmbeddingMigrationRedactor.Redact(input);

        output.ShouldNotContain(awsKey);
        output.ShouldNotContain(jwt);
        output.EndsWith("...").ShouldBeTrue();
    }

    [Fact]
    public async Task LiveWithoutTenantShouldReturnPlumbingError()
    {
        FakeStore store = new();
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions { Mode = EmbeddingMigrationMode.Live, Yes = true },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Plumbing);
        result.Message.ShouldContain("--live requires --tenant");
        store.SetConfigCalls.ShouldBe(0);
    }

    [Fact]
    public async Task LiveWithoutYesShouldReturnPlumbingError()
    {
        FakeStore store = new();
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = false,
                Interactive = true,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Plumbing);
        result.Message.ShouldContain("--yes");
        store.SetConfigCalls.ShouldBe(0);
        store.DropAndRecreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task BatchSizeAboveCapShouldReturnPlumbingError()
    {
        FakeStore store = new();
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
                BatchSize = 50_000,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Plumbing);
        result.Message.ShouldContain("--batch-size");
        store.SetConfigCalls.ShouldBe(0);
    }

    [Fact]
    public async Task InvalidTargetDimensionsShouldReturnPlumbingError()
    {
        FakeStore store = new();
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
                TargetDimensions = -1,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Plumbing);
        result.Message.ShouldContain("Invalid target embedding configuration");
        store.SetConfigCalls.ShouldBe(0);
    }

    [Fact]
    public async Task DryRunShouldReportAffectedTenantsWithoutWrites()
    {
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(2, 2, 1, 2, 1);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions { Mode = EmbeddingMigrationMode.DryRun },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Success);
        result.Tenants.Count.ShouldBe(1);
        result.Tenants[0].TenantId.ShouldBe("tenant-a");
        result.Tenants[0].DimensionMismatch.ShouldBeTrue();
        store.SetConfigCalls.ShouldBe(0);
        store.DropAndRecreateCalls.ShouldBe(0);
        store.RawWrites.Count.ShouldBe(0);
        store.NaturalLanguageWrites.Count.ShouldBe(0);
        store.RecordedFailures.Count.ShouldBe(0);
    }

    [Fact]
    public async Task LiveMigrationShouldUpdateConfigRecreateIndexesSkipResumeAndRecordFailures()
    {
        TenantEmbeddingConfig target = EmbeddingProviderDefaults.Ollama();
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(3, 3, 2, 2, 1);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        store.SyntacticUnits["tenant-a"] =
        [
            new SyntacticMigrationUnit("mu-process", "raw text", "case-1", "subject-a"),
            new SyntacticMigrationUnit("mu-skip", "already migrated", "case-1", null),
            new SyntacticMigrationUnit("mu-fail", "throw-secret", "case-1", null),
        ];
        store.RawStates[("tenant-a", "mu-skip")] = new SemanticMigrationState(target.Provider, target.Model, target.Dimensions);
        store.NaturalLanguageUnits[("tenant-a", "mu-process")] = new NaturalLanguageMigrationUnit(
            "mu-process",
            "case-1",
            "business description",
            "ai",
            "0.95",
            "model",
            new SemanticMigrationState("google", "gemini-embedding-001", 768));
        store.NaturalLanguageUnits[("tenant-a", "mu-skip")] = new NaturalLanguageMigrationUnit(
            "mu-skip",
            "case-1",
            "already migrated description",
            "ai",
            "0.90",
            "model",
            new SemanticMigrationState(target.Provider, target.Model, target.Dimensions));
        store.MigrationMarkers["tenant-a"] = true;

        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
                Resume = true,
                BatchSize = 2,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.DomainError);
        store.MarkerStarted.ShouldBeTrue();
        store.MarkerCompleted.ShouldBeFalse();
        store.SetConfigCalls.ShouldBe(1);
        store.ForceReindexValues.ShouldBe([true]);
        store.DropAndRecreateCalls.ShouldBe(1);
        store.RawWrites.Select(w => w.MemoryUnitId).ShouldBe(["mu-process"]);
        store.NaturalLanguageWrites.Select(w => w.MemoryUnitId).ShouldBe(["mu-process"]);
        result.Tenants[0].Raw.ShouldBe(new EmbeddingMigrationUnitCounters(1, 1, 0, 1));
        result.Tenants[0].NaturalLanguage.ShouldBe(new EmbeddingMigrationUnitCounters(1, 1, 1, 0));
        result.Progress.Count.ShouldBe(4);
        result.Failures.Count.ShouldBe(1);
        result.Failures[0].Message.ShouldNotContain("super-secret-client-secret");
        result.Failures[0].Message.ShouldNotContain("Bearer eyJ");
        result.Failures[0].Message.ShouldNotContain("AIzaFake");
        result.Failures[0].Message.ShouldContain("[redacted]");
        store.RecordedFailures.ShouldBe(result.Failures);
    }

    [Fact]
    public async Task LiveMigrationShouldNotInventNaturalLanguageDefaults()
    {
        TenantEmbeddingConfig target = EmbeddingProviderDefaults.Ollama();
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(1, 0, 1, 0, 1);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        store.SyntacticUnits["tenant-a"] =
        [
            new SyntacticMigrationUnit("mu-1", "raw text", "case-1", null),
        ];
        store.NaturalLanguageUnits[("tenant-a", "mu-1")] = new NaturalLanguageMigrationUnit(
            "mu-1",
            "case-1",
            "the description",
            null,
            null,
            null,
            new SemanticMigrationState("google", "gemini-embedding-001", 768));

        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
                BatchSize = 1,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Success);
        store.NaturalLanguageWrites.Count.ShouldBe(1);
        store.NaturalLanguageWrites[0].DescriptionOrigin.ShouldBeNull();
        store.NaturalLanguageWrites[0].DescriptionConfidence.ShouldBeNull();
        store.NaturalLanguageWrites[0].DescriptionConfidenceSource.ShouldBeNull();
    }

    [Fact]
    public async Task ResumeDetectionShouldRequireExactProviderModelAndDimensions()
    {
        TenantEmbeddingConfig target = EmbeddingProviderDefaults.Ollama();
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(3, 3, 0, 3, 0);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        store.SyntacticUnits["tenant-a"] =
        [
            new SyntacticMigrationUnit("mu-stale-dim", "raw 1", "case-1", null),
            new SyntacticMigrationUnit("mu-stale-provider", "raw 2", "case-1", null),
            new SyntacticMigrationUnit("mu-fully-stamped", "raw 3", "case-1", null),
        ];
        store.RawStates[("tenant-a", "mu-stale-dim")] = new SemanticMigrationState(target.Provider, target.Model, 768);
        store.RawStates[("tenant-a", "mu-stale-provider")] = new SemanticMigrationState("google", target.Model, target.Dimensions);
        store.RawStates[("tenant-a", "mu-fully-stamped")] = new SemanticMigrationState(target.Provider, target.Model, target.Dimensions);

        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
                BatchSize = 2,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Success);
        store.RawWrites.Select(w => w.MemoryUnitId).ShouldBe(["mu-stale-dim", "mu-stale-provider"]);
        result.Tenants[0].Raw.ShouldBe(new EmbeddingMigrationUnitCounters(2, 1, 0, 0));
    }

    [Fact]
    public async Task BatchSizeBoundaryShouldEmitFinalProgressOnce()
    {
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(4, 0, 0, 0, 0);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        store.SyntacticUnits["tenant-a"] =
        [
            new SyntacticMigrationUnit("mu-1", "raw 1", "case-1", null),
            new SyntacticMigrationUnit("mu-2", "raw 2", "case-1", null),
            new SyntacticMigrationUnit("mu-3", "raw 3", "case-1", null),
            new SyntacticMigrationUnit("mu-4", "raw 4", "case-1", null),
        ];

        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
                BatchSize = 2,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Success);
        result.Progress.Count(p => p.ContentKind == "payload").ShouldBe(2);
    }

    [Fact]
    public async Task CancellationShouldReturnCancelledExitCode()
    {
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(2, 0, 0, 0, 0);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        store.SyntacticUnits["tenant-a"] =
        [
            new SyntacticMigrationUnit("mu-1", "raw 1", "case-1", null),
            new SyntacticMigrationUnit("mu-2", "raw 2", "case-1", null),
        ];

        using CancellationTokenSource cts = new();
        cts.Cancel();
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
                BatchSize = 1,
            },
            cts.Token);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Cancelled);
        result.Message.ShouldContain("cancelled", Shouldly.Case.Insensitive);
    }

    [Fact]
    public async Task RollbackWithoutRetainedPreviousIndexesShouldFailClosed()
    {
        FakeStore store = new();
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Rollback,
                TenantId = "tenant-a",
                Yes = true,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.DomainError);
        result.Message.ShouldContain("Rollback is unavailable");
        store.DropAndRecreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task RollbackWithRetainedPreviousIndexesShouldStillFailClosed()
    {
        FakeStore store = new();
        store.RetainedPreviousIndexes.Add("tenant-a");
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Rollback,
                TenantId = "tenant-a",
                Yes = true,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.DomainError);
        result.Message.ShouldContain("retained previous-version indexes");
        store.DropAndRecreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task NoModeSelectedShouldReturnPlumbingErrorWithActionableCliMessage()
    {
        FakeStore store = new();
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions { Yes = true },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Plumbing);
        result.Message.ShouldContain("--dry-run");
        result.Message.ShouldContain("--live");
        result.Message.ShouldContain("--rollback");
        store.SetConfigCalls.ShouldBe(0);
        store.DropAndRecreateCalls.ShouldBe(0);
        store.MarkerStarted.ShouldBeFalse();
    }

    [Fact]
    public async Task ResumeWithoutMarkerShouldReportTenantLevelDomainErrorWithoutCompletingMarker()
    {
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(0, 0, 0, 0, 0);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        // intentionally no MigrationMarkers entry — --resume should fail closed.
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
                Resume = true,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.DomainError);
        result.Message.ShouldContain("aborted on tenant-level error");
        result.Message.ShouldContain("--resume");
        result.Failures.Count.ShouldBe(1);
        result.Failures[0].ContentKind.ShouldBe("tenant");
        result.Failures[0].ErrorCategory.ShouldBe("InvalidOperation");
        // CompleteMigrationMarkerAsync MUST NOT run when start failed; otherwise we would
        // stamp a "completed" marker over a non-existent one.
        store.MarkerStarted.ShouldBeFalse();
        store.MarkerCompleted.ShouldBeFalse();
        store.SetConfigCalls.ShouldBe(0);
        store.DropAndRecreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task TenantLevelErrorShouldRecordFailureAndReturnDomainError()
    {
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(0, 0, 0, 0, 0);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        store.DropAndRecreateException = new InvalidOperationException("redis offline");
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.DomainError);
        result.Message.ShouldContain("aborted on tenant-level error");
        store.RecordedFailures.Count.ShouldBe(1);
        store.RecordedFailures[0].ContentKind.ShouldBe("tenant");
        store.MarkerStarted.ShouldBeTrue();
        store.MarkerCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task CompleteMarkerFailureShouldReturnDomainErrorWithoutEscaping()
    {
        FakeStore store = new();
        store.Tenants.Add("tenant-a");
        store.Configs["tenant-a"] = EmbeddingProviderDefaults.Google();
        store.Counts["tenant-a"] = new EmbeddingMigrationTenantCounts(0, 0, 0, 0, 0);
        store.Indexes["tenant-a"] = new EmbeddingMigrationIndexInfo(768, 768);
        store.CompleteMarkerException = new InvalidOperationException("marker write failed");
        EmbeddingVectorMigrationService service = new(store, new FakeVectorGenerator());

        EmbeddingMigrationResult result = await service.RunAsync(
            new EmbeddingMigrationOptions
            {
                Mode = EmbeddingMigrationMode.Live,
                TenantId = "tenant-a",
                Yes = true,
            },
            CancellationToken.None);

        result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.DomainError);
        result.Message.ShouldContain("aborted on tenant-level error");
        result.Message.ShouldContain("marker write failed");
        store.MarkerStarted.ShouldBeTrue();
        store.MarkerCompleted.ShouldBeFalse();
        store.RecordedFailures.Count.ShouldBe(1);
        store.RecordedFailures[0].ContentKind.ShouldBe("tenant");
    }

    private sealed class FakeVectorGenerator : IEmbeddingMigrationVectorGenerator
    {
        public Task<float[]> GenerateAsync(string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
        {
            if (text == "throw-secret")
            {
                throw new InvalidOperationException(
                    "provider failed: client_secret=super-secret-client-secret Authorization: Bearer eyJabcdef AIzaFake123456789");
            }

            return Task.FromResult(Enumerable.Repeat(0.1f, config.Dimensions).ToArray());
        }
    }

    private sealed class FakeStore : IEmbeddingMigrationStore
    {
        public List<string> Tenants { get; } = [];

        public Dictionary<string, TenantEmbeddingConfig> Configs { get; } = [];

        public Dictionary<string, EmbeddingMigrationTenantCounts> Counts { get; } = [];

        public Dictionary<string, EmbeddingMigrationIndexInfo> Indexes { get; } = [];

        public Dictionary<string, List<SyntacticMigrationUnit>> SyntacticUnits { get; } = [];

        public Dictionary<(string TenantId, string MemoryUnitId), SemanticMigrationState> RawStates { get; } = [];

        public Dictionary<(string TenantId, string MemoryUnitId), NaturalLanguageMigrationUnit> NaturalLanguageUnits { get; } = [];

        public List<RawSemanticMigrationWrite> RawWrites { get; } = [];

        public List<NaturalLanguageSemanticMigrationWrite> NaturalLanguageWrites { get; } = [];

        public List<EmbeddingMigrationUnitFailure> RecordedFailures { get; } = [];

        public List<bool> ForceReindexValues { get; } = [];

        public Dictionary<string, bool> MigrationMarkers { get; } = [];

        public HashSet<string> RetainedPreviousIndexes { get; } = [];

        public Exception? DropAndRecreateException { get; set; }

        public Exception? CompleteMarkerException { get; set; }

        public int SetConfigCalls { get; private set; }

        public int DropAndRecreateCalls { get; private set; }

        public bool MarkerStarted { get; private set; }

        public bool MarkerCompleted { get; private set; }

        public Task<IReadOnlyList<string>> ListTenantIdsAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>(Tenants);

        public Task<TenantEmbeddingConfig> GetEmbeddingConfigAsync(string tenantId, CancellationToken ct)
            => Task.FromResult(Configs.GetValueOrDefault(tenantId) ?? EmbeddingProviderDefaults.Google());

        public Task SetEmbeddingConfigAsync(string tenantId, TenantEmbeddingConfig config, bool forceReindex, CancellationToken ct)
        {
            Configs[tenantId] = config;
            SetConfigCalls++;
            ForceReindexValues.Add(forceReindex);
            return Task.CompletedTask;
        }

        public Task<EmbeddingMigrationTenantCounts> GetCountsAsync(string tenantId, TenantEmbeddingConfig targetConfig, CancellationToken ct)
            => Task.FromResult(Counts.GetValueOrDefault(tenantId) ?? new EmbeddingMigrationTenantCounts(0, 0, 0, 0, 0));

        public Task<EmbeddingMigrationIndexInfo> GetIndexInfoAsync(string tenantId, CancellationToken ct)
            => Task.FromResult(Indexes.GetValueOrDefault(tenantId) ?? new EmbeddingMigrationIndexInfo(null, null));

        public Task DropAndRecreateSemanticIndexesAsync(string tenantId, int dimensions, CancellationToken ct)
        {
            DropAndRecreateCalls++;
            if (DropAndRecreateException is not null)
            {
                throw DropAndRecreateException;
            }

            return Task.CompletedTask;
        }

        public Task StartMigrationMarkerAsync(string tenantId, TenantEmbeddingConfig targetConfig, bool resume, CancellationToken ct)
        {
            if (resume && !MigrationMarkers.GetValueOrDefault(tenantId))
            {
                throw new InvalidOperationException("--resume specified but no prior migration marker exists.");
            }

            MarkerStarted = true;
            MigrationMarkers[tenantId] = true;
            return Task.CompletedTask;
        }

        public Task CompleteMigrationMarkerAsync(string tenantId, TenantEmbeddingConfig targetConfig, CancellationToken ct)
        {
            if (CompleteMarkerException is not null)
            {
                throw CompleteMarkerException;
            }

            MarkerCompleted = true;
            return Task.CompletedTask;
        }

        public Task RecordFailureAsync(EmbeddingMigrationUnitFailure failure, CancellationToken ct)
        {
            RecordedFailures.Add(failure);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<SyntacticMigrationUnit> EnumerateSyntacticUnitsAsync(string tenantId, int pageSize, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            foreach (SyntacticMigrationUnit unit in SyntacticUnits.GetValueOrDefault(tenantId) ?? [])
            {
                ct.ThrowIfCancellationRequested();
                yield return unit;
            }

            await Task.CompletedTask;
        }

        public Task<SemanticMigrationState?> GetRawSemanticStateAsync(string tenantId, string memoryUnitId, CancellationToken ct)
            => Task.FromResult(RawStates.GetValueOrDefault((tenantId, memoryUnitId)));

        public Task<NaturalLanguageMigrationUnit?> GetNaturalLanguageSemanticUnitAsync(string tenantId, string memoryUnitId, CancellationToken ct)
            => Task.FromResult(NaturalLanguageUnits.GetValueOrDefault((tenantId, memoryUnitId)));

        public Task WriteRawSemanticAsync(string tenantId, TenantEmbeddingConfig targetConfig, RawSemanticMigrationWrite write, CancellationToken ct)
        {
            RawWrites.Add(write);
            RawStates[(tenantId, write.MemoryUnitId)] = new SemanticMigrationState(targetConfig.Provider, targetConfig.Model, targetConfig.Dimensions);
            return Task.CompletedTask;
        }

        public Task WriteNaturalLanguageSemanticAsync(string tenantId, TenantEmbeddingConfig targetConfig, NaturalLanguageSemanticMigrationWrite write, CancellationToken ct)
        {
            NaturalLanguageWrites.Add(write);
            return Task.CompletedTask;
        }

        public Task<bool> HasRetainedPreviousVersionIndexesAsync(string tenantId, CancellationToken ct)
            => Task.FromResult(RetainedPreviousIndexes.Contains(tenantId));
    }
}
