// <copyright file="OpenBaoPlatformDocumentationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>Story 31.1 — executable guard binding <c>docs/operations/openbao.md</c> to the deployed
/// OpenBao <c>hexalith-keys</c> platform and to the four manifests under <c>deploy/openbao/</c>.
/// <para>
/// This class exists because every configuration change measured on that platform reached it without a
/// commit: the only commit that has ever touched <c>deploy/openbao/**</c> or the operations document is
/// <c>4d2e4e2f</c>, while the running release advanced across nine Helm revisions with the namespace
/// profile-hash annotations unchanged. A drift guard that reads only the manifest would have missed all
/// of it, so the measured voter count and HA mode are pinned here as literals (AC2, amended 2026-07-28)
/// and each documented value is additionally tied to its manifest source the way
/// <see cref="DeploymentConfigurationContractTests"/> does.
/// </para>
/// Structure-aware throughout: exact table headers, rows, and counts via
/// <see cref="MarkdownContractDocument"/>, narrative claims bound to their exact ATX section, and
/// anti-corruption via <see cref="ContractDocumentGuard"/>. Whole-document vocabulary never satisfies an
/// authoritative claim.</summary>
public sealed class OpenBaoPlatformDocumentationTests
{
    private const string DocRelativePath = "docs/operations/openbao.md";
    private const string EvidenceRelativePath =
        "_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md";

    private const string LimitationsHeading = "Accepted limitations";
    private const string ProfileHeading = "Deployed profile as measured";
    private const string SealLimitationKey = "Static file-based seal";
    private const string IngressLimitationKey = "Namespace-wide port 8200 ingress";

    // AC2 (amended 2026-07-28): the documented availability profile must match the platform measured on
    // 2026-07-28, not what deploy/openbao/values.yaml declares. Changing either literal below is a claim
    // that the running platform changed, and must be re-measured before it is made.
    private const string MeasuredRaftVoters = "3";
    private const string MeasuredHaMode = "ha_enabled: true";

    private const string SmokeTestApplyCommand = "kubectl apply -f deploy/openbao/smoke-test.yaml";
    private const string SmokeTestWaitCommand =
        "kubectl -n openbao wait --for=condition=complete job/hexalith-keys-smoke-test --timeout=2m";

    /// <summary>Vocabulary AC2 forbids around either accepted limitation. With three Raft voters a reader
    /// can easily mistake process-level failover for real availability, so the ban covers the whole
    /// section including its narrative subsections, not just the table cells.</summary>
    private static readonly string[] ForbiddenStrengthClaims =
        ["hardened", "production-HA", "highly available", "production-ready"];

    /// <summary>Long digest-shaped runs that are published on purpose and are not key material: image and
    /// chart digests, the three profile manifest hashes and their composite, the story baseline commit,
    /// and the runner-derived sorted method-set hashes. Any other long run in either record fails the
    /// AC3 guard, so a pasted key cannot hide among them.</summary>
    private static readonly HashSet<string> AllowedPublishedDigests = new(StringComparer.OrdinalIgnoreCase)
    {
        "900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653",
        "1c2e01185430b9bc426da870909fdccfbb4e3e4758f0c6f8cccfbceead4381ff",
        "1deba6e0456bb44ea0624a0f436b209b5ede2c496cc9be98fea5b9dbee1db539",
        "f55ff3c237fad5047d6ad7d19a56a83c6546a2f31fc5830022bc3b3a51c9c8e3",
        "f3fe70b98c64ec9072bc3ab54fd07ffdde1e4d4550c53d8f20e2bb58eb70f3eb",
        "4183b741eac062d962a8ff1860a7aa049719a75f47e38e6fdcfb0fe1aeaa5d45",
        "bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0",
        "2f99c8cd0c4a4aceb0296e78eabae46aec947034cdd32b4583437a4640c2630b",
        "327d1a9d7eaef063c656a6af9df4eea84f47ca30",
    };

    [Fact]
    public void OpenBaoPlatformRecords_Exist()
    {
        File.Exists(ResolveRepoPath(DocRelativePath)).ShouldBeTrue(
            $"OpenBao operations document not found at {DocRelativePath}");
        File.Exists(ResolveRepoPath(EvidenceRelativePath)).ShouldBeTrue(
            $"Story 31.1 platform evidence not found at {EvidenceRelativePath}");
    }

