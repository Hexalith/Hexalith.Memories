// <copyright file="AllocatedEndpointResolverTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net.Sockets;

using Aspire.Hosting.ApplicationModel;

using Hexalith.Memories.AppHost;

using Shouldly;

/// <summary>Executable tests for the AppHost allocated-endpoint lookup.</summary>
[Trait("Category", "Integration")]
public sealed class AllocatedEndpointResolverTests
{
    [Theory]
    [InlineData("0.0.0.0", "127.0.0.1")]
    [InlineData("::", "127.0.0.1")]
    [InlineData("localhost", "localhost")]
    public void Resolve_AllocatedEndpoint_ReturnsLoopbackSafeHostAndPort(string address, string expectedHost)
    {
        ContainerResource resource = new("memories-vectors");
        EndpointAnnotation endpoint = AddEndpoint(resource, "redis");
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, address, 16379);

        (string host, int port) = AllocatedEndpointResolver.Resolve(resource, "redis");

        host.ShouldBe(expectedHost);
        port.ShouldBe(16379);
    }

    [Fact]
    public void Resolve_MissingEndpointName_NamesResourceEndpointAndAvailableEndpoints()
    {
        ContainerResource resource = new("memories-vectors");
        _ = AddEndpoint(resource, "redis-primary");
        _ = AddEndpoint(resource, "metrics");

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AllocatedEndpointResolver.Resolve(resource, "redis"));

        exception.Message.ShouldBe(
            "Resource 'memories-vectors' has no endpoint named 'redis'. Available endpoints: 'redis-primary', 'metrics'.");
    }

    [Fact]
    public void Resolve_DuplicateEndpointName_NamesResourceAndEndpoint()
    {
        ContainerResource resource = new("memories-vectors");
        _ = AddEndpoint(resource, "redis");
        _ = AddEndpoint(resource, "redis");

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AllocatedEndpointResolver.Resolve(resource, "redis"));

        exception.Message.ShouldBe(
            "Resource 'memories-vectors' declares 2 endpoints named 'redis'; endpoint names must be unique.");
    }

    [Fact]
    public void Resolve_UnallocatedEndpoint_NamesResourceAndEndpoint()
    {
        ContainerResource resource = new("memories-vectors");
        _ = AddEndpoint(resource, "redis");

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AllocatedEndpointResolver.Resolve(resource, "redis"));

        exception.Message.ShouldBe("Endpoint 'memories-vectors/redis' has not been allocated yet.");
    }

    [Fact]
    public void Resolve_ResourceWithoutEndpoints_NamesResource()
    {
        ContainerResource resource = new("memories-vectors");

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AllocatedEndpointResolver.Resolve(resource, "redis"));

        exception.Message.ShouldBe("Resource 'memories-vectors' does not expose endpoints.");
    }

    private static EndpointAnnotation AddEndpoint(ContainerResource resource, string name)
    {
        EndpointAnnotation endpoint = new(ProtocolType.Tcp, name: name, targetPort: 6379);
        resource.Annotations.Add(endpoint);
        return endpoint;
    }
}
