// <copyright file="AllocatedEndpointResolver.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using Aspire.Hosting.ApplicationModel;

/// <summary>Resolves the host and port that Aspire allocated for a named resource endpoint.</summary>
internal static class AllocatedEndpointResolver
{
    /// <summary>Resolves the allocated host and port of a named endpoint.</summary>
    /// <param name="resource">The resource that owns the endpoint.</param>
    /// <param name="endpointName">The endpoint name.</param>
    /// <returns>The loopback-safe host and the allocated port.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the resource has no endpoints, when the name matches no endpoint or several endpoints, or when
    /// the endpoint is not allocated yet. Each message names the resource and the endpoint.
    /// </exception>
    internal static (string Host, int Port) Resolve(IResource resource, string endpointName)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);
        if (!resource.TryGetEndpoints(out IEnumerable<EndpointAnnotation>? endpoints))
        {
            throw new InvalidOperationException(
                $"Resource '{resource.Name}' does not expose endpoints, so endpoint '{endpointName}' cannot be resolved.");
        }

        EndpointAnnotation endpoint = SelectSingle(resource, endpointName, [.. endpoints]);
        AllocatedEndpoint allocated = endpoint.AllocatedEndpoint
            ?? throw new InvalidOperationException($"Endpoint '{resource.Name}/{endpointName}' has not been allocated yet.");

        string host = allocated.Address;
        if (string.IsNullOrWhiteSpace(host) ||
            string.Equals(host, "0.0.0.0", StringComparison.Ordinal) ||
            string.Equals(host, "::", StringComparison.Ordinal))
        {
            host = "127.0.0.1";
        }

        return (host, allocated.Port);
    }

    private static EndpointAnnotation SelectSingle(IResource resource, string endpointName, EndpointAnnotation[] endpoints)
    {
        EndpointAnnotation[] matches = [.. endpoints.Where(candidate =>
            string.Equals(candidate.Name, endpointName, StringComparison.Ordinal))];
        if (matches.Length == 1)
        {
            return matches[0];
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Resource '{resource.Name}' declares {matches.Length} endpoints named '{endpointName}'; endpoint names must be unique.");
        }

        string available = string.Join(", ", endpoints.Select(static candidate => $"'{candidate.Name}'"));
        throw new InvalidOperationException(
            $"Resource '{resource.Name}' has no endpoint named '{endpointName}'. Available endpoints: {available}.");
    }
}
