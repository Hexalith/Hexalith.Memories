// <copyright file="StoredFusionWeights.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

/// <summary>Durable representation of retrieval-axis fusion weights.</summary>
internal sealed record StoredFusionWeights(
    double SyntacticWeight = 0.4,
    double SemanticWeight = 0.4,
    double GraphWeight = 0.2,
    double NlWeight = 0.2);
