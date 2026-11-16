# Story 30.6 release caller parity

Observed 2026-11-15 at clean base `fad5ec5efb286a1b4589869a354895acd3aaf55c` plus the local 30.6 implementation.
Prior caller: `46667b0d25c707bcdc961d59895bfe5a064bfd12`. Guarded cutover: Story 30.3,
`8aee7a1323160cb39d7b1415256dbfdcb032939a`, first released in v3.0.18.
D17 retains Memories' publisher. No migration to Builds `domain-release.yml` is claimed.

The evidence classes are deliberately separate. `live-run` records a GitHub execution/readback;
`offline-fixture` records simulated destinations; `static-source` compares checked source.
A fixture supplies no live publication, registry-policy, NuGet-policy or production-runtime proof.
Every row below records both values and a verdict. Shared gaps retain their owner and new reopen trigger.

| Lane / row ID | Prior value | Current value | Evidence class | Exact evidence command or artifact | Verdict | Owner / reopen trigger |
| --- | --- | --- | --- | --- | --- | --- |
| Trigger/source | Push to main; no exact-source CI proof or publication flag | Dispatch only; current-main and successful exact-source push CI, repeated before publication; exact lowercase publication flag gates later steps | static-source | `git show 46667b0d:.github/workflows/release.yml`; current `.github/workflows/release.yml` and `tools/check-release-source.py` | stronger | Memories Release Engineer |
| Environment | `environment: production` declaration | Protected production plus nonempty required-reviewer and branch-policy API readback before/after approval | static-source | Same prior/current workflow comparison; `live-jobs.json` run 41589105475 refuses the missing reviewer rule | stronger | Memories Release Engineer |
| Builds identity | `initialize-build@main`, `initialize-dotnet@main` | `prepare-domain-release@ae77d5690a9411a2154fa4ed5231edc373942573` with identical `builds-execution-sha` | static-source | Same workflow comparison; `tools/check-release-source.py` and `tools/container-publication-contract.py` validate exact helper identity | stronger | Memories Release Engineer |
| Packages | Ten manifest IDs, including EventStoreExtensions | Same ten IDs; frozen package byte inventory for recovery | static-source | `git show 46667b0d:tools/release-packages.json`; `tools/release-packages.json`, `tools/pack-release.ps1`, `tools/archive-release-evidence.py` | stronger | Memories Release Engineer |
| Images | Five project/repository publications without a frozen complete-set contract | Same five mappings; two platforms, digest-bound workload health and runtime-config parity before publication | static-source | `git show 46667b0d:tools/publish-containers.ps1`; `tools/release-containers.json`, `tools/container-publication-contract.py` | stronger | Memories Release Engineer |
| Assets | Ten nupkgs plus production YAML (11) | Same eleven plus release-evidence archive and SHA-256 receipt (13) | static-source | `git show 46667b0d:.releaserc.json`; `.releaserc.json`; `live-release-readback.json` | stronger | Memories Release Engineer |
| NuGet OIDC | `NuGet/login@v1`, production job, one-hour temporary key | Same caller-owned login after source/destination checks, same `vars.NUGET_USER` contract | static-source | Prior/current `release.yml` comparison | parity | Memories Release Engineer; delayed-key expiry remains a separate existing ledger risk |
| Registry authorization | Preflight verifies configured credentials before publisher transports | Preflight records mode; SDK archives transported through skopeo authfile; challenge-response and policy ambiguity fail closed | static-source | Prior/current `tools/verify-container-registry.ps1`, `tools/publish-containers.ps1`, `tools/container-publication-contract.py` | stronger | Memories Release Engineer; no atomic server-side immutability proof inferred |
| Failure | Aggregate family summaries and partial-publish alert; failed prepared bytes can disappear | Same summaries/alert plus diagnostics and retained-byte recovery bundle; incomplete bundle has named blockers | static-source; offline-fixture | Prior/current `release.yml`, `tools/publish-release.ps1`, `tools/archive-release-evidence.py`; offline `partial-publication.json` (qualifier checkpoint, no live credit) | stronger | Memories Release Engineer; cancellation/incident eligibility gaps remain below |
| Recovery | Historical config-digest path; current five/ten set unsupported | Production/exact-source guarded retained-byte current recovery, fifteen member classifications, thirteen assets; historical tagged contracts remain | static-source; offline-fixture | Prior/current `.github/workflows/recover-partial-release.yml`, `tools/complete-partial-release.ps1`, `tools/container-publication-contract.py`; offline `recovery.json` (qualifier checkpoint, no live credit) | stronger | Memories Maintainer |
| Test authority | Release repeats local `tools/test-release.ps1` after build | Exact-source CI owns application tests; release repeats source proof instead; `tools/test.ps1` remains local developer runner | static-source | Prior/current workflow; `git show fad5ec5e:tools/test-release.ps1`; `tools/test.ps1`, `.github/workflows/ci.yml` | parity | Memories Maintainer; no fixture success is called live test proof |
| Version floor (`version-floor`) | No comparison against every external stable package/image version | Exact-version destination absence remains, external floor still omitted | static-source | Prior publisher and current `tools/check-release-destinations.py`; ledger Story 30.3 Final Blind 4 | shared-gap | Jérôme Piquot; reopen at `epic-30-retrospective` |
| Terminal tag recheck (`terminal-tag`) | Exact tag absence checked before preparation, then semantic-release non-force tag push | Same terminal-tag gap; final package/image recheck does not recheck tags; same-object concurrent tag can pass | static-source | Prior/current `release-preflight.ps1`, `.releaserc.json`; ledger Story 30.3 Round 3 Blind 5 | shared-gap | Jérôme Piquot; reopen at `epic-30-retrospective` |
| Complete-set dry run | None: the prior caller had no offline complete-set qualification (its inventory was ten packages/five image mappings) | Six qualifier checkpoints pass on clean base, including five complete image publications through real pinned helpers with fake destinations | offline-fixture | `complete-set-dry-run.json`, `qualification.txt`, `dry-run.md` | parity | Memories Release Engineer |
| Controlled rehearsal | None: the prior caller published on every push to main, with no controlled or owner-dispatched rehearsal | Run 43432588932 at c4ee997702ede948ada5f99eeae7b3782e92003b: dispatch, both jobs success, thirteen assets; current readback has ten package versions/five indexes | live-run | `live-runs.json`, `live-jobs.json`, `live-release-readback.json`; `gh run view 43432588932 --repo Hexalith/Hexalith.Memories` | parity | Memories Release Engineer; pre-30.5 code; no final-code live credit |
| Live failure boundary | Prior source has no readback/source guard | 41589105475 fails production readback before protected job; six other recorded dispatched runs fail semantic-release; v3.0.19–v3.0.22 have tags without Releases | live-run | `live-runs.json`, `live-jobs.json`, `live-release-readback.json` | stronger | Memories Release Engineer; pre-30.5 code |
| NuGet duplicate (`nuget-duplicate`) | `--skip-duplicate` accepts raced 409 as success | Unchanged in release publisher; current recovery refuses 409 independently | static-source | Prior/current `tools/publish-nuget.ps1`; ledger Round 2 Blind 5; `tests/tooling/publish_nuget/publish_nuget_test.py` | shared-gap | Jérôme Piquot; reopen at `epic-30-retrospective` |
| Eventual consistency (`nuget-eventual-consistency`) | Flat-container 404 can precede validation; duplicate acceptance compounds it | Same release-path gap; recovery compares retained package content and waits for validation separately | static-source | `tools/check-release-destinations.py`, `tools/publish-nuget.ps1`; ledger BH17/AA10b; `tools/container-publication-contract.py` | shared-gap | Jérôme Piquot; reopen at `epic-30-retrospective` |
| GitHub-plugin incident (`github-plugin-incident`) | Family summaries can succeed before GitHub Release/assets fail; alert skips succeeded aggregate | Same eligibility gap; Story 30.5 adds asset recovery but leaves alert eligibility unchanged | static-source | Prior/current `.releaserc.json`, `tools/create-partial-publish-issue.ps1`; ledger Round 3 Blind 8 | shared-gap | Jérôme Piquot; reopen at `epic-30-retrospective` |
| All-fail incident (`publish-failed-incident`) | Alert skips `publish-failed` | Unchanged; an all-fail post-tag publish can lack the partial-publish incident | static-source | `tools/create-partial-publish-issue.ps1`; `test_issue_helper_skips_when_summary_status_is_publish_failed`; ledger Story 30.5 D3 | shared-gap | Jérôme Piquot; reopen at `epic-30-retrospective` |
| Child guidance (`nuget-child-guidance`) | NuGet child summary gives manual-reconciliation text | NuGet child text unchanged while aggregate/incident guidance now names Recover Partial Release | static-source | `tools/publish-nuget.ps1`, `tools/publish-release.ps1`, `tools/create-partial-publish-issue.ps1`; ledger Story 30.5 Verification Gap | shared-gap | Jérôme Piquot; reopen at `epic-30-retrospective` |
| Unsafe release notices (`do-not-deploy`) | Affected v3.0.20–v3.0.25 immutable images remain offered by historical assets | Runbook refuses them as rollback targets; owner still must mark affected Releases do-not-deploy | static-source | `docs/dev/release-runbook.md` Current Release Contract and Release Rollback; ledger R3-B1/R4-B1/R4-B3 | shared-gap | Jérôme Piquot; reopen when owner marks the affected GitHub Releases do-not-deploy |
| Live recovery (`live-recovery`) | No current-set recovery path existed | Recovery is proven by source and offline fixtures only; D2's additive check observes a successful release and can never exercise recovery | static-source; offline-fixture | `recovery.json`, `partial-publication.json`; `.github/workflows/recover-partial-release.yml` | shared-gap | Jérôme Piquot; reopen at the first owner-authorized Recover Partial Release run (code review 2026-11-16) |
| Rollback target registration (R6-H4) | No release rollback rule; CI rollback only | Latest fully released guarded target v4.0.0; refuse v3.0.19–v3.0.25 and pre-30.3 caller, normal PR only | static-source | `docs/dev/release-runbook.md` Release Rollback; `live-release-readback.json` | stronger | Memories Maintainer; documentation hand-off resolved |
| Planning corrections (30.5 F6, 30.4 R6) | Original planning text understates delivered scope/status/checkpoints | Dated notes record corrected owned paths, recovery allowlist approval, runtime-config parity and released authorization evidence | static-source | `_bmad-output/planning-artifacts/epics.md` dated 2026-11-15 notes; ledger dated resolutions | stronger | Memories Maintainer; documentation hand-offs resolved |