    [Fact]
    public void AcceptedLimitations_HaveExactHeaderTwoKeyedRowsAndNoEmptyCell()
    {
        var document = new MarkdownContractDocument(ReadDoc());

        document.GetTableHeader(LimitationsHeading).ShouldBe(
            ["Limitation", "Owner", "Consequence", "Compensating controls", "Reopen trigger"]);

        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows(LimitationsHeading);
        rows.Count.ShouldBe(2, "AC2 accepts exactly two limitations; adding or dropping one is a scope change.");
        rows.Select(static row => row[0]).ShouldBe([SealLimitationKey, IngressLimitationKey]);

        foreach (IReadOnlyList<string> row in rows)
        {
            foreach (string cell in row)
            {
                // AC2 requires owner, consequence, compensating controls, and reopen trigger to be
                // substantive. A blank or placeholder cell records an acceptance nobody owns.
                cell.ShouldNotBeNullOrWhiteSpace($"Limitation row '{row[0]}' has an empty cell.");
                cell.Trim().ShouldNotBeOneOf(
                    ["TBD", "TODO", "N/A", "-", "—", "?"],
                    $"Limitation row '{row[0]}' has placeholder cell '{cell}'.");
                cell.Trim().Length.ShouldBeGreaterThan(
                    12,
                    $"Limitation row '{row[0]}' has cell '{cell}', too short to be a substantive acceptance record.");
            }
        }
    }

    [Fact]
    public void AcceptedLimitations_AreNeverDescribedWithStrengthVocabulary()
    {
        var document = new MarkdownContractDocument(ReadDoc());

        // The whole section, including both narrative subsections, is in scope: AC2 bans the claim in the
        // rows AND in any surrounding prose about either limitation. Whitespace is normalized first,
        // otherwise a multi-word claim such as "highly available" would evade the ban simply by falling
        // across a line break in this hard-wrapped document.
        string[] scopes =
        [
            NormalizeWhitespace(document.GetSection(LimitationsHeading)),
            NormalizeWhitespace(document.GetSection(SealLimitationKey)),
            NormalizeWhitespace(document.GetSection(IngressLimitationKey)),
        ];

        foreach (string scope in scopes)
        {
            foreach (string claim in ForbiddenStrengthClaims)
            {
                scope.ShouldNotContain(
                    claim,
                    Case.Insensitive,
                    $"An accepted limitation must never be described as '{claim}' (AC2).");
            }
        }
    }

    [Fact]
    public void AvailabilityProfile_IsBoundToTheMeasuredPlatformNotTheManifest()
    {
        var document = new MarkdownContractDocument(ReadDoc());

        // Pinned to the measurement, not to values.yaml. A manifest reconciled without the document
        // following it, or a document still claiming a single voter, fails here.
        ReadProfileRow(document, "Raft voters")[1].ShouldContain(
            $"`{MeasuredRaftVoters}`",
            Case.Sensitive,
            $"The documented Raft voter count must be the measured `{MeasuredRaftVoters}`.");
        ReadProfileRow(document, "HA mode")[1].ShouldContain(
            $"`{MeasuredHaMode}`",
            Case.Sensitive,
            $"The documented HA mode must be the measured `{MeasuredHaMode}`.");

        // Both halves of the amended AC2 premise must be stated plainly: leader election exists between
        // the voters, and one Kubernetes node holds all of them. Compared whitespace-normalized, because
        // the document is hard-wrapped and a phrase may straddle a line break.
        string availability = NormalizeWhitespace(document.GetSection("Availability profile"));
        availability.ShouldContain("leader election", Case.Insensitive, "State that leader election exists between the voters.");
        availability.ShouldContain("node1", Case.Sensitive, "Name the single node that hosts every voter.");
        availability.ShouldContain("failure domain", Case.Insensitive, "State that the node is the entire failure domain.");

        // The manifest must agree with the measurement too, so a values.yaml-only drift is caught.
        string values = ReadRepoFile("deploy/openbao/values.yaml");
        values.ShouldContain(
            $"  ha:\n    enabled: true\n    replicas: {MeasuredRaftVoters}\n",
            Case.Sensitive,
            "deploy/openbao/values.yaml must declare the measured HA voter count.");
        values.ShouldNotContain("  standalone:\n    enabled: true", Case.Sensitive, "The standalone single-voter path was measured off.");
    }

