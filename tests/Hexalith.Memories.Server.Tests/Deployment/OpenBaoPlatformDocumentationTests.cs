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
    /// </para></summary>
    private static readonly string[] PublishedDigestMarkers =
    [
        "sha256",
        "sha-256",
        "digest",
        "hash",
        "method-set",
        "manifest",
        "profile-id",
        "chart",

        // Git object ids are labelled by the vocabulary around them rather than by the word "sha".
        "commit",
        "baseline",
        "revision",
        "git ",
        "head",
    ];

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
        string[] scopes =
        [
            NormalizeWhitespace(document.GetSection(LimitationsHeading)),
            NormalizeWhitespace(document.GetSection(AvailabilityHeading)),
            NormalizeWhitespace(document.GetSection(ProfileHeading)),
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

        // The reconciled profile must never be silently weakened back.
        values.ShouldNotContain("tls_disable = 1", Case.Sensitive, "TLS must stay enabled on the listener.");
        values.ShouldNotContain("type: LoadBalancer", Case.Sensitive, "The Service must stay ClusterIP-only.");

        // namespace.yaml — Restricted Pod Security and the two ownership annotations.
        const string namespaceHeading = "`deploy/openbao/namespace.yaml`";
        string openBaoNamespace = ReadRepoFile("deploy/openbao/namespace.yaml");
        ShouldBindManifest("pod-security.kubernetes.io/enforce: restricted", document, namespaceHeading, "`pod-security.kubernetes.io/enforce: restricted`", openBaoNamespace, "Restricted profile");
        ShouldBindManifest("hexalith.io/platform-owner: jpiquot", document, namespaceHeading, "`hexalith.io/platform-owner: jpiquot`", openBaoNamespace, "owner");
        ShouldBindManifest("hexalith.io/security-reviewer: murat-tea-for-jpiquot", document, namespaceHeading, "`hexalith.io/security-reviewer: murat-tea-for-jpiquot`", openBaoNamespace, "reviewer");

        // service-account-hardening.yaml — the file's real effect, not the claim it used to carry.
        const string hardeningHeading = "`deploy/openbao/service-account-hardening.yaml`";
        string hardening = ReadRepoFile("deploy/openbao/service-account-hardening.yaml");
        ShouldBindManifest("automountServiceAccountToken: false", document, hardeningHeading, "`automountServiceAccountToken: false`", hardening, "overridden to `true` at pod level");

        string hardeningSection = NormalizeWhitespace(document.GetSection(hardeningHeading));
        hardeningSection.ShouldContain("pod-level", Case.Insensitive, "The section must state that the pod-level setting overrides the ServiceAccount default.");
        hardeningSection.ShouldNotContain(
            "does not receive an unused Kubernetes API token",
            Case.Insensitive,
            "The pre-Story-31.1 claim was measurably false as deployed and must not return.");

        // smoke-test.yaml — endpoint, CA-only projection, one-shot semantics.
        const string smokeTestHeading = "`deploy/openbao/smoke-test.yaml`";
        string smokeTest = ReadRepoFile("deploy/openbao/smoke-test.yaml");
        ShouldBindManifest("name: hexalith-keys-smoke-test", document, smokeTestHeading, "`kind: Job`, `name: hexalith-keys-smoke-test`", smokeTest, "Job");
        ShouldBindManifest("https://hexalith-keys.openbao.svc.cluster.local:8200", document, smokeTestHeading, "`BAO_ADDR: https://hexalith-keys.openbao.svc.cluster.local:8200`", smokeTest, "Service endpoint");
        ShouldBindManifest("/openbao/tls/ca.crt", document, smokeTestHeading, "`BAO_CACERT: /openbao/tls/ca.crt`", smokeTest, "ca.crt");
        ShouldBindManifest("ttlSecondsAfterFinished: 300", document, smokeTestHeading, "`backoffLimit: 0`, `activeDeadlineSeconds: 60`, `ttlSecondsAfterFinished: 300`", smokeTest, "five minutes");
        smokeTest.ShouldNotContain("tls-skip-verify", Case.Sensitive, "The smoke test must keep verifying OpenBao's TLS identity.");

        // The Job must project only the CA. Without `items` the whole Secret is mounted, which hands the
        // server private key to a throwaway status pod.
        smokeTest.ShouldContain("items:\n              - key: ca.crt", Case.Sensitive, "smoke-test.yaml must project only ca.crt from Secret openbao-server-tls.");
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
        divergences.Count.ShouldBeGreaterThanOrEqualTo(
            5,
            "Task 2 requires every unreconciled setting to stay named; silently shrinking this table hides an open item.");
        divergences.Select(static row => row[0]).ShouldContain(
            static key => key.Contains("has not been re-applied", StringComparison.OrdinalIgnoreCase),
            1,
            "The unproven reconciliation must stay recorded; without it the document implicitly claims the manifest reproduces the platform.");
        foreach (IReadOnlyList<string> row in divergences)
        {
            ShouldBeSubstantiveRow(row, DivergencesHeading);
        }

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
            ShouldHaveNoMatch(text, @"\b[sbr]\.[A-Za-z0-9]{20,}", relativePath, "a legacy token value");

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

        // C3: an executed command with its observed outcome, not a synthesized payload.
        evidence.ShouldContain(SmokeTestApplyCommand, Case.Sensitive, "C3 requires the exact apply command.");
        evidence.ShouldContain(SmokeTestWaitCommand, Case.Sensitive, "C3 requires the exact wait command.");
        evidence.ShouldContain("condition met", Case.Sensitive, "C3 requires the observed completion outcome.");
        evidence.ShouldContain("\"ha_enabled\": true", Case.Sensitive, "C3 requires the observed status fields.");
        evidence.ShouldContain("\"storage_type\": \"raft\"", Case.Sensitive, "C3 requires the observed status fields.");

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
            foreach (string field in new[] { "Waiver approver", "Waiver expiry", "Consequence", "Reopen trigger" })
            {
                record.ShouldContainKey(field, $"A waived C7 must record '{field}'.");
                ShouldBeSubstantiveCell(record[field], field, ReviewerEvaluationHeading);
            }
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
            // review is not a recorded evaluation.
            record.ShouldContainKey("Evaluation status");
            record["Evaluation status"].ShouldMatch(
                @"20\d{2}-\d{2}-\d{2}",
                "A signed C7 evaluation must carry its date.");
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

    /// <summary>The records AC3's "any evidence artifact" clause covers, plus the operations document.</summary>
    private static string[] ScannedRecords()
        => [DocRelativePath, EvidenceRelativePath, CreateStoryEvidenceRelativePath];

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
        return PublishedDigestMarkers.Any(marker => context.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadParagraph(string text, int lineStart, int lineEnd)
    {
        int start = lineStart;
        while (start > 0)
        {
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
