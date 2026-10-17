// <copyright file="AccessTelemetryQualificationGateFile.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using System.Text.Json;

/// <summary>Reads the existing bounded four-field qualification gate without caching authorization.</summary>
public static class AccessTelemetryQualificationGateFile
{
    private const string DefaultGatePath = "/var/run/hexalith/access-telemetry-qualification/gate.json";
    private static readonly TimeSpan MaximumGateLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan GateExpirySkew = TimeSpan.FromSeconds(5);

    /// <summary>Validates current gate bytes for the caller's already bound profile.</summary>
    /// <param name="configuredPath">The mounted gate path, or null for the existing default.</param>
    /// <param name="expectedProfileSha256">The authorized profile hash.</param>
    /// <param name="now">Current UTC from the caller's time provider.</param>
    /// <param name="reason">A bounded refusal reason.</param>
    /// <returns>True only while the exact existing gate is enabled and short-lived.</returns>
    public static bool TryValidate(string? configuredPath, string expectedProfileSha256, DateTimeOffset now, out string reason)
    {
        if (configuredPath is not null && string.IsNullOrWhiteSpace(configuredPath))
        {
            reason = "qualification_gate_unavailable";
            return false;
        }

        FileInfo? file;
        try
        {
            file = ResolveMountedGate(configuredPath ?? DefaultGatePath);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException)
        {
            reason = "qualification_gate_unavailable";
            return false;
        }

        if (file is null || file.Length is <= 0 or > 4096)
        {
            reason = "qualification_gate_unavailable";
            return false;
        }

        try
        {
            using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length is <= 0 or > 4096)
            {
                reason = "qualification_gate_unavailable";
                return false;
            }

            using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 4,
            });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 4 ||
                !root.TryGetProperty("schemaVersion", out JsonElement schemaVersion) || schemaVersion.GetInt32() != 1 ||
                !root.TryGetProperty("state", out JsonElement state) || state.GetString() != "enabled" ||
                !root.TryGetProperty("profileSha256", out JsonElement profile) || profile.GetString() != expectedProfileSha256 ||
                !root.TryGetProperty("expiresUtcMs", out JsonElement expires) || !expires.TryGetInt64(out long expiresUtcMs) ||
                expiresUtcMs <= now.ToUnixTimeMilliseconds() ||
                expiresUtcMs > now.Add(MaximumGateLifetime).Add(GateExpirySkew).ToUnixTimeMilliseconds())
            {
                reason = "qualification_gate_invalid_or_expired";
                return false;
            }

            reason = "none";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            reason = "qualification_gate_invalid_or_expired";
            return false;
        }
    }

    /// <summary>Checks the selected local profile and existing gate against current run/source and immutable expiry.</summary>
    /// <param name="options">Current configured local lifecycle identity.</param>
    /// <param name="environmentName">Actual host environment.</param>
    /// <param name="runnerRunId">Actual runner identity.</param>
    /// <param name="sourceMappingHash">Actual runner source mapping hash.</param>
    /// <param name="localProfileHash">The profile bound to the local gate.</param>
    /// <param name="gatePath">The existing mounted gate path.</param>
    /// <param name="deadline">The immutable selected capability expiry.</param>
    /// <param name="now">Current UTC.</param>
    /// <returns>True only for complete current local intent and an enabled short-lived gate.</returns>
    public static bool IsLocalEnabled(
        AccessTelemetryOptions options,
        string? environmentName,
        string? runnerRunId,
        string? sourceMappingHash,
        string? localProfileHash,
        string? gatePath,
        DateTimeOffset deadline,
        DateTimeOffset now)
        => options.Enabled && string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(options.LocalFunctionalRunId) && options.LocalFunctionalRunId == runnerRunId &&
            options.LocalFunctionalRunId == options.DeploymentId && options.LocalFunctionalSourceMappingHash == sourceMappingHash &&
            localProfileHash == options.ComponentProfileHash && !string.IsNullOrWhiteSpace(gatePath) &&
            options.Retention == TimeSpan.FromHours(1) && now < deadline &&
            AccessTelemetryOptionsValidator.IsLocalFunctionalProfileValid(
                options.LocalFunctionalProfileJson, options.ComponentProfileHash, options.LocalFunctionalRunId, options.LocalFunctionalSourceMappingHash) &&
            TryValidate(gatePath, options.ComponentProfileHash, now, out _);

    private static FileInfo? ResolveMountedGate(string gatePath)
    {
        string fullPath = Path.GetFullPath(gatePath);
        var requested = new FileInfo(fullPath);
        if (!requested.Exists)
        {
            return null;
        }

        if (requested.LinkTarget is null)
        {
            return requested;
        }

        FileSystemInfo? resolved = requested.ResolveLinkTarget(returnFinalTarget: true);
        if (resolved is not FileInfo resolvedFile || !resolvedFile.Exists)
        {
            return null;
        }

        string mountRoot = Path.GetFullPath(requested.DirectoryName ?? string.Empty);
        string resolvedPath = Path.GetFullPath(resolvedFile.FullName);
        string relative = Path.GetRelativePath(mountRoot, resolvedPath);
        if (Path.IsPathRooted(relative) || relative == ".." ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return null;
        }

        return resolvedFile;
    }
}
