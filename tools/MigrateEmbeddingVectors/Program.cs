// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Globalization;
using System.Text.Json;

using Dapr.Actors.Client;
using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis;

using CancellationTokenSource cancellation = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

ParsedCommand parsed = ParsedCommand.Parse(args);
if (parsed.ShowHelp)
{
    Console.WriteLine(ToolHelp.Text);
    return EmbeddingMigrationExitCodes.Success;
}

if (!string.IsNullOrWhiteSpace(parsed.Error))
{
    await Console.Error.WriteLineAsync(parsed.Error).ConfigureAwait(false);
    await Console.Error.WriteLineAsync("Run with --help for usage.").ConfigureAwait(false);
    return EmbeddingMigrationExitCodes.Plumbing;
}

EmbeddingMigrationOptions options = parsed.Options!;
options.Interactive = !Console.IsInputRedirected;

if (options.Mode is not EmbeddingMigrationMode.DryRun && !options.Yes && options.Interactive)
{
    await Console.Error.WriteAsync(
        $"Run {options.Mode.ToString().ToLowerInvariant()} for tenant '{options.TenantId}' now? Blue/green staging keeps active semantic indexes queryable until cutover. [y/N]: ").ConfigureAwait(false);
    string? answer = Console.ReadLine();
    if (string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
    {
        options.Yes = true;
    }
    else
    {
        await Console.Error.WriteLineAsync("Aborted by operator.").ConfigureAwait(false);
        return EmbeddingMigrationExitCodes.Plumbing;
    }
}

options.ProgressHandler = async (progress, _) =>
{
    if (!string.Equals(options.Format, "json", StringComparison.OrdinalIgnoreCase))
    {
        await Console.Out.WriteLineAsync(
            $"{progress.TenantId} {progress.ContentKind} batch {progress.BatchNumber}: " +
            $"processed={progress.ProcessedCount} skipped={progress.SkippedCount} missing={progress.MissingCount} failed={progress.FailedCount} " +
            $"total={progress.TotalCount} percent={progress.Percent.ToString("0.##", CultureInfo.InvariantCulture)} elapsed={progress.Elapsed}").ConfigureAwait(false);
    }
};

await using ToolResources resources = await ToolResources.CreateAsync(parsed.RedisConnectionString!, parsed.DaprHttpEndpoint!).ConfigureAwait(false);
EmbeddingVectorMigrationService service = resources.CreateService();
EmbeddingMigrationResult result = await service.RunAsync(options, cancellation.Token).ConfigureAwait(false);

if (string.Equals(options.Format, "json", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(JsonSerializer.Serialize(result, ProgramJson.Options));
}
else
{
    WriteHumanResult(result);
}

return result.ExitCode;

static void WriteHumanResult(EmbeddingMigrationResult result)
{
    Console.WriteLine(result.Message);
    foreach (EmbeddingMigrationTenantReport report in result.Tenants)
    {
        Console.WriteLine(
            $"{report.TenantId}: affected={report.Affected} " +
            $"current={report.CurrentConfig.Provider}/{report.CurrentConfig.Model}/{report.CurrentConfig.Dimensions} " +
            $"target={report.TargetConfig.Provider}/{report.TargetConfig.Model}/{report.TargetConfig.Dimensions} " +
            $"syntactic={report.Counts.SyntacticMemoryUnitCount} raw={report.Counts.RawSemanticUnitCount} nl={report.Counts.NaturalLanguageSemanticUnitCount} " +
            $"raw processed/skipped/missing/failed={report.Raw.Processed}/{report.Raw.Skipped}/{report.Raw.Missing}/{report.Raw.Failed} " +
            $"nl processed/skipped/missing/failed={report.NaturalLanguage.Processed}/{report.NaturalLanguage.Skipped}/{report.NaturalLanguage.Missing}/{report.NaturalLanguage.Failed} " +
            $"manualFollowUp={report.ManualFollowUpRequired}");
    }

    foreach (EmbeddingMigrationUnitFailure failure in result.Failures)
    {
        Console.Error.WriteLine($"{failure.TenantId}/{failure.MemoryUnitId}/{failure.ContentKind}: {failure.ErrorCategory}: {failure.Message}");
    }
}

internal static class ProgramJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };
}

internal sealed class ToolResources : IAsyncDisposable
{
    private readonly DaprClient _daprClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly ActorProxyFactory _actorProxyFactory;
    private readonly SimpleHttpClientFactory _httpClientFactory;

    private ToolResources(
        DaprClient daprClient,
        IConnectionMultiplexer redis,
        ActorProxyFactory actorProxyFactory,
        SimpleHttpClientFactory httpClientFactory)
    {
        _daprClient = daprClient;
        _redis = redis;
        _actorProxyFactory = actorProxyFactory;
        _httpClientFactory = httpClientFactory;
    }

    public static async Task<ToolResources> CreateAsync(string redisConnectionString, string daprHttpEndpoint)
    {
        DaprClient daprClient = new DaprClientBuilder().Build();
        IConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString).ConfigureAwait(false);
        ActorProxyOptions actorOptions = new()
        {
            HttpEndpoint = daprHttpEndpoint,
            RequestTimeout = TimeSpan.FromMinutes(2),
            JsonSerializerOptions = MemoriesJsonContext.Options,
        };
        ActorProxyFactory actorProxyFactory = new(actorOptions);
        return new ToolResources(daprClient, redis, actorProxyFactory, new SimpleHttpClientFactory());
    }

    public EmbeddingVectorMigrationService CreateService()
    {
        TenantRegistryService registry = new(_daprClient, NullLogger<TenantRegistryService>.Instance);
        RedisEmbeddingMigrationStore store = new(_redis, registry, _actorProxyFactory);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Memories:Testing:UseFakeEmbedding"] = "false" })
            .Build();
        OidcTokenProvider tokenProvider = new(_httpClientFactory, TimeProvider.System, NullLogger<OidcTokenProvider>.Instance);
        EmbeddingClient embeddingClient = new(
            _httpClientFactory,
            _daprClient,
            configuration,
            new ToolHostEnvironment(),
            tokenProvider);
        return new EmbeddingVectorMigrationService(store, new EmbeddingClientMigrationVectorGenerator(embeddingClient));
    }

    public async ValueTask DisposeAsync()
    {
        _httpClientFactory.Dispose();
        _daprClient.Dispose();
        await _redis.CloseAsync().ConfigureAwait(false);
        _redis.Dispose();
    }
}

