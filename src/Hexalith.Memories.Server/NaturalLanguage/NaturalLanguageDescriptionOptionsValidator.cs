// <copyright file="NaturalLanguageDescriptionOptionsValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using System.Globalization;
using System.IO;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.2 Task 1.7: startup-time options validator for <see cref="NaturalLanguageDescriptionOptions"/>.
/// Two gates:
/// <list type="number">
/// <item>Production guard (Risk #10): fail fast with log event 9161 Critical if
/// <see cref="NaturalLanguageDescriptionOptions.DaprComponentName"/> resolves to <c>conversation.echo</c>
/// while <see cref="IHostEnvironment.IsProduction"/> is true. The echo component is a test double that
/// echoes its input; deploying it to production would silently produce degenerate NL embeddings identical
/// to the raw embeddings.</item>
/// <item>Cross-tenant cache-sharing acknowledgment (Risk #16 / Improvement V): if the resolved DAPR
/// Conversation component YAML declares a non-zero <c>responseCacheTTL</c>, require either
/// <see cref="NaturalLanguageDescriptionOptions.AcceptCrossTenantCacheSharing"/> to be <see langword="true"/>
/// or the environment variable <c>HEXALITH_ACCEPT_CROSS_TENANT_CACHE_SHARING=1</c>. Without the
/// acknowledgment, fail fast with log event 9164 Critical. The DAPR sidecar shares its response cache
/// ACROSS tenants — non-zero TTL without acknowledgment is an unbounded privacy-incident blast radius.</item>
/// </list>
/// </summary>
public sealed partial class NaturalLanguageDescriptionOptionsValidator
    : IValidateOptions<NaturalLanguageDescriptionOptions>
{
    internal const string EchoComponentName = "conversation.echo";
    internal const string CacheAckEnvVar = "HEXALITH_ACCEPT_CROSS_TENANT_CACHE_SHARING";
    internal const string CacheAckEnvVarExpectedValue = "1";

    private readonly IHostEnvironment _environment;
    private readonly IComponentYamlReader _yamlReader;
    private readonly ILogger<NaturalLanguageDescriptionOptionsValidator> _logger;

    /// <summary>Initializes a new instance of the <see cref="NaturalLanguageDescriptionOptionsValidator"/>
    /// class.</summary>
    /// <param name="environment">The host environment (determines Production gating).</param>
    /// <param name="yamlReader">Abstraction over component YAML lookup so tests can inject fake TTL values
    /// without touching the filesystem.</param>
    /// <param name="logger">Structured logger for startup validation failures.</param>
    public NaturalLanguageDescriptionOptionsValidator(
        IHostEnvironment environment,
        IComponentYamlReader yamlReader,
        ILogger<NaturalLanguageDescriptionOptionsValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(yamlReader);
        ArgumentNullException.ThrowIfNull(logger);
        _environment = environment;
        _yamlReader = yamlReader;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, NaturalLanguageDescriptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (_environment.IsProduction()
            && string.Equals(options.DaprComponentName, EchoComponentName, StringComparison.OrdinalIgnoreCase))
        {
            NaturalLanguageIntegrationLog.EchoComponentRejectedInProduction(_logger, options.DaprComponentName);
            failures.Add(
                "9161 EchoComponentNotAllowedInProduction: "
                + $"NaturalLanguage:DaprComponentName == '{EchoComponentName}' in Production is forbidden "
                + "because the echo component returns the input unchanged, producing degenerate NL "
                + "embeddings identical to the raw embeddings. Swap the component to a real LLM provider "
                + "(conversation.openai / conversation.anthropic / conversation.googleai) in "
                + "deploy/dapr/components/conversation-llm.yaml. See Story 9.2 Risk #10.");
        }

        TimeSpan? ttl = _yamlReader.TryReadResponseCacheTtl(options.DaprComponentName);
        if (ttl.HasValue && ttl.Value > TimeSpan.Zero)
        {
            string? envAck = Environment.GetEnvironmentVariable(CacheAckEnvVar);
            bool envAcknowledged = string.Equals(envAck, CacheAckEnvVarExpectedValue, StringComparison.Ordinal);
            if (!options.AcceptCrossTenantCacheSharing && !envAcknowledged)
            {
                NaturalLanguageIntegrationLog.ResponseCacheRejectedWithoutAcknowledgment(
                    _logger,
                    options.DaprComponentName,
                    ttl.Value.ToString("c", CultureInfo.InvariantCulture));
                failures.Add(
                    "9164 ResponseCacheEnabledWithoutAcknowledgment: "
                    + $"DAPR Conversation component '{options.DaprComponentName}' declares "
                    + $"responseCacheTTL={ttl.Value} but neither "
                    + $"'NaturalLanguage:AcceptCrossTenantCacheSharing: true' nor environment variable "
                    + $"'{CacheAckEnvVar}={CacheAckEnvVarExpectedValue}' is set. The sidecar-level response "
                    + "cache is shared ACROSS tenants — non-zero TTL without explicit acknowledgment is a "
                    + "privacy-incident blast radius. See Story 9.2 Risk #16 and the 'Response caching "
                    + "opt-in procedure' section of docs/dev/eventstore-integration.md.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>Abstraction over DAPR component YAML lookup. Production implementation reads from
/// <c>deploy/dapr/components/*.yaml</c>; tests inject a fake.</summary>
public interface IComponentYamlReader
{
    /// <summary>Attempts to read the <c>responseCacheTTL</c> value for the component whose metadata.name
    /// equals <paramref name="componentName"/>. Returns <see langword="null"/> when the component is not
    /// found or does not declare the metadata key.</summary>
    /// <param name="componentName">The DAPR component metadata.name (matches
    /// <see cref="NaturalLanguageDescriptionOptions.DaprComponentName"/>).</param>
    /// <returns>The parsed TTL, or <see langword="null"/> when the component or metadata is absent.</returns>
    TimeSpan? TryReadResponseCacheTtl(string componentName);
}

/// <summary>Filesystem-backed <see cref="IComponentYamlReader"/> — reads the Conversation component YAML
/// from <c>deploy/dapr/components/conversation-llm.yaml</c> next to the server binary. Best-effort: any
/// I/O or parse failure returns <see langword="null"/> so the validator falls through to the "no TTL"
/// branch (fail open on inspection failure, not on TTL presence).</summary>
internal sealed class FileSystemComponentYamlReader : IComponentYamlReader
{
    private readonly string _componentsDirectory;

    public FileSystemComponentYamlReader(string componentsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentsDirectory);
        _componentsDirectory = componentsDirectory;
    }

    /// <inheritdoc/>
    public TimeSpan? TryReadResponseCacheTtl(string componentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);

        try
        {
            if (!Directory.Exists(_componentsDirectory))
            {
                return null;
            }

            foreach (string yamlPath in Directory.EnumerateFiles(_componentsDirectory, "*.yaml"))
            {
                string[] lines = File.ReadAllLines(yamlPath);
                if (!IsComponentWithName(lines, componentName))
                {
                    continue;
                }

                return ParseResponseCacheTtl(lines);
            }
        }
        catch (IOException)
        {
            // Fail open: we cannot prove non-zero TTL without a readable YAML, so the validator does not
            // require the acknowledgment. A genuinely non-zero TTL in production will still surface via
            // the telemetry counter memories_conversation_cache_hit_total (Risk #16 mitigation (d)).
        }
        catch (UnauthorizedAccessException)
        {
            // Same fail-open rationale.
        }

        return null;
    }

    private static bool IsComponentWithName(string[] lines, string componentName)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("name:", StringComparison.Ordinal))
            {
                string name = trimmed["name:".Length..].Trim().Trim('"');
                if (string.Equals(name, componentName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static TimeSpan? ParseResponseCacheTtl(string[] lines)
    {
        for (int i = 0; i < lines.Length - 1; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (!trimmed.StartsWith("- name:", StringComparison.Ordinal))
            {
                continue;
            }

            string metadataName = trimmed["- name:".Length..].Trim().Trim('"');
            if (!string.Equals(metadataName, "responseCacheTTL", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(metadataName, "cacheTTL", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string valueLine = lines[i + 1].TrimStart();
            if (!valueLine.StartsWith("value:", StringComparison.Ordinal))
            {
                continue;
            }

            string raw = valueLine["value:".Length..].Trim().Trim('"');
            return TryParseDuration(raw);
        }

        return null;
    }

    private static TimeSpan? TryParseDuration(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawSeconds))
        {
            return TimeSpan.FromSeconds(rawSeconds);
        }

        double totalTicks = 0;
        int index = 0;
        while (index < raw.Length)
        {
            int numberStart = index;
            while (index < raw.Length && (char.IsDigit(raw[index]) || raw[index] == '.'))
            {
                index++;
            }

            if (numberStart == index
                || !double.TryParse(raw[numberStart..index], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return null;
            }

            int unitStart = index;
            if (index < raw.Length && raw[index] == 'µ')
            {
                index++;
            }

            while (index < raw.Length && char.IsLetter(raw[index]))
            {
                index++;
            }

            string unit = raw[unitStart..index].ToLowerInvariant();
            double unitTicks = unit switch
            {
                "h" => value * TimeSpan.TicksPerHour,
                "m" => value * TimeSpan.TicksPerMinute,
                "s" => value * TimeSpan.TicksPerSecond,
                "ms" => value * TimeSpan.TicksPerMillisecond,
                "us" or "µs" => value * 10,
                "ns" => value / 100,
                _ => double.NaN,
            };

            if (double.IsNaN(unitTicks))
            {
                return null;
            }

            totalTicks += unitTicks;
        }

        return TimeSpan.FromTicks((long)Math.Round(totalTicks, MidpointRounding.AwayFromZero));
    }
}
