// <copyright file="RuntimeSecretStoreMigrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.Text.RegularExpressions;
using System.Xml.Linq;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

using YamlDotNet.RepresentationModel;

using static ProductionDeploymentArtifactsTests;

public sealed partial class RuntimeSecretStoreMigrationTests
{
    private const string Artifacts = "_bmad-output/implementation-artifacts/";
    private const string Story = Artifacts + "31-2-runtime-dapr-secret-store-migration.md";
    private const string Evidence = Artifacts + "tests/31-2-runtime-secret-store-evidence.md";
    private const string Inventory = "Runtime Kubernetes Secret inventory";
    private const string Component = "deploy/kubernetes/base/dapr/secretstore.yaml";
    private const string Dapr119ProductionOverlay = "production-dapr-1.19";
    private static readonly string[] Prerequisites = ["C1", "C2", "C3", "C4a", "C5a", "C6"];

    [Fact]
    public void ActivationGate_MatchesEpicAndCurrentCheckpointSet()
    {
        string root = GetRepoRoot();
        ValidateActivationGate(
            Read(root, "_bmad-output/planning-artifacts/epics.md"),
            Read(root, Story),
            Read(root, Artifacts + "31-1-openbao-platform-hardening-and-documentation.md"));
    }

    private static void ValidateActivationGate(string epicText, string storyText, string platformText)
    {
        var epic = new MarkdownContractDocument(epicText);
        string epicSection = epic.GetSection("Story 31.2: Runtime Dapr Secret-Store Migration to `hashicorp.vault`");
        var story = new MarkdownContractDocument(storyText);
        string task = story.GetSection("Tasks / Subtasks");
        string[] gates =
        [
            epicSection.Split("**Activation gate:**", StringSplitOptions.None)[1].Split("**Scope boundary:**", StringSplitOptions.None)[0],
            story.GetSection("Blocking Activation Gate"),
            task[..task.IndexOf("Task 1 -", StringComparison.Ordinal)],
        ];
        foreach (string gate in gates)
        {
            // Only the operative clause enumerates prerequisites. Later text records historical changes and excluded rows.
            int end = gate.IndexOf("complete`", StringComparison.Ordinal);
            end.ShouldBeGreaterThan(0);
            Regex.IsMatch(gate[..end], @"(?:all read|each occur exactly once and read)\s*`$").ShouldBeTrue();
            Regex.Matches(gate[..end], @"\bC\d+[a-z]?\b").Select(match => match.Value).ShouldBe(Prerequisites, ignoreOrder: true);
            string requirements = gate
                .Replace("C4b/C5b/C7 do not enter this migration gate.", "", StringComparison.Ordinal)
                .Replace("The dispositions of C4b, C5b and C7 belong to Story 31.1.", "", StringComparison.Ordinal)
                .Replace("At the time, C4b, C5b and C7 awaited independent countersignatures", "", StringComparison.Ordinal)
                .Replace("Do not require Story 31.1 itself to be `done`.", "", StringComparison.Ordinal)
                .Replace("the gate previously bound on Story 31.1 reaching `done`.", "", StringComparison.Ordinal)
                .Replace("retained the superseded `done` condition", "", StringComparison.Ordinal)
                .Replace("reintroducing the `done` condition", "", StringComparison.Ordinal);
            Regex.Matches(requirements, @"\bC\d+[a-z]?\b").Select(match => match.Value).ShouldBe(Prerequisites, ignoreOrder: true);
            Regex.IsMatch(requirements, @"(?i)\bdone\b").ShouldBeFalse("The activation gate must depend only on documentation checkpoints.");
        }

        var platform = new MarkdownContractDocument(platformText);
        var rows = platform.GetTableRows("Implementation Checkpoints");
        foreach (string checkpoint in Prerequisites)
        {
            var matches = rows.Where(row => row[0].StartsWith(checkpoint + " -", StringComparison.Ordinal)).ToArray();
            matches.Length.ShouldBe(1, checkpoint);
            Regex.IsMatch(matches[0][4], @"^complete(?: — .+)?$").ShouldBeTrue(checkpoint);
        }
    }

