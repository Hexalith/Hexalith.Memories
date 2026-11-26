// <copyright file="KubernetesComponentCacheReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

/// <summary>Reads the current Dapr Component from the pod's namespace using its service account.</summary>
internal sealed class KubernetesComponentCacheReader : IComponentYamlReader
{
    private const string ServiceAccountDirectory = "/var/run/secrets/kubernetes.io/serviceaccount";

    /// <inheritdoc/>
    public TimeSpan? TryReadResponseCacheTtl(string componentName)
    {
        try
        {
            string? host = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST");
            string? port = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT_HTTPS")
                ?? Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT");
            if (!IPAddress.TryParse(host, out IPAddress? address)
                || !int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out int portNumber)
                || portNumber is < 1 or > 65535)
            {
                return null;
            }

            string podNamespace = File.ReadAllText(Path.Combine(ServiceAccountDirectory, "namespace")).Trim();
            string token = File.ReadAllText(Path.Combine(ServiceAccountDirectory, "token")).Trim();
            var authorities = new X509Certificate2Collection();
            try
            {
                authorities.ImportFromPemFile(Path.Combine(ServiceAccountDirectory, "ca.crt"));
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseProxy = false,
                    ServerCertificateCustomValidationCallback = (_, certificate, peerChain, errors) =>
                    {
                        if (certificate is null || (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None)
                        {
                            return false;
                        }

                        using var chain = new X509Chain();
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain.ChainPolicy.CustomTrustStore.AddRange(authorities);
                        if (peerChain is not null)
                        {
                            chain.ChainPolicy.ExtraStore.AddRange(peerChain.ChainPolicy.ExtraStore);
                        }

                        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
                        return chain.Build(certificate);
                    },
                };
                using var client = new HttpClient(handler)
                {
                    BaseAddress = new UriBuilder(Uri.UriSchemeHttps, address.ToString(), portNumber).Uri,
                    Timeout = TimeSpan.FromSeconds(5),
                };
                return ReadComponentCacheTtlAsync(client, podNamespace, componentName, token).GetAwaiter().GetResult();
            }
            finally
            {
                foreach (X509Certificate2 authority in authorities)
                {
                    authority.Dispose();
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException
            or OperationCanceledException or CryptographicException or FormatException)
        {
            return null;
        }
    }

    /// <summary>Fetches the named Dapr Component with the pod's scoped service account token.</summary>
    internal static async Task<TimeSpan?> ReadComponentCacheTtlAsync(
        HttpClient client,
        string podNamespace,
        string componentName,
        string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/apis/dapr.io/v1alpha1/namespaces/{Uri.EscapeDataString(podNamespace)}/components/{Uri.EscapeDataString(componentName)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using Stream body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return ReadResponseCacheTtl(body);
    }

    /// <summary>Reads an unambiguous TTL from the Kubernetes Component representation.</summary>
    private static TimeSpan? ReadResponseCacheTtl(Stream body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("spec", out JsonElement spec)
                || !spec.TryGetProperty("type", out JsonElement type)
                || type.GetString() is not { } componentType
                || !componentType.StartsWith("conversation.", StringComparison.Ordinal)
                || componentType == "conversation.echo"
                || !spec.TryGetProperty("metadata", out JsonElement metadata))
            {
                return null;
            }

            TimeSpan? ttl = null;
            foreach (JsonElement item in metadata.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out JsonElement name)
                    || name.GetString() is not { } key
                    || (!string.Equals(key, "responseCacheTTL", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(key, "cacheTTL", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (ttl.HasValue
                    || item.TryGetProperty("secretKeyRef", out _)
                    || !item.TryGetProperty("value", out JsonElement value)
                    || ParseCacheTtl(value) is not { } parsed
                    || parsed < TimeSpan.Zero)
                {
                    return null;
                }

                ttl = parsed;
            }

            return ttl;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private static TimeSpan? ParseCacheTtl(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => FileSystemComponentYamlReader.TryParseDuration(value.GetString() ?? string.Empty),
            JsonValueKind.Number when value.TryGetInt32(out int seconds) => TimeSpan.FromSeconds(seconds),
            _ => null,
        };

}
