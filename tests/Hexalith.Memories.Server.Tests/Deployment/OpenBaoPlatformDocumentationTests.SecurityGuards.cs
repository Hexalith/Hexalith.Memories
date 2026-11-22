// <copyright file="OpenBaoPlatformDocumentationTests.SecurityGuards.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using Hexalith.Memories.TestHelpers.Documentation;

using Shouldly;

/// <summary>Secret-safe diagnostics and independent checkpoint validation for Story 31.1.</summary>
public sealed partial class OpenBaoPlatformDocumentationTests
{
    private const string C7SignatureScope = "platform-wide evaluation, both accepted limitations and the amended AC2 qualifier";
    /// <summary>Runs the actual published-record detector without handing scanned text or collections
    /// to an assertion formatter. A timeout also omits the regex exception's input and inner exception.</summary>
    internal static void ValidateSecretSafeRecord(string text, string relativePath)
    {
        try
        {
            ShouldHaveNoMatch(text, "-----BEGIN|-----END", relativePath, "PEM marker");
            ShouldHaveNoMatch(text, @"\bhv[sbr]\.[A-Za-z0-9_-]{8,}", relativePath, "OpenBao token");
            ShouldHaveNoMatch(text, @"\b[sbr]\.[A-Za-z0-9_-]{20,}", relativePath, "legacy token");
            RejectSecretMatches(FindUnexpectedUuids(text).Count, relativePath, "unlabelled UUID");
            ShouldHaveNoMatch(text, @"(?:Unseal|Recovery)\s+Key\s*\d*\s*:", relativePath, "init key label");
            ShouldHaveNoMatch(text, @"Initial\s+Root\s+Token\s*:", relativePath, "init root-token label");
            RejectSecretMatches(FindUnexpectedLongRuns(text).Count, relativePath, "unlabelled long run");
        }
        catch (RegexMatchTimeoutException)
        {
            throw new ShouldAssertException($"{relativePath}: credential detection timeout.");
        }
    }

    /// <summary>Constructs a failure from trusted metadata alone, bypassing expression capture.</summary>
    private static void RejectSecretMatches(int count, string relativePath, string category)
    {
        if (count != 0)
        {
            throw new ShouldAssertException($"{relativePath}: {category}; count {count}.");
        }
    }