    [Fact]
    public void SmokeTest_IsNamedExactlyAndItsExecutedResultIsRecorded()
    {
        var document = new MarkdownContractDocument(ReadDoc());
        string smokeTest = document.GetSection("Smoke test");

        smokeTest.ShouldContain(SmokeTestApplyCommand, Case.Sensitive, "AC1 requires the smoke test to be runnable with a named command.");
        smokeTest.ShouldContain(SmokeTestWaitCommand, Case.Sensitive, "AC1 requires the named completion command.");

        // A named command without a recorded outcome is not AC1's "recorded result".
        IReadOnlyList<IReadOnlyList<string>> result = document.GetTableRows("Recorded result");
        document.GetTableHeader("Recorded result").ShouldBe(["Field", "Observed value"]);
        result.Select(static row => row[0]).ShouldBe(
            ["`initialized`", "`sealed`", "`storage_type`", "`ha_enabled`", "`version`"]);
        result.Single(static row => row[0] == "`initialized`")[1].ShouldBe("`true`");
        result.Single(static row => row[0] == "`sealed`")[1].ShouldBe("`false`");
        result.Single(static row => row[0] == "`storage_type`")[1].ShouldBe("`raft`");
        result.Single(static row => row[0] == "`ha_enabled`")[1].ShouldBe("`true`");
        result.Single(static row => row[0] == "`version`")[1].ShouldBe("`2.6.0`");

        smokeTest.ShouldContain("31-1-openbao-platform-evidence.md", Case.Sensitive, "The recorded result must reference its evidence artifact.");
    }

