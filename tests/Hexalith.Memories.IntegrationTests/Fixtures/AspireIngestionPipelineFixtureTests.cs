// <copyright file="AspireIngestionPipelineFixtureTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.AppHost;

using Microsoft.IdentityModel.JsonWebTokens;

using Shouldly;

/// <summary>Unit-level guards for <see cref="AspireIngestionPipelineFixture"/> helper methods.</summary>
public sealed class AspireIngestionPipelineFixtureTests
{
    [Fact]
    public void MintDevBearer_ExplicitPastExpiry_CreatesExpiredToken()
    {
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(-1);

        string token = AspireIngestionPipelineFixture.MintDevBearer(
            "tenant-expired-probe",
            expiresAt: expiresAt);

        var jwt = new JsonWebToken(token);
        jwt.ValidTo.ShouldBeLessThan(DateTime.UtcNow);
        jwt.ValidFrom.ShouldBeLessThan(jwt.ValidTo);
    }

    [Fact]
    public void RepositoryRootLocator_NestedCurrentDirectory_ReturnsMarkerDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"memories-root-{Guid.NewGuid():N}");
        string nested = Path.Combine(root, "src", "Hexalith.Memories.AppHost", "bin");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(root, "Hexalith.Memories.slnx"), string.Empty);

        try
        {
            string resolved = RepositoryRootLocator.Resolve(currentDirectory: nested, baseDirectory: nested);
            resolved.ShouldBe(Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RepositoryRootLocator_MissingMarker_Throws()
    {
        string root = Path.Combine(Path.GetTempPath(), $"memories-root-missing-{Guid.NewGuid():N}");
        string nested = Path.Combine(root, "bin");
        Directory.CreateDirectory(nested);

        try
        {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
                RepositoryRootLocator.Resolve(currentDirectory: nested, baseDirectory: nested));

            exception.Message.ShouldContain("Hexalith.Memories.slnx");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
