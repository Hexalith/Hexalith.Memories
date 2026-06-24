// <copyright file="FormValidationResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

/// <summary>
/// The outcome of validating a <see cref="MemoriesFormRequest"/> with <see cref="ContractAwareFormValidator"/>.
/// </summary>
/// <remarks>
/// Story 17.3 (AC1) — exposes the scope-first field order, the field-associated and form-level messages,
/// and the dispatch gate. <see cref="CanDispatch"/> is the single authority the form uses before routing a
/// submission through the existing command lifecycle.
/// </remarks>
/// <param name="OrderedFields">The declared fields reordered so tenant and case scope appear first.</param>
/// <param name="Messages">All validation messages, field-associated and form-level.</param>
/// <param name="RequiresAcknowledgement">Whether a dangerous-but-valid change must be acknowledged before dispatch.</param>
/// <param name="HasErrors">Whether any blocking validation error is present.</param>
/// <param name="CanDispatch">Whether the submission may be dispatched now.</param>
/// <param name="ContractSources">The distinct named contract fields that drove these messages, for traceability.</param>
public sealed record FormValidationResult(
    IReadOnlyList<MemoriesFormField> OrderedFields,
    IReadOnlyList<FormValidationMessage> Messages,
    bool RequiresAcknowledgement,
    bool HasErrors,
    bool CanDispatch,
    IReadOnlyList<string> ContractSources)
{
    /// <summary>Gets the messages associated with a specific field.</summary>
    /// <param name="fieldKey">The field key.</param>
    /// <returns>The field-associated messages, in order.</returns>
    public IReadOnlyList<FormValidationMessage> MessagesFor(string fieldKey)
        => [.. Messages.Where(m => string.Equals(m.FieldKey, fieldKey, StringComparison.Ordinal))];

    /// <summary>Gets the form-level messages not associated with any field.</summary>
    /// <returns>The form-level messages, in order.</returns>
    public IReadOnlyList<FormValidationMessage> FormLevelMessages()
        => [.. Messages.Where(m => m.FieldKey.Length == 0)];
}
