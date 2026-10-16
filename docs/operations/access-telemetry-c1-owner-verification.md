# Owner-bound C1.15 verification

`tools/access_telemetry_c1_owner_c115.py` composes the existing owner-run,
retained-source, strict command and retained-stream inspections for one
non-Production `PG-ONPREM-2` capture. A matching result binds the supplied byte
materials to the same approved-plan assertions and discloses
`sole-owner/self-reviewed`. It supplies no live execution evidence, deployed
registration, technical gate pass, target/action authorization or acceptance.
Story 27.4, A41, sprint holds and the Spec 14 live tasks remain open.

The fixed policy is `sole-owner-qualification/2026-10-12`, and the owner is
`github:user:6775094`. The current profile is the
[isolated-target manifest adopted 2026-10-16](qualification/pg2-kvm-approved-profile-2026-10-16.json),
SHA-256 `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`, with context
`owner@memories-qualification-27-4`. Workload, Dapr 1.18.1 and all technical
image/configuration pins are unchanged. Former PG2 and PG1 captures remain
historical and cannot satisfy current-profile inspection. The owner materials have separate schemas; the legacy
fixture registration APIs retain their stronger independent-review declarations
and their existing version selection. `lookup_deployed_binding` continues to
refuse unconditionally.

## Pure API and outside approval anchors

Put the repository's `tools` directory on the Python import path. For the pure
API, all snapshot acquisition and status acquisition belong to the caller.
The inspector has no filesystem, process, network, configuration or clock dependencies. Local
checkout corroboration is a separate API described below; neither API writes an
acceptance record.

```python
from access_telemetry_c1_owner_c115 import inspect_owner_c115

inspection = inspect_owner_c115(
    registry=registry_snapshot,
    capture=capture_snapshot,
    sources=retained_source_tuple,
    streams=retained_command_stream_tuple,
    run=owner_run_snapshot,
    anchors=approved_run_anchors,
    statement=owner_registration_snapshot,
    command_contract=owner_command_snapshot,
    review_role_policy=owner_role_snapshot,
    approved_registry_sha256=approved_raw_registry_sha256,
    now=caller_supplied_aware_utc_datetime,
    withdrawn=caller_supplied_current_boolean,
)
```

| Input | Contract |
| --- | --- |
| `registry`, `capture`, `run`, `statement`, `command_contract`, `review_role_policy` | Exact `JsonSnapshot` instances containing immutable retained bytes. Every snapshot is reparsed and rehashed from `raw`; cached trees and cached hashes are discarded. |
| `sources` | Exact built-in tuple of nineteen exact `SourceSnapshot` instances, including both executed and normalized Git blob byte forms and all required metadata. |
| `streams` | Exact built-in tuple of exact `C115CommandStreams` instances in capture receipt order. Each contains a purpose and exact immutable stdout/stderr bytes. |
| `anchors` | Exact `OwnerRunAnchors` instance from the retained owner-approved plan, obtained outside the candidate record. It contains `record_sha256`, `policy_sha256`, `session_id`, `source_commit`, `target_sha256` and `custody_root`. |
| `approved_registry_sha256` | Canonical lowercase SHA-256 from the separately approved plan, covering the registry's exact raw bytes, including whitespace. The registry's canonical J1 inventory hash cannot substitute for it. |
| `now` | Explicit aware UTC built-in `datetime`, with a built-in zero-offset `timezone`. No ambient time is read. |
| `withdrawn` | Explicit current built-in boolean. Missing status, nonboolean values and withdrawal refuse. |

Do not derive approval anchors or the approved registry digest from candidates
being inspected. Matching caller-supplied approval assertions cannot authenticate
the owner or registration; the inspector cannot establish how the caller
obtained those assertions. Re-read the run and withdrawal state before later use.

The run must pass the existing closed `owner-run/v1` inspector for `observe`.
Its status must be approved, its policy/owner/assurance and non-Production scope
must match the adopted constants, and its profile/workload pins must match the
current adopted values. Its collection phase must be active at `now`; `fault`, `purge`,
`reclaim`, `close-out` or `publish` permission alone cannot substitute for
`observe`. Withdrawal, expiry and use outside the run's collection phase refuse.

The capture's profile ID, profile/workload digests, qualification session, target
digest and full source commit label must match the inspected run. The capture's
start and finish must lie within the run's collection window, and finish must be
at or before `now`. Capture comparisons preserve all seven fractional digits
(100 ns); the run retains its existing millisecond UTC `Z` spelling. Start is
inclusive. Although the capture endpoint comparison permits the collection end,
`observe` itself requires `now` strictly before that end.

## Closed owner declaration schemas

All schema names below are prefixed with `hexalith.access-telemetry.c1.`. Unknown
versions and fields refuse; there is no version inference or fallback to fixture
schemas. Registry entries retain the existing thirteen-field inventory shape.

`owner-registration/v1` contains exactly the existing fixture statement fields
plus four owner fields:

`schemaVersion`, `gate`, `profileId`, `registeredStory`, `producerPath`,
`helperPaths`, `inputPaths`, `captureSchema`, `verifierPath`, `verifierSchema`,
`commandContract`, `cleanupRequired`, `reviewRolePolicy`, `sourceCommit`,
`policyId`, `policySha256`, `ownerPrincipal`, `assuranceMode`.

