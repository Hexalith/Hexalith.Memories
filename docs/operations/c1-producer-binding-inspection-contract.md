# Offline C1 producer-binding inspection

`tools/access_telemetry_c1_producer_bindings.py` inventories closed registry
entries and compares explicit retained source bytes to the unchanged neutral
C1.15 v2 capture. It returns frozen inspection records. It creates no accepted
artifact, registration, gate verdict or permission to execute a producer.

This is the separately authorized preparation described by
[`spec-pg2-c1-offline-producer-bindings.md`](../../_bmad-output/implementation-artifacts/spec-pg2-c1-offline-producer-bindings.md).
The integrated I2–I6 tasks and P1–P7 prerequisites remain incomplete. Story 27.4,
A41, prior history, sprint holds and the disabled Production configuration retain
their existing state.

## Library APIs

Import the module with the repository's `tools` directory on the import path.
The library takes supplied immutable objects; callers own snapshot acquisition.
No API reads files, runs Git/processes/collectors/verifiers, contacts targets or
networks, or loads ambient configuration.

| API | Required inputs | Inspection result |
| --- | --- | --- |
| `inspect_registry(snapshot)` | An I1 `JsonSnapshot` containing a JSON array, including an empty array. | `RegistryInspection`: original `snapshot`, ordered tuple `entries`, and `registry_sha256`. |
| `inspect_sources(registry, capture, sources, expected_source_commit)` | A `RegistryInspection`, I1 capture `JsonSnapshot`, tuple of `SourceSnapshot` records and explicit canonical lowercase 40-hex commit label. | `SourceInspection`: rederived `registry` and selected `entry`, original `capture`, compared `source_commit`, ordered tuple `sources` of recomputed identities and `unique_retained_byte_count`. |
| `lookup_deployed_binding(gate, profile_id, inventory=None)` | Any requested gate/profile and optional inspection inventory. | Always raises `InterchangeFormatError("deployed-producer-binding-unavailable")` before inspecting inputs or using dependencies. |

All result dataclasses are frozen, all result arrays are tuples, and I1 retains
JSON objects as immutable mappings. There is no caller pass flag or accepted
output. Refusals use bounded content-free `InterchangeFormatError` codes; they
omit supplied source contents. Source bytes and retained JSON are omitted from
record representations.

## Registry input

Each array entry has exactly these thirteen fields:

`gate`, `profileId`, `registeredStory`, `registrationReceipt`, `producerPath`,
`helperPaths`, `inputPaths`, `captureSchema`, `verifierPath`, `verifierSchema`,
`commandContract`, `cleanupRequired`, `reviewRolePolicy`.

`gate` is exactly one of C1.1 through C1.25; entries are distinct and ordered by
numeric gate suffix. `profileId` is exactly `PG-ONPREM-2`. Story, producer,
verifier, helper and input paths use I1's canonical repository-relative POSIX
path rules and decoded secret/content checks. Helper/input arrays are separately
lexically ordered and unique, disjoint from the producer and each other; source
paths also reject ancestor/descendant overlap. Arrays may be empty for inventory.
`cleanupRequired` has boolean type. Both schema identifiers are bounded,
nonempty, secret-safe strings; inventory does not require an implemented schema.

The three Ref fields use the unchanged I1 `EvidenceReference` shape: exactly
`path`, `sha256`, `byteLength`, with canonical path, lowercase 64-hex SHA-256 and
positive integer length at most 1 MiB. They are returned as frozen reference
objects. Inspection does not read or authenticate their referenced artifacts.

Each `RegistryEntryInspection` exposes the thirteen fields with snake-case
names and `registry_entry_sha256`, SHA-256 of J1 for that entry. The inventory
hash covers J1 of the ordered array. `snapshot.raw` and `snapshot.sha256`
separately preserve the exact retained JSON bytes and their hash, including
original whitespace/newlines. Changing whitespace can change the retained hash
while preserving a semantic J1 hash.

