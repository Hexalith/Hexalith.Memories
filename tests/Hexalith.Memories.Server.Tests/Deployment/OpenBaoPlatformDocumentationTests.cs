// <copyright file="OpenBaoPlatformDocumentationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System;
using System.Collections.Generic;
using System.Globalization;
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
/// profile-hash annotations unchanged.
/// </para>
/// <para>
/// <b>What this guard does and does not catch (corrected by Story 31.1 code review 2026-07-28).</b> All
/// three inputs it compares — the document, <c>deploy/openbao/values.yaml</c>, and the measured literals
/// below — are files in this repository. It therefore catches document-versus-manifest divergence and any
/// edit that moves one without the other, which is what keeps a reconciled manifest and its documentation
/// honest with each other. It does <b>not</b> observe the cluster, so it would not by itself have caught
/// the recorded nine-revision drift, which changed no tracked file. Keeping
/// <see cref="MeasuredRaftVoters"/> and <see cref="MeasuredHaMode"/> true to the platform is a manual
/// re-measurement obligation carried by the story's checkpoint C2, not something asserted here.
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

    /// <summary>The story's second evidence artifact. AC3 binds "any evidence artifact", so the negative
    /// guards below cover this one too; scanning only the platform evidence left a sibling record written
    /// in the same phase unguarded.</summary>
    private const string CreateStoryEvidenceRelativePath =
        "_bmad-output/implementation-artifacts/tests/31-1-create-story-scope-evidence.md";

    private const string LimitationsHeading = "Accepted limitations";
    private const string ProfileHeading = "Deployed profile as measured";
    private const string AvailabilityHeading = "Availability profile";
    private const string DivergencesHeading = "Named divergences";
    private const string UntrackedStateHeading = "Deployed platform state not tracked in this repository";
    private const string ReviewerEvaluationHeading =
        "4. Security reviewer evaluation (Task 6, checkpoint C7)";
    private const string SmokeTestRerunHeading =
        "3.1 Re-run under the CA-only volume projection (dev-story, 2026-07-28)";
    private const string BoundedRemeasureHeading = "8. Bounded live re-measure (2026-09-06)";
    private const string OpenObligationsHeading = "6.4 Obligations this review opened";
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
    /// can easily mistake process-level failover for real availability, so the ban covers the accepted
    /// limitations, the availability narrative, and the measured-profile table — the sections where such a
    /// claim would actually be written. Spaced and hyphenated spellings are both listed because the
    /// document is hard-wrapped and either form reads the same to a reviewer.</summary>
    private static readonly string[] ForbiddenStrengthClaims =
    [
        "hardened",
        "production-HA",
        "production HA",
        "highly available",
        "high availability",
        "production-ready",
        "production ready",
        "production-grade",
    ];

    /// <summary>Placeholder cell contents that record an acceptance nobody owns.</summary>
    private static readonly string[] PlaceholderCells = ["TBD", "TODO", "N/A", "-", "—", "?", "none"];

    /// <summary>Context labels that identify a long run as a digest, hash, or commit these records publish
    /// on purpose.
    /// <para>
    /// Story 31.1 code review replaced an exact-value allow-list with this context test. The literal set
    /// failed closed on every legitimately added hash — a new method-set SHA-256, a full commit SHA — and
    /// reported it as "long key-shaped run(s) that are not allow-listed published digests", which reads as
    /// a security incident for a routine documentation edit and could only be cleared by editing test
    /// source. The trade is explicit: a pasted secret evades this check only if it is placed on a line, or
    /// in a paragraph, that also labels it as a digest or hash.
    /// </para>
    /// <para>
    /// Story 31.1 second-pass code review 2026-07-28 anchored these to whole words. They were matched as
    /// case-insensitive substrings, so <c>hash</c> fired inside <c>hashicorp</c> and <c>head</c> inside
    /// <c>headless</c>/<c>headers</c> — which exempted the entire paragraph introducing
    /// <c>secretstores.hashicorp.vault</c>, the one place an OpenBao token would plausibly be pasted.
    /// Proven by inserting a 44-character base64 payload there and observing it excused.
    /// </para></summary>
    private static readonly string[] PublishedDigestMarkers =
    [
        "sha256",
        "sha-256",
        "digest",
        "digests",
        "hash",
        "hashes",
        "hashed",
        "method-set",
        "manifest",
        "manifests",
        "profile-id",
        "chart",

        // Git object ids are labelled by the vocabulary around them rather than by the word "sha".
        "commit",
        "commits",
        "baseline",
        "revision",
        "revisions",
        "git",
        "head",
    ];

    /// <summary>Whole-word matcher for <see cref="PublishedDigestMarkers"/>. Substring matching excused a
    /// paste merely because an unrelated word happened to contain a marker.</summary>
    private static readonly Regex PublishedDigestMarkerPattern = new(
        @"(?<![A-Za-z0-9])(" + string.Join("|", PublishedDigestMarkers.Select(Regex.Escape)) + @")(?![A-Za-z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(5));

    [Fact]
    public void OpenBaoPlatformRecords_Exist()
    {
        foreach (string relativePath in ScannedRecords())
        {
            File.Exists(ResolveRepoPath(relativePath)).ShouldBeTrue(
                $"Story 31.1 platform record not found at {relativePath}");
        }
    }

    [Fact]
    public void AcceptedLimitations_HaveExactHeaderTwoKeyedRowsAndNoEmptyCell()
    {
        var document = new MarkdownContractDocument(ReadDoc());

        // NOTE: GetSection includes subordinate sections and GetTable requires exactly one table in the
        // resolved range, so the "Accepted limitations" H2 must remain the only table-bearing heading in
        // its subtree. Its two H3 narrative subsections are deliberately table-free; adding a table to
        // either fails here with "found 2", which names the count rather than the cause.
        document.GetTableHeader(LimitationsHeading).ShouldBe(
            ["Limitation", "Owner", "Consequence", "Compensating controls", "Reopen trigger"]);

        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows(LimitationsHeading);
        rows.Count.ShouldBe(2, "AC2 accepts exactly two limitations; adding or dropping one is a scope change.");
        rows.Select(static row => row[0]).ShouldBe([SealLimitationKey, IngressLimitationKey]);

        foreach (IReadOnlyList<string> row in rows)
        {
            ShouldBeSubstantiveRow(row, LimitationsHeading);
        }
    }

    [Fact]
    public void AcceptedLimitations_AreNeverDescribedWithStrengthVocabulary()
    {
        var document = new MarkdownContractDocument(ReadDoc());

        // AC2 bans the claim in the limitation rows AND in any surrounding prose about either limitation.
        // The availability narrative and the measured-profile table are in scope because that is where a
        // three-voter platform would most plausibly be overclaimed; the two H3 limitation subsections need
        // no separate entry, since GetSection already includes them in the H2 range. Whitespace is
        // normalized first, otherwise a multi-word claim such as "highly available" would evade the ban
        // simply by falling across a line break in this hard-wrapped document.
        // Story 31.1 second-pass code review 2026-07-28: the three configured scopes were also redundant —
        // GetSection includes subordinate sections — while the document PREAMBLE and five H2 sections
        // (`Owned manifests`, `Named divergences`, `Dapr secret boundaries`, `Health and access checks`,
        // `Rotation and recovery`) were unguarded. Writing "This is a production-ready secrets platform."
        // into the preamble, the most prominent position in the file, defeated AC2 entirely. The ban is now
        // document-wide: there is no section of an operations document for this platform in which a
        // strength claim would be true.
        string whole = NormalizeWhitespace(ReadDoc());

        foreach (string claim in ForbiddenStrengthClaims)
        {
            whole.ShouldNotContain(
                claim,
                Case.Insensitive,
                $"This platform must never be described as '{claim}' anywhere in the document (AC2).");
        }
    }

    [Fact]
    public void AvailabilityProfile_IsBoundToTheMeasuredPlatformNotTheManifest()
    {
        var document = new MarkdownContractDocument(ReadDoc());

        // Anchored to the start of the cell, not merely contained in it: a partially edited cell such as
        // "`1` today (was `3`)" satisfied a ShouldContain and would have passed.
        ReadProfileRow(document, "Raft voters")[1].ShouldStartWith(
            $"`{MeasuredRaftVoters}`",
            Case.Sensitive,
            $"The documented Raft voter count must be the measured `{MeasuredRaftVoters}`.");
        ReadProfileRow(document, "HA mode")[1].ShouldStartWith(
            $"`{MeasuredHaMode}`",
            Case.Sensitive,
            $"The documented HA mode must be the measured `{MeasuredHaMode}`.");

        // Both halves of the amended AC2 premise must be stated plainly: leader election exists between
        // the voters, and one Kubernetes node holds all of them. Compared whitespace-normalized, because
        // the document is hard-wrapped and a phrase may straddle a line break.
        string availability = NormalizeWhitespace(document.GetSection(AvailabilityHeading));
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
        const string valuesHeading = "`deploy/openbao/values.yaml`";

        // values.yaml — chart identity, digest pin, HA shape, TLS, seal, audit, registration, policy.
        // Each call asserts the literal in the manifest AND binds the document's deployed-value cell.
        // Where the row key already carries the manifest text, the fifth argument is omitted and the cell
        // is instead required to be a substantive description: re-matching the key against the row would
        // be circular, because ReadManifestRow selects the row BY that key.
        string values = ReadRepoFile("deploy/openbao/values.yaml");
        ShouldBindManifest("fullnameOverride: hexalith-keys", document, valuesHeading, "`fullnameOverride: hexalith-keys`", values);
        ShouldBindManifest("2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653", document, valuesHeading, "`server.image.tag`", values, "2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653");
        ShouldBindManifest("tlsDisable: false", document, valuesHeading, "`global.tlsDisable: false`", values, "tls_min_version = \"tls12\"");
        ShouldBindManifest("tls_min_version = \"tls12\"", document, valuesHeading, "`global.tlsDisable: false`", values, "tls_min_version = \"tls12\"");
        ShouldBindManifest("replicas: 3", document, valuesHeading, "`server.ha.replicas: 3`", values, ".spec.replicas = 3");
        ShouldBindManifest("storage \"raft\"", document, valuesHeading, "`server.ha.raft.enabled: true`", values, "storage \"raft\"");
        ShouldBindManifest("setNodeId: true", document, valuesHeading, "`server.ha.raft.setNodeId: true`", values, "BAO_RAFT_NODE_ID");
        ShouldBindManifest("type: ClusterIP", document, valuesHeading, "`server.service.type: ClusterIP`", values, "ClusterIP");
        ShouldBindManifest("service_registration \"kubernetes\"", document, valuesHeading, "`server.serviceAccount.serviceDiscovery.enabled: true`", values, "service_registration \"kubernetes\"");
        ShouldBindManifest("authDelegator", document, valuesHeading, "`server.authDelegator.enabled: true`", values, "system:auth-delegator");
        ShouldBindManifest("seal \"static\"", document, valuesHeading, "`server.ha.raft.config` seal stanza", values, "seal \"static\"");
        ShouldBindManifest("audit \"file\" \"persistent\"", document, valuesHeading, "`server.ha.raft.config` audit stanza", values, "audit \"file\" \"persistent\"");
        ShouldBindManifest("openebs-hostpath-retain", document, ProfileHeading, "Persistent volumes", values, "openebs-hostpath-retain");

        // The cert-manager ingress source is asserted as AGREEMENT, not presence. It has no measured
        // consumer and the document names its removal as the reopen trigger, so pinning its presence would
        // have made executing that remediation a test failure. This form fails only when the manifest and
        // the document disagree, so a coordinated removal from both is a passing state.
        bool manifestAdmitsCertManager = values.Contains("kubernetes.io/metadata.name: cert-manager", StringComparison.Ordinal);
        IReadOnlyList<string> ingressRow = ReadManifestRow(document, valuesHeading, "`server.networkPolicy.ingress`");
        ingressRow[1].Contains("cert-manager", StringComparison.OrdinalIgnoreCase).ShouldBe(
            manifestAdmitsCertManager,
            "deploy/openbao/values.yaml and the documented NetworkPolicy ingress row must agree about the cert-manager source; narrow or remove it in both together.");
        ingressRow[1].ShouldContain("hexalith-memories", Case.Sensitive, "The application namespace is the rule's only justified source and must stay documented.");

        // The reconciled profile must never be silently weakened back. Story 31.1 second-pass code review
        // 2026-07-28: a lone ShouldNotContain("tls_disable = 1") pinned one spelling — the valid HCL
        // `tls_disable = true` disabled TLS with every assertion green. Assert the positive and reject the
        // enabling forms by pattern.
        values.ShouldContain("tls_disable = 0", Case.Sensitive, "TLS must stay enabled on the listener.");
        values.ShouldNotMatch(@"tls_disable\s*=\s*""?(1|true|yes|on)""?", "TLS must never be disabled on the listener, by any spelling.");
        values.ShouldNotContain("type: LoadBalancer", Case.Sensitive, "The Service must stay ClusterIP-only.");
        values.ShouldNotContain("type: NodePort", Case.Sensitive, "The Service must stay ClusterIP-only.");

        // Raft durability. Flipping either retention policy to `Delete` destroys the encrypted Raft store
        // and the audit trail on StatefulSet deletion, while the document keeps promising retention and the
        // suite stays green. Neither key had any assertion before this review.
        values.ShouldContain("whenDeleted: Retain", Case.Sensitive, "The Raft data and audit PVCs must survive StatefulSet deletion.");
        values.ShouldContain("whenScaled: Retain", Case.Sensitive, "The Raft data and audit PVCs must survive scale-down.");

        // Secret file mode and the retry_join TLS identity: documented as deployed, previously unasserted.
        values.ShouldContain("defaultMode: 288", Case.Sensitive, "Mounted secret files must stay 0440.");
        values.ShouldContain("leader_ca_cert_file", Case.Sensitive, "retry_join must verify the leader's TLS identity.");
        values.ShouldNotMatch(@"node_id\s*=", "A static node_id would give every voter one identity; setNodeId supplies it per pod.");

        // namespace.yaml — Restricted Pod Security and the two ownership annotations.
        const string namespaceHeading = "`deploy/openbao/namespace.yaml`";
        string openBaoNamespace = ReadRepoFile("deploy/openbao/namespace.yaml");
        // Story 31.1 second-pass code review 2026-07-28: moving the assertion to row[1] closed the literal
        // circularity but not the semantic one — several literals merely restated the row key in different
        // words. Rewriting the platform-owner cell to "owner is nobody at all, unowned" left the suite green
        // (proven by mutation). Each literal below is now a measured value that does NOT appear in its key.
        ShouldBindManifest("pod-security.kubernetes.io/enforce: restricted", document, namespaceHeading, "`pod-security.kubernetes.io/enforce: restricted`", openBaoNamespace, "Restricted profile");
        ShouldBindManifest("hexalith.io/platform-owner: jpiquot", document, namespaceHeading, "`hexalith.io/platform-owner: jpiquot`", openBaoNamespace, "jpiquot");
        ShouldBindManifest("hexalith.io/security-reviewer: murat-tea-for-jpiquot", document, namespaceHeading, "`hexalith.io/security-reviewer: murat-tea-for-jpiquot`", openBaoNamespace, "murat-tea-for-jpiquot");
        ShouldBindManifest("pod-security.kubernetes.io/audit: restricted", document, namespaceHeading, "`pod-security.kubernetes.io/enforce: restricted`", openBaoNamespace, "Restricted profile");
        ShouldBindManifest("pod-security.kubernetes.io/warn: restricted", document, namespaceHeading, "`pod-security.kubernetes.io/enforce: restricted`", openBaoNamespace, "Restricted profile");
        ShouldBindManifest("kubernetes.io/metadata.name: openbao", document, namespaceHeading, "`kubernetes.io/metadata.name: openbao`", openBaoNamespace, "namespaceSelector");

        // service-account-hardening.yaml — the file's real effect, not the claim it used to carry.
        const string hardeningHeading = "`deploy/openbao/service-account-hardening.yaml`";
        string hardening = ReadRepoFile("deploy/openbao/service-account-hardening.yaml");
        ShouldBindManifest("automountServiceAccountToken: false", document, hardeningHeading, "`automountServiceAccountToken: false`", hardening, "overridden to `true` at pod level");

        string hardeningSection = NormalizeWhitespace(document.GetSection(hardeningHeading));
        hardeningSection.ShouldContain("pod-level", Case.Insensitive, "The section must state that the pod-level setting overrides the ServiceAccount default.");

        // Story 31.1 second-pass code review 2026-07-28: this ban was section-scoped, so moving the sentence
        // into the (previously unpinned) profile-table `ServiceAccount token` cell undid Task 2's explicit
        // correction with the suite green. The claim is false about this platform wherever it is written.
        NormalizeWhitespace(ReadDoc()).ShouldNotContain(
            "does not receive an unused Kubernetes API token",
            Case.Insensitive,
            "The pre-Story-31.1 claim was measurably false as deployed and must not return anywhere in the document.");

        // smoke-test.yaml — endpoint, CA-only projection, one-shot semantics.
        const string smokeTestHeading = "`deploy/openbao/smoke-test.yaml`";
        string smokeTest = ReadRepoFile("deploy/openbao/smoke-test.yaml");
        ShouldBindManifest("name: hexalith-keys-smoke-test", document, smokeTestHeading, "`kind: Job`, `name: hexalith-keys-smoke-test`", smokeTest, "hexalith-keys-smoke-test");
        ShouldBindManifest("https://hexalith-keys.openbao.svc.cluster.local:8200", document, smokeTestHeading, "`BAO_ADDR: https://hexalith-keys.openbao.svc.cluster.local:8200`", smokeTest, "hexalith-keys.openbao.svc.cluster.local");
        ShouldBindManifest("/openbao/tls/ca.crt", document, smokeTestHeading, "`BAO_CACERT: /openbao/tls/ca.crt`", smokeTest, "openbao-server-tls");
        ShouldBindManifest("ttlSecondsAfterFinished: 300", document, smokeTestHeading, "`backoffLimit: 0`, `activeDeadlineSeconds: 60`, `ttlSecondsAfterFinished: 300`", smokeTest, "one attempt");
        ShouldBindManifest("backoffLimit: 0", document, smokeTestHeading, "`backoffLimit: 0`, `activeDeadlineSeconds: 60`, `ttlSecondsAfterFinished: 300`", smokeTest, "one attempt");
        ShouldBindManifest("activeDeadlineSeconds: 60", document, smokeTestHeading, "`backoffLimit: 0`, `activeDeadlineSeconds: 60`, `ttlSecondsAfterFinished: 300`", smokeTest, "one minute");
        smokeTest.ShouldNotMatch(
            @"(?i)(tls-skip-verify|BAO_SKIP_VERIFY|VAULT_SKIP_VERIFY)",
            "The smoke test must keep verifying OpenBao's TLS identity, by any spelling.");

        // The Job must project only the CA. Without `items` the whole Secret is mounted, which hands the
        // server private key to a throwaway status pod. Story 31.1 second-pass code review 2026-07-28:
        // asserting the presence of the ca.crt entry did not stop a SECOND entry being added beside it, so
        // the projection is now pinned to exactly one key.
        smokeTest.ShouldContain("items:\n              - key: ca.crt", Case.Sensitive, "smoke-test.yaml must project only ca.crt from Secret openbao-server-tls.");
        Regex.Matches(smokeTest, @"^\s+- key: ", RegexOptions.Multiline, TimeSpan.FromSeconds(5)).Count.ShouldBe(
            1,
            "smoke-test.yaml must project exactly one Secret key; a second entry returns the server private key to the status pod.");
        smokeTest.ShouldNotContain("- key: tls.key", Case.Sensitive, "The server private key must never be projected into the status pod.");
    }

    [Fact]
    public void NamedDivergencesAndUntrackedState_CarryOwnerAndReopenTriggerPerRow()
    {
        // Every open item this story produced lives in these two tables — including the record that the
        // reconciled values.yaml has not been re-applied, which is the AC1 escape hatch Task 2 permits.
        // Before this guard existed both tables could be deleted, or any Owner cell blanked, with the
        // whole suite green.
        var document = new MarkdownContractDocument(ReadDoc());

        document.GetTableHeader(DivergencesHeading).ShouldBe(
            ["Divergence", "Owner", "Why it is open", "Reopen trigger"]);
        IReadOnlyList<IReadOnlyList<string>> divergences = document.GetTableRows(DivergencesHeading);

        // Story 31.1 second-pass code review 2026-07-28: the previous `>= 5` floor against a table of eight
        // rows permitted the deletion this method exists to prevent. Deleting the `authDelegator` row (an
        // unrevoked cluster-scoped grant) and the off-cluster-snapshot row (total data loss on node failure)
        // left the whole suite green — proven by mutation. Every row carrying open platform risk is now
        // pinned by key, so shrinking the register fails by name rather than by count.
        string[] requiredDivergenceKeys =
        [
            "has not been re-applied",
            "authDelegator",
            "off-cluster copy",
            "cert-manager",
            "profile annotations",
            "manifest hashes",
            "voter count",
        ];

        foreach (string requiredKey in requiredDivergenceKeys)
        {
            divergences.Select(static row => row[0]).ShouldContain(
                key => key.Contains(requiredKey, StringComparison.OrdinalIgnoreCase),
                1,
                $"The '{requiredKey}' divergence must stay recorded with its owner and reopen trigger.");
        }

        divergences.Count.ShouldBeGreaterThanOrEqualTo(
            requiredDivergenceKeys.Length,
            "Task 2 requires every unreconciled setting to stay named; silently shrinking this table hides an open item.");
        foreach (IReadOnlyList<string> row in divergences)
        {
            ShouldBeSubstantiveRow(row, DivergencesHeading);
        }

        IReadOnlyList<string> helmDivergence = divergences.Single(
            static row => row[0].Contains("has not been re-applied", StringComparison.OrdinalIgnoreCase));
        string.Join('\n', helmDivergence).ShouldNotContain(
            "done gate",
            Case.Insensitive,
            "The 2026-07-28 scope ratifications carved helm reproduce-release out of Story 31.1; the named-divergence row must not call it a Story 31.1 done gate.");
        helmDivergence[3].ShouldContain(
            "helm diff",
            Case.Insensitive,
            "The reopen trigger remains an empty Platform Operations helm diff.");

        document.GetTableHeader(UntrackedStateHeading).ShouldBe(
            ["Artifact", "Kind", "Owner", "Disposition and reopen trigger"]);
        IReadOnlyList<IReadOnlyList<string>> untracked = document.GetTableRows(UntrackedStateHeading);
        untracked.Count.ShouldBeGreaterThanOrEqualTo(6, "Every deployed-but-untracked artifact stays named with an owner.");
        foreach (IReadOnlyList<string> row in untracked)
        {
            ShouldBeSubstantiveRow(row, UntrackedStateHeading);
        }
    }

    [Fact]
    public void DeployedProfileTable_PinsEveryMeasuredRowByKey()
    {
        // Story 31.1 second-pass code review 2026-07-28. The table the document presents as THE measurement
        // was the least-protected structure in the file: 30 of its 33 rows were unpinned, including
        // `ServiceAccount token` — the row carrying this story's headline correction — and `Seal`. GetTable
        // requires only two cells, so `| Seal |  |` parsed and asserted nothing.
        var document = new MarkdownContractDocument(ReadDoc());

        document.GetTableHeader(ProfileHeading).ShouldBe(["Contract", "Measured value on 2026-07-28"]);

        string[] requiredProfileKeys =
        [
            "Chart", "Server image", "Raft voters", "HA mode", "Voter scheduling", "Seal",
            "Persistent volumes", "PVC retention", "Pod Security", "ServiceAccount token",
            "Token review binding", "NetworkPolicy", "Backup", "Update strategy",
        ];

        IReadOnlyList<IReadOnlyList<string>> rows = document.GetTableRows(ProfileHeading);
        foreach (string key in requiredProfileKeys)
        {
            // Exact key match: `Seal`/`Recovery seal` and `Pod Security`/`Pod security context` are
            // distinct rows that a substring test would conflate into one.
            rows.Select(static row => row[0]).ShouldContain(
                candidate => string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase),
                1,
                $"The measured-profile table must keep its '{key}' row.");
        }

        // The VALUE cell must be present and not a placeholder. The 12-character floor used on the prose
        // tables is deliberately NOT applied here: a measured value is often a short literal — `openbao`,
        // `raft`, `3` — and padding it to satisfy a length rule would make the table worse, not better.
        foreach (IReadOnlyList<string> row in rows)
        {
            row[1].Trim().ShouldNotBeNullOrWhiteSpace(
                $"Row '{row[0]}' under '{ProfileHeading}' records no measured value.");
            PlaceholderCells.ShouldNotContain(
                row[1].Trim(),
                $"Row '{row[0]}' under '{ProfileHeading}' records a placeholder instead of a measured value.");
        }

        // Both auth-delegator ClusterRoleBindings were measured; naming only the chart-rendered one
        // understates the granted privilege.
        ReadProfileRow(document, "Token review binding")[1].ShouldContain(
            "hexalith-keys-tokenreview",
            Case.Sensitive,
            "The measured token-review row must name the untracked duplicate binding as well as the chart-rendered one.");
    }

    [Fact]
    public void OperationalSections_StayBoundToTheirRecordedRemediations()
    {
        // Story 31.1 second-pass code review 2026-07-28: every prose remediation the FIRST review landed was
        // deletable with the suite green — the multi-voter health loop, the endpoint-staleness detection,
        // the `-tls-skip-verify` probe disclosure, and the Dapr boundary table. In a story whose deliverable
        // is documentation accuracy, unbound prose is not a deliverable.
        var document = new MarkdownContractDocument(ReadDoc());

        string health = NormalizeWhitespace(document.GetSection("Health and access checks"));
        health.ShouldContain("hexalith-keys-1", Case.Sensitive, "The health block must cover every voter, not only hexalith-keys-0.");
        health.ShouldContain("hexalith-keys-2", Case.Sensitive, "The health block must cover every voter, not only hexalith-keys-0.");
        health.ShouldContain("endpoints", Case.Insensitive, "The endpoint staleness re-check must stay documented.");

        string probes = NormalizeWhitespace(document.GetSection("TLS posture of the probes"));
        probes.ShouldContain("tls-skip-verify", Case.Sensitive, "The deployed readiness probe's skip-verify flag must stay disclosed (AC1 exact deployed configuration).");

        string dapr = NormalizeWhitespace(document.GetSection("Dapr secret boundaries"));
        dapr.ShouldContain("hashicorp.vault", Case.Sensitive, "The Dapr component type must stay documented.");

        string rotation = NormalizeWhitespace(document.GetSection("Rotation and recovery"));
        rotation.ShouldContain("do not delete", Case.Insensitive, "The do-not-delete-the-PVCs instruction must be preserved (Task 3).");
    }

    [Fact]
    public void SecretShapeGuard_ExcusesALabelledDigestAndReportsAnUnlabelledPaste()
    {
        // Story 31.1 second-pass code review 2026-07-28. Positive control. Every one of the 22 long runs in
        // the three scanned records is excused by context, so PlatformRecords_ContainNoSecretShapedMaterial
        // currently passes for the same reason it would pass if the detector were disabled outright. This
        // method exercises the reporting branch, so a regression in the marker logic fails here instead of
        // silently disarming AC3's only negative guard.
        const string labelled = "The image digest sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653 is pinned.";
        FindUnexpectedLongRuns(labelled).ShouldBeEmpty("A run labelled as a published digest must stay excused.");

        const string unlabelled = "Dapr reaches OpenBao through the component type `secretstores.hashicorp.vault`.\nThe value is 9Xk2Lm4Qp7Rt1Vw8Zc3Nb6Hj5Fd0Gs2Ay4Ue7Ik9Ol1Pr3Tw5=\n";
        FindUnexpectedLongRuns(unlabelled).ShouldNotBeEmpty(
            "An unlabelled key-shaped run must be reported even when a nearby word contains a marker substring such as 'hash' inside 'hashicorp'.");
    }

    [Fact]
    public void PlatformRecords_ContainNoSecretShapedMaterial()
    {
        // AC3 / NFR9, asserted over every published record. Every check is negative: the guard proves the
        // absence of key-shaped material rather than the presence of a redaction note.
        foreach (string relativePath in ScannedRecords())
        {
            string text = ReadRepoFile(relativePath);

            text.ShouldNotContain("-----BEGIN", Case.Insensitive, $"{relativePath} must contain no PEM block.");
            text.ShouldNotContain("-----END", Case.Insensitive, $"{relativePath} must contain no PEM block.");

            // Every OpenBao/Vault token prefix, not just the service-token shape: `hvs.` service, `hvb.`
            // batch, `hvr.` recovery, and the legacy `s.`, `b.`, `r.` forms. The word boundary keeps
            // ordinary prose and namespaced identifiers such as `Server.Tests` out of scope.
            ShouldHaveNoMatch(text, @"\bhv[sbr]\.[A-Za-z0-9_-]{8,}", relativePath, "an OpenBao token value");

            // Story 31.1 second-pass code review 2026-07-28: the legacy class omitted `_` and `-` while the
            // line above included them, so base64url legacy batch and recovery tokens split below the
            // threshold and evaded both this check and the long-run scan.
            ShouldHaveNoMatch(text, @"\b[sbr]\.[A-Za-z0-9_-]{20,}", relativePath, "a legacy token value");

            // A UUID-shaped credential — an AppRole role_id/secret_id or a Dapr API token — is hyphenated,
            // which breaks every hex and base64 run, so it evaded AC3 entirely. Story 31.2 migrates the
            // runtime store to hashicorp.vault, where AppRole is the common auth method. Kubernetes and
            // OpenBao also publish non-secret UUIDs (`cluster_id`, object `uid`); those are excused only
            // when their own line names them, so an unlabelled paste is still reported.
            IReadOnlyList<string> credentialShapedIds = FindUnexpectedUuids(text);
            credentialShapedIds.ShouldBeEmpty(
                $"{relativePath} contains unlabelled UUID-shaped value(s) that could be an AppRole role_id/secret_id or a Dapr API token: {string.Join(", ", credentialShapedIds)}");

            // The labelled shapes `bao operator init` prints. Matching the label plus its colon keeps
            // ordinary prose about the seal, the recovery shares, and the revoked root token readable
            // while still rejecting a pasted init dump.
            ShouldHaveNoMatch(text, @"(?:Unseal|Recovery)\s+Key\s*\d*\s*:", relativePath, "an init-dump key label");
            ShouldHaveNoMatch(text, @"Initial\s+Root\s+Token\s*:", relativePath, "an init-dump root-token label");

            // Long runs that would shape like key material, minus the digests these records publish on
            // purpose. Published values are recognized by their labelling context, so a legitimately added
            // hash does not fail closed while an unlabelled paste still does.
            IReadOnlyList<string> unexpected = FindUnexpectedLongRuns(text);
            unexpected.ShouldBeEmpty(
                $"{relativePath} contains unlabelled long key-shaped run(s): {string.Join(", ", unexpected)}");
        }
    }

    [Fact]
    public void PlatformEvidence_RecordsExecutedSmokeTestAndReviewerState()
    {
        string evidence = ReadRepoFile(EvidenceRelativePath);

        // C3: an executed command with its observed outcome, not a synthesized payload. Story 31.1
        // second-pass code review 2026-07-28: these were whole-document ShouldContain calls — the exact
        // pattern the first review rejected for the C7 half below and made structural. Every literal appears
        // in the SUPERSEDED 09:43 run as well as the current-manifest 13:24 re-run, so deleting the re-run
        // section left the method green while §6.4's own reopen trigger had no executable form at all. C3 is
        // now read from the re-run section, which is the only run produced under the shipped manifest.
        var evidenceStructure = new MarkdownContractDocument(evidence);
        string rerun = NormalizeWhitespace(evidenceStructure.GetSection(SmokeTestRerunHeading));
        rerun.ShouldContain(SmokeTestApplyCommand, Case.Sensitive, "C3 requires the exact apply command in the current-manifest re-run.");
        rerun.ShouldContain(SmokeTestWaitCommand, Case.Sensitive, "C3 requires the exact wait command in the current-manifest re-run.");
        rerun.ShouldContain("condition met", Case.Sensitive, "C3 requires the observed completion outcome of the current-manifest run.");
        rerun.ShouldContain("\"ha_enabled\": true", Case.Sensitive, "C3 requires the observed status fields of the current-manifest run.");
        rerun.ShouldContain("\"storage_type\": \"raft\"", Case.Sensitive, "C3 requires the observed status fields of the current-manifest run.");
        rerun.ShouldContain("ca.crt", Case.Sensitive, "The re-run must record that it executed under the CA-only projection.");

        string remeasure = NormalizeWhitespace(evidenceStructure.GetSection(BoundedRemeasureHeading));
        remeasure.ShouldContain("2026-09-06T21:55:01Z", Case.Sensitive, "The bounded re-measure must record its UTC timestamp.");
        remeasure.ShouldContain("jpiquot@local", Case.Sensitive, "The bounded re-measure must name context jpiquot@local.");
        remeasure.ShouldContain("unchanged", Case.Insensitive, "A matching re-measure must record unchanged against the 2026-07-28 table.");

        IReadOnlyList<IReadOnlyList<string>> openObligations = evidenceStructure.GetTableRows(OpenObligationsHeading);
        IReadOnlyList<string> helmObligation = openObligations.Single(
            static row => row[0].Contains("helm diff", StringComparison.OrdinalIgnoreCase));
        helmObligation[0].ShouldNotContain(
            "done gate",
            Case.Insensitive,
            "Evidence §6.4 must not call the helm empty-diff a Story 31.1 done gate.");

        // C7 is read structurally from its own section, and every permitted outcome passes.
        //
        // The previous form asserted the literal words "Accepted blocker", "Consequence" and "Reopen
        // trigger" against the whole 486-line artifact. That was satisfied by unrelated section 2 prose —
        // so the C7 record could be gutted while the suite stayed green — and it hard-required the story's
        // unresolved state, meaning the build would have turned red at the moment the reviewer signed.
        var evidenceDocument = new MarkdownContractDocument(evidence);
        evidenceDocument.GetTableHeader(ReviewerEvaluationHeading).ShouldBe(["Field", "Record"]);
        Dictionary<string, string> record = evidenceDocument
            .GetTableRows(ReviewerEvaluationHeading)
            .ToDictionary(static row => row[0], static row => row[1], StringComparer.OrdinalIgnoreCase);

        record.ShouldContainKey("Reviewer of record");
        record["Reviewer of record"].ShouldContain("murat-tea-for-jpiquot", Case.Sensitive, "C7 must name the reviewer of record.");
        record.ShouldContainKey("Checkpoint C7 state");

        string state = record["Checkpoint C7 state"];
        if (state.Contains("waived", StringComparison.OrdinalIgnoreCase))
        {
            // Closed by an approved waiver rather than by an evaluation. project-context requires a named
            // approver plus a time-bounded expiry or a measurable reopen trigger.
            foreach (string field in new[] { "Waiver approver", "Waiver expiry", "Consequence", "Reopen trigger", "Approver independence" })
            {
                record.ShouldContainKey(field, $"A waived C7 must record '{field}'.");
                ShouldBeSubstantiveCell(record[field], field, ReviewerEvaluationHeading);
            }

            // Story 31.1 second-pass code review 2026-07-28: the expiry was asserted present and never
            // compared to a date. Replacing every `2026-10-26` with `2020-01-01` left the suite green —
            // proven by mutation — so the "reopens automatically" the evidence promises was prose only.
            Match expiry = Regex.Match(
                record["Waiver expiry"], @"20\d{2}-\d{2}-\d{2}", RegexOptions.None, TimeSpan.FromSeconds(5));
            expiry.Success.ShouldBeTrue("A waived C7 must record a parseable ISO expiry date.");
            DateOnly.Parse(expiry.Value, CultureInfo.InvariantCulture).ShouldBeGreaterThan(
                DateOnly.FromDateTime(DateTime.UtcNow),
                "The C7 waiver has expired: checkpoint C7 reverts to `pending` / `not complete` and the security evaluation AC2 requires is owed.");
        }
        else if (state.Contains("not complete", StringComparison.OrdinalIgnoreCase)
            || state.Contains("pending", StringComparison.OrdinalIgnoreCase))
        {
            // Still open: the accepted-blocker record must be complete.
            foreach (string field in new[] { "Owner of the blocker", "Consequence", "Reopen trigger" })
            {
                record.ShouldContainKey(field, $"An open C7 must record '{field}'.");
                ShouldBeSubstantiveCell(record[field], field, ReviewerEvaluationHeading);
            }
        }
        else
        {
            // Signed: the evaluation must be dated and attributed, per Task 6 — a recommendation to seek
            // review is not a recorded evaluation. Story 31.1 second-pass code review 2026-07-28: requiring
            // only a date let a cell reading "No evaluation performed as of 2026-07-28" close C7 as signed,
            // so the signer must now be bound to the date.
            record.ShouldContainKey("Evaluation status");
            record["Evaluation status"].ShouldMatch(
                @"(?i)signed\s+by\s+\S+\s+on\s+20\d{2}-\d{2}-\d{2}",
                "A signed C7 evaluation must name its evaluator and carry its date, in the form 'signed by <evaluator> on <YYYY-MM-DD>'.");
        }
    }

    [Fact]
    public void PlatformRecords_ContainNoLeakedToolCallMarkup()
    {
        foreach (string relativePath in ScannedRecords())
        {
            IReadOnlyList<string> diagnostics = ContractDocumentGuard.FindLeakedToolCallMarkup(ReadRepoFile(relativePath));

            diagnostics.ShouldBeEmpty($"{relativePath} contains leaked tool-call markup: {string.Join("; ", diagnostics)}");
        }
    }

    /// <summary>The records AC3's "any evidence artifact" clause covers, plus the operations document.
    /// <para>
    /// Story 31.1 second-pass code review 2026-07-28 added the four owned manifests. AC3's subject is "no
    /// unseal key, recovery key, root or operator token, or other secret value" — and
    /// <c>deploy/openbao/values.yaml</c> carries the <c>seal "static"</c> stanza whose <c>current_key</c>
    /// field accepts an inline key value in place of the <c>file://</c> reference. Replacing it with a
    /// literal key failed nothing: the file was unscanned and no assertion read the field. A committed seal
    /// key beside the committed data-PVC configuration is precisely the consequence the `Static file-based
    /// seal` limitation row describes.
    /// </para></summary>
    private static string[] ScannedRecords()
        =>
        [
            DocRelativePath,
            EvidenceRelativePath,
            CreateStoryEvidenceRelativePath,
            "deploy/openbao/values.yaml",
            "deploy/openbao/namespace.yaml",
            "deploy/openbao/service-account-hardening.yaml",
            "deploy/openbao/smoke-test.yaml",
        ];

    /// <summary>Collapses every whitespace run to one space so a narrative assertion is independent of
    /// where the hard-wrapped document happens to break a line.</summary>
    private static string NormalizeWhitespace(string text)
        => Regex.Replace(text, @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(5)).Trim();

    /// <summary>Finds runs long enough and dense enough to be key material, minus the digests these
    /// records publish on purpose.
    /// <para>
    /// Two shapes are scanned. A hexadecimal run of 32 or more characters covers digests, hashes, and
    /// hex-encoded keys. A base64 run of 40 or more characters — now including <c>/</c> and trailing
    /// <c>=</c> padding, which real encoded key material contains and the previous character class
    /// excluded — covers encoded payloads, but only when it also mixes digits with upper and lower case.
    /// Encoded key material always does; a long PascalCase identifier such as a descriptive test-method
    /// name never does. Without that density requirement this guard reports its own test names, which
    /// would train a reader to ignore it.
    /// </para></summary>
    private static IReadOnlyList<string> FindUnexpectedLongRuns(string text)
    {
        var unexpected = new List<string>();
        foreach ((string pattern, bool requireMixedDensity) in new[] { ("[0-9a-fA-F]{32,}", false), ("[A-Za-z0-9+/]{40,}={0,2}", true) })
        {
            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.None, TimeSpan.FromSeconds(5)))
            {
                string value = match.Value;
                if (requireMixedDensity && !(value.Any(char.IsDigit) && value.Any(char.IsUpper) && value.Any(char.IsLower)))
                {
                    continue;
                }

                if (!HasPublishedDigestContext(text, match.Index) && !unexpected.Contains(value))
                {
                    unexpected.Add(value);
                }
            }
        }

        return unexpected;
    }

    /// <summary>Non-secret UUID-bearing fields these records publish. A UUID on a line naming one of these
    /// is a cluster or object identifier; anywhere else it is treated as possible credential material.
    /// </summary>
    private static readonly string[] PublishedIdentifierMarkers =
        ["cluster_id", "cluster id", "uid", "resourceversion", "deployment-id", "deployment id"];

    /// <summary>Finds UUID-shaped values that no identifier label on their own line accounts for.</summary>
    private static IReadOnlyList<string> FindUnexpectedUuids(string text)
    {
        var unexpected = new List<string>();
        foreach (Match match in Regex.Matches(
            text,
            @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
            RegexOptions.None,
            TimeSpan.FromSeconds(5)))
        {
            int lineStart = text.LastIndexOf('\n', Math.Min(Math.Max(match.Index - 1, 0), text.Length - 1)) + 1;
            int lineEnd = text.IndexOf('\n', match.Index);
            string line = text[lineStart..(lineEnd < 0 ? text.Length : lineEnd)];

            if (!PublishedIdentifierMarkers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase))
                && !unexpected.Contains(match.Value))
            {
                unexpected.Add(match.Value);
            }
        }

        return unexpected;
    }

    /// <summary>Decides whether a long run is labelled as a published digest, hash, or commit.
    /// <para>
    /// Inside a Markdown table the label must sit on the same row, which keeps one cell's digest from
    /// excusing an unlabelled paste in another. In flowing prose the enclosing blank-line-delimited
    /// paragraph is used instead, because these records are hard-wrapped and a label routinely lands one
    /// or two lines above the value it introduces.
    /// </para></summary>
    private static bool HasPublishedDigestContext(string text, int matchIndex)
    {
        int lineStart = text.LastIndexOf('\n', Math.Min(Math.Max(matchIndex - 1, 0), text.Length - 1)) + 1;
        int lineEnd = text.IndexOf('\n', matchIndex);
        lineEnd = lineEnd < 0 ? text.Length : lineEnd;
        string line = text[lineStart..lineEnd];

        string context = line.TrimStart().StartsWith('|') ? line : ReadParagraph(text, lineStart, lineEnd);
        return PublishedDigestMarkerPattern.IsMatch(context);
    }

    private static string ReadParagraph(string text, int lineStart, int lineEnd)
    {
        int start = lineStart;
        while (start > 1)
        {
            // `start > 1` rather than `start > 0`: at start == 1 the index below is -1, which throws
            // ArgumentOutOfRangeException on a non-empty string, so the AC3 guard would error rather than
            // report. Reachable for any scanned record whose first character is a newline.
            int previousStart = text.LastIndexOf('\n', start - 2) + 1;
            if (previousStart >= start - 1 || string.IsNullOrWhiteSpace(text[previousStart..(start - 1)]))
            {
                break;
            }

            start = previousStart;
        }

        int end = lineEnd;
        while (end < text.Length)
        {
            int nextEnd = text.IndexOf('\n', end + 1);
            nextEnd = nextEnd < 0 ? text.Length : nextEnd;
            if (string.IsNullOrWhiteSpace(text[(end + 1)..nextEnd]))
            {
                break;
            }

            end = nextEnd;
        }

        return text[start..end];
    }

    private static void ShouldHaveNoMatch(string text, string pattern, string relativePath, string description)
        => Regex.Matches(text, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5))
            .Count
            .ShouldBe(0, $"{relativePath} must not contain {description}.");

    /// <summary>Asserts a literal stays in its authoritative manifest and that the document's row binds it.
    /// <para>
    /// When <paramref name="documentedLiteral"/> is supplied it must appear in the row's deployed-value
    /// cell. When it is omitted the row key itself carries the manifest text, so the cell is instead
    /// required to be a substantive description: asserting the literal against the whole row would be
    /// circular, since <see cref="ReadManifestRow"/> selects the row by that key. The previous form joined
    /// the whole row, so in fourteen of twenty-two calls the literal matched the lookup key and the
    /// deployed-value cell was never asserted at all.
    /// </para></summary>
    private static void ShouldBindManifest(
        string manifestLiteral,
        MarkdownContractDocument document,
        string heading,
        string rowKey,
        string source,
        string? documentedLiteral = null)
    {
        source.ShouldContain(
            manifestLiteral,
            Case.Sensitive,
            $"'{manifestLiteral}' must remain in its authoritative manifest; reconcile it with {DocRelativePath}.");

        IReadOnlyList<string> row = ReadManifestRow(document, heading, rowKey);
        if (documentedLiteral is null)
        {
            ShouldBeSubstantiveCell(row[1], rowKey, heading);
            return;
        }

        row[1].ShouldContain(
            documentedLiteral,
            Case.Sensitive,
            $"the '{rowKey}' row under '{heading}' in {DocRelativePath} must bind the deployed value '{documentedLiteral}'.");
    }

    private static void ShouldBeSubstantiveRow(IReadOnlyList<string> row, string heading)
    {
        foreach (string cell in row)
        {
            ShouldBeSubstantiveCell(cell, row[0], heading);
        }
    }

    private static void ShouldBeSubstantiveCell(string cell, string rowKey, string heading)
    {
        // A blank or placeholder cell records an acceptance nobody owns.
        cell.ShouldNotBeNullOrWhiteSpace($"Row '{rowKey}' under '{heading}' has an empty cell.");
        cell.Trim().ShouldNotBeOneOf(
            PlaceholderCells,
            $"Row '{rowKey}' under '{heading}' has placeholder cell '{cell}'.");
        cell.Trim().Length.ShouldBeGreaterThan(
            12,
            $"Row '{rowKey}' under '{heading}' has cell '{cell}', too short to be a substantive record.");
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
        // Story 31.1 second-pass code review 2026-07-28: the chained form
        // `.Replace("\r\n","\n").Replace('\r','\n')` turned a `\r\r\n`-corrupted file into `\n\n`, inserting
        // a blank line that splits every Markdown table and reports as `GetTable ... found 0`. That is
        // exactly the corruption a bare append-CR normalization produces, which this repo's own CRLF
        // guidance warns about. One regex collapses any CR run plus optional LF to a single LF.
        return CarriageReturnRuns.Replace(File.ReadAllText(path), "\n");
    }

    private static readonly Regex CarriageReturnRuns =
        new(@"\r+\n?", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));

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