    [Fact]
    public void ActivationGate_RejectsMissingDuplicateIncompleteAndStatusMutations()
    {
        string root = GetRepoRoot();
        string epic = Read(root, "_bmad-output/planning-artifacts/epics.md");
        string story = Read(root, Story);
        string platform = Read(root, Artifacts + "31-1-openbao-platform-hardening-and-documentation.md");
        const string tableStart = "## Implementation Checkpoints";
        string row = platform[platform.IndexOf(tableStart, StringComparison.Ordinal)..].Split('\n')
            .Single(line => line.StartsWith("| C4a -", StringComparison.Ordinal));
        foreach (string replacement in new[] { "", row + "\n" + row, row.Replace("| complete |", "| pending |", StringComparison.Ordinal) })
        {
            Should.Throw<ShouldAssertException>(() => ValidateActivationGate(epic, story, platform.Replace(row, replacement, StringComparison.Ordinal)));
        }

        // Exercise each gate's own checkpoint list as well as missing rows in the prerequisite table.
        const string epicList = "C4a, C5a and C6 all read";
        Should.Throw<ShouldAssertException>(() => ValidateActivationGate(
            epic.Replace(epicList, "C5a and C6 all read", StringComparison.Ordinal), story, platform));
        foreach (string ending in new[] { "all read", "each occur" })
        {
            string list = "C4a, C5a, and C6 " + ending;
            Should.Throw<ShouldAssertException>(() => ValidateActivationGate(
                epic, story.Replace(list, "C5a and C6 " + ending, StringComparison.Ordinal), platform));
        }

        const string oldGate = "Story 31.1 must be done. ";
        string epicGate = "**Activation gate:** Story 31.2";
        Should.Throw<ShouldAssertException>(() => ValidateActivationGate(
            epic.Replace(epicGate, "**Activation gate:** " + oldGate + "Story 31.2", StringComparison.Ordinal), story, platform));
        foreach (string marker in new[] { "**Do not start implementation until", "Re-read Story 31.1's" })
        {
            Should.Throw<ShouldAssertException>(() => ValidateActivationGate(
                epic, story.Replace(marker, oldGate + marker, StringComparison.Ordinal), platform));
        }
    }

    [Fact]
    public void KubernetesRuntimeStore_UsesVaultBootstrapTlsAndBothScopes()
    {
        string root = GetRepoRoot();
        // Epic 29 owns deploy/dapr. Check every Kubernetes source manifest and every rendered overlay.
        var files = Directory.EnumerateFiles(Path.Combine(root, "deploy/kubernetes"), "*", SearchOption.AllDirectories)
            .Where(file => Path.GetExtension(file) is ".yaml" or ".yml" or ".json");
        foreach (string file in files)
        {
            foreach (YamlMappingNode resource in Resources(File.ReadAllText(file)).Where(IsRuntimeStore))
            {
                VerifyRuntimeStore(resource);
            }
        }

        VerifyRuntimeStore(Resources(Read(root, Component)).Single(IsRuntimeStore));
        string[] overlays = Directory.EnumerateDirectories(Path.Combine(root, "deploy/kubernetes/overlays"))
            .Select(static directory => Path.GetFileName(directory)).Order(StringComparer.Ordinal).ToArray();
        overlays.ShouldContain("production");
        overlays.ShouldContain("qualification");
        overlays.ShouldContain(Dapr119ProductionOverlay);
        foreach (string overlay in overlays)
        {
            var resources = Resources(Run(root, "kubectl", "kustomize", "deploy/kubernetes/overlays/" + overlay)).ToList();
            VerifyRuntimeStore(resources.Single(IsRuntimeStore));
            var appIds = resources.Where(resource => resource.Children.TryGetValue("kind", out var kind)
                    && kind.ToString() is "Deployment" or "StatefulSet" or "DaemonSet" or "Job" or "CronJob")
                .Select(resource => resource["kind"].ToString() == "CronJob"
                    ? resource["spec"]["jobTemplate"]["spec"]["template"] : resource["spec"]["template"])
                .Select(template => (YamlMappingNode)template["metadata"])
                .Where(meta => meta.Children.TryGetValue("annotations", out var annotations)
                    && ((YamlMappingNode)annotations).Children.ContainsKey("dapr.io/app-id"))
                .Select(meta => meta["annotations"]["dapr.io/app-id"].ToString()).ToArray();
            appIds.ShouldContain("memories");
            appIds.ShouldContain("memories-mcp");
            appIds.ShouldNotContain("eventstore", "Recheck the reserved scope against live workloads and require a scoped read on deployment.");
            var telemetry = resources.Single(item => IsResource(item, "Component", "access-telemetry-secrets"));
            string[] telemetryScopes = overlay is "production" or Dapr119ProductionOverlay
                ? ["memories-access-telemetry", "memories-access-telemetry-clock"]
                : ["memories", "memories-access-telemetry", "memories-access-telemetry-clock"];
            VerifyVaultStore(telemetry, "access-telemetry", telemetryScopes);
            foreach (string name in new[] { "statestore", "pubsub", "llm-openai", "access-telemetry-config", "access-telemetry-store" })
            {
                var consumer = resources.Single(item => IsResource(item, "Component", name));
                consumer["auth"]["secretStore"].ToString().ShouldBe(name.StartsWith("access-telemetry-", StringComparison.Ordinal)
                    ? "access-telemetry-secrets" : "secretstore");
            }

            if (overlay == Dapr119ProductionOverlay)
            {
                VerifyDapr119ProductionBoundary(resources);
            }

            // Every other component reference must resolve through an OpenBao store, never the Kubernetes default.
            foreach (YamlMappingNode component in resources.Where(item => IsKind(item, "Component")))
            {
                string name = component["metadata"]["name"].ToString();
                if (name is not ("secretstore" or "access-telemetry-secrets") && HasSecretKeyRef(component))
                {
                    (SecretStoreOf(component) is "secretstore" or "access-telemetry-secrets")
                        .ShouldBeTrue($"{overlay}: component {name} must resolve its secretKeyRef through an OpenBao store.");
                }
            }
        }
    }

