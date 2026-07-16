// <copyright file="ConsistencyInspectCommandTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Net;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 8.2 — <c>memories consistency inspect</c> CLI coverage.</summary>
public sealed class ConsistencyInspectCommandTests
{
    private const string ValidUlid = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9";

    [Fact]
    public async Task Run_HappyPath_PrintsInspectionResult()
    {
        ConsistencyStubClient stub = new();
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["inspect", "--tenant", "acme", "--id", ValidUlid]);

        exit.ShouldBe(CliExitCodes.Success);
        stdout.ToString().ShouldContain("Recommendation:");
        stdout.ToString().ShouldContain("NoOp");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_404FromServer_PrintsErrorEnvelopeWithRecoverySuggestion()
    {
        ConsistencyStubClient stub = new()
        {
            InspectionException = new MemoriesRemoteException(
                HttpStatusCode.NotFound,
                new ErrorResponse("MEMORY_UNIT_NOT_FOUND", "not found", "run verify")),
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["inspect", "--tenant", "acme", "--id", ValidUlid]);

        exit.ShouldBe(CliExitCodes.DomainError);
        stderr.ToString().ShouldContain("MEMORY_UNIT_NOT_FOUND");
    }

    [Fact]
    public async Task Run_400FromServer_PrintsInvalidIdEnvelope()
    {
        ConsistencyStubClient stub = new()
        {
            InspectionException = new MemoriesRemoteException(
                HttpStatusCode.BadRequest,
                new ErrorResponse("INVALID_MEMORY_UNIT_ID", "bad", "use ULID")),
        };
        (IServiceProvider services, StringWriter stdout, StringWriter stderr) =
            ConsistencyVerifyCommandTests.BuildServices(OutputFormat.Human, stub);

        int exit = await InvokeAsync(services, ["inspect", "--tenant", "acme", "--id", ValidUlid]);

        exit.ShouldBe(CliExitCodes.DomainError);
        stderr.ToString().ShouldContain("INVALID_MEMORY_UNIT_ID");
    }

    private static async Task<int> InvokeAsync(IServiceProvider services, string[] args)
    {
        var root = new System.CommandLine.Command("consistency");
        root.Subcommands.Add(ConsistencyInspectCommand.Build(services));
        return await root.Parse(args).InvokeAsync();
    }
}
