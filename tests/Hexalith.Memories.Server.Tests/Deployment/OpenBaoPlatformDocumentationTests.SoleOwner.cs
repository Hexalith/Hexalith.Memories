// <copyright file="OpenBaoPlatformDocumentationTests.SoleOwner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

public sealed partial class OpenBaoPlatformDocumentationTests
{
    private const string CloseoutSpec = "spec-31-1-openbao-platform-hardening-and-documentation-4.md";
    private const string PeerTlsSpec = "spec-31-1-eval-01-peer-tls-remediation.md";
    private const string DecisionOwner = "Jérôme Piquot (`jpiquot`)";
    private const string DecisionDate = "2026-11-22";
    private const string OwnerRoute = "Sole-owner closeout of Story 31.1";
    private const string OwnerDeclaration = "I approve sole-owner closeout for 31.1, accept the documented static seal and namespace-wide ingress, and will track the peer-TLS finding separately.";
    private const string DeclarationDigest = "ef8e896223c43acf0ec13cece966d84ccee1d8e435c4613f984ce66d350fc2a9";
    private const string VerificationHeading = "2026-11-22 closeout check";
    private const string SignatureHeading = "Signature for the November 22 verification";
    private const string VerificationDigest = "748731eb663b76bdc7a9d53724683c48c46dc36c3db3fc60e325fecf7aa45b01";
    private const string OwnerSummary = "On " + DecisionDate + ", " + DecisionOwner + " closed Story 31.1 by sole-owner risk acceptance for C4b, C5b and C7. No independent countersignatures were obtained. EVAL-01 remains open with Platform Operations.";
    private const string WaiverHistory = "The July C7 waiver expired on 2026-10-26.";
    private const string OwnerSummaryHeading = "9.4 Current independent-review ownership";
    private const string HandoffHeading = "Story 31.1 disposition on 2026-11-22";
    private const string PeerFindingStatus = "EVAL-01 remained open with Platform Operations owner " + DecisionOwner + " at Story 31.1 closeout. The decision did not approve or execute peer-TLS remediation.";

    private static readonly (string Checkpoint, string Decision)[] OwnerDispositions =
    [
        ("C4b", "accepted the documented static file-based seal"),
        ("C5b", "accepted the documented namespace-wide port 8200 ingress"),
        ("C7", "closed documentation of the measured profile under amended AC2"),
    ];

    // This exception is one attributable decision. A new owner, date, profile or declaration needs its own authority.
    private static readonly Dictionary<string, string> OwnerDecisionFields = new(StringComparer.Ordinal)
    {
        ["Evaluation status"] = OwnerRoute,
        ["Platform Operations owner"] = DecisionOwner,
        ["Decision owner"] = "Sole contributor and Platform Operations owner " + DecisionOwner,
        ["Decision date"] = DecisionDate,
        ["Owner declaration"] = OwnerDeclaration,
        ["Decision provenance"] = "Owner declaration in the project conversation, recorded in the [approved Story 31.1 closeout spec](../" + CloseoutSpec + "#intent).",
        ["Accepted profile"] = "Section 9.1, measured 2026-11-16: OpenBao 2.6.2, chart label 0.29.5, three healthy servers on node1, voting membership unmeasured.",
        ["Technical outcome"] = "The [2026-11-16 assistant evaluation](31-1-assistant-security-evaluation-2026-11-16.md) requested changes. The [November 22 assistant signature](../" + CloseoutSpec + "#signature-for-the-november-22-verification) covers the build and tests.",
        ["Peer-TLS finding"] = "EVAL-01 remains open with Platform Operations owner " + DecisionOwner + ". [Peer-TLS remediation](../" + PeerTlsSpec + ") requires separate approval and execution.",
        ["Independent reviewer"] = "**unassigned** — the owner persona supplies no independent authority",
        ["Independent countersignatures"] = "None. The owner accepted the risks without an independent signature for C4b, C5b or C7.",
        ["Waiver approver"] = "**Administrator / Jérôme Piquot (`jpiquot`)**, approved 2026-07-28 during Story 31.1 code review",
        ["Waiver scope"] = "Checkpoint C7 only — the reviewer's recorded evaluation. It waives **no** acceptance criterion, neither accepted limitation, and no other checkpoint. AC2's documentation obligations were met independently and are asserted executably",
        ["Waiver rationale"] = "The platform is already deployed and this story documents rather than changes it. Both limitations are recorded with owner, consequence, compensating controls and reopen trigger, and the documentation is bound by an executed guard, so the record is reviewable on its own evidence while the evaluation is scheduled",
        ["Compensating controls"] = "Historical waiver record from 2026-07-28: Restricted Pod Security enforced on the namespace; RBAC scoped to the platform ServiceAccounts; four `hexalith-keys*` Services `ClusterIP` with no ingress on those Services; persistent JSON audit device; `OpenBaoPlatformDocumentationTests` executing over both published records on every CI run so the accepted-limitation text cannot silently soften",
        ["Historical waiver reopen trigger"] = "Whichever comes first: the 2026-10-26 expiry; a dated, named evaluation by `murat-tea-for-jpiquot` covering the platform as measured, both accepted limitations and the amended AC2 qualifier, appended to this artifact as its own row; any change to the seal configuration, the NetworkPolicy ingress, or the voter count; or Story 31.2 activation",
        ["Waiver expiry"] = "**2026-10-26** (90 days). C7 reopened on expiry. The waiver is historical only.",
        ["Reopen trigger"] = "Reassess on seal/ingress/voter changes or Story 31.2 activation.",
    };

