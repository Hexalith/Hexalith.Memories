// <copyright file="FileConfigurationSourceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Configuration;

using Shouldly;

public class FileConfigurationSourceTests
{
    private const string FakeHome = "C:/fake-home";

    [Fact]
    public void TryResolve_FileDoesNotExist_ReturnsFalseWithoutThrowing()
    {
        FileConfigurationSource source = CreateSource(exists: false, contents: string.Empty);

        bool resolved = source.TryResolve(out Uri? endpoint, out string? apiToken);

        resolved.ShouldBeFalse();
        endpoint.ShouldBeNull();
        apiToken.ShouldBeNull();
    }

    [Fact]
    public void TryResolve_EmptyFile_ReturnsFalseWithoutThrowing()
    {
        FileConfigurationSource source = CreateSource(exists: true, contents: string.Empty);

        bool resolved = source.TryResolve(out Uri? endpoint, out string? apiToken);

        resolved.ShouldBeFalse();
    }

    [Fact]
    public void TryResolve_MalformedJson_ThrowsInvalidConfigurationExceptionWithPath()
    {
        FileConfigurationSource source = CreateSource(exists: true, contents: "{ not valid");

        InvalidConfigurationException exception = Should.Throw<InvalidConfigurationException>(
            () => source.TryResolve(out _, out _));

        exception.FilePath.ShouldContain(".hexalith");
        exception.Message.ShouldContain(".hexalith");
    }

    [Fact]
    public void TryResolve_ValidJsonWithUnknownFields_IgnoresUnknownFields()
    {
        // Forward-compat: unknown properties don't break older clients.
        const string contents = """
        {
            "endpoint": "https://file.example.com/",
            "apiToken": "filetoken",
            "futureFeature": "unknown",
            "timeoutSeconds": 60
        }
        """;
        FileConfigurationSource source = CreateSource(exists: true, contents: contents);

        bool resolved = source.TryResolve(out Uri? endpoint, out string? apiToken);

        resolved.ShouldBeTrue();
        endpoint.ShouldBe(new Uri("https://file.example.com/"));
        apiToken.ShouldBe("filetoken");
    }

    [Fact]
    public void TryResolve_EmptyEndpointString_TreatedAsUnset()
    {
        // AC #6.2: "endpoint": "" must fall through, not throw on Uri construction.
        const string contents = """{ "endpoint": "", "apiToken": "" }""";
        FileConfigurationSource source = CreateSource(exists: true, contents: contents);

        bool resolved = source.TryResolve(out Uri? endpoint, out string? apiToken);

        resolved.ShouldBeFalse();
        endpoint.ShouldBeNull();
        apiToken.ShouldBeNull();
    }

    [Fact]
    public void TryResolve_InvalidUriInFile_Throws()
    {
        const string contents = """{ "endpoint": "not a uri" }""";
        FileConfigurationSource source = CreateSource(exists: true, contents: contents);

        InvalidConfigurationException exception = Should.Throw<InvalidConfigurationException>(
            () => source.TryResolve(out _, out _));
        exception.Message.ShouldContain("not an absolute URI");
    }

    [Fact]
    public void GetConfigFilePath_WhenHomeIsMissing_ReturnsNull()
    {
        var source = new FileConfigurationSource(() => null, _ => false, _ => string.Empty);

        source.GetConfigFilePath().ShouldBeNull();
    }

    private static FileConfigurationSource CreateSource(bool exists, string contents)
        => new(
            () => FakeHome,
            _ => exists,
            _ => contents);
}
