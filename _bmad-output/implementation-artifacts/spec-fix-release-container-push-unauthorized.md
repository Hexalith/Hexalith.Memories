---
title: 'Fix release container push: replace docker-daemon push with skopeo'
type: 'bugfix'
created: '2026-07-15'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'a9155110d534a24c9c782293314f8aaa50a484fe'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Every release since v2.6.5 fails container publication with `unauthorized: authentication required` on `docker push`, even though the in-job preflight proves the `HEXALITH_ZOT_*` credentials have push authorization. Root cause: zot now allows anonymous read, so it answers the docker daemon's `GET /v2/` ping with 200; docker caches "no auth needed" and never sends credentials on push (known bug project-zot/zot#2928). Each release ends partial-publish (NuGet + tag exist, images missing), and the recovery workflow fails identically.

**Approach:** In `tools/publish-containers.ps1 -Push`, publish the prebuilt archives with `skopeo` (preinstalled on ubuntu-24.04 runners; its containers/image library negotiates per-request auth challenges correctly), authenticating via a temporary containers-auth.json built from the same env values the preflight validates. Preserve outcome semantics, summary schema, and secret redaction.

## Boundaries & Constraints

**Always:**
- Keep the non-`-Push` build path credential-free and unchanged.
- Preserve publish-summary.json schema, dispositions, and config-digest reconciliation (match → `already-present`; conflict → `digest-conflict` fail-closed; absent → push).
- Credentials only via a chmod-600 temp authfile deleted in `finally` — never argv, never logged; keep `Protect-LogText` redaction everywhere.
- Keep the `authorization-failed` classification with its actionable message.
- Keep equivalent python fixture coverage for every push-path scenario.

**Ask First:**
- Workflow file changes beyond what the script strictly requires.
- Any semantic change to `verify-container-registry.ps1` (proven correct; stays).

**Never:**
- No server-side (zot/ingress) fixes from this repo; no reintroducing `docker login`/`docker push`; no weakening fail-closed behavior; no touching NuGet publish, preflight, or semantic-release config.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Happy push | Valid archives + creds, tags absent | Both members `pushed`, summary `succeeded` | N/A |
| Tag exists, same config digest | Remote digest == archive digest | `already-present`, no copy | N/A |
| Tag exists, different digest | Digest mismatch | `digest-conflict` | Fail closed, no push |
| Auth rejected on copy | stderr matches unauthorized/auth required/access denied | `authorization-failed` + actionable message | Summary `publish-failed`, exit 1 |
| Missing creds env | Username or API key unset | All `not-attempted`, throw before any skopeo call | Exit 1 |
| skopeo missing | Not on PATH in `-Push` mode | All `not-attempted`, actionable error before push | Exit 1 |
| Remote inspect fails | `skopeo inspect` nonzero (e.g. manifest unknown) | Proceed to push; keep inspect stderr as failure context (parity with today) | Push outcome decides |
| Archive missing/empty | No archive file | `archive-missing` (unchanged) | Fail closed |

</frozen-after-approval>

## Code Map

- `tools/publish-containers.ps1` -- rework `Connect-ContainerRegistry` (docker login, l.123-163), `Publish-ContainerArchive` (load/tag/inspect/manifest/push, l.268-357), `$Push` block (l.385-397)
- `tools/verify-container-registry.ps1` -- reference for authfile base64 construction; do not change
- `tests/tooling/publish_containers/publish_containers_test.py` -- `write_fake_docker` shim + ~20 push tests to convert to skopeo
- `tests/tooling/publish_containers/release_orchestration_test.py`, `partial_release_completion_test.py` -- adjust only if docker-coupled
- `docs/dev/release-runbook.md` -- auth-mechanism + troubleshooting text to update
- `.github/workflows/release.yml`, `recover-partial-release.yml` -- both call the script; no change expected

## Tasks & Acceptance

