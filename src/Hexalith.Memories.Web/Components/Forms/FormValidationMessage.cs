// <copyright file="FormValidationMessage.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

using Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// A single field-associated or form-level contract-aware validation message.
/// </summary>
/// <remarks>
/// Story 17.3 (AC1) — messages expose only a localization key and a stable code; no raw field values are
/// embedded, so the same message is safe for visible text, accessible names, copied text, diagnostics,
/// logs, and snapshots.
/// </remarks>
/// <param name="FieldKey">The associated field key, or an empty string for a form-level message.</param>
/// <param name="Code">The machine-readable validation code.</param>
/// <param name="Classification">Whether the message blocks dispatch, requires acknowledgement, or is advisory.</param>
/// <param name="Severity">The display severity tier.</param>
/// <param name="MessageKey">The localization key for the message text.</param>
public sealed record FormValidationMessage(
    string FieldKey,
    FormValidationCode Code,
    FormMessageClassification Classification,
    InteractionSeverity Severity,
    string MessageKey);