    /// <summary>Validates reviewer appointment, signatures and parent completion independently.
    /// The optional clock is fixed only by in-memory controls; real records use today's UTC date.</summary>
    private static void ValidateCurrentSecurityRecords(IReadOnlyDictionary<string, string> sources, DateOnly? currentDate = null)
    {
        DateOnly today = currentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var evidence = new MarkdownContractDocument(sources["evidence"]);
        evidence.GetTableHeader(ReviewerEvaluationHeading).ShouldBe(["Field", "Record"]);
        Dictionary<string, string> record = evidence.GetTableRows(ReviewerEvaluationHeading)
            .ToDictionary(static row => row[0], static row => row[1], StringComparer.Ordinal);
        var story = new MarkdownContractDocument(sources["story"]);
        (string status, string epic, string tracking) = ReadParentTracking(sources);
        if (IsSoleOwnerRecord(record))
        {
            ValidateSoleOwnerEvidence(evidence, record, today);
            status.ShouldBe("done");
            ValidateDocumentationPrerequisites(story, new DateOnly(2026, 11, 22));
            ValidateSoleOwnerCloseout(sources, story, epic, tracking);
            return;
        }

        string evaluation = record["Evaluation status"].Replace("**", string.Empty, StringComparison.Ordinal);
        string reviewer = RemoveReviewMarkdown(record["Independent reviewer"]).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        bool assigned = !string.Equals(reviewer, "unassigned", StringComparison.OrdinalIgnoreCase);
        bool wholeReviewSigned = evaluation.StartsWith("Signed by ", StringComparison.OrdinalIgnoreCase);
        bool perCheckpoint = evaluation.StartsWith("Per-checkpoint evaluations.", StringComparison.Ordinal);
        var completed = new Dictionary<string, (string Signer, string Date)>(StringComparer.Ordinal);
        if (assigned)
        {
            ValidateIndependentIdentity(reviewer);
            ValidateIndependence(record, "Evaluator independence", reviewer);
            record["Owner of the blocker"].ShouldContain(reviewer, Case.Sensitive);
            record["Owner of the blocker"].ShouldNotContain("unassigned", Case.Insensitive);
        }

        record["Scheduling accountable owner"].ShouldContain("Administrator (`jpiquot`)", Case.Sensitive);
        record["Waiver expiry"].ShouldContain("2026-10-26", Case.Sensitive);
        if (wholeReviewSigned)
        {
            (string signer, string date) = ValidateSignature(evaluation, "C4b/C5b/C7", "both accepted limitations", today);
            reviewer.ShouldBe(signer);
            ValidateProvenance(record, "Evaluator provenance", signer);
            record["Consequence"].ShouldContain($"independently evaluated by {signer} on {date}", Case.Sensitive);
            record["Reopen trigger"].ShouldStartWith("Reassess", Case.Sensitive);
            foreach (string checkpoint in new[] { "C4b", "C5b", "C7" })
            {
                completed.Add(checkpoint, (signer, date));
            }
        }
        else if (!perCheckpoint)
        {
            evaluation.ShouldStartWith("Not signed.", Case.Sensitive);
            RemoveReviewMarkdown(record["Reopen trigger"]).ShouldContain("2026-10-26 already fired", Case.Sensitive);
        }

        foreach ((string checkpoint, string scope) in new[]
        {
            ("C4b", "static file-based seal"), ("C5b", "namespace-wide port 8200 ingress"), ("C7", C7SignatureScope),
        })
        {
            bool hasEvaluation = record.TryGetValue($"Checkpoint {checkpoint} evaluation", out string? gateEvaluation);
            if (hasEvaluation)
            {
                // Explicit checkpoint evidence must never be silently ignored by a legacy global branch.
                perCheckpoint.ShouldBeTrue("Separate checkpoint evidence needs the per-checkpoint evaluation format.");
                (string signer, string date) = ValidateSignature(gateEvaluation!, checkpoint, scope, today);
                ValidateIndependence(record, $"Checkpoint {checkpoint} independence", signer);
                ValidateProvenance(record, $"Checkpoint {checkpoint} provenance", signer);
                completed.Add(checkpoint, (signer, date));
            }
            else
            {
                record.ContainsKey($"Checkpoint {checkpoint} provenance").ShouldBeFalse("Orphan provenance is inconsistent.");
                record.ContainsKey($"Checkpoint {checkpoint} independence").ShouldBeFalse("Orphan independence evidence is inconsistent.");
            }

            IReadOnlyList<string> row = ReadSecurityCheckpoint(story, checkpoint);
            string state = record[$"Checkpoint {checkpoint} state"];
            row[1].ShouldContain("Administrator", Case.Sensitive);
            if (completed.TryGetValue(checkpoint, out (string Signer, string Date) signature))
            {
                state.ShouldBe($"signed / complete by {signature.Signer} on {signature.Date}");
                row[1].ShouldContain(signature.Signer, Case.Sensitive);
                row[3].ShouldStartWith($"signed by {signature.Signer} on {signature.Date}", Case.Sensitive);
                row[4].ShouldBe("complete");
                row[5].ShouldBe(signature.Date);
            }
            else
            {
                state.ShouldContain("`pending` / `not complete`", Case.Sensitive);
                state.ShouldNotContain("waived, not evaluated", Case.Insensitive);
                Match pendingDate = Regex.Match(RemoveReviewMarkdown(state), @"\b(?:completion date|completed on)\b[\s:=—-]*(?:(?:is|on|of)\s+)?\d",
                    RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
                pendingDate.Success.ShouldBeFalse("A pending checkpoint has no completion date.");
                row[1].ShouldContain(reviewer, Case.Sensitive);
                row[3].ShouldStartWith("pending", Case.Sensitive);
                row[4].ShouldStartWith("not complete", Case.Sensitive);
                row[5].ShouldBe("—", $"Pending {checkpoint} has no completion date.");
            }
        }

        string summary = NormalizeWhitespace(RemoveReviewMarkdown(evidence.GetSection("9.4 Current independent-review ownership")));
        summary.ShouldContain("waiver expired on 2026-10-26", Case.Insensitive);
        summary.ShouldContain("Administrator (jpiquot) accountable for scheduling and escalation", Case.Sensitive);
        summary.ShouldContain($"Independent reviewer {reviewer}", Case.Sensitive);
        var document = new MarkdownContractDocument(sources["runbook"]);
        document.GetTableHeader(DivergencesHeading).ShouldBe(["Divergence", "Owner", "Why it is open", "Reopen trigger"]);
        IReadOnlyList<string> divergence = document.GetTableRows(DivergencesHeading)
            .Single(static row => row[0] == "The security reviewer of record is not independent of the platform owner");
        divergence[1].ShouldContain("Administrator accountable for scheduling", Case.Sensitive);
        divergence[1].ShouldContain($"independent reviewer {reviewer}", Case.Sensitive);
        divergence[2].ShouldContain("waiver expired on 2026-10-26 and is historical only", Case.Sensitive);
        divergence[3].ShouldContain("independent reviewer", Case.Sensitive);
        string preamble = sources["runbook"].Split("\n## ", StringSplitOptions.None)[0];
        NormalizeWhitespace(preamble).ShouldContain($"Independent reviewer: **{reviewer}**", Case.Sensitive);
        if (completed.Count < 3)
        {
            status.ShouldBe("in-progress", "All mandatory checkpoints are required for parent completion.");
        }
        else if (status == "done")
        {
            ValidateDocumentationPrerequisites(story, today);
        }

        ValidateReviewReopenTrigger(record["Reopen trigger"], completed.Count < 3);

        if (perCheckpoint)
        {
            foreach (string checkpoint in new[] { "C4b", "C5b", "C7" })
            {
                string expected = completed.TryGetValue(checkpoint, out (string Signer, string Date) signature)
                    ? $"independently signed / complete by {signature.Signer} on {signature.Date}, completion date {signature.Date}"
                    : "pending / not complete with no completion date";
                IEnumerable<string> scopedSummaries = new[] { summary, divergence[2], epic, tracking, record["Consequence"] };
                if (checkpoint != "C7")
                {
                    scopedSummaries = scopedSummaries.Append(NormalizeWhitespace(document.GetSection(LimitationsHeading)));
                }

                foreach (string scopedSummary in scopedSummaries)
                {
                    MatchCollection statements = Regex.Matches(scopedSummary, @"\b" + checkpoint + @" is (?<state>[^;]+);",
                        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
                    statements.Count.ShouldBe(1, $"Each current summary needs one explicit {checkpoint} state.");
                    statements[0].Groups["state"].Value.ShouldBe(expected);
                    string remaining = scopedSummary.Remove(statements[0].Index, statements[0].Length);
                    Regex.IsMatch(remaining, @"\b" + checkpoint + @"\s+(?:is|remains|stays)\b",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5))
                        .ShouldBeFalse("Current summaries must not contain a second conflicting checkpoint statement.");
                    scopedSummary.ShouldNotContain("C4b/C5b/C7", Case.Sensitive, "Separate states must replace the obsolete all-gates summary.");
                }
            }

            if (completed.Count > 0)
            {
                assigned.ShouldBeTrue("A signed checkpoint needs a consistently recorded independent appointment.");
                evidence.GetSection(ReviewerEvaluationHeading).ShouldNotContain("No reviewer signature was obtained", Case.Sensitive);
            }

            string limitationState = completed.ContainsKey("C4b") && completed.ContainsKey("C5b")
                ? "countersignatures are complete" : "countersignatures remain pending";
            NormalizeWhitespace(document.GetSection(LimitationsHeading)).ShouldContain(limitationState, Case.Sensitive);
        }
        else if (wholeReviewSigned)
        {
            (string signer, string date) = completed["C7"];
            string closure = $"C4b/C5b/C7 are independently signed / complete by {signer} on {date}";
            summary.ShouldContain(closure, Case.Sensitive);
            summary.ShouldContain($"completion date {date}", Case.Insensitive);
            summary.ShouldNotContain("pending / not complete", Case.Insensitive);
            summary.ShouldNotContain("unassigned", Case.Insensitive);
            divergence[2].ShouldContain(closure, Case.Sensitive);
            divergence[2].ShouldNotContain("pending", Case.Insensitive);
            epic.ShouldContain(closure, Case.Sensitive);
            tracking.ShouldContain(closure, Case.Sensitive);
            tracking.ShouldNotContain("pending/not complete", Case.Insensitive);
            NormalizeWhitespace(document.GetSection(LimitationsHeading)).ShouldContain("countersignatures are complete", Case.Sensitive);
            evidence.GetSection(ReviewerEvaluationHeading).ShouldNotContain("No reviewer signature was obtained", Case.Sensitive);
        }
        else
        {
            summary.ShouldContain("C4b/C5b/C7 are pending / not complete", Case.Sensitive);
            summary.ShouldContain("no completion date", Case.Sensitive);
            summary.ShouldContain("No independent signed evidence exists", Case.Sensitive);
            divergence[2].ShouldContain("C4b, C5b and C7 are pending / not complete with no completion date", Case.Sensitive);
            epic.ShouldContain("C4b/C5b/C7 remain pending / not complete", Case.Sensitive);
            tracking.ShouldContain("C7 waiver expiry 2026-10-26 has fired", Case.Sensitive);
            tracking.ShouldContain("C4b/C5b/C7 are pending/not complete with no completion date", Case.Sensitive);
        }
    }

    private static (string Status, string Epic, string Tracking) ReadParentTracking(IReadOnlyDictionary<string, string> sources)
    {
        string epic = NormalizeWhitespace(new MarkdownContractDocument(sources["epic"])
            .GetSection("Story 31.1: OpenBao Platform Hardening and Documentation"));
        epic.ShouldContain("Helm reproduction is separate Platform Operations work", Case.Sensitive);
        MatchCollection statuses = Regex.Matches(sources["story"], @"^Status: (?<status>[^\r\n]+)$", RegexOptions.Multiline, TimeSpan.FromSeconds(5));
        statuses.Count.ShouldBe(1, "Story 31.1 needs one current status.");
        string status = statuses[0].Groups["status"].Value;
        status.ShouldBeOneOf("done", "in-progress");
        MatchCollection epicStatuses = Regex.Matches(epic, @"\*\*Status:\*\* (?<status>[\w-]+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        epicStatuses.Count.ShouldBe(1);
        epicStatuses[0].Groups["status"].Value.ShouldBe(status);
        string sprint = sources["sprint"];
        MatchCollection declarations = Regex.Matches(sprint,
            @"^[ \t]*31-1-openbao-platform-hardening-and-documentation:[ \t]*(?<status>[^\r\n]*)$",
            RegexOptions.Multiline, TimeSpan.FromSeconds(5));
        declarations.Count.ShouldBe(1, "Story 31.1 needs one status declaration, including opposite-value duplicates.");
        declarations[0].Groups["status"].Value.Trim().ShouldBe(status);
        int trackingStart = sprint.IndexOf("# Story 31.1 renamed", StringComparison.Ordinal);
        trackingStart.ShouldBeGreaterThanOrEqualTo(0);
        int trackingEnd = sprint.IndexOf("\n  31-2-", trackingStart, StringComparison.Ordinal);
        trackingEnd.ShouldBeGreaterThan(trackingStart);
        string tracking = sprint[trackingStart..trackingEnd];
        return (status, epic, tracking);
    }

    private static void ValidateDocumentationPrerequisites(MarkdownContractDocument story, DateOnly today)
    {
        foreach (string checkpoint in new[] { "C1", "C2", "C3", "C4a", "C5a", "C6" })
        {
            IReadOnlyList<string> row = ReadSecurityCheckpoint(story, checkpoint);
            Regex.IsMatch(RemoveReviewMarkdown(row[4]), @"^complete(?: — \S.*)?$",
                RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5)).ShouldBeTrue("Parent completion requires an unconditional complete state.");
            Regex.IsMatch(RemoveReviewMarkdown(row[3]), @"^verified(?: — \S.*)?$",
                RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5)).ShouldBeTrue("Parent completion requires a verified prerequisite review.");
            ShouldBeSubstantiveCell(row[2], checkpoint, CheckpointsHeading);
            DateOnly.TryParseExact(row[5], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                .ShouldBeTrue("A completed checkpoint needs a valid completion date.");
            date.ShouldBeLessThanOrEqualTo(today);
        }
    }

    /// <summary>Requires the exact checkpoint row, never another section's vocabulary.</summary>
    private static IReadOnlyList<string> ReadSecurityCheckpoint(MarkdownContractDocument story, string checkpoint)
    {
        story.GetTableHeader(CheckpointsHeading).ShouldBe(["Checkpoint", "Accountable owner", "Required evidence artifact and command", "Review state", "Completion state", "Completion date"]);
        IReadOnlyList<string>[] rows = story.GetTableRows(CheckpointsHeading)
            .Where(row => row[0].StartsWith($"{checkpoint} -", StringComparison.Ordinal)).ToArray();
        rows.Length.ShouldBe(1);
        return rows[0];
    }

    /// <summary>Checks the signature's own authority, real calendar date and current profile scope.</summary>
    private static (string Signer, string Date) ValidateSignature(string evaluation, string checkpoint, string scope, DateOnly today)
    {
        Match signature = Regex.Match(evaluation,
            @"^Signed by (?<signer>[A-Za-z0-9_.@-]+) on (?<date>20\d{2}-\d{2}-\d{2})\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        signature.Success.ShouldBeTrue("A completed checkpoint needs its own named, dated signature.");
        string signer = signature.Groups["signer"].Value;
        string dateText = signature.Groups["date"].Value;
        ValidateIndependentIdentity(signer);
        DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            .ShouldBeTrue("A signature needs a valid ISO calendar date.");
        date.ShouldBeGreaterThanOrEqualTo(new DateOnly(2026, 11, 16));
        date.ShouldBeLessThanOrEqualTo(today);
        string clause = checkpoint == "C4b/C5b/C7"
            ? "covers current measured profile and C4b/C5b/C7; independently evaluated both accepted limitations and the amended AC2 qualifier"
            : $"covers {checkpoint} current measured profile and {scope}";
        evaluation.ShouldStartWith($"Signed by {signer} on {dateText}; {clause}", Case.Insensitive);
        evaluation.ShouldNotMatch(@"(?i)\b(?:(?:does|do)\s+not\s+cover|doesn't\s+cover|not\s+covered)\b",
            "A signature cannot explicitly deny its claimed evaluation scope.");
        return (signer, dateText);
    }

    /// <summary>Retains the rejected owner and owner-persona identities for every appointment/signature.</summary>
    private static void ValidateIndependentIdentity(string signer)
    {
        Regex.IsMatch(signer, @"^[A-Za-z0-9_.@-]+$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5)).ShouldBeTrue();
        // Known owner/persona accounts remain non-independent when written as email identities.
        signer.Split('@')[0].ToLowerInvariant().ShouldNotBeOneOf("jpiquot", "murat-tea-for-jpiquot", "administrator", "unassigned");
    }

    /// <summary>Binds substantive independence evidence to the authority that actually signed.</summary>
    private static void ValidateIndependence(IReadOnlyDictionary<string, string> record, string field, string signer)
    {
        record.ContainsKey(field).ShouldBeTrue($"Missing {field}.");
        string prefix = $"Reviewer {signer} is independent of Platform Operations owner jpiquot and waiver approver jpiquot; evidence:";
        record[field].ShouldStartWith(prefix, Case.Sensitive);
        string basis = record[field][prefix.Length..];
        ShouldBeSubstantiveCell(basis, field, ReviewerEvaluationHeading);
        RequireSettledReviewEvidence(basis);
        basis.ShouldNotMatch(@"(?i)\b(?:reviewer|evaluator)\s+is\s+(?:(?:the|an?)\s+)?(?:(?:platform|implementation|operations)\s+)?owner\b|\bnot independent\b",
            "The independence basis must not identify the reviewer as the owner or deny independence.");
    }

    /// <summary>Requires a signer-bound verified source and verification method, distinct from appointment.</summary>
    private static void ValidateProvenance(IReadOnlyDictionary<string, string> record, string field, string signer)
    {
        record.ContainsKey(field).ShouldBeTrue($"Missing {field}.");
        string prefix = $"Verified signature by {signer}; source: ";
        record[field].ShouldStartWith(prefix, Case.Sensitive);
        string[] provenance = record[field][prefix.Length..].Split("; method: ", StringSplitOptions.None);
        provenance.Length.ShouldBe(2, "Verified approval needs both durable source and verification method.");
        foreach (string part in provenance)
        {
            ShouldBeSubstantiveCell(part, field, ReviewerEvaluationHeading);
            RequireSettledReviewEvidence(part);
        }
    }

    /// <summary>Removes the actual bold/code wrappers without rewriting allowed identifier characters.</summary>
    private static string RemoveReviewMarkdown(string text)
        => text.Replace("**", string.Empty, StringComparison.Ordinal).Replace("`", string.Empty, StringComparison.Ordinal);

    /// <summary>Rejects evidence that explicitly says verification or its source is still outstanding.</summary>
    private static void RequireSettledReviewEvidence(string evidence)
        => evidence.ShouldNotMatch(@"(?i)\bnot\s+(?:yet\s+)?(?:independently\s+)?(?:available|verified|supplied|received|established|confirmed)\b|\b(?:source|reference|signature|approval|verification|independence|appointment|confirmation|evidence)(?:\s+(?:is|remains|still))?\s+(?:pending|unverified|unresolved|awaiting|outstanding|TBD|TODO)\b|\b(?:pending|unverified|unresolved)\s+(?:source|reference|signature|approval|verification|independence|appointment|confirmation|evidence)\b",
            "Completed review evidence cannot explicitly leave its source, verification or independence unresolved.");

    /// <summary>Retains expired-waiver obligations and reassessment on measured-profile changes.</summary>
    private static void ValidateReviewReopenTrigger(string trigger, bool hasPendingCheckpoint)
    {
        string current = NormalizeWhitespace(RemoveReviewMarkdown(trigger));
        current.ShouldNotMatch(@"(?i)\bwaiver\s+(?:extended|renewed)\b|\bno\s+further\s+review\s+needed\b",
            "No review state renews the historical waiver or removes reassessment.");
        if (hasPendingCheckpoint)
        {
            current.ShouldContain("2026-10-26 already fired", Case.Sensitive);
            current.ShouldMatch(@"(?i)\bobtain\b[^.;]*\bindependent\b[^.;]*\bevaluation\b");
        }

        current.ShouldContain("reassess on seal/ingress/voter changes or Story 31.2 activation", Case.Insensitive);
    }
}