There are zero `regression` verdicts. Coordinator independently verified the lane comparison on
2026-11-16 against prior/current callers, inventories, publisher configuration, shared gaps and live identities. This is a retrospective comparison of
capabilities, not approval to publish or proof that every shared gap is fixed.

## Removal record

**Gate passed; removal performed on 2026-11-16 (Europe/Paris).** The coordinator's independent
lane comparison verified zero regression verdicts. Owner-approved D6 then allowed exact historical
manifest/index capture; the two required snapshots and the supplemental snapshot after isolated
module qualification all have SHA-256 `68165b901643f2005f450474c0cf7ba3710ced1c0d5c7da51cd2b3ba486d2158`.
Both `compare --expect identical` calls exit 0; timestamps, argv and exact output are persisted in
`identity-d6-proof.json` and `identity-rollback-compare.txt` / `identity-rollback-modules-compare.txt`.

The rollback release lanes pass, with the complete 104-ID container set qualified in five fresh
module processes after retained historical all-in-one fixture flakes. No assertions were skipped
or changed. `rollback-d6-isolated-modules-verified.json` proves exact discovered/executed ID equality.
The rehearsal addendum records the fallback and the ad hoc collector correction's provenance.

The removal deletes only `tools/test-release.ps1`, removes its CONTRIBUTING inventory mention,
replaces the runnable instruction with `./tools/test.ps1`, and replaces its C# runner facts and
dead parser/fixture family with one absence fact. The independent deferred-ID guard remains.
The C# inventory class changes from 141 to 101 cases (-40); all 101 pass after a clean isolated
Debug/source build, with zero skips/warnings/errors. `retirement.diff` and `ci-inventory-d6*.txt`
retain the diff and checks. The release_dispatch absence assertion and story_scope fixture text
remain intact. No publisher, recovery or release-workflow behavior changed.

