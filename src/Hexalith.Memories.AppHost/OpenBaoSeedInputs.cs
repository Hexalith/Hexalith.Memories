// <copyright file="OpenBaoSeedInputs.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using System.Collections.ObjectModel;
using System.Text.Json;

/// <summary>Holds protected secret values supplied to one OpenBao initialization generation.</summary>
internal sealed class OpenBaoSeedInputs
{
    private const string ClockSecretName = "access-telemetry-clock-key";
    private const string MarkerSecretName = "access-telemetry-marker-key";

    private OpenBaoSeedInputs(
        IReadOnlyDictionary<string, string> runtimeSecrets,
        IReadOnlyDictionary<string, string> accessTelemetrySecrets)
    {
        RuntimeSecrets = runtimeSecrets;
        AccessTelemetrySecrets = accessTelemetrySecrets;
    }

    /// <summary>Gets the runtime secret names and values.</summary>
    internal IReadOnlyDictionary<string, string> RuntimeSecrets { get; }

    /// <summary>Gets the access-telemetry secret names and values.</summary>
    internal IReadOnlyDictionary<string, string> AccessTelemetrySecrets { get; }

    /// <summary>Parses protected JSON parameter values without retaining their source text.</summary>
    /// <param name="runtimeSecretsJson">A JSON object containing runtime secret-name/value pairs.</param>
    /// <param name="accessTelemetrySecretsJson">A JSON object containing the marker and clock pairs.</param>
    /// <returns>The validated seed set.</returns>
    internal static OpenBaoSeedInputs Create(string runtimeSecretsJson, string accessTelemetrySecretsJson)
    {
        IReadOnlyDictionary<string, string> runtime = Parse(runtimeSecretsJson, "runtime");
        IReadOnlyDictionary<string, string> access = Parse(accessTelemetrySecretsJson, "access-telemetry");

        if (access.Count != 2 ||
            !access.ContainsKey(MarkerSecretName) ||
            !access.ContainsKey(ClockSecretName))
        {
            throw new ArgumentException(
                "The access-telemetry seed input must contain exactly the marker and clock secret names.",
                nameof(accessTelemetrySecretsJson));
        }

        return new OpenBaoSeedInputs(runtime, access);
    }

    private static IReadOnlyDictionary<string, string> Parse(string json, string scope)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException($"The {scope} seed input must be a JSON object.", nameof(json));
            }

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (!IsSafeSecretName(property.Name))
                {
                    throw new ArgumentException(
                        $"The {scope} secret name is not valid.",
                        nameof(json));
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new ArgumentException(
                        $"The {scope} secret value for the validated secret name must be a string.",
                        nameof(json));
                }

                if (!result.TryAdd(property.Name, property.Value.GetString()!))
                {
                    throw new ArgumentException(
                        $"The {scope} seed input contains a duplicate secret name.",
                        nameof(json));
                }
            }

            return new ReadOnlyDictionary<string, string>(result);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                $"The {scope} seed input is not valid JSON.",
                nameof(json),
                exception);
        }
    }

    private static bool IsSafeSecretName(string name)
        => name is not "." and not ".." && name.Length > 0 && name.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
}
