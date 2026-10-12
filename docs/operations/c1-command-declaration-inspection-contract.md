# Offline PG2 C1.15 command declaration inspection

Prepared on **2026-10-12** for the bounded Story 27.4 command inspection slice.
The pure API compares retained capture declarations with the existing producer's
exact read-only argument grammar and sequence. It returns inspection data only.
Production remains disabled, parent Story 27.4 stays in-progress and A41 stays
open. Historical dispositions and integrated I2–I6 / P1–P7 holds remain unchanged.

## API and immutable output

`tools/access_telemetry_c1_capture_semantics.py` exposes:

```python
inspect_c115_command_declarations(snapshot: JsonSnapshot) -> C115CommandInspection
```

The caller supplies one explicit immutable `JsonSnapshot`. The inspector first
calls unchanged `inspect_c115_observation_pins(snapshot)`, which reuses unchanged
`parse_capture(snapshot)`. This preserves structural field/type/hash checks,
serial command and source chronology, exact profile/workload/target/runtime/image
pins, neutral markers and the observed-only boundary. Blocked, malformed,
contradictory or unpinned captures return no command inspection.

The result is a frozen, slotted dataclass with exactly these fields:

| Field | Meaning |
| --- | --- |
| `capture` | The identical caller snapshot, retaining exact immutable bytes, SHA-256 and recursively frozen declarations; excluded from the result representation. |
| `pod_count` | Number of structurally checked `observations.pods` entries. |
| `pod_names` | Immutable tuple of those Pod names in observation order. |
| `command_count` | Number of checked command receipts, equal to `3 * pod_count + 3`. |
| `command_purposes` | Immutable tuple of checked purposes in receipt order. |
| `command_arguments` | Immutable tuple of immutable argument tuples in receipt order; excluded from the result representation. |

Exact snapshot bytes, including whitespace, escaping and trailing newline, are
retained without normalization or serialization. The output contains no pass,
acceptance, authority or execution flag or handle.

## Exact producer argument declarations

The inherited structural producer check requires this exact ten-member list:

```text
["-Gate", "C1.15", "-ProfileId", "PG-ONPREM-2", "-QualificationSessionId", SESSION,
 "-EvidenceDirectory", DIRECTORY, "-CommandTimeoutSeconds", TIMEOUT]
```

`SESSION` must exactly equal `qualificationSessionId` and satisfy its existing
bounded ASCII identifier grammar. `DIRECTORY` remains the existing nonempty,
secret-safe declared text argument; this API reads no path and establishes no
archive custody or ownership. `TIMEOUT` is a canonical decimal string from `1`
through `300`, with no leading zero, sign or whitespace. These declaration checks
supply no session authorization, freshness policy or permission to run a producer.
The producer path/identity/authentication markers and each command executable
retain their exact inherited checks; every target executable is `kubectl`.

## Exact command grammar and sequence

This exact grammar applies only to target `kubectl` receipts in `commands`.
Git receipts in `sourceCommands` retain structural, hash and chronology checks
only; their argument meaning is not compared to a producer command grammar.
Those checks establish no Git execution or source eligibility claim.

Every target token and argument position compares exactly. There is no trimming, case
folding, Unicode normalization, shell parsing, equivalent-option substitution,
option reordering or acceptance of additional arguments. The fixed scope is:

```text
SCOPE = ["--context", "jpiquot@local", "-n", "hexalith-memories"]
PROJECTION = "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"
```

The required receipts are, in order:

| Purpose | Exact argument array |
| --- | --- |
| `current-context` | `["config", "current-context"]` |
| `lifecycle-pods` | `SCOPE + ["get", "pods", "-l", "app.kubernetes.io/name=memories-access-telemetry", "-o", PROJECTION]` |
| `daprd-version:POD` | `SCOPE + ["exec", POD, "-c", "daprd", "--", "/daprd", "--version"]` |
| `metadata:POD` | `SCOPE + ["exec", POD, "-c", "lifecycle", "--", "/bin/sh", "-ec", METADATA_PROBE]` |
| `alpha-opt-in:POD` | `SCOPE + ["exec", POD, "-c", "lifecycle", "--", "/bin/sh", "-ec", ALPHA_PROBE]` |
| `lifecycle-pods-recheck` | The identical `lifecycle-pods` argument array. |

