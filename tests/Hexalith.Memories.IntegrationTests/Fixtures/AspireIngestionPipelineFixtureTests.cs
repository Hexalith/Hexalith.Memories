// <copyright file="AspireIngestionPipelineFixtureTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

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
}
