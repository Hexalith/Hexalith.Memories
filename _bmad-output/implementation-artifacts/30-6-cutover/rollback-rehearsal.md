# Story 30.6 rollback rehearsal

The original 2026-11-15 report below is retained. The D6 addendum at the end records the
current complete identity proof, repeated rehearsal and isolated-module qualification.

Date: 2026-11-15. Base: `fad5ec5efb286a1b4589869a354895acd3aaf55c`.
Target: **v4.0.0**, peeled source `c4ee997702ede948ada5f99eeae7b3782e92003b`.
The targeted live readback (`live-release-readback.json`) confirms thirteen Release assets,
ten listed NuGet 4.0.0 versions and five 4.0.0 image indexes. It is not a complete identity snapshot.
The runbook refuses v3.0.19–v3.0.25 and the pre-30.3 push caller as rollback targets.

The rehearsal created a detached root worktree and a worktree of the root-declared Builds
submodule at its unchanged gitlink `e2bbbd0893f586e4d447ef8358ff747ea3d34435`, both beneath the
private `run-with-temp` TMPDIR. No nested submodule was initialized. The release surface from
`rollback-surface.txt` was restored with:

```bash
git restore --source v4.0.0 --worktree -- <the paths in rollback-surface.txt>
```

`rollback-surface.diff` preserves the exact scratch diff. It restores the caller, publisher,
recovery tools and their tests. The dependency graph, main working tree and execution pins
were not changed. No workflow was dispatched, no PR created and nothing was pushed. Each
child ran in the foreground; both temporary worktrees were removed after all lanes finished.

All commands below ran inside that rollback tree under `run-with-temp`, with `TERM=dumb` and
`PYTHONDONTWRITEBYTECODE=1`. Original argv, exit codes and durations remain in `rollback-results.json`. The environment-only
NuGet rerun is in `rollback-nuget-padding-results.json`; `rollback-final-results.json` combines
the passing final local lanes without changing the original failure evidence.

| Checkpoint | Owner | Exact command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Workflow syntax | Platform Operations | `actionlint .github/workflows/release.yml .github/workflows/recover-partial-release.yml`; `rollback-actionlint.txt` | pending independent review | complete; exit 0 |
| Package lane | Platform Operations | `python3 -B -m unittest discover -s tests/tooling/release_packages -p '*_test.py' -v`; `rollback-release_packages.txt` | pending independent review | complete; exit 0 |
| Preflight lane | Platform Operations | Same discovery command with `tests/tooling/release_preflight`; `rollback-release_preflight.txt` | pending independent review | complete; exit 0 |
| Dispatch lane | Platform Operations | Same discovery command with `tests/tooling/release_dispatch`; `rollback-release_dispatch.txt` | pending independent review | complete; exit 0 |
| NuGet lane | Platform Operations | Same discovery command with `tests/tooling/publish_nuget`, padded private TMPDIR below; `rollback-publish_nuget-padded.txt` | pending independent review | complete; 32/32 methods pass, zero skips, exit 0; original failures retained |
| Container lane | Platform Operations | Same discovery command with `tests/tooling/publish_containers`; `rollback-publish_containers.txt` | pending independent review | complete; 104/104 methods, zero skips, exit 0 |
| Temporary worktree cleanup | Platform Operations | `git worktree list`; no 30.6 worktree remains | pending independent review | complete after all child jobs finished |
| Before/after published identities | Platform Operations | Intended `identity-before.json`, `identity-after-rollback.json`, `compare --expect identical`; `snapshot-capture-refused.txt` | blocked by historical manifest shape | not complete; no snapshots written and no identical verdict claimed |

## Named blockers

**SNAPSHOT-HISTORICAL-MANIFEST — owner Jérôme Piquot.** The required full before-capture
refused at `registry memories:0.0.0-preview.0.474: not an image index`, exit 1. The tag resolves
to a historical single-image manifest, whereas the frozen contract requires an index digest
for every tag. No fallback or partial snapshot was written. Consequently this local rehearsal
is not temporally bracketed by complete snapshots. Resolve that contract decision, capture
before, repeat the rehearsal, capture after and require the identical comparison before giving
rollback-mutation proof credit. Published identities were never deliberately changed, but that
statement does not replace the requested complete before/after proof.

## Resolved local diagnostic issue

**ROLLBACK-V4-NUGET-DIAGNOSTIC — resolved by environment-only rerun; owner Memories Maintainer.** Exact v4.0.0
`IssueHelperReliabilityTests.test_summary_root_must_be_an_object` asserts the contiguous phrase
`JSON summary object` in a flattened stderr string. PowerShell's 80-column redirected diagnostic
renderer inserts a `|` margin between words when the private TMPDIR path wraps. All six malformed
summary subcases refuse with the intended exception and no GitHub calls, but the exact text
assertion fails. The current source normalizes margin bars; restoring v4.0.0 restores the old
assertion as required by D3. No fixture or release-path source was patched.