    [Fact]
    public void OwnedManifests_EachHaveADocumentedSectionTiedToTheirSource()
    {
        var document = new MarkdownContractDocument(ReadDoc());

        // values.yaml — chart identity, digest pin, HA shape, TLS, seal, audit, registration, policy.
        string values = ReadRepoFile("deploy/openbao/values.yaml");
        ShouldAppearInBoth("fullnameOverride: hexalith-keys", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`fullnameOverride: hexalith-keys`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.image.tag`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("tlsDisable: false", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`global.tlsDisable: false`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("tls_min_version = \"tls12\"", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`global.tlsDisable: false`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("replicas: 3", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.ha.replicas: 3`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("storage \"raft\"", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.ha.raft.enabled: true`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("setNodeId: true", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.ha.raft.setNodeId: true`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("type: ClusterIP", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.service.type: ClusterIP`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("service_registration \"kubernetes\"", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.serviceAccount.serviceDiscovery.enabled: true`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("authDelegator", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.authDelegator.enabled: true`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("cert-manager", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.networkPolicy.ingress`"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("seal \"static\"", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.ha.raft.config` seal stanza"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("audit \"file\" \"persistent\"", ReadManifestRow(document, "`deploy/openbao/values.yaml`", "`server.ha.raft.config` audit stanza"), values, "deploy/openbao/values.yaml");
        ShouldAppearInBoth("openebs-hostpath-retain", ReadProfileRow(document, "Persistent volumes"), values, "deploy/openbao/values.yaml");

        // The reconciled profile must never be silently weakened back.
        values.ShouldNotContain("tls_disable = 1", Case.Sensitive, "TLS must stay enabled on the listener.");
        values.ShouldNotContain("type: LoadBalancer", Case.Sensitive, "The Service must stay ClusterIP-only.");

        // namespace.yaml — Restricted Pod Security and the two ownership annotations.
        string openBaoNamespace = ReadRepoFile("deploy/openbao/namespace.yaml");
        ShouldAppearInBoth("pod-security.kubernetes.io/enforce: restricted", ReadManifestRow(document, "`deploy/openbao/namespace.yaml`", "`pod-security.kubernetes.io/enforce: restricted`"), openBaoNamespace, "deploy/openbao/namespace.yaml");
        ShouldAppearInBoth("hexalith.io/platform-owner: jpiquot", ReadManifestRow(document, "`deploy/openbao/namespace.yaml`", "`hexalith.io/platform-owner: jpiquot`"), openBaoNamespace, "deploy/openbao/namespace.yaml");
        ShouldAppearInBoth("hexalith.io/security-reviewer: murat-tea-for-jpiquot", ReadManifestRow(document, "`deploy/openbao/namespace.yaml`", "`hexalith.io/security-reviewer: murat-tea-for-jpiquot`"), openBaoNamespace, "deploy/openbao/namespace.yaml");

        // service-account-hardening.yaml — the file's real effect, not the claim it used to carry.
        string hardening = ReadRepoFile("deploy/openbao/service-account-hardening.yaml");
        ShouldAppearInBoth("automountServiceAccountToken: false", ReadManifestRow(document, "`deploy/openbao/service-account-hardening.yaml`", "`automountServiceAccountToken: false`"), hardening, "deploy/openbao/service-account-hardening.yaml");

        string hardeningSection = NormalizeWhitespace(document.GetSection("`deploy/openbao/service-account-hardening.yaml`"));
        hardeningSection.ShouldContain("pod-level", Case.Insensitive, "The section must state that the pod-level setting overrides the ServiceAccount default.");
        hardeningSection.ShouldNotContain(
            "does not receive an unused Kubernetes API token",
            Case.Insensitive,
            "The pre-Story-31.1 claim was measurably false as deployed and must not return.");

        // smoke-test.yaml — endpoint, CA path, one-shot semantics.
        string smokeTest = ReadRepoFile("deploy/openbao/smoke-test.yaml");
        ShouldAppearInBoth("name: hexalith-keys-smoke-test", ReadManifestRow(document, "`deploy/openbao/smoke-test.yaml`", "`kind: Job`, `name: hexalith-keys-smoke-test`"), smokeTest, "deploy/openbao/smoke-test.yaml");
        ShouldAppearInBoth("https://hexalith-keys.openbao.svc.cluster.local:8200", ReadManifestRow(document, "`deploy/openbao/smoke-test.yaml`", "`BAO_ADDR: https://hexalith-keys.openbao.svc.cluster.local:8200`"), smokeTest, "deploy/openbao/smoke-test.yaml");
        ShouldAppearInBoth("/openbao/tls/ca.crt", ReadManifestRow(document, "`deploy/openbao/smoke-test.yaml`", "`BAO_CACERT: /openbao/tls/ca.crt`"), smokeTest, "deploy/openbao/smoke-test.yaml");
        ShouldAppearInBoth("ttlSecondsAfterFinished: 300", ReadManifestRow(document, "`deploy/openbao/smoke-test.yaml`", "`backoffLimit: 0`, `activeDeadlineSeconds: 60`, `ttlSecondsAfterFinished: 300`"), smokeTest, "deploy/openbao/smoke-test.yaml");
        smokeTest.ShouldNotContain("tls-skip-verify", Case.Sensitive, "The smoke test must keep verifying OpenBao's TLS identity.");
    }

    [Fact]
    public void PlatformRecords_ContainNoSecretShapedMaterial()
    {
        // AC3 / NFR9, asserted over both published records. Every check is negative: the guard proves the
        // absence of key-shaped material rather than the presence of a redaction note.
        foreach (string relativePath in new[] { DocRelativePath, EvidenceRelativePath })
        {
            string text = ReadRepoFile(relativePath);

            text.ShouldNotContain("-----BEGIN", Case.Insensitive, $"{relativePath} must contain no PEM block.");
            text.ShouldNotContain("-----END", Case.Insensitive, $"{relativePath} must contain no PEM block.");

            // Service (`hvs.`) and legacy (`s.`) OpenBao/Vault token shapes. The word boundary keeps
            // ordinary prose and namespaced identifiers such as `Server.Tests` out of scope.
            ShouldHaveNoMatch(text, @"\bhvs\.[A-Za-z0-9_-]{8,}", relativePath, "an `hvs.` service-token value");
            ShouldHaveNoMatch(text, @"\bs\.[A-Za-z0-9]{20,}", relativePath, "a legacy `s.` token value");

            // The labelled shapes `bao operator init` prints. Matching the label plus its colon keeps
            // ordinary prose about the seal, the recovery shares, and the revoked root token readable
            // while still rejecting a pasted init dump.
            ShouldHaveNoMatch(text, @"(?:Unseal|Recovery)\s+Key\s*\d*\s*:", relativePath, "an init-dump key label");
            ShouldHaveNoMatch(text, @"Initial\s+Root\s+Token\s*:", relativePath, "an init-dump root-token label");

            // Long undifferentiated runs that would shape like key material. Published digests are
            // allow-listed by exact value, so a new long run fails closed instead of blending in.
            IReadOnlyList<string> unexpected = FindUnexpectedLongRuns(text);
            unexpected.ShouldBeEmpty(
                $"{relativePath} contains long key-shaped run(s) that are not allow-listed published digests: {string.Join(", ", unexpected)}");
        }
    }

    [Fact]
    public void PlatformEvidence_RecordsExecutedSmokeTestAndReviewerState()
    {
        string evidence = ReadRepoFile(EvidenceRelativePath);

        // C3: an executed command with its observed outcome, not a synthesized payload.
        evidence.ShouldContain(SmokeTestApplyCommand, Case.Sensitive, "C3 requires the exact apply command.");
        evidence.ShouldContain(SmokeTestWaitCommand, Case.Sensitive, "C3 requires the exact wait command.");
        evidence.ShouldContain("condition met", Case.Sensitive, "C3 requires the observed completion outcome.");
        evidence.ShouldContain("\"ha_enabled\": true", Case.Sensitive, "C3 requires the observed status fields.");
        evidence.ShouldContain("\"storage_type\": \"raft\"", Case.Sensitive, "C3 requires the observed status fields.");

        // C7: either a dated named evaluation, or an accepted blocker that says so plainly. A
        // recommendation to seek review is neither, so the blocker record must carry all three parts.
        evidence.ShouldContain("Accepted blocker", Case.Insensitive, "C7 must record the reviewer state honestly.");
        evidence.ShouldContain("murat-tea-for-jpiquot", Case.Sensitive, "C7 must name the reviewer of record.");
        evidence.ShouldContain("Reopen trigger", Case.Insensitive, "An accepted blocker needs a reopen trigger.");
        evidence.ShouldContain("Consequence", Case.Insensitive, "An accepted blocker needs a stated consequence.");
    }

    [Fact]
    public void PlatformRecords_ContainNoLeakedToolCallMarkup()
    {
        foreach (string relativePath in new[] { DocRelativePath, EvidenceRelativePath })
        {
            IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(ReadRepoFile(relativePath));

            diagnostics.ShouldBeEmpty($"{relativePath} contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
        }
    }

    /// <summary>Collapses every whitespace run to one space so a narrative assertion is independent of
    /// where the hard-wrapped document happens to break a line.</summary>
    private static string NormalizeWhitespace(string text)
        => Regex.Replace(text, @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(5)).Trim();

    /// <summary>Finds runs long enough and dense enough to be key material, minus the digests these
    /// records publish on purpose.
    /// <para>
    /// Two shapes are scanned. A hexadecimal run of 32 or more characters covers digests, hashes, and
    /// hex-encoded keys. A base64-alphabet run of 40 or more characters covers encoded payloads, but only
    /// when it also mixes digits with upper and lower case — encoded key material always does, while a
    /// long PascalCase identifier such as a descriptive test-method name never does. Without that density
    /// requirement this guard reports its own test names, which would train a reader to ignore it.
    /// </para></summary>
    private static IReadOnlyList<string> FindUnexpectedLongRuns(string text)
    {
        var unexpected = new List<string>();
        foreach ((string pattern, bool requireMixedDensity) in new[] { ("[0-9a-fA-F]{32,}", false), ("[A-Za-z0-9+]{40,}", true) })
        {
            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.None, TimeSpan.FromSeconds(5)))
            {
                string value = match.Value;
                if (requireMixedDensity && !(value.Any(char.IsDigit) && value.Any(char.IsUpper) && value.Any(char.IsLower)))
                {
                    continue;
                }

                if (!AllowedPublishedDigests.Contains(value) && !unexpected.Contains(value))
                {
                    unexpected.Add(value);
                }
            }
        }

        return unexpected;
    }

    private static void ShouldHaveNoMatch(string text, string pattern, string relativePath, string description)
        => Regex.Matches(text, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5))
            .Count
            .ShouldBe(0, $"{relativePath} must not contain {description}.");

    private static void ShouldAppearInBoth(string literal, IReadOnlyList<string> contractRow, string source, string sourceName)
    {
        source.ShouldContain(literal, Case.Sensitive, $"'{literal}' must remain in its authoritative source {sourceName}.");
        string.Join('\n', contractRow).ShouldContain(
            literal,
            Case.Sensitive,
            $"'{literal}' must remain in its authoritative table row in {DocRelativePath}; reconcile it with {sourceName}.");
    }

    private static IReadOnlyList<string> ReadProfileRow(MarkdownContractDocument document, string key)
        => document.GetTableRows(ProfileHeading).Single(row => string.Equals(row[0], key, StringComparison.Ordinal));

    private static IReadOnlyList<string> ReadManifestRow(MarkdownContractDocument document, string heading, string key)
        => document.GetTableRows(heading).Single(row => string.Equals(row[0], key, StringComparison.Ordinal));

    private static string ReadDoc() => ReadRepoFile(DocRelativePath);

    private static string ReadRepoFile(string relativePath)
    {
        string path = ResolveRepoPath(relativePath);
        File.Exists(path).ShouldBeTrue($"Authoritative file not found at {path}");
        return File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static string ResolveRepoPath(string relativePath)
        => Path.Combine(ResolveRepoRoot(), Path.Combine(relativePath.Split('/')));

    private static string ResolveRepoRoot()
    {
        // Walk up from the test binary to the repo root identified by the Hexalith.Memories.slnx marker.
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }
}
