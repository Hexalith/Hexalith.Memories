// <copyright file="SemanticSearchDimensionMismatchException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

/// <summary>Thrown when semantic search vectors are incompatible with configured embedding dimensions.</summary>
public sealed class SemanticSearchDimensionMismatchException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="SemanticSearchDimensionMismatchException"/> class.</summary>
    /// <param name="queryDimensions">The number of dimensions in the query embedding.</param>
    /// <param name="configuredDimensions">The configured number of embedding dimensions.</param>
    public SemanticSearchDimensionMismatchException(int queryDimensions, int configuredDimensions)
        : base($"Semantic search embedding dimensions do not match the tenant configuration. Query embedding has {queryDimensions} dimensions but the tenant expects {configuredDimensions}.")
    {
        QueryDimensions = queryDimensions;
        ConfiguredDimensions = configuredDimensions;
    }

    /// <summary>Initializes a new instance of the <see cref="SemanticSearchDimensionMismatchException"/> class.</summary>
    /// <param name="queryDimensions">The number of dimensions in the query embedding.</param>
    /// <param name="configuredDimensions">The configured number of embedding dimensions.</param>
    /// <param name="innerException">The underlying Redis or infrastructure exception.</param>
    public SemanticSearchDimensionMismatchException(int queryDimensions, int configuredDimensions, Exception innerException)
        : base(
            $"Semantic search embedding dimensions do not match the configured Redis Vector index. Query embedding has {queryDimensions} dimensions but the tenant expects {configuredDimensions}. Reindex the tenant after changing embedding dimensions.",
            innerException)
    {
        QueryDimensions = queryDimensions;
        ConfiguredDimensions = configuredDimensions;
    }

    /// <summary>Gets the number of dimensions in the query embedding.</summary>
    public int QueryDimensions { get; }

    /// <summary>Gets the configured number of embedding dimensions.</summary>
    public int ConfiguredDimensions { get; }
}