The three per-Pod rows repeat for every `observations.pods` entry, in exactly that
array's order, between initial selection and final recheck. Any observation Pod
order is permitted when the receipts follow it. Summary-array order has no effect.
The receipt purpose and `exec` argument must bind the same Pod; later Pods receive
all the same checks. Missing, surplus and duplicate receipts retain inherited
structural refusals. A reordered complete purpose set can satisfy structural
chronology after re-timestamping and still fails the strict sequence comparison.

`METADATA_PROBE` is exactly this single argument; no substitution or suffix is
permitted. The credential variable reference remains a literal declaration,
never a credential value, environment read or instruction to execute it:

```text
if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"
```

`ALPHA_PROBE` is exactly this single argument. Each `\n` below is a backslash and
letter `n` in the argument, with no embedded newline:

```text
printf "%s\n%s\n" "${AccessTelemetryLifecycle__ComponentIsAlpha-__MISSING__}" "${AccessTelemetryLifecycle__AllowAlphaComponent-__MISSING__}"
```

## Refusals and compatibility

Refusals use `InterchangeFormatError` with bounded, content-free messages and
suppressed exception context. They include no offending scope, argument, probe,
source path, credential or capture content.

| Boundary | Refusal |
| --- | --- |
| Existing structural/hash/chronology, producer arguments, neutral markers or pin checks | The unchanged inherited code, including `artifact-literal-mismatch` and `c115-image-pin-mismatch`. |
| Receipt purposes differ from the required complete sequence | `c115-command-sequence-mismatch`. |
| A complete correctly ordered receipt has any different argument array | `c115-command-arguments-mismatch`. |

A correctly rehashed command targeting another context, namespace, selector, Pod,
container, executable or operation cannot return a strict inspection. Existing
`parse_capture` and `inspect_c115_observation_pins` consumers keep their prior
behavior. Simplified structural fixtures can still return pin inspections while
this separate strict API refuses their argument grammar. No existing accepting
consumer, registry, deployed refusal or producer is modified.

## Proof limits and remaining work

A match proves only consistency of untrusted declarations. It proves no command
execution, authentic actual target, source provenance or eligibility, stream
content, Pod-selection completeness, source/archive custody, image authenticity,
freshness, independent authority, registration, gate acceptance or C1 credit.
`dirty-development` declarations remain inspectable; independent source checks
still own cleanliness and retained source byte identity.

The API performs no filesystem, process, network, ambient configuration or clock
access. It contacts no target, injects no fault, purges no data and publishes no
artifact. This pure API needs no current owner record. Any separate current owner
inspection must reuse `dispatch_owner_run_files`; that inspection grants no
execution authority. An exact declaration match cannot authorize a collector,
qualification session or accepting consumer. Producer/verifier/command/cleanup
registration, integrated authority/custody/stream proofs, one-Pod source fixture
registration and all-gate acceptance remain separate outstanding work.

## Reproducible local verification

From the repository root:

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture*.py' -v
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v
git diff --check
```

The new tests author independent complete argv fixtures, cover one/two/three Pods
and their order, rehash substituted arguments, distinguish old inspection success
from new refusal, and retain exact input bytes. Each pure API case runs under the
existing counted filesystem/process/network/environment/clock denial barriers;
known prebound aliases are denied too. Deliberate barrier probes verify detection;
all API cases require zero dependency attempts. The full affected lane retains
its existing isolated local TLS fixtures and contacts no live authority or target.

Execution, review and protected-state preservation receipts belong to the bounded
slice's [verification archive](../../_bmad-output/implementation-artifacts/tests/27-4-capture-command-verification/verification-receipts.zip)
and [specification](../../_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-17.md).
Fixture success grants no operational authority or status transition outside this
local slice.