Every inventory field matches the selected reinspected C1.15 entry; the source
commit matches the owner run, capture and all nineteen source snapshots. The
four owner fields match the run. The statement excludes `registrationReceipt`
to avoid a circular digest. `cleanupRequired` is strictly `false` in both the
entry and statement. Its `commandContract` and `reviewRolePolicy` Refs must
equal the selected entry's Refs exactly, including their paths.

`owner-command-contract/v1` contains exactly seven fields:

`schemaVersion`, `gate`, `profileId`, `producerPath`, `executable`, `purposes`,
`readOnly`.

Gate/profile/producer match the entry; executable is `kubectl`, and `readOnly`
is strictly `true`. Purposes are exactly the six ordered grammar kinds:

```json
["current-context", "lifecycle-pods", "daprd-version", "metadata", "alpha-opt-in", "lifecycle-pods-recheck"]
```

Pod suffixes and repeated kinds belong only to the capture receipts. This owner
schema always applies the strict v2 argv grammar: context, initial selection,
version/metadata/alpha for each observed Pod in its observation order, then final
selection. Command count is `3 * Pod count + 3`. Every argument, fixed probe and
Pod/container scope is compared exactly; rehashing foreign argv cannot admit it.

`owner-review-role-policy/v1` contains exactly seven fields:

`schemaVersion`, `gate`, `profileId`, `policyId`, `policySha256`,
`ownerPrincipal`, `assuranceMode`.

Gate/profile match the entry, and the four owner fields match the inspected run.
The owner schema does not imply independent review. Fixture role fields such as
`reviewerRole` and `producerExcluded` are outside this closed schema.

The selected registry's three Refs bind the exact retained registration,
command-contract and role-policy lengths and raw SHA-256 values. Their unchanged
Ref shape is exactly `path`, `sha256`, `byteLength`, using canonical relative
paths, lowercase SHA-256 and positive lengths at most 1 MiB. Ref comparison
establishes supplied byte identity and declaration consistency only.

## Source, stream and budget checks

The source comparison reuses the existing normalization and metadata rules:
strict UTF-8, `utf8-crlf-to-lf`, regular Git mode `100644` or `100755`, no clean
filter or working-tree encoding, and `ident: false`. All nineteen producer,
helper and input paths must match the registry/capture inventory exactly, with
no duplicate, missing or substituted sources. Executed SHA-256, normalized
SHA-256 and Git blob SHA-1 are recomputed and compared with every receipt.
Commit labels are compared without authenticating a repository or Git object.

Require an observed clean capture, a clean worktree label, zero failures/skips,
and an unchanged final source recheck. The existing structural reader and pinned
observation inspector remain authoritative. Stream inspection hashes every
untrimmed stdout/stderr byte sequence, applies existing secret/content checks
to every command including later Pods, and compares actual retained output with
the declared observations. The final Pod selection must preserve the initial
name, UID, label and lifecycle/sidecar image identities; reordering unchanged
Pods is allowed. Retained bytes cannot prove that any command actually ran or
that the observations came from the declared target.

Before any reparse or digest work, each supplied JSON artifact, each source byte
form and each stdout/stderr stream is capped at 1 MiB. One call is capped at
32 MiB across all six JSON artifacts, all thirty-eight source byte forms and
every stdout/stderr occurrence. Identical bytes count every time they appear;
there is no deduplication. The exact limits are allowed, and one extra byte
refuses. This aggregate rule is stricter than the legacy source inspector's
unique-byte accounting, whose API and result remain unchanged.

## Frozen result and refusals

`OwnerC115Inspection` exposes these frozen fields:

| Field | Meaning |
| --- | --- |
| `owner_run` | Reinspected `OwnerRunInspection`, with the exact retained run, action `observe`, supplied instant and self-review disclosure. |
| `registration` | Frozen `OwnerRegistrationInspection` containing `source`, `statement`, `command_contract`, and `review_role_policy`. All material snapshots preserve their original bytes. |
| `commands` | Existing `C115CommandInspection` with Pod names, command purposes and argv tuples. |
| `retained_streams` | Existing `C115StreamInspection` with a detached immutable stream tuple preserving every exact purpose/stdout/stderr value and derived Pod/command counts. |
| `sources` | Detached immutable tuple of copied retained source snapshots, with source byte pairs concealed from representations. |
| `retained_byte_count` | Total supplied byte occurrences charged by this call. |
| `owner_principal`, `assurance_mode` | `github:user:6775094` and `sole-owner/self-reviewed`. |

Source, command and stream inspections share the same reparsed capture object
and exact capture bytes. The registration's source inspection retains the
reparsed registry bytes. JSON trees are immutable mappings/tuples, source and
stream collections are tuples, and every result dataclass is frozen. Retained
snapshots, source bytes, streams and argv are concealed from the combined
result's representation. No pass, acceptance or execution flag is returned.
Source/stream records and their tuple containers are copied after preflight and
before inspection. Their immutable byte objects preserve identity and the
occurrence count; later caller field mutation cannot change the retained result.