COLUMNS=240 and a 240-column PTY still report redirected PowerShell BufferSize/WindowSize 80.
One fresh exact-source rehearsal of only the NuGet lane used `/proc/self/cwd` as a shorter TMPDIR
alias into its own disposable worktree. It also failed the same six subcases; see
`rollback-publish_nuget-short-cwd.txt` and `rollback-nuget-environment.json`. That attempt's
worktrees were also removed. The original failure remains in `rollback-publish_nuget.txt`.
A subsequent evidence-based path-length experiment resolved this local test issue without source
or test edits. A child directory with twenty padding characters under the wrapper-owned TMPDIR
moves the diagnostic wrap before the full asserted phrase. The exact historical method passes
(`rollback-nuget-padding-20-targeted.txt`), followed by the full unchanged historical NuGet lane:
32/32 methods, zero skips, exit 0 (`rollback-publish_nuget-padded.txt`). No output was normalized.
The exact environment setup, inside the disposable job, is:

```bash
NUGET_SCRATCH="$TMPDIR/pppppppppppppppppppp"
mkdir -p "$NUGET_SCRATCH"
TMPDIR="$NUGET_SCRATCH" TMP="$NUGET_SCRATCH" TEMP="$NUGET_SCRATCH" TERM=dumb python3 -B -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v
```

`rollback-nuget-padding-results.json` retains argv, padding length, exit codes and timing.
The fresh target worktrees were removed after the full lane finished. All required local release
lanes now pass. The overall mutation-proof checkpoint remains incomplete solely because no full
before/after snapshots can be captured under the frozen all-index policy.

## Hardened GitHub digest readback

After refusing null/missing asset digests in the snapshot tool, a separate read-only API audit
examined all **183 Releases and 1,700 assets** on 2026-11-15. Every asset has a valid API SHA-256
digest; none is missing, null or invalid. `github-asset-digest-audit.json` retains the endpoints,
per-release counts and zero-invalid result. This is not a complete identity snapshot and does
not change the original registry refusal or provide a before/after identity verdict.

Rolling back to v4.0.0 drops Story 30.5 retained-byte recovery bundles. After publication,
continuation is qualified recovery or a named blocker; published versions/tags/assets and Latest
are never undone or adjusted by rollback.

## D6 completed rehearsal addendum — 2026-11-16 (Europe/Paris)

Owner-approved D6 resolves the initial historical-format refusal without an allowlist, cutoff,
conversion or registry mutation: recognized OCI/Docker image manifests and indexes retain their
exact top-level digest and media type; only the five new additive-release tags must be indexes.
The initial `snapshot-capture-refused.txt` and every earlier rehearsal/failure log remain unchanged.

The new before-snapshot contains 196 `v*` tag object/peeled identities, 183 Releases and 1,700
assets, every version/listed state for ten packages, and 463 image tag identities across five
repositories (418 Docker schema-2 manifests and 45 OCI indexes). All original response-byte
SHA-256 values match the registry headers. Candidate 4.0.1 is absent in all four domains.

The scratch rollback restored exactly `rollback-surface.txt` from v4.0.0 at current HEAD,
with the unchanged root Builds gitlink. `rollback-d6-surface.diff` retains its diff. Actionlint,
release_packages (28), release_preflight (50), release_dispatch (17) and publish_nuget (32)
pass; NuGet uses the previously recorded private TMPDIR padding. No workflow was dispatched,
no source/fixture/assertion was changed to obtain a pass, and nothing was pushed. Every D6
worktree was removed after its foreground child jobs finished.

### Historical full-discovery flake and focused-validation fallback

Three complete exact-v4.0.0 container discoveries each pass 103/104 methods and fail a different
positive cancellation control: `test_probe_verifies_and_cancels_all_repository_upload_sessions`,
`test_standard_unauthorized_basic_challenge_is_accepted`, then
`test_default_receipt_lands_where_the_authorize_phase_reads_it_by_default`. The errors name
memories-mcp, memories-domain-service and memories-access-telemetry respectively. These are
loopback fixture transport failures, not registry reads or writes. The target fixture emits
bodyless HTTP/1.0 204 responses without an explicit `Connection: close`; current baseline
`76de3ea4` already documents/fixes the race with the next synchronous DELETE. Its HttpClient
is disposed after the session loop and has a 30-second timeout; no proxy environment variables
were set. The focused controls pass. Single-CPU affinity also fails in full discovery and is
not a qualified remedy. Original logs/metadata preserve all three failed discoveries.

