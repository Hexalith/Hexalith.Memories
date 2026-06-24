// <copyright file="MemoriesFormField.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

/// <summary>
/// A single contract-aware form field declaration validated by <see cref="ContractAwareFormValidator"/>.
/// </summary>
/// <remarks>
/// Story 17.3 — the field carries a localization key for its visible label and the typed constraints the
/// validator enforces. Field values are never echoed back into messages without sanitization, and allowed
/// tokens/ranges originate from the upstream contract, not from web-only vocabularies.
/// </remarks>
/// <param name="FieldKey">Stable machine identifier used to associate messages with the field.</param>
/// <param name="LabelKey">Localization key for the visible field label.</param>
/// <param name="Kind">The validation kind for the field.</param>
/// <param name="Value">The current field value, or null when unset.</param>
/// <param name="Required">Whether the field must be present (applies to scope and text fields).</param>
/// <param name="AllowedTokens">Allowed contract enum tokens for <see cref="MemoriesFormFieldKind.ContractEnum"/>.</param>
/// <param name="Minimum">Inclusive minimum for <see cref="MemoriesFormFieldKind.NumericRange"/>.</param>
/// <param name="Maximum">Inclusive maximum for <see cref="MemoriesFormFieldKind.NumericRange"/>.</param>
public sealed record MemoriesFormField(
    string FieldKey,
    string LabelKey,
    MemoriesFormFieldKind Kind,
    string? Value,
    bool Required = false,
    IReadOnlyList<string>? AllowedTokens = null,
    double? Minimum = null,
    double? Maximum = null);
