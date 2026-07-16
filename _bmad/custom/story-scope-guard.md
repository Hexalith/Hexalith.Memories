# Historical Slice Story Scope Guard

## Authority

Current PRD, epics, architecture, approved sprint changes, current source, and
current tests define present scope. Historical story artifacts are evidence of
what happened; they do not define the shape of new work.

## Mandatory classification

Before reusing any previous or historical story reference, classify it as:

1. `current-narrow-pattern` — only a focused implementation/test pattern that
   has been re-verified against current source; whole-story shape is not reused.
2. `historical-reference-only` — dependency, decision, or evidence context.
3. `anti-template` — broad, bundled, umbrella, checkpoint-heavy, superseded,
   alias-only, reserved, or explicitly guarded scope that must not shape a new
   story.

Any artifact containing `Historical Scope Guard`, `historical broad`,
`bundled infrastructure`, `not valid patterns for future story creation`,
`must split`, `do not reopen`, or equivalent language is an anti-template
unless current epics explicitly approve a narrower use.

## Creation gate

- Select work from current epic intent and current code evidence, not numeric
  story adjacency.
- Do not copy an anti-template's tasks, AC density, file list, or proof shape.
- Split multiple independently demonstrable outcomes into newly numbered
  stories before setting `ready-for-dev`.
- An explicitly approved umbrella/checkpoint story may remain one tracking
  story only when every checkpoint has its own owner, evidence command/artifact,
  review state, and completion state.
- Add `Historical Context Classification` and `Slice Proof` sections to the
  generated story whenever any prior story influences it.
- Treat an unresolved violation as a Critical Miss: do not set
  `ready-for-dev` and do not update sprint status.

## Review gate

- In full review, inspect both the story specification and implementation diff.
- Confirm the implementation stays within one approved slice or independently
  proves every explicitly approved checkpoint.
- Confirm externally observable proof is present wherever current artifacts
  require API, CLI, contract, trace, integration, or downstream-consumer proof.
- Rate confirmed anti-template reuse or hidden multi-slice scope as `high`.
- Route to `decision_needed` when the correct split requires a human scope
  choice; otherwise route an unambiguous correction to `patch`.
- Never dismiss a confirmed violation as editorial or historical noise.
