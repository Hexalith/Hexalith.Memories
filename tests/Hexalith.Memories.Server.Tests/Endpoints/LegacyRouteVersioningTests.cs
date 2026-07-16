// <copyright file="LegacyRouteVersioningTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Net;

using Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Microsoft.AspNetCore.Mvc.Testing;

using Shouldly;

/// <summary>Ensures the pre-GA route cutover does not retain or redirect unversioned REST aliases.</summary>
public sealed class LegacyRouteVersioningTests : IDisposable
{
    private readonly EventStoreWebAppFactory _factory = new();

    [Theory]
    [InlineData("GET", "/api/search?tenantId=tenant-a&query=test")]
    [InlineData("POST", "/api/ingest")]
    [InlineData("GET", "/api/tenants/tenant-a")]
    public async Task UnversionedRestRoute_IsNotMappedOrRedirected(string method, string path)
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using HttpRequestMessage request = new(new HttpMethod(method), path);

        using HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.Location.ShouldBeNull();
    }

    public void Dispose() => _factory.Dispose();
}