**Execution:**
- [x] `tools/publish-containers.ps1` -- In `-Push` mode: check skopeo availability; write temp authfile `{"auths":{"<registry>":{"auth":"<base64 user:key>"}}}`; get archive config digest via `skopeo inspect --raw docker-archive:<archive>`; remote-inspect via `skopeo inspect --raw --authfile <f> docker://<ref>` + digest reconciliation; push via `skopeo copy --authfile <f> docker-archive:<archive> docker://<ref>`; remove docker login/load/tag/local-inspect; keep outcome/summary/redaction contracts
- [x] `tests/tooling/publish_containers/publish_containers_test.py` -- replace fake docker with plan/state-driven fake skopeo; port push-path tests (call order, authfile not argv, digest reconciliation, auth-failure classification, redaction, missing-creds/missing-skopeo); cover matrix rows
- [x] `tests/tooling/publish_containers/release_orchestration_test.py` + `partial_release_completion_test.py` -- run; fix any docker-coupled fixtures
- [x] `docs/dev/release-runbook.md` -- document skopeo authfile push and why daemon push is forbidden against this registry (zot#2928)

**Acceptance Criteria:**
- Given valid creds and absent remote tags, when the script runs `-Push` against fixtures, then both members are `pushed` and summary `succeeded`.
- Given fixture auth rejection on copy, when publishing, then `authorization-failed` with actionable message and zero secret leakage in output/summary.
- Given the full fixture suite, when `python3 -m unittest discover -s tests/tooling/publish_containers -p "*_test.py"` runs, then all pass.
- Given the merged fix, when the next releasable commit lands on main, then both images appear at `https://registry.hexalith.com/v2/<repo>/tags/list`.

## Design Notes

- Evidence: in run 29376871137 the preflight's preemptive Basic POST got 202 for both repos 95s before `docker push` got 401. Live probes: `GET /v2/` returns 200 anonymously **and with bad creds** — the daemon ping never yields a challenge, so docker pushes unauthenticated.
- Authfile (not `--dest-creds`) keeps secrets off argv and is byte-exact with the proven probe's base64.
- skopeo reads the SDK's gzipped `*.tar.gz` docker-archives directly; `docker load`/`tag` become unnecessary.
- `skopeo inspect --raw` returns the manifest; config digest = `.config.digest` (missing → fail closed, as today).

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/publish_containers -p "*_test.py"` -- expected: all pass
- `python3 -m unittest discover -s tests/tooling/release_preflight -p "*_test.py"` -- expected: all pass

**Manual checks (if no CLI):**
- After merge: next Release run's `Run semantic-release` step succeeds; new version listed in both registry tag lists.

## Suggested Review Order

**Credential mechanism (entry point)**

- Scoped temp authfile replaces docker login; byte-identical to the proven preflight probe.
  [`publish-containers.ps1:126`](../../tools/publish-containers.ps1#L126)

- Push gate: creds check, skopeo availability, fail-closed not-attempted before any side effect.
  [`publish-containers.ps1:395`](../../tools/publish-containers.ps1#L395)

- Authfile lifecycle: created once per run, always deleted in finally.
  [`publish-containers.ps1:404`](../../tools/publish-containers.ps1#L404)

**Push and digest reconciliation via skopeo**

- Local digest now read from the archive manifest — no docker daemon involved.
  [`publish-containers.ps1:283`](../../tools/publish-containers.ps1#L283)

- Archive embedded-reference guard: fails closed only on positive mismatch evidence.
  [`publish-containers.ps1:321`](../../tools/publish-containers.ps1#L321)

- Remote inspect with authfile; already-present/digest-conflict semantics preserved.
  [`publish-containers.ps1:325`](../../tools/publish-containers.ps1#L325)

- skopeo copy pushes the archive directly; auth-failure classifier extended with forbidden.
  [`publish-containers.ps1:339`](../../tools/publish-containers.ps1#L339)

**Release-blocking contract pins**

- Inventory test now pins the skopeo contract instead of the removed docker strings.
  [`CiTestInventoryTests.cs:381`](../../tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs#L381)

- Absence pins prevent silent docker-daemon push reintroduction.
  [`CiTestInventoryTests.cs:389`](../../tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs#L389)

**Fixture harness**

- Plan/state-driven fake skopeo validates authfile bytes, mode, and forbidden credential flags.
  [`publish_containers_test.py:113`](../../tests/tooling/publish_containers/publish_containers_test.py#L113)

- Shared assertion: every authenticated call must use a valid 0600 authfile.
  [`publish_containers_test.py:323`](../../tests/tooling/publish_containers/publish_containers_test.py#L323)

- Happy path asserts no secrets in argv and both copies performed.
  [`publish_containers_test.py:359`](../../tests/tooling/publish_containers/publish_containers_test.py#L359)

- Mismatched archive reference fails closed before any copy.
  [`publish_containers_test.py:445`](../../tests/tooling/publish_containers/publish_containers_test.py#L445)

**Docs**

- Mechanism and root-cause rationale (zot#2928 mixed-ACL ping behavior).
  [`release-runbook.md:38`](../../docs/dev/release-runbook.md#L38)

- skopeo added to release prerequisites.
  [`release-runbook.md:98`](../../docs/dev/release-runbook.md#L98)

- Troubleshooting signature for docker-path reintroduction.
  [`release-runbook.md:348`](../../docs/dev/release-runbook.md#L348)