    private static void VerifyDapr119ProductionBoundary(IReadOnlyList<YamlMappingNode> resources)
    {
        const string sidecar = "ghcr.io/dapr/daprd:1.19.2@sha256:7839fbb66d4be24ec52e347bec2dca66a91bef3079d8c8d57676f6d3ef92654f";
        string[] workloadNames = ["memories", "memories-mcp", "memories-access-telemetry", "memories-access-telemetry-clock"];
        foreach (string workloadName in workloadNames)
        {
            var deployment = resources.Single(resource => IsResource(resource, "Deployment", workloadName));
            var annotations = deployment["spec"]["template"]["metadata"]["annotations"];
            annotations["dapr.io/sidecar-image"].ToString().ShouldBe(sidecar);
            string configurationName = annotations["dapr.io/config"].ToString();
            var configuration = resources.Single(resource => IsResource(resource, "Configuration", configurationName));
            var scopes = (YamlSequenceNode)configuration["spec"]["secrets"]["scopes"];
            var kubernetes = (YamlMappingNode)scopes.Children.Single(scope => scope["storeName"].ToString() == "kubernetes");
            kubernetes["defaultAccess"].ToString().ShouldBe("deny");
            kubernetes.Children.Keys.Select(key => key.ToString()).ShouldBe(["storeName", "defaultAccess"], ignoreOrder: true);
        }

        VerifyNamedSecretGrants(resources, "memories-dapr-secret-reader",
            ["app-api-token", "dapr-api-token", "redis-secret", "openbao-runtime-bootstrap"]);
        VerifyNamedSecretGrants(resources, "memories-access-telemetry-secret-reader",
            ["redis-secret", "app-api-token", "dapr-api-token", "openbao-access-telemetry-bootstrap"]);
    }

    private static void VerifyNamedSecretGrants(IReadOnlyList<YamlMappingNode> resources, string roleName, string[] expectedNames)
    {
        var role = resources.Single(resource => IsResource(resource, "Role", roleName));
        var rule = ((YamlSequenceNode)role["rules"]).Children.Single();
        var names = (YamlSequenceNode)rule["resourceNames"];
        names.Children.Select(name => name.ToString()).ShouldBe(expectedNames, ignoreOrder: true);
        ((YamlSequenceNode)rule["verbs"]).Children.Select(verb => verb.ToString()).ShouldBe(["get"]);
    }

    private static bool IsKind(YamlMappingNode resource, string kind)
        => resource.Children.TryGetValue("kind", out var type) && type.ToString() == kind;

    private static string? SecretStoreOf(YamlMappingNode resource)
        => resource.Children.TryGetValue("auth", out var auth) && auth is YamlMappingNode map
            && map.Children.TryGetValue("secretStore", out var store) ? store.ToString() : null;

    private static bool HasSecretKeyRef(YamlMappingNode resource)
        => resource.Children.TryGetValue("spec", out var spec) && spec is YamlMappingNode map
            && map.Children.TryGetValue("metadata", out var metadata) && metadata is YamlSequenceNode items
            && items.Children.OfType<YamlMappingNode>().Any(static item => item.Children.ContainsKey("secretKeyRef"));

    private static void VerifyRuntimeStore(YamlMappingNode resource)
        => VerifyVaultStore(resource, "runtime", ["eventstore", "memories"]);

