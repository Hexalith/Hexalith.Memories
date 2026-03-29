namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Text.RegularExpressions;

/// <summary>Validates tenant identifiers before they are used in Redis or FalkorDB resource names.</summary>
internal static partial class TenantIdGuard
{
    public static void Validate(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!SafeTenantIdRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(
                $"TenantId '{tenantId}' contains invalid characters. Only alphanumeric and hyphens are allowed.",
                nameof(tenantId));
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\-]+$")]
    private static partial Regex SafeTenantIdRegex();
}
