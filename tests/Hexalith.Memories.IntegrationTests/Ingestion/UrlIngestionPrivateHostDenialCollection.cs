// <copyright file="UrlIngestionPrivateHostDenialCollection.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

/// <summary>Runs the private-host denial topology without overlapping any shared fixture.</summary>
[CollectionDefinition(nameof(UrlIngestionPrivateHostDenialCollection), DisableParallelization = true)]
public sealed class UrlIngestionPrivateHostDenialCollection;
