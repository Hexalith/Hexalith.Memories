// <copyright file="OutputFormatterRouterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Output;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

public sealed class OutputFormatterRouterTests
{
    [Fact]
    public void Write_UnregisteredFormat_ThrowsFormatterNotRegisteredException()
    {
        ServiceCollection services = [];
        services.AddSingleton<IOutputFormatter<string>>(new StubFormatter(OutputFormat.Human));
        services.AddSingleton<OutputFormatterRouter>();
        using ServiceProvider provider = services.BuildServiceProvider();

        OutputFormatterRouter router = provider.GetRequiredService<OutputFormatterRouter>();
        using var writer = new StringWriter();

        FormatterNotRegisteredException ex = Should.Throw<FormatterNotRegisteredException>(
            () => router.Write(OutputFormat.Json, "x", writer));

        ex.Format.ShouldBe(OutputFormat.Json);
        ex.ModelType.ShouldBe(typeof(string));
        ex.Message.ShouldContain("IOutputFormatter<String>");
        ex.Message.ShouldContain("'Json'");
    }

    [Fact]
    public void Write_RegisteredFormat_DelegatesToFormatter()
    {
        ServiceCollection services = [];
        services.AddSingleton<IOutputFormatter<string>>(new StubFormatter(OutputFormat.Human));
        services.AddSingleton<OutputFormatterRouter>();
        using ServiceProvider provider = services.BuildServiceProvider();

        OutputFormatterRouter router = provider.GetRequiredService<OutputFormatterRouter>();
        using var writer = new StringWriter();

        router.Write(OutputFormat.Human, "payload", writer);

        writer.ToString().ShouldBe("human:payload");
    }

    private sealed class StubFormatter : IOutputFormatter<string>
    {
        public StubFormatter(OutputFormat format)
        {
            Format = format;
        }

        public OutputFormat Format { get; }

        public void Write(string value, TextWriter writer) => writer.Write($"human:{value}");
    }
}
