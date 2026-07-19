// <copyright file="AccessTelemetryDevelopmentSecrets.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using System.Security.Cryptography;
using System.Text.Json;

/// <summary>Contains one protected development seed set and its non-secret verification key.</summary>
internal sealed record AccessTelemetryDevelopmentSecrets(string SeedJson, string VerificationPublicKey)
{
    private static readonly Lazy<string> GeneratedSeedJson = new(GenerateSeedJson);

    /// <summary>Creates or validates the access-telemetry development seed parameter.</summary>
    /// <param name="configuredSeedJson">An optional protected configuration override.</param>
    /// <returns>The stable seed JSON and derived public verification key.</returns>
    internal static AccessTelemetryDevelopmentSecrets Create(string? configuredSeedJson)
    {
        string seedJson = configuredSeedJson ?? GeneratedSeedJson.Value;
        OpenBaoSeedInputs seeds = OpenBaoSeedInputs.Create("{}", seedJson);
        string signingKey = seeds.AccessTelemetrySecrets["access-telemetry-clock-key"];

        try
        {
            using ECDsa clockKey = ECDsa.Create();
            clockKey.ImportPkcs8PrivateKey(Convert.FromBase64String(signingKey), out int bytesRead);
            if (bytesRead == 0)
            {
                throw new InvalidOperationException("The access-telemetry clock seed is malformed.");
            }

            return new AccessTelemetryDevelopmentSecrets(
                seedJson,
                Convert.ToBase64String(clockKey.ExportSubjectPublicKeyInfo()));
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("The access-telemetry clock seed is malformed.", exception);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException("The access-telemetry clock seed is malformed.", exception);
        }
    }

    private static string GenerateSeedJson()
    {
        using ECDsa generated = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var seeds = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["access-telemetry-marker-key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["access-telemetry-clock-key"] = Convert.ToBase64String(generated.ExportPkcs8PrivateKey()),
        };
        return JsonSerializer.Serialize(seeds);
    }
}
