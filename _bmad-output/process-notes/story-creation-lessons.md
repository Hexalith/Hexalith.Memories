# Story Creation Lessons

This ledger was bootstrapped automatically by `jobs/preflight-predev-hardening.py`
because this repository had no existing story-creation lessons file.

Use this file to record durable lessons for recurring BMAD story creation,
party-mode review, advanced elicitation, and code-review automation.

## L08 - Party Review vs. Elicitation

- Party-mode review is the cross-role critique and triage pass before
  development; it should produce dated trace evidence when completed.
- Advanced elicitation is a separate hardening pass after a completed
  party-mode trace exists; a recommendation to run elicitation is not itself
  completed elicitation evidence.

## L09 - Historical Broad Slices Are Anti-Templates

- Completed broad technical slices may be used only as historical context or
  dependency evidence. They must not be copied as the shape for new stories.
- When epics mark a story as historical completed scope, any reopened,
  reimplemented, or analogous work must be split into newly numbered vertical
  stories with independently observable evidence.
- Story creation must classify every previous or historical story reference as
  current narrow pattern, historical reference only, or anti-template before
  carrying anything forward. Numeric adjacency is not evidence of relevance.
- Reusable implementation details must be re-verified against current source;
  a historical story's task structure, acceptance-criteria breadth, file list,
  or completion evidence is never reusable by default.
- Review must flag a story or implementation that reuses a historical broad
  slice as a template, hides independently demonstrable outcomes behind one
  story, or accepts internal-only proof where observable evidence is required.
- Enforce this rule through `_bmad/custom/story-scope-guard.md` and committed
  `_bmad/custom/bmad-{create-story,code-review}.toml` overrides. Do not enforce
  it by editing generated `.agents/skills/**` files, which updates overwrite.
- After any BMad skill refresh, run the customization-resolution fixture before
  the next story creation or review.
- The policy binds at authoring and registration, at any status. A story does
  not become compliant by staying in the backlog.
- Every route that authors or registers stories must load the policy —
  create-story, correct-course, create-epics-and-stories, spec, and
  sprint-planning. A correction that splits a story is a story-authoring route
  and is bound by the policy for every story it creates.
- `DW 27.3-CR16` (2026-07-27) is the reference failure: the 2026-07-26 split
  produced two anti-templates because the splitting route did not load the
  policy and the gate did not bind at `backlog`. Its own proposal is titled
  "the split reproduced the shape it was executed to cure".
- Configuration coverage is not execution proof.
  `tools/check-story-slice-scope.py` checks that the required record exists and
  is well formed; it does not judge classification correctness or outcome
  independence. A green gate is never evidence the record is right.
- The gate is calibrated against live stories, and calibration found real false
  positives: gate identifiers named in prose because they were *transferred
  away* (Story 27.3), decision and discovery tables whose rows begin with a gate
  identifier, and legitimately qualified classifications such as
  `current-narrow-pattern (whole-story shape is anti-template)`. Re-run
  `tests/tooling/story_slice_scope` after any parser change.

## L10 - Story Phase Records Are Cumulative Handoff Contracts

- `create-story` owns the runner-derived baseline and initial cumulative File
  List; `dev-story` records the actual implementation delta against it.
- Story-bound `qa-gap-closure` recalculates cumulative counts and File List
  membership from the development handoff instead of appending stale prose.
- `code-review` independently verifies live counts, same-unit arithmetic, and
  cumulative File List completeness after any selected review patches.
- Each phase appends its own canonical Change Log row and stops status
  advancement when required evidence, counts, or in-scope paths disagree.
- Keep enforcement update-safe in committed `_bmad/custom` policies and
  resolver fixtures. Generated `.agents/skills/**` files remain untouched.

## L11 - Review-Found Workflow/Runtime Defects Become Remediation Checklist Items

- Epic 21 reviews caught workflow and runtime defects that happy-path tests
  missed: missing Dapr activity registration, unobserved child workflows,
  owner-check race gaps, duplicate-detector false positives, rollback marker
  overwrite, and staging index cleanup gaps. Those defect classes are now a
  durable pre-development and pre-close checklist, not tribal memory in a closed
  retrospective (Epic 21 retro action item 5).
- `_bmad/custom/remediation-runtime-checklist.md` defines five self-scoping
  categories — Dapr activity registration, observed child workflows,
  owner-checked cleanup/dedup, rollback-marker/staging preservation, and File
  List reconciliation (deferred to `story-phase-ledger.md`, not duplicated). It
  applies only to changes touching those runtime surfaces; a non-touching change
  records an explicit not-applicable note and fabricates no category item.
