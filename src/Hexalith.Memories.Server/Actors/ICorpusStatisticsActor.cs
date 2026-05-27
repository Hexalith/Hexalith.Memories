// <copyright file="ICorpusStatisticsActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Dapr.Actors;

/// <summary>DAPR Actor interface for per-tenant corpus statistics caching.</summary>
public interface ICorpusStatisticsActor : IActor
{
    /// <summary>Gets the number of documents in the tenant's RediSearch index.</summary>
    /// <returns>The document count, or 0 if statistics are not yet available.</returns>
    Task<int> GetDocumentCountAsync();

    /// <summary>Gets the average document length in bytes for the tenant's corpus.</summary>
    /// <returns>The average document length, or 0.0 if statistics are not yet available.</returns>
    Task<double> GetAverageDocumentLengthAsync();

    /// <summary>Gets the full corpus statistics snapshot.</summary>
    /// <returns>The <see cref="CorpusStatistics"/> for this tenant.</returns>
    Task<CorpusStatistics> GetStatisticsAsync();
}