The baseline's focused-validation fallback runs **every complete module once in a fresh Python
process**, without filtering methods, skipping assertions, source changes, response shims,
runtime monkeypatches or output normalization. Exact commands, from the restored scratch tree:

```bash
python3 -B -m unittest discover -s tests/tooling/publish_containers -p partial_release_completion_test.py -v
python3 -B -m unittest discover -s tests/tooling/publish_containers -p publication_contract_test.py -v
python3 -B -m unittest discover -s tests/tooling/publish_containers -p publish_containers_test.py -v
python3 -B -m unittest discover -s tests/tooling/publish_containers -p registry_authorization_test.py -v
python3 -B -m unittest discover -s tests/tooling/publish_containers -p release_orchestration_test.py -v
```

All five processes exit 0: **9 + 50 + 26 + 12 + 7 = 104/104 methods**, zero skips.
`rollback-d6-isolated-modules-verified.json` retains every canonical discovered/executed ID,
the exact commands and per-module log artifacts; sorted ID equality is true with no duplicates.
The original ad hoc collector mistakenly appended the method name twice to the already-qualified
parenthesized verbose IDs. Its outer equality check exited 1 while all five test processes
exited 0. `rollback-d6-isolated-modules-results.json` remains unchanged. The verified derivative
reads the canonical parenthesized IDs from those same unchanged logs and checks their counts
against each `Ran N tests` result. No test was rerun for that metadata correction. The complete
set qualifies through separate module processes; the all-in-one discoveries remain failed.

### Temporal bracket and identical identities

`identity-d6-proof.json` persists the before completion timestamp from the snapshot/receipt
filesystem mtimes before Git storage, every rehearsal's UTC start/end, both after-capture
start/finish times, exact comparison argv/output and SHA-256 identities. It does not depend on
Git preserving filesystem mtimes. The supplemental after-snapshot brackets the complete module
fallback as well as all previous rehearsals.

Before receipt written: `2026-11-15T22:59:49.586886+00:00`.

First repeat: `2026-11-15T23:00:42Z` to `2026-11-15T23:04:11Z`.

Isolated modules: `2026-11-15T23:15:57Z` to `2026-11-15T23:17:51Z`.

Initial after-capture: `2026-11-15T23:13:48.692449+00:00` to `2026-11-15T23:15:17.456931+00:00`.

Supplemental after-capture: `2026-11-15T23:18:26.246774+00:00` to `2026-11-15T23:19:52.789052+00:00`.

```bash
python3 -B tools/release-identity-snapshot.py compare _bmad-output/implementation-artifacts/30-6-cutover/identity-before.json _bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback.json --expect identical
python3 -B tools/release-identity-snapshot.py compare _bmad-output/implementation-artifacts/30-6-cutover/identity-before.json _bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback-modules.json --expect identical
```

Both exit 0 with exact stdout `[release-identity] pass: identical` and empty stderr. All three
snapshot JSON files have SHA-256 `68165b901643f2005f450474c0cf7ba3710ced1c0d5c7da51cd2b3ba486d2158`.
Their sorted JSON bytes are identical; each receipt verifies. No published identity changed.

| Checkpoint | Owner | Exact command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Repeated rollback syntax/packages/preflight/dispatch/NuGet | Platform Operations | `rollback-d6-results.json`, `rollback-d6-*.txt` | reviewed 2026-11-16 | complete; actionlint and four non-container lanes pass |
| Complete historical container set | Platform Operations | Five exact module commands above; `rollback-d6-isolated-modules-verified.json` | reviewed 2026-11-16 | complete; all 104 IDs pass once in isolated modules, zero skips; full-discovery flakes retained |
| Full immutable identity bracket | Platform Operations | `identity-d6-proof.json`, both exact comparisons above and snapshot receipts | reviewed 2026-11-16 | complete; all identities identical through final fallback |
| Scratch cleanup / no push | Platform Operations | Rehearsal result JSON records and `git worktree list --porcelain` | reviewed 2026-11-16 | complete; every D6 worktree removed; no push |
| Prior runner retirement | Memories Maintainer | `parity-matrix.md`, `retirement.diff`, `ci-inventory-d6.txt` | coordinator verified zero-regression parity; reviewed 2026-11-16 | complete; old runner removed; C# inventory 101/101 pass |

D2's next owner-dispatched release remains an explicit additive-snapshot obligation. This local
proof supplies no live final-code publication credit and authorizes no dispatch or remote change.

Final review follow-up — 2026-11-16: the stronger guards retain the same snapshot format. `identity-post-review.json` is byte-identical to the temporal-bracket snapshots, and its capture/comparison exit 0. Original refusal, failed discoveries and passing isolated modules remain unchanged.