    private static bool IsSoleOwnerRecord(IReadOnlyDictionary<string, string> record)
        => record.ContainsKey("Owner declaration") || record.ContainsKey("Decision owner")
            || (record.TryGetValue("Evaluation status", out string? evaluation) && evaluation == OwnerRoute);

    private static void ValidateSoleOwnerEvidence(MarkdownContractDocument evidence, IReadOnlyDictionary<string, string> record, DateOnly today)
    {
        today.ShouldBeGreaterThanOrEqualTo(new DateOnly(2026, 11, 22));
        foreach ((string field, string value) in OwnerDecisionFields)
        {
            RequireDecision(record.TryGetValue(field, out string? actual) && actual == value, field);
        }

        foreach ((string checkpoint, string decision) in OwnerDispositions)
        {
            string field = $"Checkpoint {checkpoint} state";
            string expected = $"{DecisionDate} — {DecisionOwner} {decision} by sole-owner decision.";
            RequireDecision(record.TryGetValue(field, out string? actual) && actual == expected, field);
        }

        // Only the dated decision and historical waiver fields belong in this exception.
        // Independent signatures and additional decision fields would introduce a second authority.
        string[] historyFields =
        [
            "Reviewer of record", "Approver independence", "Waiver approved", "Historical waiver consequence",
            "Historical scheduling accountable owner",
        ];
        RequireDecision(!record.Keys.Except(OwnerDecisionFields.Keys, StringComparer.Ordinal)
            .Except(OwnerDispositions.Select(static gate => $"Checkpoint {gate.Checkpoint} state"), StringComparer.Ordinal)
            .Except(historyFields, StringComparer.Ordinal).Any(), "unexpected decision field");
        RequireDecision(record.TryGetValue("Waiver approved", out string? approved) && approved.StartsWith("2026-07-28.", StringComparison.Ordinal), "Waiver approved");
        RequireDecision(record.TryGetValue("Approver independence", out string? independence) && independence.StartsWith("**Not independent", StringComparison.Ordinal), "Approver independence");

        string narrative = string.Join(" ", evidence.GetSection(ReviewerEvaluationHeading)
            .Split('\n').Where(static line => !line.StartsWith('|')));
        ValidateOwnerSummary(narrative);

        RequireDecision(evidence.GetTableHeader(CurrentProfileEvidenceHeading).SequenceEqual(["Contract", "Measured value on 2026-11-16"]), "profile table header");
        RequireDecision(ReadManifestRow(evidence, CurrentProfileEvidenceHeading, "Server version")[1] == "`2.6.2` on all three servers", "profile server version");
        RequireDecision(ReadManifestRow(evidence, CurrentProfileEvidenceHeading, "Chart label")[1].Contains("openbao-0.29.5", StringComparison.Ordinal), "profile chart label");
        string voters = ReadManifestRow(evidence, CurrentProfileEvidenceHeading, "Raft voters")[1];
        RequireDecision(voters.Contains("all Running on `node1`", StringComparison.Ordinal)
            && voters.Contains("three healthy servers observed, voting membership unmeasured", StringComparison.Ordinal), "profile voting membership");
    }