## Checkpoints

| Checkpoint | Owner | Exact evidence command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Clean-base offline dry run | Memories Release Engineer | `dry-run.md`, `complete-set-dry-run.json` | reviewed 2026-11-16 | local execution complete; six checkpoints pass; no live credit |
| Controlled rehearsal | Memories Release Engineer | `live-runs.json`, `live-jobs.json`, `live-release-readback.json` run 43432588932 | owner selected D2; reviewed 2026-11-16 (code review) | complete historical run/readback; pre-30.5 only |
| Final-code live proof | Jérôme Piquot | Runbook exact next-release `capture` and `compare --expect additive` commands | accepted 2026-11-16 | obligation; not complete; consequence: no final-code live publication credit; reopen trigger: next owner-authorized dispatch plus additive comparison |
| Lane comparison | Memories Release Engineer | This matrix and prior/current sources | coordinator independently verified 2026-11-16 | complete; zero regression rows |
| Prior path removal | Memories Release Engineer | This removal record, `retirement.diff`, `ci-inventory-d6.txt` | parity gate coordinator verified; reviewed 2026-11-16 | complete; old runner retired; C# 101/101 pass |
| Snapshot fixtures | Memories Maintainer | `run-with-temp python3 -B -m unittest discover -s tests/tooling/release_cutover -p '*_test.py' -v`; `snapshot-fixtures-code-review.txt` (D6 history: `snapshot-fixtures-d6.txt`, 16/16) | reviewed 2026-11-16 | 34/34 methods pass, zero skips; refusal reasons pinned; `code-review-mutation-check.txt` catches 7/7 mutations |
| Rollback lanes | Platform Operations | `rollback-rehearsal.md` D6 addendum; `rollback-d6-results.json`, `rollback-d6-isolated-modules-verified.json` | reviewed 2026-11-16 | actionlint and all five lanes pass; container set 104/104 in isolated modules, historical full-discovery flakes retained |
| Published identities unchanged | Platform Operations | `identity-d6-proof.json`; both exact identical comparisons and all snapshot receipts | reviewed 2026-11-16 | complete; all three snapshots byte-identical, including the final module fallback bracket |
| Target refusal rule | Memories Maintainer | Runbook Release Rollback, R6-H4 resolution | reviewed 2026-11-16 | documented; fully released v4.0.0 target read back |

GitHub digest audit after acceptance hardening: `github-asset-digest-audit.json` checks all 183
Releases and 1,700 assets, with zero missing/null/invalid SHA-256 digests. This read-only result
is supplemented by the complete D6 identity snapshots. Original failures/refusals remain retained.

Final guard/coverage review: 29/29 snapshot fixtures pass. `post-review-capture-verified.json` and `post-review-compare.txt` confirm the final tool captures the same complete identities. The four snapshot JSON byte streams are identical.

Code review correction (2026-11-16): the Complete-set dry run and Controlled rehearsal prior values now record the prior caller's
actual value (none), Failure/Recovery cite their offline qualifier artifacts, and `live-recovery` registers the recovery
live-proof gap. The verdicts are unchanged: zero `regression` rows.
