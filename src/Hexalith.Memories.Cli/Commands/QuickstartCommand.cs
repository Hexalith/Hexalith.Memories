// <copyright file="QuickstartCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Memories.Cli.Errors;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;
using Hexalith.Memories.Cli.Quickstart;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builds <c>memories quickstart</c>. Story 7.4 ships the six-step onboarding wizard (prereqs,
/// boot-command hint, health probe, tenant provision, sample ingest, validation search).
/// </summary>
public static class QuickstartCommand
{
    /// <summary>Command name used in JSON error envelopes (ADR-7.3-002).</summary>
    public const string CommandName = "quickstart";

    /// <summary>Total-timeout default for step 3 health probe.</summary>
    public static readonly TimeSpan DefaultHealthTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Poll interval for step 3 health probe.</summary>
    public static readonly TimeSpan DefaultHealthPollInterval = TimeSpan.FromSeconds(1);

    /// <summary>Command description — must satisfy the NFR30 audit (Task 9).</summary>
    public const string CommandDescription = """
Guided quickstart: verify prerequisites, print the stack boot command, probe server health, provision a sample tenant, ingest a sample document, and run a validation search.

Examples:
    memories quickstart
    memories quickstart --tenant acme-quickstart
    memories quickstart --dry-run --format json
    memories quickstart --skip-prereq-check --skip-boot-check
    memories quickstart --tenant-timeout-seconds 120
""";

    private const string BootCommand = "dotnet run --project src/Hexalith.Memories.AppHost";

    private const int TotalSteps = 6;

    private static readonly string[] StepTitles =
    [
        "Verifying prerequisites",
        "Printing stack boot command",
        "Probing server health",
        "Provisioning sample tenant",
        "Ingesting sample document",
        "Running validation search",
    ];

    /// <summary>Builds the <c>quickstart</c> subcommand.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <returns>The configured command.</returns>
    public static Command Build(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tenantOption = new Option<string?>("--tenant")
        {
            Description = "Tenant identifier for the sample (default: 'quickstart-YYYYMMDD' in UTC — collides across runs on the same day by design for idempotent rerun per ADR-7.4-004; pass a unique id if running against a shared endpoint).",
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return null;
                }

                string raw = result.Tokens[0].Value;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    result.AddError("--tenant must be a non-empty identifier (omit the flag to use the default 'quickstart-YYYYMMDD').");
                    return null;
                }

                return raw;
            },
        };
        var skipBootOption = new Option<bool>("--skip-boot-check")
        {
            Description = "Skip the server-reachability probe (step 3). Useful when a fixture already guarantees the server is up.",
        };
        var skipPrereqOption = new Option<bool>("--skip-prereq-check")
        {
            Description = "Skip the Docker/.NET/port prerequisite block (step 1). Useful in container-based CI where Docker-on-Docker is unavailable.",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Print every step and the exact action it would perform without mutating any state. Exits 0.",
        };
        var tenantTimeoutOption = new Option<int?>("--tenant-timeout-seconds")
        {
            Description = "Maximum seconds to wait for sample tenant provisioning to become Active (default: 30).",
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return null;
                }

                string raw = result.Tokens[0].Value;
                if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int seconds) || seconds <= 0)
                {
                    result.AddError("--tenant-timeout-seconds must be a positive integer.");
                    return null;
                }

                return seconds;
            },
        };

        var command = new Command("quickstart", CommandDescription)
        {
            tenantOption,
            skipBootOption,
            skipPrereqOption,
            dryRunOption,
            tenantTimeoutOption,
        };

        command.SetAction((parseResult, ct) => ExecuteAsync(
            services,
            new QuickstartOptions(
                TenantId: parseResult.GetValue(tenantOption),
                SkipBootCheck: parseResult.GetValue(skipBootOption),
                SkipPrereqCheck: parseResult.GetValue(skipPrereqOption),
                DryRun: parseResult.GetValue(dryRunOption),
                TenantProvisionTimeout: parseResult.GetValue(tenantTimeoutOption) is int tenantTimeoutSeconds
                    ? TimeSpan.FromSeconds(tenantTimeoutSeconds)
                    : null),
            ct));

        return command;
    }

    internal static async Task<int> ExecuteAsync(IServiceProvider services, QuickstartOptions options, CancellationToken ct)
    {
        CliCommandExecutor executor = services.GetRequiredService<CliCommandExecutor>();
        CliConsole console = services.GetRequiredService<CliConsole>();
        PrerequisiteChecks prereq = services.GetRequiredService<PrerequisiteChecks>();

        return await executor.ExecuteAsync(CommandName, async (config, innerCt) =>
        {
            HealthProbe probe = services.GetRequiredService<HealthProbe>();
            QuickstartTenantProvisioner provisioner = services.GetRequiredService<QuickstartTenantProvisioner>();
            QuickstartSampleFlow sampleFlow = services.GetRequiredService<QuickstartSampleFlow>();

            string tenantId = options.TenantId ?? $"quickstart-{DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

            long startTimestamp = Stopwatch.GetTimestamp();
            var results = new List<QuickstartStepResult>(TotalSteps);
            string? sampleCaseId = null;
            string? sampleRunToken = null;
            int? triggeringStepId = null;

            for (int stepId = 1; stepId <= TotalSteps; stepId++)
            {
                innerCt.ThrowIfCancellationRequested();

                // Step 2 is pure stdout — always runs regardless of upstream failures. Step 6's
                // null-run-token cascade (spec Task 6.3) is handled inside RunValidationStepAsync
                // rather than by the generic short-circuit so the spec-pinned message surfaces.
                if (triggeringStepId is int failedAt && stepId != 2 && stepId != 6)
                {
                    var skipped = new QuickstartStepResult(
                        Id: stepId,
                        Title: StepTitles[stepId - 1],
                        Status: QuickstartStepStatus.Skip,
                        Duration: TimeSpan.Zero,
                        Message: $"Skipped due to upstream failure at step {failedAt}.",
                        Suggestion: null,
                        ErrorCode: null);
                    results.Add(skipped);
                    EmitStepProgress(console, skipped);
                    continue;
                }

                QuickstartStepResult stepResult = stepId switch
                {
                    1 => await RunPrereqStepAsync(console, prereq, options, innerCt).ConfigureAwait(false),
                    2 => RunBootCommandStep(console, options),
                    3 => await RunHealthStepAsync(console, probe, options, innerCt).ConfigureAwait(false),
                    4 => await RunTenantStepAsync(console, provisioner, tenantId, options, innerCt).ConfigureAwait(false),
                    5 => await RunIngestStepAsync(console, sampleFlow, tenantId, options, innerCt, r => { sampleCaseId = r.CaseId; sampleRunToken = r.RunToken; }).ConfigureAwait(false),
                    6 => await RunValidationStepAsync(console, sampleFlow, tenantId, sampleCaseId, sampleRunToken, options, innerCt).ConfigureAwait(false),
                    _ => throw new InvalidOperationException("Unreachable step id."),
                };

                results.Add(stepResult);

                if (stepResult.Status == QuickstartStepStatus.Fail && triggeringStepId is null)
                {
                    triggeringStepId = stepId;
                }
            }

            int elapsedMs = (int)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            string overallStatus = results.Any(r => r.Status == QuickstartStepStatus.Fail) ? "fail" : "ok";
            var envelope = new QuickstartEnvelopeData(results, overallStatus, elapsedMs);

            EmitFinalOutput(console, envelope);

            int exitCode = ComputeExitCode(envelope);
            return exitCode;
        }, ct).ConfigureAwait(false);
    }

    private static int ComputeExitCode(QuickstartEnvelopeData envelope)
    {
        if (envelope.OverallStatus == "ok")
        {
            return CliExitCodes.Success;
        }

        int highest = CliExitCodes.DomainError;
        foreach (QuickstartStepResult step in envelope.Steps)
        {
            if (step.Status != QuickstartStepStatus.Fail)
            {
                continue;
            }

            int candidate = step.ErrorCode is null
                ? CliExitCodes.Plumbing
                : ErrorMessageCatalog.Resolve(step.ErrorCode).ExitCode;
            if (candidate > highest)
            {
                highest = candidate;
            }
        }

        return highest;
    }

    private static async Task<QuickstartStepResult> RunPrereqStepAsync(
        CliConsole console,
        PrerequisiteChecks prereq,
        QuickstartOptions options,
        CancellationToken ct)
    {
        EmitStepHeader(console, stepId: 1);

        if (options.SkipPrereqCheck)
        {
            var skip = new QuickstartStepResult(
                Id: 1,
                Title: StepTitles[0],
                Status: QuickstartStepStatus.Skip,
                Duration: TimeSpan.Zero,
                Message: "Skipped via --skip-prereq-check.",
                Suggestion: null,
                ErrorCode: null);
            EmitStepProgress(console, skip);
            return skip;
        }

        if (options.DryRun)
        {
            return DryRunResult(
                console,
                stepId: 1,
                message: "Would run Docker, .NET SDK, port, OS, and DAPR CLI checks.");
        }

        long startTimestamp = Stopwatch.GetTimestamp();

        // Run the sub-checks sequentially; surface each sub-check diagnostic as a nested stdout line.
        PrerequisiteCheckResult docker = await prereq.CheckDockerAsync(ct).ConfigureAwait(false);
        WriteSubCheckLine(console, 1, "Docker", docker);
        if (!docker.Passed)
        {
            return BuildFail(1, docker, Stopwatch.GetElapsedTime(startTimestamp), "DOCKER_UNAVAILABLE", console);
        }

        PrerequisiteCheckResult dotnet = await prereq.CheckDotnetSdkAsync(ct).ConfigureAwait(false);
        WriteSubCheckLine(console, 1, ".NET SDK", dotnet);
        if (!dotnet.Passed)
        {
            return BuildFail(1, dotnet, Stopwatch.GetElapsedTime(startTimestamp), "DOTNET_VERSION_INSUFFICIENT", console);
        }

        PrerequisiteCheckResult ports = await prereq
            .CheckPortAvailabilityAsync(PrerequisiteChecks.DefaultPorts, ct)
            .ConfigureAwait(false);
        WriteSubCheckLine(console, 1, "Ports", ports);
        if (!ports.Passed)
        {
            return BuildFail(1, ports, Stopwatch.GetElapsedTime(startTimestamp), "PORT_IN_USE", console);
        }

        PrerequisiteCheckResult os = prereq.CheckOsPlatform();
        WriteSubCheckLine(console, 1, "OS", os);

        PrerequisiteCheckResult dapr = await prereq.CheckDaprCliAsync(ct).ConfigureAwait(false);
        WriteSubCheckLine(console, 1, "DAPR CLI", dapr);

        var ok = new QuickstartStepResult(
            Id: 1,
            Title: StepTitles[0],
            Status: QuickstartStepStatus.Ok,
            Duration: Stopwatch.GetElapsedTime(startTimestamp),
            Message: "All prerequisites satisfied.",
            Suggestion: null,
            ErrorCode: null);
        EmitStepProgress(console, ok);
        return ok;
    }

    private static QuickstartStepResult RunBootCommandStep(CliConsole console, QuickstartOptions options)
    {
        EmitStepHeader(console, stepId: 2);

        string message = $"Run in a dedicated terminal: {BootCommand}";
        QuickstartStepStatus status = options.DryRun ? QuickstartStepStatus.DryRun : QuickstartStepStatus.Ok;

        var result = new QuickstartStepResult(
            Id: 2,
            Title: StepTitles[1],
            Status: status,
            Duration: TimeSpan.Zero,
            Message: message,
            Suggestion: null,
            ErrorCode: null);
        EmitStepProgress(console, result);
        return result;
    }

    private static async Task<QuickstartStepResult> RunHealthStepAsync(
        CliConsole console,
        HealthProbe probe,
        QuickstartOptions options,
        CancellationToken ct)
    {
        EmitStepHeader(console, stepId: 3);

        if (options.SkipBootCheck)
        {
            var skip = new QuickstartStepResult(
                Id: 3,
                Title: StepTitles[2],
                Status: QuickstartStepStatus.Skip,
                Duration: TimeSpan.Zero,
                Message: "Skipped via --skip-boot-check.",
                Suggestion: null,
                ErrorCode: null);
            EmitStepProgress(console, skip);
            return skip;
        }

        if (options.DryRun)
        {
            return DryRunResult(
                console,
                stepId: 3,
                message: $"Would poll /health at {DefaultHealthPollInterval.TotalSeconds:F0}s intervals for up to {DefaultHealthTimeout.TotalSeconds:F0}s.");
        }

        HealthProbeResult result = await probe
            .WaitForReadyAsync(DefaultHealthTimeout, DefaultHealthPollInterval, ct)
            .ConfigureAwait(false);

        if (result.Ready)
        {
            var ok = new QuickstartStepResult(
                Id: 3,
                Title: StepTitles[2],
                Status: QuickstartStepStatus.Ok,
                Duration: result.Elapsed,
                Message: $"Server ready ({result.Elapsed.TotalMilliseconds:F0}ms).",
                Suggestion: null,
                ErrorCode: null);
            EmitStepProgress(console, ok);
            return ok;
        }

        string suggestion =
            $"Server did not become ready within {DefaultHealthTimeout.TotalSeconds:F0}s. "
            + $"Verify '{BootCommand}' is running in another terminal. "
            + "If the AppHost is running but on a different port (Aspire Testing fixtures randomize ports), "
            + "check the Aspire dashboard for the 'memories' port and re-run with '--endpoint http://localhost:<port>'."
            + (result.LastError is null ? string.Empty : $" Last probe error: {result.LastError}");

        var fail = new QuickstartStepResult(
            Id: 3,
            Title: StepTitles[2],
            Status: QuickstartStepStatus.Fail,
            Duration: result.Elapsed,
            Message: $"Server did not become ready within {DefaultHealthTimeout.TotalSeconds:F0}s.",
            Suggestion: suggestion,
            ErrorCode: "SERVER_NOT_READY");
        EmitStepProgress(console, fail);
        return fail;
    }

    private static async Task<QuickstartStepResult> RunTenantStepAsync(
        CliConsole console,
        QuickstartTenantProvisioner provisioner,
        string tenantId,
        QuickstartOptions options,
        CancellationToken ct)
    {
        EmitStepHeader(console, stepId: 4);

        if (options.DryRun)
        {
            return DryRunResult(
                console,
                stepId: 4,
                message: $"Would POST /api/v1/tenants with id='{tenantId}'.");
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            QuickstartTenantResult result = await provisioner
                .EnsureSampleTenantAsync(
                    tenantId,
                    options.TenantProvisionTimeout ?? QuickstartTenantProvisioner.DefaultProvisionTimeout,
                    ct)
                .ConfigureAwait(false);

            TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            if (result.AlreadyExisted && result.ErrorCode is null)
            {
                var skip = new QuickstartStepResult(
                    Id: 4,
                    Title: StepTitles[3],
                    Status: QuickstartStepStatus.Skip,
                    Duration: elapsed,
                    Message: result.Diagnostic,
                    Suggestion: null,
                    ErrorCode: null);
                EmitStepProgress(console, skip);
                return skip;
            }

            if (result.ErrorCode is not null)
            {
                ErrorTranslation translation = ErrorMessageCatalog.Resolve(result.ErrorCode);
                var fail = new QuickstartStepResult(
                    Id: 4,
                    Title: StepTitles[3],
                    Status: QuickstartStepStatus.Fail,
                    Duration: elapsed,
                    Message: result.Diagnostic,
                    Suggestion: translation.CliSuggestion ?? ErrorMessageCatalog.UnknownCodeSuggestion,
                    ErrorCode: result.ErrorCode);
                EmitStepProgress(console, fail);
                return fail;
            }

            var ok = new QuickstartStepResult(
                Id: 4,
                Title: StepTitles[3],
                Status: QuickstartStepStatus.Ok,
                Duration: elapsed,
                Message: result.Diagnostic,
                Suggestion: null,
                ErrorCode: null);
            EmitStepProgress(console, ok);
            return ok;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (MemoriesRemoteException ex)
        {
            return BuildRemoteFail(4, StepTitles[3], ex, Stopwatch.GetElapsedTime(startTimestamp), console);
        }
        catch (Exception ex)
        {
            return BuildExceptionFail(4, StepTitles[3], ex, Stopwatch.GetElapsedTime(startTimestamp), "provisioning the sample tenant", console);
        }
    }

    private static async Task<QuickstartStepResult> RunIngestStepAsync(
        CliConsole console,
        QuickstartSampleFlow sampleFlow,
        string tenantId,
        QuickstartOptions options,
        CancellationToken ct,
        Action<SampleIngestResult> captureResult)
    {
        EmitStepHeader(console, stepId: 5);

        if (options.DryRun)
        {
            return DryRunResult(
                console,
                stepId: 5,
                message: $"Would POST /api/v1/ingest for tenant '{tenantId}' with the embedded sample document.");
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            SampleIngestResult result = await sampleFlow
                .IngestSampleAsync(tenantId, ct)
                .ConfigureAwait(false);
            captureResult(result);

            TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            if (!result.Success)
            {
                ErrorTranslation translation = ErrorMessageCatalog.Resolve(result.ErrorCode);
                var fail = new QuickstartStepResult(
                    Id: 5,
                    Title: StepTitles[4],
                    Status: QuickstartStepStatus.Fail,
                    Duration: elapsed,
                    Message: result.Diagnostic,
                    Suggestion: translation.CliSuggestion ?? ErrorMessageCatalog.UnknownCodeSuggestion,
                    ErrorCode: result.ErrorCode);
                EmitStepProgress(console, fail);
                return fail;
            }

            var ok = new QuickstartStepResult(
                Id: 5,
                Title: StepTitles[4],
                Status: QuickstartStepStatus.Ok,
                Duration: elapsed,
                Message: result.Diagnostic,
                Suggestion: null,
                ErrorCode: null);
            EmitStepProgress(console, ok);
            return ok;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (MemoriesRemoteException ex)
        {
            return BuildRemoteFail(5, StepTitles[4], ex, Stopwatch.GetElapsedTime(startTimestamp), console);
        }
        catch (Exception ex)
        {
            return BuildExceptionFail(5, StepTitles[4], ex, Stopwatch.GetElapsedTime(startTimestamp), "ingesting the sample document", console);
        }
    }

    private static async Task<QuickstartStepResult> RunValidationStepAsync(
        CliConsole console,
        QuickstartSampleFlow sampleFlow,
        string tenantId,
        string? sampleCaseId,
        string? runToken,
        QuickstartOptions options,
        CancellationToken ct)
    {
        EmitStepHeader(console, stepId: 6);

        if (options.DryRun)
        {
            return DryRunResult(
                console,
                stepId: 6,
                message: $"Would run hybrid search for '{QuickstartSampleFlow.ValidationQuery}' against tenant '{tenantId}'.");
        }

        if (string.IsNullOrEmpty(sampleCaseId) || string.IsNullOrEmpty(runToken))
        {
            var skip = new QuickstartStepResult(
                Id: 6,
                Title: StepTitles[5],
                Status: QuickstartStepStatus.Skip,
                Duration: TimeSpan.Zero,
                Message: "Skipped: no sample memory unit id from upstream step 5.",
                Suggestion: null,
                ErrorCode: null);
            EmitStepProgress(console, skip);
            return skip;
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            SampleValidationResult result = await sampleFlow
                .ValidateSearchAsync(tenantId, sampleCaseId, runToken, ct)
                .ConfigureAwait(false);

            TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            if (!result.Success)
            {
                string errorCode = result.FailureKind switch
                {
                    SampleValidationFailureKind.PositiveReturnedZero => "SAMPLE_VALIDATION_ZERO_RESULTS",
                    SampleValidationFailureKind.NegativeCanaryReturnedResults => "SAMPLE_VALIDATION_CANARY_NONZERO_RESULTS",
                    SampleValidationFailureKind.NegativeCanaryError => "SAMPLE_VALIDATION_CANARY_ERROR",
                    _ => "SAMPLE_VALIDATION_ZERO_RESULTS",
                };

                string suggestion = result.FailureKind switch
                {
                    SampleValidationFailureKind.PositiveReturnedZero =>
                        $"Run 'memories search query --tenant {tenantId} --query \"{QuickstartSampleFlow.ValidationQuery}\"' in a few seconds to retry manually. Check server logs for ingestion errors.",
                    SampleValidationFailureKind.NegativeCanaryReturnedResults =>
                        $"Check server logs for index corruption or a misconfigured fusion algorithm. Run 'memories search query --tenant {tenantId} --axis syntactic --query \"{QuickstartSampleFlow.NegativeCanaryQuery}\" --explain' to inspect the lexical match path.",
                    SampleValidationFailureKind.NegativeCanaryError =>
                        $"Retry the canary search for tenant '{tenantId}' in a few seconds and inspect server logs if the failure persists.",
                    _ =>
                        "Run with --verbose for diagnostic detail.",
                };

                var fail = new QuickstartStepResult(
                    Id: 6,
                    Title: StepTitles[5],
                    Status: QuickstartStepStatus.Fail,
                    Duration: elapsed,
                    Message: result.Diagnostic,
                    Suggestion: suggestion,
                    ErrorCode: errorCode);
                EmitStepProgress(console, fail);
                return fail;
            }

            var ok = new QuickstartStepResult(
                Id: 6,
                Title: StepTitles[5],
                Status: QuickstartStepStatus.Ok,
                Duration: elapsed,
                Message: result.Diagnostic,
                Suggestion: null,
                ErrorCode: null);
            EmitStepProgress(console, ok);
            return ok;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (MemoriesRemoteException ex)
        {
            return BuildRemoteFail(6, StepTitles[5], ex, Stopwatch.GetElapsedTime(startTimestamp), console);
        }
        catch (Exception ex)
        {
            return BuildExceptionFail(6, StepTitles[5], ex, Stopwatch.GetElapsedTime(startTimestamp), "running the validation search", console);
        }
    }

    private static QuickstartStepResult BuildFail(
        int stepId,
        PrerequisiteCheckResult subCheck,
        TimeSpan duration,
        string errorCode,
        CliConsole console)
    {
        var fail = new QuickstartStepResult(
            Id: stepId,
            Title: StepTitles[stepId - 1],
            Status: QuickstartStepStatus.Fail,
            Duration: duration,
            Message: subCheck.Diagnostic,
            Suggestion: subCheck.RecoverySuggestion ?? ErrorMessageCatalog.UnknownCodeSuggestion,
            ErrorCode: errorCode);
        EmitStepProgress(console, fail);
        return fail;
    }

    private static QuickstartStepResult BuildRemoteFail(
        int stepId,
        string title,
        MemoriesRemoteException ex,
        TimeSpan duration,
        CliConsole console)
    {
        ErrorTranslation translation = ErrorMessageCatalog.Resolve(ex.Error.Code);
        var fail = new QuickstartStepResult(
            Id: stepId,
            Title: title,
            Status: QuickstartStepStatus.Fail,
            Duration: duration,
            Message: translation.CliMessage ?? ex.Error.Message,
            Suggestion: translation.CliSuggestion ?? ex.Error.Suggestion,
            ErrorCode: ex.Error.Code);
        EmitStepProgress(console, fail);
        return fail;
    }

    private static QuickstartStepResult BuildCatalogFail(
        int stepId,
        string title,
        TimeSpan duration,
        string errorCode,
        string message,
        CliConsole console)
    {
        ErrorTranslation translation = ErrorMessageCatalog.Resolve(errorCode);
        var fail = new QuickstartStepResult(
            Id: stepId,
            Title: title,
            Status: QuickstartStepStatus.Fail,
            Duration: duration,
            Message: message,
            Suggestion: translation.CliSuggestion ?? ErrorMessageCatalog.UnknownCodeSuggestion,
            ErrorCode: errorCode);
        EmitStepProgress(console, fail);
        return fail;
    }

    private static QuickstartStepResult BuildExceptionFail(
        int stepId,
        string title,
        Exception exception,
        TimeSpan duration,
        string operation,
        CliConsole console)
        => exception switch
        {
            TaskCanceledException => BuildCatalogFail(
                stepId,
                title,
                duration,
                "REQUEST_TIMEOUT",
                $"Timed out while {operation}.",
                console),
            HttpRequestException http when IsConnectionRefused(http) => BuildCatalogFail(
                stepId,
                title,
                duration,
                "CONNECTION_REFUSED",
                $"Cannot connect to Memories Server while {operation}.",
                console),
            HttpRequestException => BuildCatalogFail(
                stepId,
                title,
                duration,
                "UNEXPECTED_ERROR",
                $"HTTP request failed while {operation}.",
                console),
            SocketException socket when IsConnectionRefused(socket) => BuildCatalogFail(
                stepId,
                title,
                duration,
                "CONNECTION_REFUSED",
                $"Cannot connect to Memories Server while {operation}.",
                console),
            SocketException socket => BuildCatalogFail(
                stepId,
                title,
                duration,
                "UNEXPECTED_ERROR",
                $"Network error while {operation}: {socket.SocketErrorCode}.",
                console),
            AuthenticationException => BuildCatalogFail(
                stepId,
                title,
                duration,
                "TLS_ERROR",
                $"TLS validation failed while {operation}.",
                console),
            UriFormatException => BuildCatalogFail(
                stepId,
                title,
                duration,
                "INVALID_ENDPOINT",
                $"Configured endpoint is not a valid URI while {operation}.",
                console),
            _ => BuildCatalogFail(
                stepId,
                title,
                duration,
                "UNEXPECTED_ERROR",
                $"Unexpected error while {operation}: {exception.GetType().Name}.",
                console),
        };

    private static QuickstartStepResult DryRunResult(CliConsole console, int stepId, string message)
    {
        var result = new QuickstartStepResult(
            Id: stepId,
            Title: StepTitles[stepId - 1],
            Status: QuickstartStepStatus.DryRun,
            Duration: TimeSpan.Zero,
            Message: message,
            Suggestion: null,
            ErrorCode: null);
        EmitStepProgress(console, result);
        return result;
    }

    private static void EmitStepHeader(CliConsole console, int stepId)
    {
        if (console.Format != OutputFormat.Human)
        {
            return;
        }

        console.Out.WriteLine($"[{stepId}/{TotalSteps}] {StepTitles[stepId - 1]}");
    }

    private static void EmitStepProgress(CliConsole console, QuickstartStepResult step)
    {
        if (console.Format == OutputFormat.Json)
        {
            return;
        }

        string label = StatusLabel(step.Status);
        string line = $"[{step.Id}/{TotalSteps}] {label}: {step.Message}";

        if (console.Format == OutputFormat.Table)
        {
            // Table mode: per-step detail line goes to stderr so the stdout table stays pipe-friendly.
            console.Error.WriteLine(line);
        }
        else
        {
            console.Out.WriteLine(line);
        }

        if (!string.IsNullOrEmpty(step.Suggestion) && step.Status == QuickstartStepStatus.Fail)
        {
            string suggestionLine = $"  Suggestion: {step.Suggestion}";
            if (console.Format == OutputFormat.Table)
            {
                console.Error.WriteLine(suggestionLine);
            }
            else
            {
                console.Out.WriteLine(suggestionLine);
            }
        }
    }

    private static void WriteSubCheckLine(CliConsole console, int stepId, string name, PrerequisiteCheckResult result)
    {
        if (console.Format != OutputFormat.Human)
        {
            return;
        }

        string status = result.IsSkipped ? "SKIP" : result.Passed ? "OK" : "FAIL";
        console.Out.WriteLine($"[{stepId}/{TotalSteps}]   {name}: {status} — {result.Diagnostic}");
    }

    private static void EmitFinalOutput(CliConsole console, QuickstartEnvelopeData envelope)
    {
        switch (console.Format)
        {
            case OutputFormat.Json:
                WriteJsonEnvelope(console.Out, envelope);
                return;
            case OutputFormat.Table:
                WriteTable(console.Out, envelope);
                return;
            default:
                WriteHumanSummary(console.Out, envelope);
                return;
        }
    }

    private static void WriteJsonEnvelope(TextWriter writer, QuickstartEnvelopeData envelope)
    {
        var outputEnvelope = new CliOutputEnvelope<QuickstartEnvelopeData>(
            CliOutputEnvelope<QuickstartEnvelopeData>.CurrentSchemaVersion,
            CommandName,
            envelope);

        JsonTypeInfo typeInfo = CliJsonContext.Options.GetTypeInfo(typeof(CliOutputEnvelope<QuickstartEnvelopeData>));
        string json = JsonSerializer.Serialize(outputEnvelope, typeInfo);
        writer.WriteLine(json);
    }

    private static void WriteHumanSummary(TextWriter writer, QuickstartEnvelopeData envelope)
    {
        string elapsed = (envelope.ElapsedMs / 1000.0).ToString("F1", CultureInfo.InvariantCulture);
        writer.WriteLine($"Quickstart complete in {elapsed}s across {envelope.Steps.Count} steps.");
    }

    private static void WriteTable(TextWriter writer, QuickstartEnvelopeData envelope)
    {
        const string header = "STEP | STATUS";
        var builder = new StringBuilder();
        builder.AppendLine(header);
        builder.AppendLine(new string('-', header.Length));
        foreach (QuickstartStepResult step in envelope.Steps)
        {
            builder.Append(step.Id.ToString(CultureInfo.InvariantCulture).PadRight(4));
            builder.Append(" | ");
            builder.AppendLine(StatusLabel(step.Status));
        }

        writer.Write(builder.ToString());
    }

    private static string StatusLabel(QuickstartStepStatus status) => status switch
    {
        QuickstartStepStatus.Ok => "OK",
        QuickstartStepStatus.Fail => "FAIL",
        QuickstartStepStatus.Skip => "SKIP",
        QuickstartStepStatus.DryRun => "DRY-RUN",
        _ => status.ToString().ToUpperInvariant(),
    };

    private static bool IsConnectionRefused(HttpRequestException exception)
        => exception.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused };

    private static bool IsConnectionRefused(SocketException exception)
        => exception.SocketErrorCode == SocketError.ConnectionRefused;
}

/// <summary>Parsed wizard options. Passed by value into each step method for deterministic testability.</summary>
/// <param name="TenantId">The --tenant flag value, or null to derive the default.</param>
/// <param name="SkipBootCheck">True when --skip-boot-check was specified.</param>
/// <param name="SkipPrereqCheck">True when --skip-prereq-check was specified.</param>
/// <param name="DryRun">True when --dry-run was specified.</param>
/// <param name="TenantProvisionTimeout">Optional tenant provisioning timeout override.</param>
internal sealed record QuickstartOptions(
    string? TenantId,
    bool SkipBootCheck,
    bool SkipPrereqCheck,
    bool DryRun,
    TimeSpan? TenantProvisionTimeout = null);