internal sealed class SimpleHttpClientFactory : IHttpClientFactory, IDisposable
{
    private readonly HttpClient _shared = new();

    public HttpClient CreateClient(string name) => _shared;

    public HttpClient GetSharedClient() => _shared;

    public void Dispose() => _shared.Dispose();
}

internal sealed class ToolHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;

    public string ApplicationName { get; set; } = "MigrateEmbeddingVectors";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}

internal sealed class ParsedCommand
{
    public bool ShowHelp { get; private init; }

    public string? Error { get; private init; }

    public EmbeddingMigrationOptions? Options { get; private init; }

    public string? RedisConnectionString { get; private init; }

    public string? DaprHttpEndpoint { get; private init; }

    public static ParsedCommand Parse(string[] args)
    {
        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
        {
            return new ParsedCommand { ShowHelp = true };
        }

        EmbeddingMigrationOptions options = new();
        string redis = Environment.GetEnvironmentVariable("MEMORIES_REDIS") ?? "localhost:6379";
        string daprHttp = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT") ?? "http://localhost:3500";
        int selectedModes = 0;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--dry-run":
                    options.Mode = EmbeddingMigrationMode.DryRun;
                    selectedModes++;
                    break;
                case "--live":
                    options.Mode = EmbeddingMigrationMode.Live;
                    selectedModes++;
                    break;
                case "--rollback":
                    options.Mode = EmbeddingMigrationMode.Rollback;
                    selectedModes++;
                    break;
                case "--abort":
                    options.Mode = EmbeddingMigrationMode.Abort;
                    selectedModes++;
                    break;
                case "--tenant":
                    if (!TryReadValue(args, ref i, arg, out string? tenantValue, out string? tenantError))
                    {
                        return new ParsedCommand { Error = tenantError };
                    }

                    options.TenantId = tenantValue;
                    break;
                case "--target-provider":
                    if (!TryReadValue(args, ref i, arg, out string? providerValue, out string? providerError))
                    {
                        return new ParsedCommand { Error = providerError };
                    }

                    options.TargetProvider = providerValue;
                    break;
                case "--target-model":
                    if (!TryReadValue(args, ref i, arg, out string? modelValue, out string? modelError))
                    {
                        return new ParsedCommand { Error = modelError };
                    }

                    options.TargetModel = modelValue;
                    break;
                case "--target-dimensions":
                    if (!TryReadValue(args, ref i, arg, out string? dimRaw, out string? dimError))
                    {
                        return new ParsedCommand { Error = dimError };
                    }

                    if (!int.TryParse(dimRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dimensions))
                    {
                        return new ParsedCommand { Error = $"--target-dimensions must be an integer, got '{dimRaw}'." };
                    }

                    options.TargetDimensions = dimensions;
                    break;
                case "--batch-size":
                    if (!TryReadValue(args, ref i, arg, out string? batchRaw, out string? batchError))
                    {
                        return new ParsedCommand { Error = batchError };
                    }

                    if (!int.TryParse(batchRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int batchSize))
                    {
                        return new ParsedCommand { Error = $"--batch-size must be an integer, got '{batchRaw}'." };
                    }

                    options.BatchSize = batchSize;
                    break;
                case "--yes":
                    options.Yes = true;
                    break;
                case "--resume":
                    options.Resume = true;
                    break;
                case "--format":
                    if (!TryReadValue(args, ref i, arg, out string? formatValue, out string? formatError))
                    {
                        return new ParsedCommand { Error = formatError };
                    }

                    options.Format = formatValue!;
                    break;
                case "--redis":
                    if (!TryReadValue(args, ref i, arg, out string? redisValue, out string? redisError))
                    {
                        return new ParsedCommand { Error = redisError };
                    }

                    redis = redisValue!;
                    break;
                case "--dapr-http":
                    if (!TryReadValue(args, ref i, arg, out string? daprValue, out string? daprError))
                    {
                        return new ParsedCommand { Error = daprError };
                    }

                    daprHttp = daprValue!;
                    break;
                default:
                    return new ParsedCommand { Error = $"Unknown option '{arg}'." };
            }
        }

        if (selectedModes != 1)
        {
            return new ParsedCommand { Error = "Select exactly one mode: --dry-run, --live, --rollback, or --abort." };
        }

        if (!string.Equals(options.Format, "human", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(options.Format, "json", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedCommand { Error = "--format must be 'human' or 'json'." };
        }

        return new ParsedCommand { Options = options, RedisConnectionString = redis, DaprHttpEndpoint = daprHttp };
    }

    private static bool TryReadValue(string[] args, ref int index, string option, out string? value, out string? error)
    {
        if (index + 1 >= args.Length)
        {
            value = null;
            error = $"{option} requires a value.";
            return false;
        }

        index++;
        value = args[index];
        error = null;
        return true;
    }
}