Failures raise `OwnerC115InspectionError` with bounded content-free codes and
no original exception cause/context. A failure returns no partial combined
inspection. Unsupported inputs, unknown versions, changed approval assertions,
foreign scope, inconsistent Ref/source/argv/output and budget/time violations
all refuse before any forbidden dependency could be called.
Missing required fields on exact dataclass inputs also refuse with
`owner-c115-required-field-missing`, including missing byte fields during budget
preflight. Forged stream purposes must be exact built-in strings before any
comparison, so caller equality callbacks cannot run.

## Current approved plan files

`tools/access_telemetry_c1_owner_c115_plan_files.py` provides
`inspect_owner_c115_from_plan_files` for complete offline C1.15 inspection while
the current approved plan, run record and withdrawal status remain held.

```python
from access_telemetry_c1_owner_c115_plan_files import inspect_owner_c115_from_plan_files

inspection = inspect_owner_c115_from_plan_files(
    plan_path="/absolute/private-custody/plan.json",
    approved_plan_sha256=outside_approved_raw_plan_sha256,
    custody_root="/absolute/private-custody",
    registry=registry_snapshot,
    capture=capture_snapshot,
    sources=retained_source_tuple,
    streams=retained_command_stream_tuple,
    statement=owner_registration_snapshot,
    command_contract=owner_command_snapshot,
    review_role_policy=owner_role_snapshot,
    now=caller_supplied_aware_utc_datetime,
)
```

The outside approved digest pins the exact raw plan bytes. The plan retains the
byte dispatcher's seven required bindings: `recordSha256`, `policySha256`,
`sessionId`, `sourceCommit`, `targetSha256`, `custodyRoot`, and `statusPath`.
This API also requires `recordPath` and a canonical lowercase raw
`registrySha256`. Other approved plan fields remain open; there is no new
acceptance schema. `registrySha256` binds the supplied registry's exact raw
bytes, including whitespace, rather than its canonical inventory digest.
The six `OwnerRunAnchors` come solely from these outside-digest-approved plan
bytes. Callers supply neither a run snapshot nor anchors nor withdrawal state
to this API. The existing byte dispatcher still requires only its seven fields;
the existing private-file dispatcher still requires `recordPath` without
requiring `registrySha256`.

Before filesystem callbacks, the wrapper checks the closed supplied snapshot,
source and stream types and occurrence budgets, copies source/stream records,
and reparses all five supplied JSON snapshots from immutable bytes. Forged
cached trees/hashes are discarded; caller mutations during filesystem callbacks
cannot substitute for those detached inputs. `now` must be explicit aware UTC.
The acquired run participates in the full inspector's six-artifact 32 MiB
occurrence budget; the plan/status are separately bounded to 1 MiB each and do
not become supplied C1.15 evidence occurrences.

Acquisition and completion proceed in this order:

1. Open and read the canonical private plan, verify its outside digest and
   seven bindings, require `recordPath`, and match its custody root to the caller.
2. Open and read the plan-named run record, then the plan-named current status.
   Status has exactly `planSha256`, `recordSha256`, `sessionId`, and `withdrawn`,
   with matching bindings and a built-in boolean withdrawal value.
3. Inspect the current run for `observe` with that validated status. Withdrawal,
   expiry, phase/policy/owner/PG2 drift and invalid current files reject before
   full C1.15 inspection.
4. Run the complete existing C1.15 inspector against the detached evidence,
   acquired run, approved-plan anchors and approved raw registry digest.
5. Rewind and reread bounded exact plan bytes, then run bytes, then status bytes,
   checking named file/ancestor identities and metadata with every read. Check
   the effective OS user again and close all descriptors before returning.

The three canonical distinct paths must lie strictly below the exact approved
custody root. Files and directories at/below that root must be owned by the
effective OS user and deny group/other permissions. Files must be regular and
single-link. Descriptor-relative no-follow acquisition rejects symlinks and
unsafe named ancestors; identity checks also reject aliased artifacts and
ancestor movement. All file and ancestor descriptors survive the full offline
inspection and final readback. Exact-byte readback detects observed same-size
rewrites even when coarse timestamps repeat. Any final drift discards even a
computed valid inspection. Every call reacquires current files without caches.

Success returns the existing frozen `OwnerC115Inspection`, including exact
detached evidence, `observe`, and `sole-owner/self-reviewed`. Refusals raise
bounded content-free `OwnerC115PlanFilesError` codes beginning
`owner-c115-plan-files-`, after descriptor cleanup and without an original
exception cause/context or partial result. No process, producer, Git, network
or ambient clock runs during this wrapper.

Held-descriptor custody covers only the acquired plan/run/status files.
Supplied source pairs, streams and declaration materials receive byte
consistency checks; their acquisition custody is not established here. OS-user
ownership does not authenticate the GitHub owner. Sequential acquisition and
final checks provide no simultaneous snapshot, complete evidence custody or
continuing lock after return. Current-plan acquisition now has this offline
composition; concrete live plans, live collection, complete retained custody
and technical gate/result acceptance remain separate work. Spec 14 live tasks,
Story 27.4, Production activation and A41 close-out remain open.

