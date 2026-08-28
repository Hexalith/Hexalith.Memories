// <copyright file="DiagnosticStoreDeleteResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Reports whether a diagnostic entry existed and was structurally deleted.</summary>
/// <param name="Deleted"><see langword="true"/> when an entry was removed; otherwise <see langword="false"/>.</param>
public sealed record DiagnosticStoreDeleteResult(bool Deleted);
