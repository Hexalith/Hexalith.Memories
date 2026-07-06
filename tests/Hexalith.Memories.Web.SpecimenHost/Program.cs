// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Hexalith.Memories.Web.SpecimenHost.Components;
using Hexalith.Memories.Web.Specimens;
using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization();
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

WebApplication app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/", () => Results.Redirect(Epic17SpecimenManifest.RoutePrefix));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
