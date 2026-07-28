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

This gate binds when a story is **authored or registered** — the moment it is
written into a story file, `epics.md`, or `sprint-status.yaml` — at any status,
including `backlog`. `ready-for-dev` is a second, stricter checkpoint, not the
first one. A story that violates this policy is in violation while it sits in
the backlog; it does not become compliant by not being selected yet.

- Select work from current epic intent and current code evidence, not numeric
  story adjacency.
- Do not copy an anti-template's tasks, AC density, file list, or proof shape.
- Split multiple independently demonstrable outcomes into newly numbered
  stories before the story is registered at any status.
- A correction, split, or replan that creates stories must satisfy this policy
  for every story it creates. A split must not reproduce the shape it was
  executed to cure.
- An explicitly approved umbrella/checkpoint story may remain one tracking
  story only when every checkpoint has its own owner, evidence command/artifact,
  review state, and completion state.
- Add `Historical Context Classification` and `Slice Proof` sections to the
  generated story whenever any prior story influences it.
- Treat an unresolved violation as a Critical Miss: do not set
  `ready-for-dev` and do not update sprint status.

## Executable subset

`tools/check-story-slice-scope.py` enforces the mechanically checkable subset of
this policy: that the classification and slice-proof record exists, that every
classification row carries exactly one of the three labels, that an
`anti-template` row states its permitted use, and that a story enumerating more
than five independently verifiable gates carries one checkpoint row per gate
with owner, evidence command or artifact, review state, and completion state.

The gate does not judge whether a label is **correct**, whether a reuse is
genuinely narrow, or whether two outcomes are genuinely independently
deployable. Those judgements stay with the creation and review gates below. A
green gate is evidence the record exists, never evidence the record is right.

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