    private static void VerifyVaultStore(YamlMappingNode resource, string suffix, string[] scopes)
    {
        resource["spec"]["type"].ToString().ShouldBe("secretstores.hashicorp.vault");
        resource["spec"]["version"].ToString().ShouldBe("v1");
        ((YamlSequenceNode)resource["scopes"]).Children.Select(scope => scope.ToString()).ShouldBe(scopes, ignoreOrder: true);
        var metadata = ((YamlSequenceNode)resource["spec"]["metadata"]).Children.ToDictionary(item => item["name"].ToString());
        // An extra key such as vaultTokenMountPath, caCert or caPath would change authentication or trust.
        metadata.Keys.ShouldBe(
            ["vaultAddr", "tlsServerName", "skipVerify", "vaultKVPrefix", "vaultKVUsePrefix", "enginePath", "vaultValueType", "caPem", "vaultToken"],
            ignoreOrder: true);
        Dictionary<string, string> values = new()
        {
            ["vaultAddr"] = "https://hexalith-keys.openbao.svc.cluster.local:8200",
            ["tlsServerName"] = "hexalith-keys.openbao.svc.cluster.local",
            ["skipVerify"] = "false",
            ["vaultKVPrefix"] = "hexalith/memories/" + suffix,
            ["vaultKVUsePrefix"] = "true",
            ["enginePath"] = "secret",
            ["vaultValueType"] = "map",
        };
        foreach (var pair in values)
        {
            // Compare as booleans. A misconfigured field could carry sensitive text.
            (metadata[pair.Key]["value"].ToString() == pair.Value).ShouldBeTrue(pair.Key);
        }

        foreach (var (name, key) in new[] { ("caPem", "ca.pem"), ("vaultToken", "token") })
        {
            ((YamlMappingNode)metadata[name]).Children.ContainsKey("value").ShouldBeFalse(name);
            metadata[name]["secretKeyRef"]["name"].ToString().ShouldBe($"openbao-{suffix}-bootstrap");
            metadata[name]["secretKeyRef"]["key"].ToString().ShouldBe(key);
        }

        var spec = (YamlMappingNode)resource["spec"];
        (!spec.Children.TryGetValue("ignoreErrors", out var ignore) || ignore.ToString() == "false").ShouldBeTrue();
        (!resource.Children.TryGetValue("auth", out var auth) || auth["secretStore"].ToString() == "kubernetes").ShouldBeTrue();
    }

    private static IEnumerable<YamlMappingNode> Resources(string text)
    {
        var yaml = new YamlStream();
        using var reader = new StringReader(text);
        yaml.Load(reader);
        return yaml.Documents.Select(doc => doc.RootNode).OfType<YamlMappingNode>();
    }

    private static bool IsRuntimeStore(YamlMappingNode resource) => IsResource(resource, "Component", "secretstore");

    private static bool IsResource(YamlMappingNode resource, string kind, string name)
        => resource.Children.TryGetValue("kind", out var type) && type.ToString() == kind && resource["metadata"]["name"].ToString() == name;

    [Fact]
    public void Inventory_CoversJulyAndMeasuredNamesInBothDirections()
    {
        string root = GetRepoRoot();
        var doc = new MarkdownContractDocument(Read(root, "docs/operations/openbao.md"));
        doc.GetTableHeader(Inventory).ShouldBe(["Secret", "Namespace", "Referenced by", "Justification class", "Owner", "Reopen trigger"]);
        var rows = doc.GetTableRows(Inventory);
        foreach (var row in rows)
        {
            row.All(cell => !string.IsNullOrWhiteSpace(cell) && !Regex.IsMatch(cell, @"(?i)^(?:tbd|todo|n/a|—|-)$")).ShouldBeTrue();
            row[3].ShouldBeOneOf("openbao-bootstrap", "direct-pod-input", "story-27.3-retained", "platform-31.1", "residue");
        }

        string history = new MarkdownContractDocument(Read(root, Story)).GetSection("Measured Runtime State At Creation");
        var expected = Regex.Matches(history.Split("**Kubernetes Secrets present", StringSplitOptions.None)[1], @"(?m)^\| `([\w-]+)` \|")
            .Select(m => ("hexalith-memories", m.Groups[1].Value)).ToHashSet();
        expected.Count.ShouldBe(15);
        var measured = new MarkdownContractDocument(Read(root, Evidence)).GetTableRows("Current Secret names and types");
        foreach (var row in measured)
        {
            row.Count.ShouldBe(2);
            row.All(cell => !string.IsNullOrWhiteSpace(cell)).ShouldBeTrue();
            expected.Add(("hexalith-memories", row[0]));
        }

        foreach (string platform in new[] { "openbao-seal", "openbao-server-tls", "hexalith-keys-pki", "openbao-operator-credentials" })
        {
            expected.Add(("openbao", platform));
        }

        rows.Select(row => (row[1], row[0])).ShouldBe(expected, ignoreOrder: true);
    }

