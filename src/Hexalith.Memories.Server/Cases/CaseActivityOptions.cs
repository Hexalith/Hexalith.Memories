// <copyright file="CaseActivityOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Cases;

/// <summary>Configures bounded case activity projection storage.</summary>
internal sealed class CaseActivityOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Cases:Activity";

    /// <summary>Minimum retained activity stream entries per case.</summary>
    public const int MinStreamMaxLength = 50;

    /// <summary>Default retained activity stream entries per case.</summary>
    public const int DefaultStreamMaxLength = 500;

    /// <summary>Maximum retained activity stream entries per case.</summary>
    public const int MaxStreamMaxLength = 10_000;

    /// <summary>Gets or sets the approximate Redis stream maximum length per case.</summary>
    public int StreamMaxLength { get; set; } = DefaultStreamMaxLength;

    /// <summary>Clamps the configured stream maximum length to the safe envelope.</summary>
    /// <param name="value">The configured value.</param>
    /// <returns>The clamped stream maximum length.</returns>
    public static int ClampStreamMaxLength(int value)
        => Math.Clamp(value, MinStreamMaxLength, MaxStreamMaxLength);
}