    private static void ValidateSoleOwnerCloseout(IReadOnlyDictionary<string, string> sources, MarkdownContractDocument story, string epic, string tracking)
    {
        var decisionSpec = new MarkdownContractDocument(sources["decision"]);
        string intent = decisionSpec.GetSection("Intent");
        RequireDecision(intent.Contains($"On {DecisionDate} the sole contributor, Platform Operations owner {DecisionOwner}, approved the following Story 31.1 decision in the project conversation:", StringComparison.Ordinal), "declaration provenance");
        string[] quotes = intent.Split('\n').Where(static line => line.StartsWith('>')).ToArray();
        RequireDecision(quotes.Length == 1 && quotes[0] == "> " + OwnerDeclaration, "declaration");
        RequireDecision(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(quotes[0][2..]))) == DeclarationDigest, "declaration digest");
        ValidateTechnicalAttestation(sources["decision"], decisionSpec);

        foreach ((string checkpoint, string decision) in OwnerDispositions)
        {
            IReadOnlyList<string> row = ReadSecurityCheckpoint(story, checkpoint);
            RequireDecision(row[1] == "Platform Operations owner " + DecisionOwner, $"{checkpoint} owner");
            RequireDecision(row[2] == $"`31-1-openbao-platform-evidence.md` section 4, {checkpoint} decision for the section 9.1 measured profile", $"{checkpoint} evidence");
            RequireDecision(row[3] == $"verified — {DecisionOwner} {decision} by sole-owner decision", $"{checkpoint} review");
            RequireDecision(row[4] == "complete", $"{checkpoint} completion");
            RequireDecision(row[5] == DecisionDate, $"{checkpoint} date");
        }

        var evidence = new MarkdownContractDocument(sources["evidence"]);
        NormalizeWhitespace(evidence.GetSection(OwnerSummaryHeading)).ShouldBe(OwnerSummary + " " + WaiverHistory + " Section 4 records the declaration and each disposition.");

        var runbook = new MarkdownContractDocument(sources["runbook"]);
        runbook.GetTableHeader(DivergencesHeading).ShouldBe(["Divergence", "Owner", "Why it is open", "Reopen trigger"]);
        IReadOnlyList<string> divergence = ReadManifestRow(runbook, DivergencesHeading, "The security reviewer of record is not independent of the platform owner");
        RequireDecision(divergence.Skip(1).SequenceEqual(["Platform Operations owner " + DecisionOwner, OwnerSummary + " " + WaiverHistory, OwnerDecisionFields["Reopen trigger"]]), "runbook reviewer disposition");
        string limitations = NormalizeWhitespace(runbook.GetSection(LimitationsHeading).Split('|')[0]);
        limitations.ShouldBe("On 2026-11-22 Jérôme Piquot (`jpiquot`) accepted the static file-based seal (C4b) and namespace-wide port 8200 ingress (C5b) for the measured profile without independent countersignatures. Each row records the consequence, compensating controls and reopen trigger.");

        string preamble = sources["runbook"].Split("\n## ", StringSplitOptions.None)[0];
        preamble.ShouldContain("Independent reviewer: **unassigned**", Case.Sensitive);
        ValidateOwnerSummary(preamble);
        string checkpointNarrative = string.Join(" ", story.GetSection(CheckpointsHeading)
            .Split('\n').Where(static line => !line.TrimStart().StartsWith('|')));
        ValidateOwnerSummary(checkpointNarrative);
        ValidateOwnerSummary(epic.Split("**Split note", StringSplitOptions.None)[0]);
        RejectConflictingCloseoutClaims(epic.Replace(OwnerSummary, string.Empty, StringComparison.Ordinal).Replace(WaiverHistory, string.Empty, StringComparison.Ordinal));
        ValidateOwnerSummary(tracking);
        ValidateOwnerSummary(new MarkdownContractDocument(sources["context"]).GetSection("Requirements & Constraints"));
        ValidateOwnerSummary(new MarkdownContractDocument(sources["handoff"]).GetSection(HandoffHeading));

        string findingAtCloseout = new MarkdownContractDocument(sources["remediation"]).GetSection("Story 31.1 decision on 2026-11-22");
        NormalizeWhitespace(findingAtCloseout).ShouldBe(PeerFindingStatus);
    }

    private static void ValidateTechnicalAttestation(string source, MarkdownContractDocument spec)
    {
        string heading = "### " + VerificationHeading;
        RequireDecision(source.Split('\n').Count(line => line == heading) == 1, "signed verification heading");
        string section = heading + "\n" + spec.GetSection(VerificationHeading).TrimEnd('\n') + "\n";
        byte[] covered = Encoding.UTF8.GetBytes(section.ReplaceLineEndings("\r\n"));
        RequireDecision(covered.Length == 1205 && Convert.ToHexStringLower(SHA256.HashData(covered)) == VerificationDigest, "signed verification bytes");

        string signature = NormalizeWhitespace(spec.GetSection(SignatureHeading));
        string attestation = "Signed electronically by **/s/ Codex**, AI assistant who ran the verification, at "
            + "`2026-11-22T11:43:12Z` (UTC). This typed signature covers the preceding "
            + $"`{VerificationHeading}` from its heading through its final CRLF (1205 bytes, SHA-256 `{VerificationDigest}`).";
        RequireDecision(signature.Contains(attestation, StringComparison.Ordinal), "November 22 technical signature");
    }

    // Keep scanned record values and collections out of diagnostics, even when validation fails first.
    private static void RequireDecision(bool valid, string field)
    {
        if (!valid)
        {
            throw new ShouldAssertException($"Story 31.1 closeout evidence mismatch: {field}.");
        }
    }

    private static void ValidateOwnerSummary(string text)
    {
        string normalized = NormalizeWhitespace(text);
        normalized.ShouldContain(OwnerSummary, Case.Sensitive);
        int decision = normalized.IndexOf(OwnerSummary, StringComparison.Ordinal);
        string remaining = normalized.Remove(decision, OwnerSummary.Length).Replace(WaiverHistory, string.Empty, StringComparison.Ordinal);
        RejectConflictingCloseoutClaims(remaining);
    }

    private static void RejectConflictingCloseoutClaims(string remaining)
    {
        remaining.ShouldNotMatch(
            @"(?ix)\b(?:C4b|C5b|C7|Story\ 31\.1)\b\s+(?:(?:is|are|remain[s]?|stays?|still|now|was|has\ been)\s+)*(?:in-progress|pending|not\ complete|blocked|waived|independently\ (?:signed|approved|reviewed)|countersigned)\b
              |\b(?:independently\ (?:signed|approved)|independent\ (?:reviewer|authority|evaluator)\s+(?:has\ )?(?:signed|approved)|(?:independent\ )?countersignatures?\s+(?:(?:are|were|remain|remains)\s+)?(?:obtained|complete|pending))\b
              |\b(?:EVAL-01|peer[-\ ]TLS(?:\ finding)?)\b\s+(?:(?:is|was|has\ been|now|remains|marked)\s+)*(?:fixed|resolved|remediated|closed)\b
              |(?:^\s*|[.!?;]\s+|\#\s+)(?:(?:C4b|C5b|C7)\ is\ incomplete|(?:the\ )?owner\ (?:has\ )?resolved\ EVAL-01|Production\ qualification\ is\ approved\ by\ this\ decision)\b
              |\b(?:waiver\ (?:was\ )?(?:renewed|extended)|(?:this\ decision|sole-owner\ (?:decision|closeout))\s+(?:qualifies|authorizes|approves|resolves|fixes)\s+(?:Production|migration|EVAL-01|peer[-\ ]TLS))\b",
            "Current summaries must agree with the dated owner decision and the open peer-TLS finding.");
    }
}
