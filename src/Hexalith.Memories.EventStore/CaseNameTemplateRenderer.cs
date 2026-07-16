// <copyright file="CaseNameTemplateRenderer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Validated token-replacement renderer for <see cref="TenantEventRoutingOptions.CaseNameTemplate"/>.
/// Only the allow-listed tokens <c>{aggregateType}</c> and <c>{tenantId}</c> are accepted; any remaining
/// brace patterns are rejected up-front so the renderer never silently accepts unsupported placeholders or
/// composite-format fragments from configuration.</summary>
internal static class CaseNameTemplateRenderer
{
    internal const string TokenAggregateType = "{aggregateType}";

    internal const string TokenTenantId = "{tenantId}";

    /// <summary>Renders <paramref name="template"/> by substituting only the allow-listed tokens.</summary>
    /// <param name="template">The template string from configuration.</param>
    /// <param name="tenantId">The resolved tenant id.</param>
    /// <param name="aggregateType">The resolved aggregate type.</param>
    /// <returns>The rendered case name.</returns>
    internal static string Render(string template, string tenantId, string aggregateType)
    {
        ArgumentException.ThrowIfNullOrEmpty(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);

        string unsupported = template
            .Replace(TokenAggregateType, string.Empty, StringComparison.Ordinal)
            .Replace(TokenTenantId, string.Empty, StringComparison.Ordinal);
        if (unsupported.Contains('{', StringComparison.Ordinal) || unsupported.Contains('}', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Unsupported case-name template token in '{template}'. Allowed tokens: {TokenAggregateType}, {TokenTenantId}.",
                nameof(template));
        }

        return template
            .Replace(TokenAggregateType, aggregateType, StringComparison.Ordinal)
            .Replace(TokenTenantId, tenantId, StringComparison.Ordinal);
    }
}