- Enforce it through committed
  `_bmad/custom/bmad-{create-story,dev-story,code-review}.toml` overrides that
  load the policy fact and one `REMEDIATION_RUNTIME_CHECKLIST:` directive each.
  Do not enforce it by editing generated `.agents/skills/**` or
  `.claude/skills/**` files; those updates overwrite.
- A full review that newly finds a workflow/runtime dispatch, cleanup, or
  rollback defect adds it to the reviewed story and carries it into this
  checklist for future remediation stories.
- After any BMad skill refresh, run the customization-resolution fixture
  (`tests/tooling/bmad_customization/bmad_customization_test.py`) before the next
  story creation or review.

## L12 - Epic Acceptance Claims Are Advisory Until Re-Derived

- Epic 25 shipped three acceptance claims that were false against the code: the
  "60 server literals" count (25.3), "the CLI has no `Client.Rest` reference"
  (25.5), and the "double authorization" shorthand for the four MCP tools
  (25.6), which was one authorization decision plus a redundant accessor seam.
  25.3 also refuted the "TraverseAsync is experimental" premise, which turned a
  planned reorder into a real breaking change needing a maintainer decision.
- All four were caught by developers who refused the premise mid-implementation,
  never by the process. Epic 26 repeated the pattern. Catching drift during
  development is luck; the check belongs at story creation, before the
  acceptance criteria are drafted.
- Current source, tests, and configuration are authoritative. Epic, PRD,
  architecture, and audit text is planning intent recorded at a point in time
  and is advisory until re-derived. Where they disagree, the code wins.
- `_bmad/custom/epic-ac-verification.md` defines four always-verifiable claim
  classes (quantitative, existence/absence, behavioral, location), a canonical
  `### Epic AC Verification` table under `## Dev Notes`, and three verdicts:
  `confirmed`, `corrected`, `unverifiable`. Every row quotes the claim and
  carries a command another agent can re-run; "reviewed the code" is not
  evidence, and a paraphrase that is easier to confirm is not the claim.
- A `corrected` verdict is not discharged by fixing the story alone. That is
  precisely what Epic 25 did three times, leaving `epics.md` wrong for the next
  reader. Correct the source planning artifact or leave a dated correction note
  there; escalate rather than absorb a correction that changes scope, epic
  intent, or a ratified decision.
- Never weaken an acceptance criterion to make it match the code when the
  criterion states an end state the code has not reached. That is a `confirmed`
  implementation gap, not a `corrected` claim.
- Prose in a planning document is not a gate. The `epics.md` audit-anchor
  preflight already covered Epic 20-26 and did not fire, because story creation
  reads `epics.md` for content, not as an activation-time obligation. The
  preflight is now epic-number-independent and points at the policy, but the
  enforcement lives in committed
  `_bmad/custom/*.toml` overrides that load the policy fact and one
  `EPIC_AC_VERIFICATION:` directive each, on all seven story-authoring
  routes (2026-07-28). Do not
  enforce it by editing generated `.agents/skills/**` or `.claude/skills/**`
  files; those updates overwrite.
- Enforcement is LLM-obeyed prose. The fixture proves the wiring resolves on
  both surfaces, not that a given story actually verified its claims. An
  executable pre-review gate (Epic 26 action item 7) remains the durable answer
  and stays open.
- After any BMad skill refresh, run the customization-resolution fixture
  (`tests/tooling/bmad_customization/bmad_customization_test.py`) before the next
  story creation or review.
- The policy binds at authoring and registration, at any status. A story
  registered with an unverified claim is in violation while it sits in the
  backlog; it does not become compliant by not being selected yet.
- Every route that authors or registers a story or an epic acceptance claim must
  load the policy — create-story, dev-story, code-review, correct-course,
  create-epics-and-stories, spec, and sprint-planning. The route that writes a
  claim owns verifying it; a claim written by one route and verified by a later
  one has already reached a reader as fact.
- `bmad-create-epics-and-stories` is the origin route: the three Epic 25 false
  claims were written there. A guard that skips it catches false claims one or
  more hops downstream of where they enter.
- A process correction is itself bound by the guards it extends. This guard was
  created on 2026-07-28 with the same route-coverage and binding-point defect
  that a sibling correction (`story-scope-guard.md`) had cured hours earlier.
  The recurrence was found by cross-reading the two proposals, not by either
  one's own review. When adding a guard, check the known defect shapes of its
  siblings before wiring it.
