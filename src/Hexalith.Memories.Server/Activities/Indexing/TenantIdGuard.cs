namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Text.RegularExpressions;

/// <summary>Validates tenant identifiers before they are used in Redis or FalkorDB resource names.</summary>
internal static partial class TenantIdGuard
{
    private static readonly HashSet<string> _reservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "statestore",
        "memories",
        "dapr",
        "system",
        "admin",
        "default",
        "global",
    };

    /// <summary>Gets the canonical reserved tenant names.</summary>
    internal static IReadOnlySet<string> ReservedNames => _reservedNames;

    /// <summary>Validates that the tenant ID is well-formed and not a reserved name.</summary>
    /// <param name="tenantId">The tenant identifier to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the tenant ID is invalid or reserved.</exception>
    public static void Validate(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!SafeTenantIdRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(
                $"TenantId '{tenantId}' contains invalid characters. Only alphanumeric and hyphens are allowed.",
                nameof(tenantId));
        }

        if (_reservedNames.Contains(tenantId))
        {
            throw new ArgumentException(
                $"'{tenantId}' is a reserved name and cannot be used as a tenant ID.",
                nameof(tenantId));
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\-]+$")]
    private static partial Regex SafeTenantIdRegex();
}