## Owner-compatible local checkout corroboration

`tools/access_telemetry_c1_owner_c115_provenance.py` adds
`corroborate_owner_c115_sources`. It takes every explicit input of the pure API
above, plus `repository_root`, and runs the complete owner inspector before any
filesystem or Git access. A previously returned inspection cannot substitute
for fresh retained inputs, current `now` or withdrawal status.

```python
from access_telemetry_c1_owner_c115_provenance import corroborate_owner_c115_sources

corroboration = corroborate_owner_c115_sources(
    registry=registry_snapshot,
    capture=capture_snapshot,
    sources=retained_source_tuple,
    streams=retained_command_stream_tuple,
    run=owner_run_snapshot,
    anchors=approved_run_anchors,
    statement=owner_registration_snapshot,
    command_contract=owner_command_snapshot,
    review_role_policy=owner_role_snapshot,
    approved_registry_sha256=approved_raw_registry_sha256,
    repository_root="/absolute/canonical/standalone-checkout",
    now=caller_supplied_aware_utc_datetime,
    withdrawn=caller_supplied_current_boolean,
)
```

The supported checkout is a standalone local repository with a real `.git`
directory. Its root must be an explicit canonical absolute path, without empty,
`.` or `..` components or a trailing slash. The descriptor-relative Linux lane
requires `O_NOFOLLOW` and `/proc/self/fd`; symlinks in the root/source path,
linked worktrees and submodule checkouts using `.git` files are unsupported.
Alternate object stores, common Git directories and worktree-specific config
files refuse. Uninitialized Git links are supported only when descriptor-relative
no-follow checks find their checkout paths absent or empty real directories.
The clean-status requirement still applies: removing a tracked Git-link
directory can appear as a deletion and therefore refuses as a dirty checkout.
Nonempty/initialized nested repositories, linked Git-link paths and tracked
symlinks refuse before status could enter another checkout. Local objects must already
be present: no fetch, clone or target request is attempted.

After pure inspection, an owner safety preflight independently reads and
validates `.git/config` before any repository-aware Git command. No Git config
reader runs during this validation. An include or include FIFO in the inspected
config therefore refuses before Git setup. Config must be a regular single-link file
within the 1 MiB bound. A closed UTF-8 syntax supports ordinary Git-written
core/user sections and quoted remote/branch subsections. Ordinary quoted
subsection names can contain spaces, punctuation such as `+`, UTF-8 characters,
and Git-escaped quotes/backslashes; section and key permissions stay closed. Includes, filters,
external helper keys, extra configuration scopes, line continuations, legacy
dotted sections, invalid quoting and unknown keys refuse. Ordinary standalone
clone identity, remote URL/fetch and branch settings remain supported.
`core.fsmonitor` and hooks paths are supported because no hook operation runs
and every owner Git command fixes `core.fsmonitor=false`.

Descriptor-relative no-follow checks validate the entire `.git` metadata tree,
including `objects`, object-store descendants, `index`, `HEAD`, refs and
`info/attributes`. Symlink files/directories, hard-linked files, repeated
identities, devices and FIFOs refuse before repository-aware Git. Traversal is
bounded to 8192 entries, 64 directory levels and 1 MiB of retained path/state
accounting. The config
bytes, metadata identities/modes/sizes/timestamps and Git-link checkout identities
are retained, reread during preflight and compared at final checks. The preflight
validates every index path, rejects unsupported tracked modes, independently
reads `.git/info/attributes` before the first Git child and the relevant
`.gitattributes` files without following links, then checks filter attributes across all tracked files, including unrelated paths. Each
attribute file, including `.git/info/attributes`, has the existing 1 MiB bound; one preflight retains at most
32 MiB of attribute bytes, refusing before retaining the file that would exceed
the aggregate. Unique attribute-path accounting is capped at 8192 entries and
1 MiB. Ordinary unrelated index paths are validated as bounded relative native
UTF-8 paths, independently of evidence references. Colon, tilde and space names
are supported. Empty/absolute paths, NUL, dot traversal and `.git` components
refuse; the existing stricter evidence-reference rules still apply to all owner
materials and source inventory paths. Filter checks use argv batches capped at
32 KiB of UTF-8 path bytes plus NUL/pointer accounting, leaving headroom for the
literal launcher and fixed flags under Linux's 131072-byte minimum ARG_MAX.
Every batch keeps stdin at `/dev/null`, retains the per-child 1 MiB output bound,
and verifies every returned path/filter/value triple exactly.
Missing Git-link directories retain the dirty-checkout refusal; empty real
uninitialized entries work. Each declared Git link's index commit must match
HEAD. Status uses `--ignore-submodules=all`, so a Git link initialized after
preflight cannot make status enter a nested repository; final safety checks
still refuse its changed/nonempty directory.