    // Only the three infrastructure/composition boundaries are excluded. EventStore and ServiceDefaults ship inside
    // the Server and host products, so they are scanned. New product projects enter automatically.
    private static readonly HashSet<string> BoundaryProjects = ["AppHost", "Aspire", "Redis"];
    private const string Provider = @"(?ix)openbao|vaultsharp|hashicorp[.:_-]?vault|\b(?:I?(?:Bao|Vault)(?:Api|Http)?Client\w*|TokenAuthMethodInfo)\b|
        (?:vault|bao)[:_.-]*(?:addr(?:ess)?|endpoint|token|client|credential|role[_-]?id|secret[_-]?id|url|uri|config|approle)|
        KubernetesClient|ReadNamespacedSecret|ListNamespacedSecret|x-vault-(?:token|namespace)|https?://[^""'\s/]*(?:vault|:8200\b)|/v1/(?:auth/|sys/|secret/(?:data|metadata)/)|
        [""'](?:Vault|Bao)[""']|\bhv[sbr]\.[A-Za-z0-9_-]{8,}|[""'][sbr]\.[A-Za-z0-9_-]{20,}";

    [Fact]
    public void ProductProjects_DoNotAccessTheSecretProviderDirectly()
    {
        string root = GetRepoRoot();
        var files = Run(root, "git", "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", "src")
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
        // git ls-files --cached also lists deleted but unstaged files.
        files = files.Where(file => File.Exists(Path.Combine(root, file))).ToArray();
        var projects = files.Where(file => Path.GetExtension(file) == ".csproj")
            .Where(file => !BoundaryProjects.Contains(Path.GetFileNameWithoutExtension(file).Replace("Hexalith.Memories.", "", StringComparison.Ordinal)))
            .ToArray();
        projects.Length.ShouldBeGreaterThanOrEqualTo(14);
        foreach (string project in projects)
        {
            RejectProviderPackages(project, root);

            string directory = project[..(project.LastIndexOf('/') + 1)];
            foreach (string path in files.Where(file => file.StartsWith(directory, StringComparison.Ordinal)
                && (Path.GetExtension(file).ToLowerInvariant() is ".cs" or ".csproj" or ".json" or ".xml" or ".yaml" or ".yml"
                    or ".razor" or ".props" or ".targets" or ".config" or ".http" or ".js" or ".ts" or ".resx" or ".ini" or ".toml"
                    || Path.GetFileName(file).StartsWith(".env", StringComparison.Ordinal))))
            {
                RejectProviderReference(Read(root, path), path);
            }
        }

        // Central build and package files apply to every product project.
        foreach (string path in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
        {
            RejectProviderPackages(path, root);
            RejectProviderReference(Read(root, path), path);
        }
    }

    private static void RejectProviderPackages(string path, string root)
    {
        XDocument xml = XDocument.Load(Path.Combine(root, path));
        foreach (var package in xml.Descendants().Where(el => el.Name.LocalName is "PackageReference" or "PackageVersion"))
        {
            string reference = (string?)package.Attribute("Include") ?? (string?)package.Attribute("Update") ?? "";
            if (Regex.IsMatch(reference, @"(?i)(?:^|[._-])(?:vault|bao)(?:$|[._-])"))
            {
                throw new ShouldAssertException($"Direct provider package in {path}.");
            }

            RejectProviderReference(reference, path);
        }
    }

    private static void RejectProviderReference(string source, string path)
    {
        if (Regex.IsMatch(source, Provider, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5)))
        {
            throw new ShouldAssertException($"Direct provider access in {path}.");
        }
    }

    [Fact]
    public void PublishedMigrationEvidence_ContainsNoSecretMaterial()
    {
        string root = GetRepoRoot();
        OpenBaoPlatformDocumentationTests.ValidateSecretSafeRecord(Read(root, Evidence), Evidence);
        string inventory = new MarkdownContractDocument(Read(root, "docs/operations/openbao.md")).GetSection(Inventory);
        OpenBaoPlatformDocumentationTests.ValidateSecretSafeRecord(inventory, Inventory);
    }
}
