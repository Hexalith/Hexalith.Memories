// <copyright file="UrlHostValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// SSRF defense: classifies a URL's scheme and resolved host. Rejects non-http(s) schemes and
/// private/loopback/link-local/multicast/reserved addresses when AllowPrivateHosts=false.
/// </summary>
public static class UrlHostValidator
{
    /// <summary>
    /// Returns true if <paramref name="uri"/> is safe to fetch under the supplied options.
    /// DNS is resolved synchronously; callers on hot paths should dispatch to a worker thread.
    /// </summary>
    public static bool IsAllowedHost(Uri uri, UrlFetcherOptions options)
        => IsAllowedHost(uri, options, static host => Dns.GetHostAddresses(host));

    /// <summary>Resolver-injectable overload for tests.</summary>
    internal static bool IsAllowedHost(Uri uri, UrlFetcherOptions options, Func<string, IPAddress[]> resolver)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(resolver);

        if (uri.Scheme is not "http" and not "https")
        {
            return false;
        }

        if (options.AllowPrivateHosts)
        {
            return true;
        }

        string host = uri.IdnHost;

        if (IPAddress.TryParse(host, out IPAddress? literal))
        {
            return !IsPrivateOrReserved(literal);
        }

        IPAddress[] addresses;
        try
        {
            addresses = resolver(host);
        }
        catch (SocketException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (addresses.Length == 0)
        {
            return false;
        }

        foreach (IPAddress address in addresses)
        {
            if (IsPrivateOrReserved(address))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsPrivateOrReserved(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = address.GetAddressBytes();

            // 0.0.0.0/8 — current network
            if (bytes[0] == 0)
            {
                return true;
            }

            // 10.0.0.0/8 — private
            if (bytes[0] == 10)
            {
                return true;
            }

            // 127.0.0.0/8 — loopback (also caught by IsLoopback)
            if (bytes[0] == 127)
            {
                return true;
            }

            // 169.254.0.0/16 — link-local (AWS/GCP metadata lives here)
            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }

            // 172.16.0.0/12 — private
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16 — private
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 100.64.0.0/10 — carrier-grade NAT
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
            {
                return true;
            }

            // 224.0.0.0/4 — multicast; 240.0.0.0/4 — reserved
            if (bytes[0] >= 224)
            {
                return true;
            }

            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            {
                return true;
            }

            byte[] bytes = address.GetAddressBytes();

            // fc00::/7 — unique local
            if ((bytes[0] & 0xFE) == 0xFC)
            {
                return true;
            }

            // Unspecified ::
            bool allZero = true;
            foreach (byte b in bytes)
            {
                if (b != 0)
                {
                    allZero = false;
                    break;
                }
            }

            if (allZero)
            {
                return true;
            }

            return false;
        }

        // Anything else (e.g., AppleTalk) — reject conservatively.
        return true;
    }
}
