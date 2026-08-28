// <copyright file="DiagnosticStoreEntry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Represents a metadata-only diagnostic probe entry.</summary>
/// <param name="ResourceId">The safe resource identifier.</param>
/// <param name="ContentDigest">The safe bounded digest or sentinel token. Raw content is forbidden.</param>
public sealed record DiagnosticStoreEntry(string ResourceId, string ContentDigest);