Each owner Git read runs through a trusted literal Python launcher using
`/usr/bin/python3 -I -S -B`, without importing checkout code, before execing the
fixed `/usr/bin/git`. The supported enforcement runtime is Linux with native
x86-64 ABI, seccomp BPF and `PR_SET_NO_NEW_PRIVS`, plus the installed standard
Python `ctypes` module and those two executable paths. The execution policy
assumes the literal launcher and installed Python/Git binaries are trusted.
Its execve restriction compares the address of the prepared pathname buffer;
it does not inspect that buffer's contents or authenticate executable contents. The launcher verifies the
architecture and installs a closed syscall filter; unsupported architectures or
failed installation refuse, with no unenforced fallback. Setup and execution
share the per-child deadline; there is no parent preexec callback. No external
packages or system dependency changes are required. The enforcement uses the
[Linux seccomp filter interface](https://docs.kernel.org/userspace-api/seccomp_filter.html).

The filter denies process creation, sockets/networking, execve with other
pathname pointers, file creation/truncation/removal/mode changes, writable file writes and every shared
file mapping (including read-only mappings that could later be made writable).
Unknown/new syscalls and alternative ABIs refuse. Git startup's existing
`O_RDWR` `/dev/null` open is permitted; creation, truncation, appending and
write-only opens remain denied. Writes are confined to the inherited stdout
pipe and stderr `/dev/null`; closing/replacing those descriptors, descriptor
duplication, close-range operations and fcntl changes that could close output
on exec are denied. This enforcement blocks a clean filter introduced after
preflight even when status sees a same-size dirty file. Fixed owner flags force
filemode, ctime, default stat and ordinary index checks regardless of supported
checkout settings. `core.preloadIndex=false` and `index.threads=1` keep index
reads/status sequential, including checkouts large enough to trigger Git's
ordinary preload-thread threshold.

The shared local-source lane then checks the approved
full commit against a clean `HEAD`, rejecting tracked/untracked dirt and
unsupported index flags, including assume-unchanged and skip-worktree. All
nineteen detached reinspected source pairs must match their tracked paths, Git
blob identities and modes at that commit, and their exact working bytes.
The registered story and verifier must be distinct tracked regular files at
the same commit, outside the nineteen-source inventory; their normalized UTF-8
working bytes must match their blobs. Nothing executes those files.

Regular source files are individually bounded at 1 MiB and must have a single
hard link. Attributes allow text normalization with LF/CRLF and reject filters,
active ident expansion or working-tree encoding. A final sweep rereads the
sources and auxiliary files and rechecks attributes, index flags, clean `HEAD`,
alternate stores and root identity before returning any result. Git calls use
the existing fixed local read commands (`rev-parse`, `ls-files`, `status`,
`ls-tree`, `cat-file`, `check-attr`), an isolated environment and disabled
network protocols/lazy fetching/replacement objects. The owner preflight never
calls Git's config reader. Standard input is `/dev/null`. Every owner child owns
a new process group, shares the existing 10-second deadline and 1 MiB output
cap, and has its entire group killed during cleanup, with the immediate child
reaped and its output closed. Enforced Git cannot create descendants. Legacy
fixture children retain their existing command flags and lifecycle.

The frozen `OwnerC115LocalSourceCorroboration` contains `inspection` (concealed
from its representation), `source_commit`, `repository_root`, ordered `paths`
and corresponding `git_blob_oids`, plus `owner_principal` and `assurance_mode`.
The inspection retains all six exact reparsed snapshot bytes, detached source
pairs, detached command streams, occurrence-counted budgets and
`sole-owner/self-reviewed`. Only those detached reinspected sources and the
inspected registration feed local comparison; later caller mutations cannot
replace their checked bytes.

Failures raise `OwnerC115LocalSourceCorroborationError`, with bounded
content-free `owner-c115-...` codes, no original cause/context and no partial
result. Pure-owner refusals retain their codes; local-lane refusals receive the
`owner-c115-` prefix. Malformed retained inputs refuse before filesystem,
process, network, configuration or clock dependencies. Root failures and local
OS/decoder errors normalize to the same bounded refusal contract. The legacy
`corroborate_local_sources` API and both fixture command-contract versions
retain their valid results and existing lane; the safety preflight applies to
owner corroboration. Unsafe owner-checkout refusal ordering can therefore differ
from legacy fixture corroboration.

This is point-in-time evidence of local file/commit correspondence. Git status
excludes ignored untracked files; unrelated ignored content is outside the
returned evidence and is not claimed absent or inspected. The syscall filter is not an OS filesystem sandbox and does not constrain
read paths to an immutable checkout. Metadata/attribute/config checks and final
rereads provide point-in-time checks; concurrent changes can still occur between
reads. They do not lock the repository or authenticate its
custody, a producer execution or a target observation. Caller approval anchors
remain assertions obtained outside candidate materials. Complete retained
custody, concrete live plans, live collection and technical gate/result
acceptance remain separate work. The current-plan API below composes local
checkout corroboration within held plan/run/status acquisition. Spec 14 live
tasks, Story 27.4, A41 and Production activation remain open.

## Current-plan local checkout corroboration

`tools/access_telemetry_c1_owner_c115_provenance.py` also provides
`corroborate_owner_c115_sources_from_plan_files`. Supply the same inputs as
`inspect_owner_c115_from_plan_files`, plus `repository_root`. There are no
caller-supplied run, anchors, withdrawal value or approved registry digest:
those come solely from current files bound to the outside-approved plan bytes.
The existing owner policy approval persists; this composition adds no approval
or acceptance authority.

```python
from access_telemetry_c1_owner_c115_provenance import corroborate_owner_c115_sources_from_plan_files

corroboration = corroborate_owner_c115_sources_from_plan_files(
    plan_path="/absolute/private-custody/plan.json",
    approved_plan_sha256=outside_approved_raw_plan_sha256,
    custody_root="/absolute/private-custody",
    registry=registry_snapshot,
    capture=capture_snapshot,
    sources=retained_source_tuple,
    streams=retained_command_stream_tuple,
    statement=owner_registration_snapshot,
    command_contract=owner_command_snapshot,
    review_role_policy=owner_role_snapshot,
    repository_root="/absolute/canonical/standalone-checkout",
    now=caller_supplied_aware_utc_datetime,
)
```

The call completes these steps before returning:

1. Check closed supplied containers, explicit UTC `now` and occurrence budgets;
   detach source/stream records and rebuild all five supplied snapshots before
   filesystem callbacks.
2. Acquire the private approved plan, its run record and its current status,
   preserving the existing outside digest, exact bindings, owner/PG2/self-review
   checks, `observe` requirement and withdrawal/expiry refusals.
3. Run exactly one full C1.15 inspection, charging the acquired run in the
   six-artifact 32 MiB budget. All current-file and evidence validation precedes
   checkout access and Git. Only the detached inspected sources feed comparison.
4. Hold all plan/run/status and ancestor descriptors through the existing owner
   safety preflight and enforced bounded local Git lane described above. The
   existing standalone clean-checkout, local-object, deadline, output budget,
   syscall enforcement and child-cleanup requirements apply unchanged.
5. Rewind and reread exact bounded plan bytes, run bytes and status bytes in
   that order, with status last. Revalidate named file/ancestor identities and
   metadata, check the effective user, and close all held descriptors before
   releasing the result.

Success returns the existing frozen `OwnerC115LocalSourceCorroboration`, with
its detached inspection, local identities and `sole-owner/self-reviewed`
disclosure. It contains no gate verdict. Withdrawal or file/ancestor drift
during Git or final readback discards a computed result. Cleanup failures also
refuse. `OwnerC115PlanSourceCorroborationError` carries a bounded content-free
`owner-c115-plan-source-...` code after descriptor and child cleanup, without an
original cause/context or partial result. Existing inspection, explicit-anchor
corroboration and deployed-binding refusal contracts remain unchanged.

This workspace's initialized submodules are unsupported by the owner checkout
lane. Corroboration needs an existing clean standalone checkout with a real
`.git` directory and all matching local objects. Creating that checkout remains
later preparation; this API performs no clone, fetch, dependency update or Git
mutation and never contacts the target or runs candidate code/collectors.

Held custody covers only the acquired plan/run/status files. Source pairs,
streams and declarations still receive consistency checks without complete
acquisition custody. Sequential acquisition and final readback establish no
simultaneous snapshot or continuing lock; local correspondence proves no
producer execution, target observation, registration, technical gate pass or
acceptance. Spec 14 live tasks, Story 27.4, A41 close-out and Production
activation remain open.

## Approved retained-evidence files

`tools/access_telemetry_c1_owner_c115_retained.py` provides
`corroborate_owner_c115_retained_files` to acquire the evidence inside the held
approved-plan composition. Its five arguments are the complete public input:

```python
from access_telemetry_c1_owner_c115_retained import corroborate_owner_c115_retained_files

corroboration = corroborate_owner_c115_retained_files(
    plan_path="/absolute/private-custody/plan.json",
    approved_plan_sha256=outside_approved_raw_plan_sha256,
    custody_root="/absolute/private-custody",
    repository_root="/absolute/canonical/standalone-checkout",
    now=caller_supplied_aware_utc_datetime,
)
```

The approved plan retains its existing bindings, including `recordPath` and
`registrySha256`, and must also contain `evidenceManifestPath` and
`evidenceManifestSha256`. The latter digest binds the exact raw evidence
manifest bytes before parsing or following any manifest reference. The registry
continues to match both its manifest reference and the approved plan's raw
`registrySha256`. The existing owner policy approval persists.

The evidence manifest uses the closed schema
`hexalith.access-telemetry.c1.owner-retained-evidence/v1`. It has exactly these
seven fields:

| Field | Required value |
| :---- | :------------- |
| `schemaVersion` | `hexalith.access-telemetry.c1.owner-retained-evidence/v1` |
| `registry` | Exact private-file reference to registry JSON |
| `capture` | Exact private-file reference to capture JSON |
| `statement` | Exact private-file reference to owner registration JSON |
| `commandContract` | Exact private-file reference to owner command JSON |
| `reviewRolePolicy` | Exact private-file reference to owner role policy JSON |
| `sources` | Exactly nineteen complete source descriptors |

Each file reference has exactly `path` and `sha256`: `path` is a canonical
absolute path strictly below the exact custody root, and `sha256` is the
lowercase 64-hex digest of the exact bytes. This acquisition reference is
separate from the existing relative declaration Refs inside registry materials;
those retain their existing format and byte-length bindings.

Every source descriptor has exactly nine fields, retaining all
`SourceSnapshot` metadata while replacing its two byte slots with file
references:

```json
{
  "path": "tools/example.ps1",
  "sourceCommit": "0123456789abcdef0123456789abcdef01234567",
  "executedBytes": {
    "path": "/absolute/private-custody/evidence/source-00.executed",
    "sha256": "0000000000000000000000000000000000000000000000000000000000000000"
  },
  "blobBytes": {
    "path": "/absolute/private-custody/evidence/source-00.blob",
    "sha256": "0000000000000000000000000000000000000000000000000000000000000000"
  },
  "normalization": "utf8-crlf-to-lf",
  "gitMode": "100644",
  "cleanFilter": null,
  "workingTreeEncoding": null,
  "ident": false
}
```

These illustrative paths/digests are placeholders. The nineteen real semantic
source paths must match the existing registry and capture inventory. Git mode
may also be `100755`. Both byte forms require their own distinct retained file,
even when their bytes are identical. All five JSON refs, nineteen descriptors,
metadata, canonical file paths, digests and duplicate names are validated as a
complete inventory before any evidence reference is followed.

A suitable layout is:

```text
private-custody/                         owner-only directory
  plan.json                             outside-approved exact bytes
  record.json                           plan-named owner run
  status.json                           fresh plan-bound withdrawal state
  evidence/
    evidence.json                       plan-bound evidence manifest
    registry.json
    capture.json
    capture.json.streams/
      manifest.json                     existing retained-streams/v1 format
      0000.stdout
      0000.stderr
      ...                               one ordered pair per capture command
    statement.json
    command_contract.json
    review_role_policy.json
    source-00.executed
    source-00.blob
    ...                                 nineteen distinct byte pairs
```

The capture reuses the unchanged ordered stream sidecar: exactly
`manifest.json` and the four-digit `stdout`/`stderr` pairs bound to the capture's
receipts. Its schema, exact directory inventory, receipt order, hashes and
stream semantics remain required. Inventory refs cannot alias these fixed
sidecar files. Every control/evidence artifact path and device/inode must be
distinct. All files must be regular, single-link, owned by the effective OS
user and have no group/other permissions. Every directory at/below custody
must meet the same ownership/privacy requirements. Acquisition follows no
symlink, including in ancestors.

The call completes in this order:

1. Validate explicit aware UTC `now`, acquire approved plan/run/status, and
   perform the existing current `observe`, owner/PG2/self-review, phase, expiry
   and fresh withdrawal checks before acquiring any evidence.
2. Acquire and authenticate the plan-bound manifest; validate its complete
   inventory before following refs. Acquire the exact referenced JSON/source
   bytes and the capture's ordered streams with shared identity tracking.
3. Run exactly one complete `inspect_owner_c115` using only acquired bytes and
   plan-derived anchors. Any invalid schema, scope, identity, hash, byte budget
   or semantic input refuses before checkout access or Git.
4. Corroborate the detached inspection through the existing enforced local Git
   lane, preserving all checkout safety, per-child deadlines and cleanup rules.
5. Reread bounded exact evidence bytes and revalidate their named ancestor
   chains while every evidence/control descriptor remains held. Complete outer
   plan/run/status readback in that order, finishing with status last; close all
   descriptors before releasing the existing frozen corroboration result.

Each artifact is limited to 1 MiB. An incremental 32 MiB semantic occurrence
budget charges the acquired run, all five JSON materials, both byte forms of
every source and every stdout/stderr occurrence. Equal bytes still count each
time. Newly acquired evidence, including both manifests, receives a size
preflight before reading or hashing; an evidence occurrence that would exceed
the aggregate budget also refuses before its read. The existing plan/run/status
readers instead use bounded reads of at most 1 MiB plus one overflow-detection
byte, then reject oversized control artifacts. Plan, status, evidence manifest
and stream manifest have separate 1 MiB caps and do not consume the semantic
occurrence budget.

OS descriptor capacity is another resource requirement: every held artifact
retains its own complete ancestor chain. Larger or deeper valid evidence sets
can exhaust descriptor capacity below the byte budgets. Acquisition failure
unwinds the registered descriptor stacks and any owned children before a
content-free refusal; cleanup faults also discard results. The API does not
change OS descriptor limits.

Same-size byte drift, changed names/ancestors/permissions, withdrawal during
Git or final readback, and cleanup faults discard computed results. Failures
raise `OwnerC115RetainedFilesError` with bounded content-free
`owner-c115-retained-...` codes after descriptor/child cleanup and exception
handlers, without original cause/context or a partial result.

The returned detached inspection discloses `sole-owner/self-reviewed`. Local
OS ownership authenticates no GitHub principal. Sequential checks provide no
simultaneous snapshot or continuing lock. This API does not execute candidate
code/collectors, contact targets, register evidence, accept gates or mutate Git
or dependencies. Preparing a clean standalone checkout with a real `.git`
directory and matching local objects remains later work; the initialized
submodule workspace remains unsupported for corroboration. Live tasks,
Story 27.4's deployment verification, Production activation and A41 close-out
remain open; A41 remains partially closed.

## Reproducible offline verification

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_retained.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_plan_provenance.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115_plan_files.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*provenance.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
```

The focused fixtures cover same-run positive inspection, all one/two/three-Pod
permutations, immutability, forged caches, exact budget/time boundaries, missing
or withdrawn runs, owner schema/policy changes, and source/Ref/argv/output drift.
Counted barriers deny filesystem, process, network, configuration and clocks
during every inspector case and test representative aliases bound before the
barriers across the composed modules. A counted C-call guard denies native
datetime/date clock methods before execution while preserving their class
identity for closed-type validation. Cross-scope negative evidence is named explicitly in
`test_rehashed_cross_namespace_and_tenant_targets_refuse_before_dependencies`,
`test_rehashed_foreign_session_and_source_commit_refuse`,
`test_rehashed_later_pod_argv_scope_and_fixed_probe_substitutions_refuse`, and
`test_rehashed_later_pod_output_swap_and_final_recheck_drift_refuse`.

The current-plan fixtures reuse the existing private-files and multi-Pod
fixtures without rediscovering their TestCase classes. They verify every-call
fresh acquisition, approved-plan bindings, descriptor lifetime/cleanup,
detachment before filesystem callbacks, invalid-container refusal before I/O,
run/status refusal before C1.15, exact acquired-run occurrence budgets, and
withdrawal or coarse-timestamp byte drift after a valid inspection. Cross-scope
negative evidence is named in
`test_rehashed_foreign_namespace_tenant_session_commit_refuse`,
`test_rehashed_foreign_later_pod_arguments_and_outputs_refuse`, and
`test_foreign_source_ref_and_raw_output_consistency_refuse`.

The current-plan provenance fixtures compose those private files with real
disposable standalone checkouts. They assert exactly one full inspection,
held file/ancestor lifetime throughout all Git children, status-last final
readback, fresh approved replacements, detached sources and occurrence budgets.
Counted barriers prove invalid containers precede file reads and invalid
current files/evidence precede checkout/Git access; successful calls permit only
the existing bounded local Git lane. Cross-scope negative evidence is named in
`test_rehashed_foreign_tenant_and_namespace_refuse_before_git`,
`test_rehashed_foreign_session_and_commit_refuse_before_git`, and
`test_rehashed_later_pod_foreign_argv_outputs_and_recheck_refuse_before_git`.
`test_mid_git_withdrawal_discards_result_after_cleaning_held_files_and_children`
and `test_final_readback_withdrawal_after_successful_git_discards_computed_result`
prove withdrawal discards computed results. Same-size/coarse-timestamp drift,
ancestor movement, OS cleanup faults and Git deadline failures retain bounded
refusals and descriptor/child cleanup.

The owner-provenance fixtures create actual disposable standalone Git
checkouts. They cover one/two/three Pods, exact local command ordering and
environment and safety preflight, raw-cache forgery and caller mutation, rehashed foreign
namespace/tenant/Pod inputs, byte/time/status boundaries, missing/dirty/foreign
checkouts, links/aliases, attributes/index flags, final rereads and child cleanup.
Safety regressions assert that same-size dirty-source filters, filters on
unrelated tracked paths and filters inside initialized Git links cannot create
their harmless marker before refusal. A real 1,100-file index remains clean under the enforced native no-process
policy. Native probes deny process/network/file
mutation, output descriptor replacement, shared mappings and other execve
pathname pointers while still execing the trusted installed Git; an actual checkout with oversized aggregate attribute
bytes refuses before retaining the over-budget file; unsupported enforcement refuses. Owned-child probes also exercise actual
PR_SET_SECCOMP installation failure, unknown/x32 syscall denial and the native
i386 architecture kill path, without installing a filter in the test parent.
Real packed objects retain all nineteen positive source identities. Disposable
boundary fixtures verify exact metadata entry/state/depth limits and attribute
path entry/state refusals. Native regression fixtures verify info-attribute
per-file/aggregate limits, ordinary quoted config names, colon/tilde filenames,
850 long paths under a 512 KiB stack/131072-byte ARG_MAX, and case-colliding
untracked files with core.ignorecase=true. Drift regressions
cover filters and nested initialization introduced after preflight, external
include FIFOs, metadata directory/file aliases, unrelated cached-byte and mode
drift, auxiliary final mode drift, supported config changes between preflights
and an injected descendant during timeout cleanup. Positive fixtures retain
declared uninitialized Git links with empty checkout directories; missing
checkout paths retain their clean-status refusal. Includes, helpers,
unsupported layouts, config/metadata drift and disabled fsmonitor are covered.
Counted barriers around invalid retained inputs deny ambient dependencies and
known aliases across both APIs and their composed inspectors. Discovery counts
are recorded separately from executed test results in the respective standalone
specs.

These results remain offline verification. Approval of a run, matching owner
materials and passing fixtures supply no deployed registration, gate acceptance,
Production activation or A41 close-out credit.
