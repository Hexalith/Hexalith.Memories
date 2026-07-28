# Epic AC Verification Policy

## Authority

Current source, current tests, and current configuration are the authority on
what the codebase contains. Epic, PRD, architecture, and audit acceptance text
is planning intent recorded at a point in time; its counts, existence claims,
and behavioral descriptions are **advisory until re-derived**. Where the two
disagree, the code wins and the planning artifact is corrected.

Epic 25 found three epic acceptance claims false against the code: the
"60 server literals" count (Story 25.3), "the CLI has no `Client.Rest`
reference" (Story 25.5), and the "redundant double authorization" shorthand for
the four MCP tools (Story 25.6), which was one authorization decision plus a
redundant accessor read/failure seam. Story 25.3 also refuted the premise that
`TraverseAsync` is experimental, turning a planned reorder into a real breaking
change. Every one of those was caught by a developer who refused the premise
mid-implementation, not by the process; Epic 26 repeated the pattern. This
policy moves the check to story creation so a corrected premise never reaches a
developer. It is the enforcement artifact for Epic 25 retrospective action item
2 and the Epic 26 follow-through row of the same number.

Enforcement is LLM-obeyed prose loaded through the committed customizations —
the same mechanism as the sibling `story-scope-guard.md`,
`story-phase-ledger.md`, and `remediation-runtime-checklist.md` policies, not an
automated gate. This policy adds verification obligations; it never relaxes an
existing guard, and it never authorizes widening story scope.

## Applicability

This policy applies to **every** story, spec, or remediation slice created from
epic, PRD, architecture, or audit-anchor text — regardless of epic number. It is
not limited to remediation epics: Epic 25 was a remediation epic, and the
`epics.md` audit-anchor preflight that already covered it did not catch the
drift.

A claim is **verifiable** when a command, file read, or symbol lookup against
the current worktree can confirm or refute it. Four claim classes are always
verifiable and must always be checked:

1. **Quantitative** — counts, sizes, occurrence totals, durations, line counts
   ("60 server literals", "22 copy-pasted decode blocks", "a 60-line skeleton").
2. **Existence and absence** — "X exists", "there is no `Client.Rest`
   reference", "the deferral comment at `Server/Program.cs:3122`".
3. **Behavioral** — "the four tools double-authorize", "`TraverseAsync` is
   experimental", "the production overlay scales this to zero".
4. **Location** — file paths, type and member names, line anchors, config keys.

A claim about future intent, business value, or design preference is **not**
verifiable; record it as intent and do not manufacture evidence for it.

## Canonical story section

Every governed story must contain this section under `## Dev Notes`. The
obligation attaches when the story is **authored or registered** — the moment it
is written into a story file, `epics.md`, or `sprint-status.yaml` — at any
status, including `backlog`. `ready-for-dev` is a second, stricter checkpoint,
not the first one. A story registered with an unverified claim is in violation
while it sits in the backlog; it does not become compliant by not being selected
yet.

```markdown
### Epic AC Verification

Verified <date> against <commit-or-branch>.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
```

One row per verifiable claim in the story's acceptance criteria and in the epic
text the story inherits. Quote the claim; do not paraphrase it into something
easier to confirm.

`Verdict` is exactly one of:

- `confirmed` — the observed result matches the claim.
- `corrected` — the observed result refutes the claim. The row records the
  observed truth, and the story's own acceptance criteria are written from that
  observed truth, never from the refuted claim.
- `unverifiable` — no command, read, or lookup available in this environment can
  settle it. Record the exact blocker, owner, consequence, and reopen trigger,
  the same way `story-phase-ledger.md` records blocked discovery. An
  `unverifiable` claim may not be used as load-bearing justification for scope.

A story whose inherited text contains no verifiable claim records the explicit
note `epic AC verification: no verifiable claim in inherited epic text` rather
than omitting the section. State applicability explicitly; do not leave it
implied.

## Correcting the planning artifact

A `corrected` verdict is not discharged by fixing the story alone. That is the
exact failure Epic 25 recorded: three stories each corrected a premise locally
and the epic text stayed wrong for the next reader.

- Apply the correction to the source planning artifact — `epics.md`, the PRD
  section, or the audit-anchor line — in the same change, or record a dated
  correction note at the claim with the observed value and the story that
  measured it.
- When a correction changes the story's scope, an epic's intent, or a ratified
  decision — Story 25.3's `TraverseAsync` reorder is the exemplar — do not
  absorb it. Escalate for a human decision and record that decision before
  `ready-for-dev`.
- Never delete or weaken an acceptance criterion to make it match the code when
  the criterion states a desired end state the code has not reached yet. That is
  a `confirmed` gap in the implementation, not a `corrected` claim.

## Creation gate

This gate binds every route that authors or registers a story or an epic
acceptance claim — story creation, correct-course, epic-and-story generation,
spec authoring, and sprint planning — not only the story-creation route. The
route that writes a claim owns verifying it; a claim written by one route and
verified by a later one has already reached a reader as fact.

- Verify before drafting acceptance criteria, not after. The verified result is
  the input to the story text; a story argued backwards from a false claim is
  the failure this policy exists to prevent.
- Record every row with a command another agent can re-run. A `grep -rn`, a
  test-runner discovery command, or a file read with an explicit path and line
  range all qualify; "reviewed the code" does not.
- Fail closed: do not write a verifiable claim into a story file, `epics.md`, or
  `sprint-status.yaml` at any status, do not set `ready-for-dev`, and do not
  mutate sprint status while any verifiable claim lacks a verdict, any
  `corrected` claim lacks its planning-artifact correction or recorded
  escalation, or any `unverifiable` claim lacks its blocker record.

## Development gate

- Read the creation-time table first, and re-derive any verdict the
  implementation contradicts.
- A claim marked `confirmed` at creation that development proves false is a
  defect in the creation-time verification, not a routine discovery: append the
  corrected row, correct the planning artifact, and say so in the completion
  notes so the next retrospective can see it.
- A story that opens by correcting its own premise mid-implementation has
  already failed this policy. Record it rather than absorbing it silently.
- For a story created before this policy, append a dated adoption row covering
  the claims the story is built on instead of reconstructing history.
- Fail closed before setting `review` when a contradicted verdict is left
  standing.

## Review gate

- Independently re-run the commands behind every `corrected` row and every row
  the reviewed diff touches. Do not accept the author's verdict unchallenged.
- Reject a `confirmed` verdict the diff contradicts, an `unverifiable` record
  whose blocker the environment does not actually impose, and a row that
  paraphrases the epic claim into a weaker one that is easier to confirm.
- Confirm each `corrected` row carries its planning-artifact correction or its
  recorded human decision.
- Route an unambiguous missing row, stale verdict, or uncorrected planning
  artifact to `patch`; route a correction that changes scope or epic intent to
  `decision_needed`.
- Fail closed: an unverified or contradicted claim blocks `done`.
