// <copyright file="SemanticKeyFamily.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

/// <summary>Identifies a registered semantic Redis hash key family.</summary>
internal enum SemanticKeyFamily
{
    /// <summary>The key and bounded record shape do not match a registered family.</summary>
    Unknown = 0,

    /// <summary>The key and bounded record shape match more than one registered family.</summary>
    Ambiguous,

    /// <summary>An active, unchunked raw semantic hash.</summary>
    ActiveRawBase,

    /// <summary>An active raw semantic chunk hash.</summary>
    ActiveRawChunk,

    /// <summary>An active current-namespace natural-language semantic hash.</summary>
    ActiveNaturalLanguage,

    /// <summary>A non-active raw semantic migration-staging hash.</summary>
    RawStaging,

    /// <summary>A non-active natural-language semantic migration-staging hash.</summary>
    NaturalLanguageStaging,

    /// <summary>A non-active legacy nested-namespace natural-language semantic hash.</summary>
    LegacyNaturalLanguage,
}
