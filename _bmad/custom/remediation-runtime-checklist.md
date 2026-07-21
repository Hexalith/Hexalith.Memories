# Remediation Runtime Defect Checklist

## Authority

Epic 21 review found operationally significant workflow and runtime defects that
happy-path tests did not expose: missing Dapr workflow activity registration,
unobserved child workflows, owner-check race gaps, duplicate-detector false
positives, migration inventory miscounting, missing tenant index repair,
rollback marker overwrite, and staging index cleanup gaps. This checklist turns
those findings into durable pre-development and pre-close review items so future
remediation-track work re-catches the same defect classes by construction rather
than by luck. It is the enforcement artifact for Epic 21 retrospective action
item 5 and the team agreement that runtime-dispatch and rollback review findings
become future checklist items. Enforcement is LLM-obeyed prose loaded through the
committed customizations — the same mechanism as the sibling `story-scope-guard.md`
and `story-phase-ledger.md` policies, not an automated gate.

Current PRD, epics, architecture, and `project-context.md` remain authoritative
for behavior. This checklist adds coverage obligations; it never relaxes an
existing guard.

## Applicability

This checklist is self-scoping. It applies to **any** story, spec, refactor, fix,
or review — remediation-track work (Epics 20+ audit remediation) is the primary
case, but the obligation is not limited to it — whose changes **touch** any
runtime surface below. "Touch" means a behavioral change to dispatch,
registration, cleanup, dedup, or rollback; a comment-only, formatting-only, or
test-only edit to such a file does not by itself trigger the checklist.

Runtime surfaces:

- Dapr workflow orchestration, child-workflow invocation, or workflow/activity
  registration.
- Cleanup, deletion, compensation, or dedup of shared or tenant-scoped state.
- Migration, blue/green cutover, rollback, abort, or staging keys, indexes, or
  aliases.

A change that touches none of these records an explicit note — for example
`remediation runtime checklist: not applicable — no workflow/runtime surface touched` — and fabricates no category item. State applicability explicitly; do
not leave it implied.

## Categories

Each applicable category needs explicit story/spec coverage and, at close-out,
either passing focused evidence or an accepted blocker with owner, consequence,
and reopen trigger. Where a category names several distinct failure modes, cover
each one the change actually touches — a single test does not discharge unrelated
sub-defects. **Category 5 is the exception:** it is satisfied by the story phase
ledger's File List reconciliation, not by a separate acceptance item or test; do
not create a duplicate File-List item. Categories map to the Epic 21 defect they
guard against.

1. **Dapr workflow activity registration.** Every activity a workflow or child
   workflow invokes is registered with the Dapr workflow runtime/DI, and a test
   exercises the dispatch path. Guards against missing activity registration
   that fails only at runtime (Story 21.5).
2. **Observed child workflows.** Every child workflow that is started is awaited
   and its result checked; no fire-and-forget orchestration. Guards against
   unobserved child workflows whose failures were invisible (Story 21.7).
3. **Owner-checked cleanup and dedup.** Cleanup, deletion, compensation, and
   dedup paths verify ownership (owner lock, ETag/CAS, or first-writer-wins)
   before mutating or removing shared or tenant-scoped state, stay idempotent
   under duplicate and late events, and repair — not miscount — inventory and
   tenant indexes. Cover each of these sub-defects the change touches:
   owner-check races, duplicate-detector false positives, migration inventory
   miscounting, and missing tenant index repair (Stories 21.5, 21.7). For
   tenant-scoped state this overlaps the `project-context.md` rule *"Tenant
   isolation requires attached negative evidence"*; satisfy that rule's
   negative-evidence obligation once and cite it here rather than producing a
   second, divergent record.
4. **Rollback marker and staging-artifact preservation.** Migration and rollback
   markers are preserved, never overwritten, across retry, abort, and rollback;
   staging keys, indexes, and aliases are cleaned up on rollback and abort.
   Guards against rollback marker overwrite and staging index cleanup gaps
   (Story 21.9).
5. **File List reconciliation.** Every workflow, activity, registration,
   migration, and cleanup file the change touches is reconciled into the story
   File List. Satisfied by `story-phase-ledger.md` Cumulative File List
   Reconciliation (`matched N/N`); it is not re-specified or separately tested
   here.

## Creation gate

- Classify applicability first, from the actual change surfaces. For each
  applicable category other than Category 5, add one explicit acceptance or task
  item that names the defect it guards and requires adversarial or negative
  coverage (for example: unregistered-activity dispatch failure, unobserved
  child-workflow failure surfacing, cross-owner cleanup denial,
  rollback-marker-preserved-after-abort).
- A non-applicable change records the not-applicable note instead.
- Fail closed: do not set `ready-for-dev` and do not mutate sprint status while
  an applicable category has neither coverage nor the not-applicable note.

## Review gate

- Independently re-derive applicability from the diff. Reject a not-applicable
  note — or an accepted blocker — that the touched surfaces contradict; do not
  accept the author's self-classification unchallenged.
- Confirm each applicable category (except Category 5, which the phase ledger
  covers) is backed by an executed, passing test or a re-validated accepted
  blocker with owner, consequence, and reopen trigger; happy-path or build-only
  evidence does not satisfy a category.
- Any workflow/runtime dispatch, cleanup, or rollback defect the review newly
  finds becomes a checklist item on the story under review; a genuinely new
  defect *class* is added as a category to this policy file (and the fixture
  updated) so future stories inherit it.
- Route an unambiguous missing item or coverage gap to `patch`; route ambiguous
  scope or ownership to `decision_needed`.
- Fail closed: an applicable category left unproven blocks `done`.
