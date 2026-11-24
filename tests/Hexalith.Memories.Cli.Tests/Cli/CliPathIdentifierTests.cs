// <copyright file="CliPathIdentifierTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Text.Json;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

public sealed class CliPathIdentifierTests
{
    [Theory]
    [InlineData("search inspect --tenant acme/beta --case case-1 --id mu-1")]
    [InlineData("search inspect --tenant acme --case case/1 --id mu-1")]
    [InlineData("search inspect --tenant acme --case case-1 --id mu/1")]
    [InlineData("search lookup --tenant acme --case case/1 --source-uri file:///doc.pdf")]
    [InlineData("consistency inspect --tenant acme --id mu/1")]
    [InlineData("consistency verify --tenant acme/beta")]
    [InlineData("consistency repair --tenant acme/beta --yes")]
    [InlineData("export case --tenant acme --case case/1")]
    [InlineData("export tenant --tenant acme/beta")]
    [InlineData("handlers mismatches --tenant acme/beta")]
    [InlineData("status telemetry --tenant acme/beta")]
    [InlineData("quickstart --tenant acme/beta")]
    public async Task Command_WithSlashInPathIdentifier_ReturnsInvalidInputWithoutResolvingClient(string arguments)
    {
        using var stdout = new StringWriter();
        IServiceCollection collection = CliServices.BuildCollection();
        collection.AddSingleton(new CliConsole
        {
            In = TextReader.Null,
            Out = stdout,
            Error = TextWriter.Null,
            Format = OutputFormat.Json,
            IsInteractive = false,
        });
        collection.Replace(ServiceDescriptor.Transient<MemoriesClient>(_ =>
            throw new InvalidOperationException("The client must not be resolved for an invalid identifier.")));
        using ServiceProvider services = collection.BuildServiceProvider();
        var root = RootCommandFactory.Build(services, services.GetRequiredService<CliGlobalOptions>());

        int exitCode = await root.Parse(arguments).InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        exitCode.ShouldBe(CliExitCodes.Plumbing);
        using JsonDocument response = JsonDocument.Parse(stdout.ToString());
        JsonElement error = response.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().ShouldBe("INVALID_INPUT");
        error.GetProperty("message").GetString().ShouldBe("Identifiers in the request path cannot contain '/'.");
    }
}
