// <copyright file="OpenBaoPlatformDocumentationTests.SecurityControls.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>Fixed adversarial controls exercise the same guards as real records, without real approvals.</summary>
public sealed partial class OpenBaoPlatformDocumentationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public void SecretShapeGuard_RejectsEachCategoryWithoutDisclosingScannedInput(int control)
    {
        // Keep candidate text out of theory arguments, assertion messages, and public case identities.
        string[] candidates =
        [
            "-----BEGIN SYNTHETIC MATERIAL-----", "-----end synthetic material-----",
            "hvs.synthetic_1", "hvb.synthetic_2", "hvr.synthetic_3",
            "s.synthetic_url-_1_2_3_4_5", "b.synthetic_url-_1_2_3_4_5", "r.synthetic_url-_1_2_3_4_5",
            "Unseal Key 1: synthetic", "Recovery Key 2: synthetic", "Initial Root Token: synthetic",
            "01234567-89ab-cdef-0123-456789abcdef", new string('a', 40),
            "9Xk2Lm4Qp7Rt1Vw8Zc3Nb6Hj5Fd0Gs2Ay4Ue7Ik9Ol1Pr3Tw5=",
        ];
        const string before = "SYNTHETIC-SURROUNDING-BEFORE";
        const string after = "SYNTHETIC-SURROUNDING-AFTER";
        const string path = "synthetic/guard-control.md";
        string candidate = candidates[control];
        string input = before + "\n" + candidate + "\n" + after;
        ShouldAssertException failure = Should.Throw<ShouldAssertException>(() => ValidateSecretSafeRecord(input, path));
        string rendered = failure.ToString();
        rendered.Contains(candidate, StringComparison.Ordinal).ShouldBeFalse("Detected material must stay out of full exception rendering.");
        rendered.Contains(before, StringComparison.Ordinal).ShouldBeFalse("Surrounding scanned text must stay private.");
        rendered.Contains(after, StringComparison.Ordinal).ShouldBeFalse("Surrounding scanned text must stay private.");
        failure.InnerException.ShouldBeNull();
        failure.Message.StartsWith(path + ": ", StringComparison.Ordinal).ShouldBeTrue();
        failure.Message.Contains("; count 1.", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void SecretShapeGuard_AcceptsPublishedMetadataAndReportsOnlySafeCounts()
    {
        const string hex = "900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653";
        const string id = "01234567-89ab-cdef-0123-456789abcdef";
        foreach (string label in PublishedDigestMarkers)
        {
            ValidateSecretSafeRecord($"Published {label}: {hex}", "synthetic/metadata.md");
        }

        foreach (string label in PublishedIdentifierMarkers)
        {
            ValidateSecretSafeRecord($"{label}: {id}", "synthetic/metadata.md");
        }

        string input = new string('a', 40) + "\n\n" + new string('b', 40);
        ShouldAssertException failure = Should.Throw<ShouldAssertException>(() => ValidateSecretSafeRecord(input, "synthetic/count.md"));
        failure.Message.ShouldBe("synthetic/count.md: unlabelled long run; count 2.");
    }

    [Fact]
    public void CurrentSecurityRecords_AcceptIndependentAppointmentWithoutCompletion()
        => ValidateCurrentSecurityRecords(CreateAppointedSecurityControl());

    [Theory]
    [InlineData("jpiquot")]
    [InlineData("murat-tea-for-jpiquot")]
    [InlineData("administrator")]
    [InlineData("jpiquot@itaneo.com")]
    [InlineData("JPIQUOT@ITANEO.COM")]
    [InlineData("murat-tea-for-jpiquot@itaneo.com")]
    [InlineData("administrator@itaneo.com")]
    public void CurrentSecurityRecords_RejectOwnerOrPersonaAppointment(string reviewer)
    {
        foreach (Dictionary<string, string> source in new[] { CreateAppointedSecurityControl(), CreateSignedSecurityControl() })
        {
            Dictionary<string, string> control = source.ToDictionary(static pair => pair.Key,
                pair => pair.Value.Replace("independent-security-reviewer", reviewer, StringComparison.Ordinal));
            Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        }
    }

    [Theory]
    [InlineData("C4b")]
    [InlineData("C5b")]
    [InlineData("C7")]
    public void CurrentSecurityRecords_AcceptEachSeparatelySignedCheckpointWithPendingPeers(string checkpoint)
        => ValidateCurrentSecurityRecords(CreateCheckpointSecurityControl([checkpoint]), new DateOnly(2026, 11, 18));

    [Fact]
    public void CurrentSecurityRecords_AcceptDifferentIndependentSignersAndDatesForAllMandatoryGates()
    {
        Dictionary<string, string> control = CreateCheckpointSecurityControl(["C4b", "C5b", "C7"]);
        ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18));
        SetSyntheticParentStatus(control, "done");
        ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(21)]
    public void CurrentSecurityRecords_RejectIncompleteOrInconsistentCheckpointEvidence(int corruption)
    {
        Dictionary<string, string> control = CreateCheckpointSecurityControl(["C4b"]);
        const string evaluationField = "Checkpoint C4b evaluation";
        const string signer = "independent-seal-reviewer";
        switch (corruption)
        {
            case 0:
                control["evidence"] = RemoveSyntheticEvidenceRow(control["evidence"], "Checkpoint C4b provenance");
                break;
            case 1:
                control["evidence"] = RemoveSyntheticEvidenceRow(control["evidence"], "Checkpoint C4b independence");
                break;
            case 2:
                control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, evaluationField, 1,
                    "Signed by independent-seal-reviewer on 2026-02-30; covers C4b current measured profile and static file-based seal");
                break;
            case 3:
                control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, evaluationField, 1,
                    "Signed by independent-seal-reviewer on 2026-11-19; covers C4b current measured profile and static file-based seal");
                break;
            case 4:
                control["evidence"] = ReplaceSectionText(control["evidence"], ReviewerEvaluationHeading,
                    "covers C4b current measured profile", "covers C4b historical profile");
                break;
            case 5:
                control["evidence"] = ReplaceSectionText(control["evidence"], ReviewerEvaluationHeading,
                    "and static file-based seal", "and an unrelated limitation");
                break;
            case 6:
                control = control.ToDictionary(static pair => pair.Key,
                    pair => pair.Value.Replace(signer, "jpiquot", StringComparison.Ordinal));
                break;
            case 7:
                control = control.ToDictionary(static pair => pair.Key,
                    pair => pair.Value.Replace(signer, "murat-tea-for-jpiquot", StringComparison.Ordinal));
                break;
            case 8:
                control["story"] = ReplaceSyntheticCheckpointCell(control["story"], "C5b", 5, "2026-11-16");
                break;
            case 9:
                SetSyntheticParentStatus(control, "done");
                break;
            case 10:
                control["evidence"] = ReplaceSectionText(control["evidence"], "9.4 Current independent-review ownership",
                    "C4b is independently signed", "C4b is pending");
                break;
            case 11:
                control["runbook"] = ReplaceTableCell(control["runbook"], DivergencesHeading,
                    "The security reviewer of record is not independent of the platform owner", 2,
                    "C4b/C5b/C7 are pending / not complete; waiver expired on 2026-10-26 and is historical only");
                break;
            case 12:
                control["epic"] = control["epic"].Replace("C5b is pending / not complete with no completion date;", "C5b is complete;", StringComparison.Ordinal);
                break;
            case 13:
                control["sprint"] = control["sprint"].Replace("C7 is pending / not complete with no completion date;", "C7 is complete;", StringComparison.Ordinal);
                break;
            case 14:
                control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading,
                    "Checkpoint C4b provenance", 1, "Verified signature by another-reviewer; source: synthetic signed source; method: synthetic signature check");
                break;
            case 15:
                control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading,
                    "Checkpoint C4b provenance", 1, $"Verified signature by {signer}; source: synthetic signed source");
                break;
            case 16:
                control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading,
                    "Checkpoint C5b state", 1, "`pending` / `not complete`; completion date 2026-11-16");
                break;
            case 17:
                control["story"] = ReplaceSyntheticCheckpointCell(control["story"], "C4b", 5, "2026-11-17");
                break;
            case 18:
                control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, evaluationField, 1,
                    "Signed by independent-seal-reviewer on 2026-11-15; covers C4b current measured profile and static file-based seal");
                break;
            case 19:
                control["evidence"] = ReplaceSectionText(control["evidence"], ReviewerEvaluationHeading,
                    "covers C4b current measured profile", "covers C5b current measured profile");
                break;
            case 20:
                control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Consequence", 1,
                    "Both accepted limitations await an independent evaluation");
                break;
            case 21:
                control["runbook"] = ReplaceSectionText(control["runbook"], LimitationsHeading,
                    "C4b is independently signed", "C4b is pending");
                break;
        }

        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18)));
    }

    [Theory]
    [InlineData("C1")]
    [InlineData("C2")]
    [InlineData("C3")]
    [InlineData("C4a")]
    [InlineData("C5a")]
    [InlineData("C6")]
    public void CurrentSecurityRecords_RejectParentCompletionBeforeEveryMandatoryDocumentationGate(string checkpoint)
    {
        Dictionary<string, string> control = CreateSignedSecurityControl();
        control["story"] = ReplaceSyntheticCheckpointCell(control["story"], checkpoint, 4, "not complete");
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
    }

    [Fact]
    public void CurrentSecurityRecords_RejectUnverifiedLegacyClosureAndInconsistentAppointment()
    {
        Dictionary<string, string> missingProvenance = CreateSignedSecurityControl();
        missingProvenance["evidence"] = RemoveSyntheticEvidenceRow(missingProvenance["evidence"], "Evaluator provenance");
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(missingProvenance));

        Dictionary<string, string> missingIndependence = CreateAppointedSecurityControl();
        missingIndependence["evidence"] = RemoveSyntheticEvidenceRow(missingIndependence["evidence"], "Evaluator independence");
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(missingIndependence));

        Dictionary<string, string> inconsistentOwner = CreateAppointedSecurityControl();
        inconsistentOwner["story"] = ReplaceSyntheticCheckpointCell(inconsistentOwner["story"], "C5b", 1,
            "Independent reviewer another-reviewer; Administrator accountable");
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(inconsistentOwner));

        Dictionary<string, string> inconsistentPreamble = CreateAppointedSecurityControl();
        inconsistentPreamble["runbook"] = inconsistentPreamble["runbook"].Replace("Independent reviewer: **independent-security-reviewer**",
            "Independent reviewer: **another-reviewer**", StringComparison.Ordinal);
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(inconsistentPreamble));
    }

    [Theory]
    [InlineData("independent_security_reviewer")]
    [InlineData("security_reviewer@example.invalid")]
    public void CurrentSecurityRecords_AcceptUnderscoreAppointmentAndSignatureIdentities(string reviewer)
    {
        foreach (Dictionary<string, string> source in new[]
        {
            CreateAppointedSecurityControl(), CreateSignedSecurityControl(), CreateCheckpointSecurityControl(["C4b"]),
        })
        {
            Dictionary<string, string> control = source.ToDictionary(static pair => pair.Key,
                pair => pair.Value.Replace("independent-security-reviewer", reviewer, StringComparison.Ordinal)
                    .Replace("independent-seal-reviewer", reviewer, StringComparison.Ordinal));
            ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void CurrentSecurityRecords_RejectMissingC7ScopeAndExplicitCoverageDenials(int corruption)
    {
        Dictionary<string, string> control = corruption == 4
            ? CreateSignedSecurityControl() : CreateCheckpointSecurityControl([corruption < 2 ? "C7" : "C4b"]);
        string field = corruption == 4 ? "Evaluation status" : corruption < 2 ? "Checkpoint C7 evaluation" : "Checkpoint C4b evaluation";
        string evaluation = new MarkdownContractDocument(control["evidence"]).GetTableRows(ReviewerEvaluationHeading)
            .Single(row => row[0] == field)[1];
        evaluation = corruption switch
        {
            0 => evaluation.Replace(C7SignatureScope, "platform-wide evaluation", StringComparison.Ordinal),
            1 => evaluation.Replace(" and the amended AC2 qualifier", string.Empty, StringComparison.Ordinal),
            2 => evaluation.Replace("; covers ", "; does not cover ", StringComparison.Ordinal),
            _ => evaluation + "; does not cover current measured profile",
        };
        control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, field, 1, evaluation);
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void CurrentSecurityRecords_RejectUnresolvedOrContradictoryAuthorityEvidence(int corruption)
    {
        Dictionary<string, string> control = CreateCheckpointSecurityControl(["C4b"]);
        const string signer = "independent-seal-reviewer";
        string field = corruption is 2 or 3 ? "Checkpoint C4b independence" : "Checkpoint C4b provenance";
        string value = corruption switch
        {
            0 => $"Verified signature by {signer}; source: not yet available; method: synthetic signature verification control",
            1 => $"Verified signature by {signer}; source: records/signed-review.pdf; method: verification is pending",
            2 => $"Reviewer {signer} is independent of Platform Operations owner jpiquot and waiver approver jpiquot; evidence: reviewer is the platform owner",
            3 => $"Reviewer {signer} is independent of Platform Operations owner jpiquot and waiver approver jpiquot; evidence: independence confirmation is pending",
            4 => $"Verified signature by {signer}; source: records/signed-review.pdf; method: signature not verified by external authority",
            _ => $"Verified signature by {signer}; source: signed approval reference unresolved; method: synthetic signature verification control",
        };
        control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, field, 1, value);
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18)));
    }

    [Fact]
    public void CurrentSecurityRecords_AcceptCompletedReferencesAndExplicitNonOwnerIndependence()
    {
        Dictionary<string, string> control = CreateCheckpointSecurityControl(["C4b"]);
        control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Checkpoint C4b independence", 1,
            "Reviewer independent-seal-reviewer is independent of Platform Operations owner jpiquot and waiver approver jpiquot; evidence: reviewer is not the platform owner; external security team appointment verified");
        control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Checkpoint C4b provenance", 1,
            "Verified signature by independent-seal-reviewer; source: records/signed_review_2026-11-16.pdf; method: detached signature verified against independent authority key");
        ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18));
        Dictionary<string, string> closed = CreateSignedSecurityControl();
        closed["story"] = ReplaceSyntheticCheckpointCell(closed["story"], "C1", 4, "complete — executed evidence retained");
        closed["story"] = ReplaceSyntheticCheckpointCell(closed["story"], "C1", 3, "verified — substantive evidence checked");
        ValidateCurrentSecurityRecords(closed);
    }

    [Theory]
    [InlineData("completion date is 2026-11-16")]
    [InlineData("completion date: 11/16/2026")]
    public void CurrentSecurityRecords_RejectNumericPendingCompletionDates(string completion)
    {
        Dictionary<string, string> control = CreateCheckpointSecurityControl(["C4b"]);
        control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Checkpoint C5b state", 1,
            "`pending` / `not complete`; " + completion);
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CurrentSecurityRecords_RejectConditionalUnreviewedOrUnevidencedPrerequisites(int corruption)
    {
        Dictionary<string, string> control = CreateSignedSecurityControl();
        int cell = corruption switch { 0 => 4, 1 => 3, _ => 2 };
        string value = corruption switch { 0 => "complete only after missing evidence is supplied", 1 => "pending — review still owed", _ => string.Empty };
        control["story"] = ReplaceSyntheticCheckpointCell(control["story"], "C1", cell, value);
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    public void CurrentSecurityRecords_RejectWaiverRenewalAndMissingReassessment(bool completed, int corruption)
    {
        Dictionary<string, string> control = CreateCheckpointSecurityControl(completed ? ["C4b", "C5b", "C7"] : ["C4b"]);
        string trigger = corruption switch
        {
            0 => "Waiver extended to 2027-10-26; no further review needed",
            1 => "Expiry 2026-10-26 already fired; obtain independent evaluation; reassess only after a public release",
            _ => "Obtain independent evaluation; reassess on seal/ingress/voter changes or Story 31.2 activation",
        };
        control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Reopen trigger", 1, trigger);
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18)));
    }

    [Fact]
    public void CurrentSecurityRecords_RejectOppositeDuplicateSprintStatus()
    {
        Dictionary<string, string> control = CreateAppointedSecurityControl();
        control["sprint"] += "\n  31-1-openbao-platform-hardening-and-documentation: done\n";
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void CurrentSecurityRecords_RejectConflictingCheckpointClaimsInEveryCurrentSummary(int scope)
    {
        Dictionary<string, string> control = CreateCheckpointSecurityControl(["C4b"]);
        const string before = "C4b is independently signed";
        const string after = "C4b remains pending; no independent signature exists. C4b is independently signed";
        (string key, string? heading) = scope switch
        {
            0 => ("evidence", "9.4 Current independent-review ownership"),
            1 => ("runbook", DivergencesHeading),
            2 => ("epic", "Story 31.1: OpenBao Platform Hardening and Documentation"),
            3 => ("sprint", null),
            4 => ("evidence", ReviewerEvaluationHeading),
            _ => ("runbook", LimitationsHeading),
        };
        if (scope == 4)
        {
            string consequence = new MarkdownContractDocument(control["evidence"]).GetTableRows(ReviewerEvaluationHeading)
                .Single(static row => row[0] == "Consequence")[1];
            control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Consequence", 1,
                consequence.Replace(before, after, StringComparison.Ordinal));
        }
        else
        {
            control[key] = heading is null
                ? control[key].Replace(before, after, StringComparison.Ordinal)
                : ReplaceSectionText(control[key], heading, before, after);
        }
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 18)));
    }

    [Fact]
    public void C7EvidenceGuard_AcceptsSeparateSignatureAndRejectsMissingCheckpointEvaluation()
    {
        Dictionary<string, string> control = CreateCheckpointSecurityControl(["C7"]);
        ValidateSecurityReviewerEvidence(control["evidence"], new DateOnly(2026, 11, 18));
        string missing = RemoveSyntheticEvidenceRow(control["evidence"], "Checkpoint C7 evaluation");
        Should.Throw<ShouldAssertException>(() => ValidateSecurityReviewerEvidence(missing, new DateOnly(2026, 11, 18)));
    }

    /// <summary>Constructs a consistent appointment that supplies no signature or completion date.</summary>
    private static Dictionary<string, string> CreateAppointedSecurityControl()
    {
        const string reviewer = "independent-security-reviewer";
        Dictionary<string, string> control = CreateUnsignedSecurityControl()
            .ToDictionary(static pair => pair.Key, pair => pair.Value.Replace("unassigned", reviewer, StringComparison.Ordinal));
        control["evidence"] = AddSyntheticEvidenceRow(control["evidence"], "Evaluator independence",
            $"Reviewer {reviewer} is independent of Platform Operations owner jpiquot and waiver approver jpiquot; evidence: synthetic independent appointment control");
        return control;
    }

    /// <summary>Uses fixed, separate checkpoint signatures. No fixture depends on mutable real records.</summary>
    private static Dictionary<string, string> CreateCheckpointSecurityControl(string[] signedCheckpoints)
    {
        Dictionary<string, string> control = CreateAppointedSecurityControl();
        control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Evaluation status", 1,
            "Per-checkpoint evaluations. Only explicitly signed checkpoints are complete.");
        var statements = new List<string>();
        foreach ((string checkpoint, string signer, string date, string scope) in new[]
        {
            ("C4b", "independent-seal-reviewer", "2026-11-16", "static file-based seal"),
            ("C5b", "independent-ingress-reviewer", "2026-11-17", "namespace-wide port 8200 ingress"),
            ("C7", "independent-platform-reviewer", "2026-11-18", C7SignatureScope),
        })
        {
            if (signedCheckpoints.Contains(checkpoint, StringComparer.Ordinal))
            {
                control["evidence"] = AddSyntheticEvidenceRow(control["evidence"], $"Checkpoint {checkpoint} evaluation",
                    $"Signed by {signer} on {date}; covers {checkpoint} current measured profile and {scope}");
                control["evidence"] = AddSyntheticEvidenceRow(control["evidence"], $"Checkpoint {checkpoint} provenance",
                    $"Verified signature by {signer}; source: synthetic signed approval reference for {checkpoint}; method: synthetic verified authority control");
                control["evidence"] = AddSyntheticEvidenceRow(control["evidence"], $"Checkpoint {checkpoint} independence",
                    $"Reviewer {signer} is independent of Platform Operations owner jpiquot and waiver approver jpiquot; evidence: synthetic independent evaluator control");
                control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, $"Checkpoint {checkpoint} state", 1,
                    $"signed / complete by {signer} on {date}");
                control["story"] = ReplaceSyntheticCheckpointCell(control["story"], checkpoint, 1,
                    $"Independent reviewer {signer}; Administrator accountable for scheduling");
                control["story"] = ReplaceSyntheticCheckpointCell(control["story"], checkpoint, 3, $"signed by {signer} on {date}");
                control["story"] = ReplaceSyntheticCheckpointCell(control["story"], checkpoint, 4, "complete");
                control["story"] = ReplaceSyntheticCheckpointCell(control["story"], checkpoint, 5, date);
                statements.Add($"{checkpoint} is independently signed / complete by {signer} on {date}, completion date {date};");
            }
            else
            {
                statements.Add($"{checkpoint} is pending / not complete with no completion date;");
            }
        }

        if (signedCheckpoints.Length == 3)
        {
            control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Reopen trigger", 1,
                "Reassess on seal/ingress/voter changes or Story 31.2 activation");
        }

        string states = string.Join(" ", statements);
        control["evidence"] = ReplaceTableCell(control["evidence"], ReviewerEvaluationHeading, "Consequence", 1, states);
        control["evidence"] = ReplaceSectionText(control["evidence"], ReviewerEvaluationHeading,
            "**No reviewer signature was obtained.** Synthetic unsigned control.",
            "Separate checkpoint signatures are recorded below; unfinished reviews remain pending.");
        string limitations = new MarkdownContractDocument(control["runbook"]).GetSection(LimitationsHeading);
        control["runbook"] = ReplaceSectionText(control["runbook"], LimitationsHeading, limitations,
            limitations + "\n" + string.Join(" ", statements.Take(2)) + "\n");
        string summary = states + " Independent reviewer independent-security-reviewer; Administrator (`jpiquot`) accountable for scheduling and escalation. The historical waiver expired on 2026-10-26.";
        control["evidence"] = ReplaceSectionText(control["evidence"], "9.4 Current independent-review ownership",
            new MarkdownContractDocument(control["evidence"]).GetSection("9.4 Current independent-review ownership"), "\n" + summary + "\n");
        control["runbook"] = ReplaceTableCell(control["runbook"], DivergencesHeading,
            "The security reviewer of record is not independent of the platform owner", 2,
            states + " waiver expired on 2026-10-26 and is historical only");
        if (signedCheckpoints.Contains("C4b", StringComparer.Ordinal) && signedCheckpoints.Contains("C5b", StringComparer.Ordinal))
        {
            control["runbook"] = control["runbook"].Replace("countersignatures remain pending", "countersignatures are complete", StringComparison.Ordinal);
        }

        control["epic"] = control["epic"].Replace("C4b/C5b/C7 remain pending / not complete.", states, StringComparison.Ordinal);
        control["sprint"] = control["sprint"].Replace("C4b/C5b/C7 are pending/not complete with no completion date.", states, StringComparison.Ordinal);
        return control;
    }

    /// <summary>Changes the three tracking statuses together to isolate the parent-completion rule.</summary>
    private static void SetSyntheticParentStatus(IDictionary<string, string> control, string status)
    {
        control["story"] = control["story"].Replace("Status: in-progress", "Status: " + status, StringComparison.Ordinal);
        control["epic"] = control["epic"].Replace("**Status:** in-progress", "**Status:** " + status, StringComparison.Ordinal);
        control["sprint"] = control["sprint"].Replace("31-1-openbao-platform-hardening-and-documentation: in-progress",
            "31-1-openbao-platform-hardening-and-documentation: " + status, StringComparison.Ordinal);
    }

    /// <summary>Edits a checkpoint cell using its exact key and section.</summary>
    private static string ReplaceSyntheticCheckpointCell(string story, string checkpoint, int cell, string value)
    {
        string key = ReadSecurityCheckpoint(new MarkdownContractDocument(story), checkpoint)[0];
        return ReplaceTableCell(story, CheckpointsHeading, key, cell, value);
    }

    /// <summary>Adds one field to the existing reviewer-evaluation table.</summary>
    private static string AddSyntheticEvidenceRow(string evidence, string key, string value)
        => ReplaceSectionText(evidence, ReviewerEvaluationHeading, "| Checkpoint C7 state |",
            $"| {key} | {value} |\n| Checkpoint C7 state |");

    /// <summary>Removes a field only within the authoritative evaluation table.</summary>
    private static string RemoveSyntheticEvidenceRow(string evidence, string key)
    {
        string line = new MarkdownContractDocument(evidence).GetSection(ReviewerEvaluationHeading)
            .Split('\n').Single(candidate => candidate.StartsWith($"| {key} |", StringComparison.Ordinal));
        return ReplaceSectionText(evidence, ReviewerEvaluationHeading, line + "\n", string.Empty);
    }

    [Fact]
    public void SoleOwnerCloseout_AcceptsTheDatedDecisionWithAllPrerequisites()
    {
        Dictionary<string, string> control = CreateOwnerSecurityControl();
        ValidateSecurityReviewerEvidence(control["evidence"], new DateOnly(2026, 11, 22));
        ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 22));
    }

    [Theory]
    [InlineData("Owner declaration")]
    [InlineData("Decision provenance")]
    [InlineData("Decision owner")]
    [InlineData("Decision date")]
    [InlineData("Waiver approver")]
    [InlineData("Waiver scope")]
    [InlineData("Waiver rationale")]
    [InlineData("Compensating controls")]
    [InlineData("Historical waiver reopen trigger")]
    [InlineData("Accepted profile")]
    [InlineData("Technical outcome")]
    [InlineData("Peer-TLS finding")]
    [InlineData("Checkpoint C4b state")]
    [InlineData("Checkpoint C5b state")]
    [InlineData("Checkpoint C7 state")]
    public void SoleOwnerCloseout_RejectsMissingOrAlteredEvidence(string field)
    {
        Dictionary<string, string> control = CreateOwnerSecurityControl();
        string evidence = control["evidence"];
        foreach (string corrupt in new[]
        {
            RemoveSyntheticEvidenceRow(evidence, field),
            ReplaceTableCell(evidence, ReviewerEvaluationHeading, field, 1, "signed by murat-tea-for-jpiquot"),
        })
        {
            control["evidence"] = corrupt;
            Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        }
    }

    [Theory]
    [InlineData("C1")]
    [InlineData("C2")]
    [InlineData("C3")]
    [InlineData("C4a")]
    [InlineData("C5a")]
    [InlineData("C6")]
    [InlineData("C4b")]
    [InlineData("C5b")]
    [InlineData("C7")]
    public void SoleOwnerCloseout_RejectsMissingOrIncompleteMandatoryCheckpoints(string checkpoint)
    {
        Dictionary<string, string> control = CreateOwnerSecurityControl();
        string story = control["story"];
        string row = story.Split('\n').Single(line => line.StartsWith("| " + checkpoint + " -", StringComparison.Ordinal));
        control["story"] = story.Replace(row + "\n", string.Empty, StringComparison.Ordinal);
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        control["story"] = ReplaceSyntheticCheckpointCell(story, checkpoint, 4, "pending");
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
    }

    [Theory]
    [InlineData("evidence")]
    [InlineData("story")]
    [InlineData("runbook")]
    [InlineData("epic")]
    [InlineData("sprint")]
    [InlineData("handoff")]
    [InlineData("context")]
    [InlineData("remediation")]
    public void SoleOwnerCloseout_RejectsConflictingCurrentSummaries(string key)
    {
        Dictionary<string, string> control = CreateOwnerSecurityControl();
        string summary = key == "remediation" ? PeerFindingStatus : OwnerSummary;
        string original = control[key];
        foreach (string conflict in new[] { "C4b remains pending.", "C7 is independently signed.", "The independent reviewer approved C7.", "EVAL-01 is closed by this decision." })
        {
            control[key] = original.Replace(summary, summary + " " + conflict, StringComparison.Ordinal);
            Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        }
    }

    [Fact]
    public void SoleOwnerCloseout_RejectsMixedApprovalRoutesAndMisattributedSource()
    {
        Dictionary<string, string> control = CreateOwnerSecurityControl();
        string evidence = control["evidence"];
        control["evidence"] = AddSyntheticEvidenceRow(evidence, "Checkpoint C7 evaluation", "Signed by jpiquot on 2026-11-22");
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        control["evidence"] = evidence;
        control["decision"] = control["decision"].Replace(DecisionOwner, "Codex", StringComparison.Ordinal);
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        control = CreateOwnerSecurityControl();
        control["story"] = ReplaceSyntheticCheckpointCell(control["story"], "C1", 5, "2026-11-23");
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control, new DateOnly(2026, 11, 24)));
        control = CreateSignedSecurityControl();
        string attributedToOwner = control["evidence"].Replace("independent-security-reviewer", "jpiquot", StringComparison.Ordinal);
        Should.Throw<ShouldAssertException>(() => ValidateSecurityReviewerEvidence(attributedToOwner));
    }

    [Fact]
    public void SoleOwnerCloseout_RejectsChangedSignedBytesOrMissingOrMismatchedSignature()
    {
        Dictionary<string, string> control = CreateOwnerSecurityControl();
        string decision = control["decision"];
        foreach (string changed in new[]
        {
            decision.Replace("All 107 passed", "All 108 passed", StringComparison.Ordinal),
            decision.Replace("/s/ Codex", "/s/ jpiquot", StringComparison.Ordinal),
            decision.Replace(VerificationDigest, new string('0', 64), StringComparison.Ordinal),
        })
        {
            control["decision"] = changed;
            Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        }

        control["decision"] = decision[..decision.IndexOf("### " + SignatureHeading, StringComparison.Ordinal)];
        Should.Throw<InvalidOperationException>(() => ValidateCurrentSecurityRecords(control));
    }

    [Fact]
    public void SoleOwnerCloseout_RejectsBroadenedWaiverAndReportsOnlyTheTrustedField()
    {
        Dictionary<string, string> control = CreateOwnerSecurityControl();
        string evidence = control["evidence"];
        control["evidence"] = ReplaceTableCell(evidence, ReviewerEvaluationHeading, "Waiver scope", 1,
            OwnerDecisionFields["Waiver scope"] + " Also covers C4b and C5b.");
        Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        const string candidate = "hvs.synthetic_scope_secret";
        control["evidence"] = ReplaceTableCell(evidence, ReviewerEvaluationHeading, "Owner declaration", 1, candidate);
        ShouldAssertException error = Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        error.Message.ShouldBe("Story 31.1 closeout evidence mismatch: Owner declaration.");
        error.ToString().Contains(candidate, StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public void SoleOwnerCloseout_RejectsContradictionsInCurrentEvidenceProseAndPriorSprintComments()
    {
        foreach (string conflict in new[] { "C4b is incomplete.", "The owner resolved EVAL-01.", "Production qualification is approved by this decision." })
        {
            Dictionary<string, string> control = CreateOwnerSecurityControl();
            control["evidence"] = ReplaceSectionText(control["evidence"], ReviewerEvaluationHeading, OwnerSummary, OwnerSummary + " " + conflict);
            Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
            control = CreateOwnerSecurityControl();
            control["sprint"] = control["sprint"].Replace("  # " + OwnerSummary, "  # " + conflict + "\n  # " + OwnerSummary, StringComparison.Ordinal);
            Should.Throw<ShouldAssertException>(() => ValidateCurrentSecurityRecords(control));
        }

        Dictionary<string, string> history = CreateOwnerSecurityControl();
        history["handoff"] += "\n\nOn November 16 the signer reported no approval. If C4b is incomplete in a future review, record the evidence.";
        history["remediation"] += "\n\n## Later independent work\n\nThe owner resolved EVAL-01 following separate approval.";
        ValidateCurrentSecurityRecords(history);
    }

    // Fixed, in-memory inputs keep both the historical independent route and this dated exception
    // testable regardless of which route current records exercise.
    private static Dictionary<string, string> CreateOwnerSecurityControl()
    {
        Dictionary<string, string> control = CreateUnsignedSecurityControl();
        SetSyntheticParentStatus(control, "done");
        var fields = new Dictionary<string, string>(OwnerDecisionFields, StringComparer.Ordinal)
        {
            ["Reviewer of record"] = "Historical owner persona",
            ["Waiver approved"] = "2026-07-28. Historical waiver.",
            ["Approver independence"] = "**Not independent**",
        };
        foreach ((string checkpoint, string decision) in OwnerDispositions)
        {
            fields[$"Checkpoint {checkpoint} state"] = $"{DecisionDate} — {DecisionOwner} {decision} by sole-owner decision.";
            string evidence = $"`31-1-openbao-platform-evidence.md` section 4, {checkpoint} decision for the section 9.1 measured profile";
            string row = $"| {checkpoint} - {decision} | Platform Operations owner {DecisionOwner} | {evidence} | verified — {DecisionOwner} {decision} by sole-owner decision | complete | {DecisionDate} |";
            string old = control["story"].Split('\n').Single(line => line.StartsWith("| " + checkpoint + " -", StringComparison.Ordinal));
            control["story"] = control["story"].Replace(old, row, StringComparison.Ordinal);
        }

        control["story"] = control["story"].Replace("## " + CheckpointsHeading, "## " + CheckpointsHeading + "\n\n" + OwnerSummary, StringComparison.Ordinal) + "\n";
        control["evidence"] = $"## {ReviewerEvaluationHeading}\n\n{OwnerSummary}\n\n| Field | Record |\n| --- | --- |\n"
            + string.Join("\n", fields.Select(static entry => $"| {entry.Key} | {entry.Value} |"))
            + $"\n\n## Measured deployment\n\n### {CurrentProfileEvidenceHeading}\n\n| Contract | Measured value on 2026-11-16 |\n| --- | --- |\n"
            + "| Server version | `2.6.2` on all three servers |\n| Chart label | openbao-0.29.5 |\n"
            + "| Raft voters | all Running on `node1`; three healthy servers observed, voting membership unmeasured |\n\n"
            + $"### {OwnerSummaryHeading}\n\n{OwnerSummary} {WaiverHistory} Section 4 records the declaration and each disposition.\n";
        control["runbook"] = $"""
            Independent reviewer: **unassigned**. {OwnerSummary}

            ## {DivergencesHeading}

            | Divergence | Owner | Why it is open | Reopen trigger |
            | --- | --- | --- | --- |
            | The security reviewer of record is not independent of the platform owner | Platform Operations owner {DecisionOwner} | {OwnerSummary} {WaiverHistory} | Reassess on seal/ingress/voter changes or Story 31.2 activation. |

            ## {LimitationsHeading}

            On 2026-11-22 Jérôme Piquot (`jpiquot`) accepted the static file-based seal (C4b) and namespace-wide port 8200 ingress (C5b) for the measured profile without independent countersignatures. Each row records the consequence, compensating controls and reopen trigger.
            """.ReplaceLineEndings("\n");
        control["epic"] = "### Story 31.1: OpenBao Platform Hardening and Documentation\n\n**Status:** done. " + OwnerSummary + " Helm reproduction is separate Platform Operations work.";
        control["sprint"] = "# Story 31.1 renamed\n  # " + OwnerSummary + "\n  31-1-openbao-platform-hardening-and-documentation: done\n  31-2-runtime-secret-store-migration: ready-for-dev";
        control["context"] = "## Requirements & Constraints\n\n" + OwnerSummary;
        control["handoff"] = "## " + HandoffHeading + "\n\n" + OwnerSummary;
        control["decision"] = $"## Intent\n\nOn {DecisionDate} the sole contributor, Platform Operations owner {DecisionOwner}, approved the following Story 31.1 decision in the project conversation:\n\n> {OwnerDeclaration}";
        // Only immutable, digest-pinned verification history comes from disk. The decision and all current states are fixture data.
        var signedHistory = new MarkdownContractDocument(ReadRepoFile("_bmad-output/implementation-artifacts/" + CloseoutSpec));
        control["decision"] += $"\n\n### {VerificationHeading}\n{signedHistory.GetSection(VerificationHeading)}\n### {SignatureHeading}\n{signedHistory.GetSection(SignatureHeading)}";
        control["remediation"] = "## Story 31.1 decision on 2026-11-22\n\n" + PeerFindingStatus;
        return control;
    }
}
