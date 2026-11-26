// <copyright file="DegradationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Search;

using Hexalith.Memories.IntegrationTests.Fixtures;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed partial class DegradationIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public DegradationIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;
}