internal static class ToolHelp
{
    public const string Text = """
    MigrateEmbeddingVectors --dry-run|--live|--rollback|--abort [options]

    Required mutation safety:
      --live --tenant <tenantId> --yes
      --rollback --tenant <tenantId> --yes
      --abort --tenant <tenantId> --yes

    Options:
      --dry-run                 Inventory affected tenants without writes.
      --live                    Execute blue/green migration for one tenant.
      --rollback                Restore retained previous blue/green targets.
      --abort                   Clean or restore an interrupted migration safely.
      --tenant <tenantId>       Tenant for live/rollback/abort, optional dry-run filter.
      --target-provider <name>  Target provider, default ollama.
      --target-model <name>     Target model, default qwen3-embedding:4b.
      --target-dimensions <n>   Target dimensions, default 2560.
      --batch-size <n>          Progress batch size, default 100, max 10000.
      --yes                     Confirm non-interactive mutation.
      --resume                  Resume a previous live migration attempt (marker must exist).
      --format <human|json>     Output format, default human.
      --redis <connection>      Redis connection string, default MEMORIES_REDIS or localhost:6379.
      --dapr-http <endpoint>    DAPR sidecar HTTP endpoint, default DAPR_HTTP_ENDPOINT or http://localhost:3500.
    """;
}
