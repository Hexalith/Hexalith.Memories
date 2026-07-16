// <copyright file="FusionWeights.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Configures the relative weight of each search axis in hybrid fusion scoring.</summary>
public sealed record FusionWeights
{
    /// <summary>Gets the weight for the syntactic (BM25) search axis.</summary>
    public double SyntacticWeight { get; init; } = 0.4;

    /// <summary>Gets the weight for the semantic (vector) search axis.</summary>
    public double SemanticWeight { get; init; } = 0.4;

    /// <summary>Gets the weight for the graph (proximity) search axis.</summary>
    public double GraphWeight { get; init; } = 0.2;

    /// <summary>Gets the weight for the natural-language semantic search axis.</summary>
    public double NlWeight { get; init; } = 0.2;

    /// <summary>Validates that all weights are non-negative and at least one is positive.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any weight is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when all weights are zero.</exception>
    public void Validate()
    {
        if (!double.IsFinite(SyntacticWeight))
        {
            throw new ArgumentOutOfRangeException(nameof(SyntacticWeight), SyntacticWeight, "Fusion weight must be a finite number.");
        }

        if (!double.IsFinite(SemanticWeight))
        {
            throw new ArgumentOutOfRangeException(nameof(SemanticWeight), SemanticWeight, "Fusion weight must be a finite number.");
        }

        if (!double.IsFinite(GraphWeight))
        {
            throw new ArgumentOutOfRangeException(nameof(GraphWeight), GraphWeight, "Fusion weight must be a finite number.");
        }

        if (!double.IsFinite(NlWeight))
        {
            throw new ArgumentOutOfRangeException(nameof(NlWeight), NlWeight, "Fusion weight must be a finite number.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(SyntacticWeight);
        ArgumentOutOfRangeException.ThrowIfNegative(SemanticWeight);
        ArgumentOutOfRangeException.ThrowIfNegative(GraphWeight);
        ArgumentOutOfRangeException.ThrowIfNegative(NlWeight);

        if (SyntacticWeight == 0.0 && SemanticWeight == 0.0 && GraphWeight == 0.0 && NlWeight == 0.0)
        {
            throw new ArgumentException("At least one fusion weight must be greater than zero.");
        }
    }
}
