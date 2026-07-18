// <copyright file="RetentionConfigurationSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Authority from which the bounded retention value was obtained.</summary>
public enum RetentionConfigurationSource
{
    /// <summary>No source was supplied.</summary>
    Missing,

    /// <summary>Dapr configuration supplied the value.</summary>
    DaprConfiguration,

    /// <summary>The bounded Development default supplied the value.</summary>
    DevelopmentDefault,

    /// <summary>A test composition supplied a short-lived value.</summary>
    TestComposition,
}
