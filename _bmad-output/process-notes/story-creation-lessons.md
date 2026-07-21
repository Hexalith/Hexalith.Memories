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
