// <copyright file="AccessTelemetryYamlLeastPrivilegeValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using YamlDotNet.RepresentationModel;

/// <summary>Structure-aware validator for the exact lifecycle ACL grants and component scope.</summary>
internal static class AccessTelemetryYamlLeastPrivilegeValidator
{
    private static readonly string[] ExpectedGrants =
    [
        "access-telemetry-adapter|/v1/access-telemetry/physical-reclamation-evidence|POST|allow",
        "memories|/v1/access-telemetry/heartbeat|POST|allow",
        "memories|/v1/access-telemetry/validate|POST|allow",
        "memories|/v1/access-telemetry/write|POST|allow",
        "memories-access-telemetry-inspector|/v1/access-telemetry/inspect|GET|allow",
    ];

    private static readonly string[] ExpectedPolicyIds =
    [
        "access-telemetry-adapter",
        "memories",
        "memories-access-telemetry-inspector",
    ];

    private static readonly Dictionary<string, string> ExpectedNamespaces = new(StringComparer.Ordinal)
    {
        ["access-telemetry-adapter"] = "hexalith-memories-qualification",
        ["memories"] = "hexalith-memories",
        ["memories-access-telemetry-inspector"] = "hexalith-memories",
    };

    /// <summary>Parses a YAML string and requires exactly one mapping document.</summary>
    /// <param name="yaml">The YAML document text.</param>
    /// <returns>The parsed root mapping.</returns>
    public static YamlMappingNode LoadSingleMapping(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        var stream = new YamlStream();
        using var reader = new StringReader(yaml);
        stream.Load(reader);
        if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode root)
        {
            throw new InvalidDataException("Lifecycle YAML must contain exactly one mapping document.");
        }

