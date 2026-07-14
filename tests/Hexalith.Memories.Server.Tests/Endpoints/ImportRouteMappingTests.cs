// <copyright file="ImportRouteMappingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Net;
using System.Text;

using Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Microsoft.AspNetCore.Mvc.Testing;

using Shouldly;

/// <summary>Guards the additive tenant- and case-import route mappings.</summary>
public sealed class ImportRouteMappingTests : IDisposable
{
    private readonly EventStoreWebAppFactory _factory = new();

    [Theory]
    [InlineData("/api/v1/tenants/acme/import")]
    [InlineData("/api/v1/tenants/acme/cases/case-1/import")]
    public async Task ImportRoute_IsMapped(string path)
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using StringContent content = new("{}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(path, content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    }

    public void Dispose() => _factory.Dispose();
}
