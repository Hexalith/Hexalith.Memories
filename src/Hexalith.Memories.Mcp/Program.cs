// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Hexalith.Memories.Mcp;
using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.ServiceDefaults.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(configureRedisInstrumentation: false);

McpCompositionRoot.ConfigureServices(builder.Services);

WebApplication app = builder.Build();

app.UseMiddleware<DaprApplicationTokenMiddleware>();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();

app.MapMcp("/mcp").RequireAuthorization();

app.Run();

/// <summary>Top-level program shim so test assemblies can target <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