        return root;
    }

    /// <summary>Parses and validates the authoritative lifecycle configuration and state component.</summary>
    /// <param name="lifecycleYaml">The lifecycle Dapr Configuration YAML.</param>
    /// <param name="componentYaml">The lifecycle Dapr state Component YAML.</param>
    public static void Validate(string lifecycleYaml, string componentYaml)
        => Validate(LoadSingleMapping(lifecycleYaml), LoadSingleMapping(componentYaml));

    /// <summary>Validates parsed YAML node trees against the exact least-privilege contract.</summary>
    /// <param name="lifecycleRoot">The lifecycle Dapr Configuration root.</param>
    /// <param name="componentRoot">The lifecycle Dapr state Component root.</param>
    public static void Validate(YamlMappingNode lifecycleRoot, YamlMappingNode componentRoot)
    {
        ArgumentNullException.ThrowIfNull(lifecycleRoot);
        ArgumentNullException.ThrowIfNull(componentRoot);

        RequireRequiredKeys(lifecycleRoot, "configuration root", "apiVersion", "kind", "metadata", "spec");
        RequireScalar(lifecycleRoot, "apiVersion", "dapr.io/v1alpha1", "configuration apiVersion");
        RequireScalar(lifecycleRoot, "kind", "Configuration", "configuration kind");
        RequireScalar(GetMapping(lifecycleRoot, "metadata", "configuration"), "name", "memories-access-telemetry-config", "configuration name");
        YamlMappingNode configurationSpec = GetMapping(lifecycleRoot, "spec", "configuration");
        RequireRequiredKeys(configurationSpec, "configuration spec", "features", "secrets", "accessControl");
        _ = GetSequence(configurationSpec, "features", "configuration spec");
        _ = GetMapping(configurationSpec, "secrets", "configuration spec");
        YamlMappingNode accessControl = GetMapping(configurationSpec, "accessControl", "configuration spec");
        RequireRequiredKeys(accessControl, "access control", "defaultAction", "trustDomain", "policies");
        RequireScalar(accessControl, "defaultAction", "deny", "access control default action");
        RequireScalar(accessControl, "trustDomain", "public", "access control trust domain");

        YamlSequenceNode policies = GetSequence(accessControl, "policies", "access control");
        var policyIds = new List<string>(policies.Children.Count);
        var grants = new List<string>();
        foreach (YamlNode policyNode in policies.Children)
        {
            YamlMappingNode policy = RequireMapping(policyNode, "policy");
            RequireRequiredKeys(policy, "policy", "appId", "namespace", "trustDomain", "defaultAction", "operations");
            string appId = GetScalar(policy, "appId", "policy");
            policyIds.Add(appId);
            if (!ExpectedNamespaces.TryGetValue(appId, out string? expectedNamespace))
            {
                throw new InvalidDataException($"Unexpected policy identity '{appId}'.");
            }

            RequireScalar(policy, "namespace", expectedNamespace, $"{appId} namespace");
            RequireScalar(policy, "trustDomain", "public", $"{appId} trust domain");
            RequireScalar(policy, "defaultAction", "deny", $"{appId} default action");

            YamlSequenceNode operations = GetSequence(policy, "operations", $"{appId} policy");
            foreach (YamlNode operationNode in operations.Children)
            {
                YamlMappingNode operation = RequireMapping(operationNode, $"{appId} operation");
                RequireRequiredKeys(operation, $"{appId} operation", "name", "httpVerb", "action");
                string operationName = GetScalar(operation, "name", $"{appId} operation");
                string action = GetScalar(operation, "action", $"{appId} operation");
                IReadOnlyList<string> verbs = GetScalarSequence(operation, "httpVerb", $"{appId} {operationName}");
                if (verbs.Count != 1)
                {
                    throw new InvalidDataException($"{appId} {operationName} must grant exactly one HTTP verb.");
                }

                grants.Add($"{appId}|{operationName}|{verbs[0]}|{action}");
            }
        }

        RequireExactSet(policyIds, ExpectedPolicyIds, "policy identities");
        RequireExactSet(grants, ExpectedGrants, "policy grants");

        RequireRequiredKeys(componentRoot, "component root", "apiVersion", "kind", "metadata", "spec", "auth", "scopes");
        RequireScalar(componentRoot, "apiVersion", "dapr.io/v1alpha1", "component apiVersion");
        RequireScalar(componentRoot, "kind", "Component", "component kind");
        RequireScalar(GetMapping(componentRoot, "metadata", "component"), "name", "access-telemetry-store", "component name");
        YamlMappingNode componentSpec = GetMapping(componentRoot, "spec", "component");
        RequireRequiredKeys(componentSpec, "component spec", "type", "version", "initTimeout", "metadata");
        RequireScalar(componentSpec, "type", "state.postgresql", "component type");
        RequireScalar(componentSpec, "version", "v2", "component version");
        RequireScalar(componentSpec, "initTimeout", "1m", "component init timeout");
        _ = GetSequence(componentSpec, "metadata", "component spec");
        RequireScalar(GetMapping(componentRoot, "auth", "component"), "secretStore", "access-telemetry-secrets", "component secret store");
        RequireExactSet(
            GetScalarSequence(componentRoot, "scopes", "component"),
            ["memories-access-telemetry"],
            "component scopes");
    }

    private static string GetScalar(YamlMappingNode parent, string key, string context)
    {
        if (!parent.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ||
            value is not YamlScalarNode scalar ||
            string.IsNullOrEmpty(scalar.Value))
        {
            throw new InvalidDataException($"{context} must contain the non-empty scalar '{key}'.");
        }

        return scalar.Value;
    }

    private static IReadOnlyList<string> GetScalarSequence(YamlMappingNode parent, string key, string context)
    {
        YamlSequenceNode sequence = GetSequence(parent, key, context);
        var values = new List<string>(sequence.Children.Count);
        foreach (YamlNode child in sequence.Children)
        {
            if (child is not YamlScalarNode scalar || string.IsNullOrEmpty(scalar.Value))
            {
                throw new InvalidDataException($"{context} '{key}' must contain only non-empty scalars.");
            }

            values.Add(scalar.Value);
        }

        return values;
    }

    private static YamlMappingNode GetMapping(YamlMappingNode parent, string key, string context)
    {
        if (!parent.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value))
        {
            throw new InvalidDataException($"{context} is missing mapping '{key}'.");
        }

        return RequireMapping(value, $"{context} '{key}'");
    }

    private static YamlSequenceNode GetSequence(YamlMappingNode parent, string key, string context)
    {
        if (!parent.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) || value is not YamlSequenceNode sequence)
        {
            throw new InvalidDataException($"{context} must contain sequence '{key}'.");
        }

        return sequence;
    }

    private static YamlMappingNode RequireMapping(YamlNode node, string context)
        => node as YamlMappingNode ?? throw new InvalidDataException($"{context} must be a mapping.");

    private static void RequireRequiredKeys(YamlMappingNode mapping, string context, params string[] required)
    {
        string[] actual = mapping.Children.Keys
            .Select(key => key is YamlScalarNode scalar && scalar.Value is not null
                ? scalar.Value
                : throw new InvalidDataException($"{context} contains a non-scalar key."))
            .ToArray();
        string[] missing = required.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (missing.Length != 0)
        {
            throw new InvalidDataException($"{context} is missing required keys [{string.Join(", ", missing)}].");
        }
    }

    private static void RequireExactSet(
        IEnumerable<string> actualValues,
        IEnumerable<string> expectedValues,
        string context)
    {
        string[] actual = actualValues.Order(StringComparer.Ordinal).ToArray();
        string[] expected = expectedValues.Order(StringComparer.Ordinal).ToArray();
        if (actual.Length != expected.Length || !actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Unexpected {context}. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
        }
    }

    private static void RequireScalar(
        YamlMappingNode parent,
        string key,
        string expected,
        string context)
    {
        string actual = GetScalar(parent, key, context);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unexpected {context}. Expected '{expected}', actual '{actual}'.");
        }
    }
}