## Retained source input and comparison

Every `SourceSnapshot` requires explicit values for all nine fields:

```python
SourceSnapshot(
    path="tools/verify-access-telemetry-c1.ps1",
    source_commit=expected_source_commit,
    executed_bytes=supplied_executed_bytes,
    blob_bytes=supplied_blob_bytes,
    normalization="utf8-crlf-to-lf",
    git_mode="100644",
    clean_filter=None,
    working_tree_encoding=None,
    ident=False,
)
```

Supported modes are regular Git files `100644` and `100755`. Clean filters,
working-tree encoding transforms, ident expansion, other normalization labels
and nonregular modes, including symlinks/submodules, refuse. These metadata
values are supplied assertions; inspection cannot determine actual attributes
or filesystem modes. Mutable byte buffers and mutable source containers refuse.

I1 first validates the capture's exact existing C1.15/PG2
`hexalith.access-telemetry.c1.evidence/v2` schema and receipt consistency. The
comparison additionally requires observed, clean, unchanged sources, a clean
worktree label, and the full expected commit label in the capture and every
retained source record. Neutral capture values remain unchanged:
`gateStatus: not-evaluated`, `productionGatePassed: false`,
`productionLifecycleWrites: not-evaluated`, `independentDisposition: pending`
and `producer.identityAuthentication: not-evaluated`.

The selected registry producer must equal the capture's producer. Its helpers
must be the profile and component-backend PowerShell helpers, and its input set
must complete the exact nineteen-path capture inventory: three PowerShell
sources plus sixteen inputs. The supplied tuple must contain exactly nineteen
records before any source iteration, shape validation or digest work. Missing,
extra, duplicated or substituted retained sources refuse. Other capture-schema strings can be inventoried but cannot be
compared by this API.

Both byte forms must be strict UTF-8. Decode executed bytes, replace CRLF with
LF and encode UTF-8; those exact bytes must equal the supplied blob bytes. Lone
CR, non-ASCII text and an existing source UTF-8 BOM are preserved. Recompute
SHA-256 separately over executed and blob bytes, and recompute Git SHA-1 over
ASCII `blob <byte-length>\0` followed by blob bytes. Compare these results to
every receipt's `executedBytesSha256`, `gitNormalizedSha256`,
`headGitNormalizedSha256` and `gitBlobOid`; require tracked, unmodified,
matching-HEAD receipt labels. CRLF executed bytes can have a different SHA-256
from LF blob bytes.

Each `SourceByteInspection` returns `path`, `source_commit`,
`executed_bytes_sha256`, `git_normalized_sha256` and `git_blob_oid`. Returned
source records are lexically ordered by path. Results retain neither source
byte pairs nor their transform/mode metadata; callers must retain the original
`SourceSnapshot` objects to reproduce the comparison. This comparison binds
supplied bytes to supplied receipts and compares commit labels. It cannot authenticate
a repository/commit, ownership, custody, real source state, producer execution,
command grammar, verifier behavior, registration approval or role policy.
An ordinary `done` story label, matching hash or successful fixture supplies no
deployed eligibility.

## Byte budgets and reproducible verification

I1 limits each JSON snapshot to 1 MiB. Both source byte forms separately have
that same 1 MiB limit, checked before source decoding/hashing. One comparison
also limits the total unique retained bytes to 32 MiB, counting registry JSON,
capture JSON and both forms of every source. Identical byte sequences are charged
once even across JSON/source records, paths and byte forms. The aggregate budget
is checked before source decoding and digest comparison. Exact-boundary inputs
are supported; exceeding either bound refuses. `unique_retained_byte_count`
measures this call's processed unique byte contents, not every byte retained by
the returned object. Git SHA-1 is requested with `usedforsecurity=False` because
it identifies a Git blob; an unavailable provider refuses with a bounded error.
UTF-8/provider refusals retain no original exception context containing bytes.

