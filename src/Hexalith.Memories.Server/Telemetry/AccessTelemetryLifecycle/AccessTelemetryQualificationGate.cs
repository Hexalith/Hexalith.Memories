// <copyright file="AccessTelemetryQualificationGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Text.Json;

/// <summary>Reads the short-lived, file-mounted non-Production qualification gate.</summary>
internal sealed class AccessTelemetryQualificationGate(
    IHostEnvironment environment,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    /// <summary>The sole approved Story 27.4 profile hash.</summary>
    public const string ApprovedProfileSha256 = "dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14";

    private const string DefaultGatePath = "/var/run/hexalith/access-telemetry-qualification/gate.json";

    /// <summary>Validates the current gate without caching a past authorization.</summary>
    /// <param name="reason">A bounded reason when the gate is closed.</param>
    /// <returns><see langword="true"/> only for a current exact-profile Qualification gate.</returns>
    public bool TryValidate(out string reason)
    {
        if (!environment.IsEnvironment("Qualification"))
        {
            reason = "qualification_environment_required";
            return false;
        }

        string gatePath = configuration["AccessTelemetryQualification:GatePath"] ?? DefaultGatePath;
        var file = new FileInfo(gatePath);
        if (!file.Exists || file.LinkTarget is not null || file.Length is <= 0 or > 4096)
        {
            reason = "qualification_gate_unavailable";
            return false;
        }

        try
        {
            using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
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
                !root.TryGetProperty("profileSha256", out JsonElement profile) || profile.GetString() != ApprovedProfileSha256 ||
                !root.TryGetProperty("expiresUtcMs", out JsonElement expires) || !expires.TryGetInt64(out long expiresUtcMs) ||
                expiresUtcMs <= timeProvider.GetUtcNow().ToUnixTimeMilliseconds())
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
}