Run the independent isolated fixtures from the repository root:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_producer_bindings.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
```

The fixture byte contents, commits, Ref paths and observations are visibly
isolated. Tests retain literal J1 and Git blob vectors, exercise source/registry
drift and transform denials, enforce exact budgets and immutable containers, and
install forbidden filesystem/process/network/configuration dependencies around
the library APIs. Fixture results grant no operational acceptance.


## Offline preservation manifest

This durable manifest copies the exact twelve protected paths and SHA-256 values
from the pre-implementation receipt. It records offline workspace validation;
it supplies no accepted artifact, gate verdict or checkpoint credit. Paths are
relative to this repository root, and hashes cover exact working-file bytes.

```json
{
  "_bmad-output/implementation-artifacts/sprint-status.yaml": "0e288f08029843870db90402c0cc3d3a1ea5b661dc3c11282729e397a7fcc05a",
  "_bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md": "5bfdb89ef34f8cc8f113df5a0638865bf246e49c084f1c608159e1ea71d820dd",
  "_bmad-output/implementation-artifacts/epic-20-retro-2026-07-04.md": "0d56469ca0e7495fca82c106188800be98cb83503ef7e7f292c2efb6c18ebda9",
  "_bmad-output/implementation-artifacts/deferred-work.md": "8acedc1f0563b34baf2409e94fad13688d4e0cdfd7c38b2dcc7c864d82f9d48f",
  "_bmad-output/project-context.md": "0b62f5ad02d34cb4ad5ddacb5ad0693d0251874f5733fbb4de87c7593636e665",
  "docs/dev/telemetry.md": "2f7a252dd2c9d229058b929b90e2dc3b6e7ee338cec28d9f4eb494dbe7846b2a",
  "deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml": "0c2b4b836d14be457ab8ccc79a534cd9997193f9c7b10f3d11b8b100a8b40ab2",
  "tools/verify_access_telemetry_lifecycle.py": "027da804c413097b7aea5907affceda612b279cfd0eb99f67c2aee2e6aefbd2b",
  "tools/verify-access-telemetry-lifecycle.py": "2c13c085b47d13d8b16a0fdcedaabc935cb7b46bbcd6348afb78c9b608fdcc84",
  "tools/verify-access-telemetry-c1.ps1": "08f132c1ad75e30a94553013ef412a32156d3754784d623ed144d99e2d92e69a",
  "tools/access_telemetry_c1_interchange.py": "1ac1a117a23c326974b3dac9d61b9638c0bac9279d11c975d048931c57aec9de",
  "tools/access_telemetry_c1_github_authority.py": "0710249c787a3e71548ba1dc9dfefde1791cf40b94ee15e148f7ff5e171fbb3d"
}
```

Recheck from the repository root without a network, collector or target call:

```bash
python3 - <<'PY'
from pathlib import Path
import hashlib
import json

contract = Path("docs/operations/c1-producer-binding-inspection-contract.md").read_text()
section = contract.split("## Offline preservation manifest", 1)[1]
manifest = json.loads(section.split("```json\n", 1)[1].split("\n```", 1)[0])
assert len(manifest) == 12
mismatches = [path for path, expected in manifest.items()
              if hashlib.sha256(Path(path).read_bytes()).hexdigest() != expected]
if mismatches:
    raise SystemExit("preservation-mismatch: " + ", ".join(mismatches))
print("12 protected paths unchanged (offline validation only)")
PY
```

Executed offline on 2026-10-08: the manifest command returned
`12 protected paths unchanged (offline validation only)` with exit code zero.
Final focused verification passed 28 tests; the full interchange regression
passed 195 tests, both with zero failures/errors/skips. All three independent
review layers were triaged, direct corrections were verified, and no findings
were deferred. HEAD and all nine root gitlinks remained unchanged; prior history,
existing readiness changes and the approved frozen intent were preserved.
These results supply no checkpoint or deployed acceptance credit.
