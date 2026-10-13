# Offline PG2 C1.15 retained output inspection

Prepared on **2026-10-13** for the bounded Story 27.4 retained output slice.
The pure inspector binds caller-supplied stdout/stderr bytes to every exact
command receipt, then derives the declared observations from those outputs.
It returns frozen consistency data. Production remains disabled, parent Story
27.4 stays in-progress and A41 stays open. Historical dispositions and integrated
I2–I6 / P1–P7 holds remain unchanged.

## API and immutable output

`tools/access_telemetry_c1_capture_streams.py` exposes:

```python
C115CommandStreams(purpose: str, stdout: bytes, stderr: bytes)
inspect_c115_retained_streams(
    snapshot: JsonSnapshot,
    streams: tuple[C115CommandStreams, ...],
) -> C115StreamInspection
```

The caller supplies one explicit immutable capture and an exact built-in tuple
containing one exact `C115CommandStreams` instance per target command, in receipt
order. Each purpose must exactly equal its receipt purpose. stdout and stderr
must be built-in `bytes`; lists, iterators, mutable buffers, memory views and
subclasses are refused. Git `sourceCommands` are outside this stream API.

The inspector first reuses unchanged `inspect_c115_command_declarations`,
including structural parsing, neutral markers, receipt/source chronology and
hashes, exact command grammar/order and PG2 identity/runtime/image pins. It
preserves every inherited refusal. Blocked captures remain ineligible. The
existing declaration and pin inspection APIs retain their prior behavior.

The result is a frozen, slotted dataclass:

| Field | Meaning |
| --- | --- |
| `capture` | The identical caller snapshot and its exact bytes, digest and recursively frozen declarations; omitted from the representation. |
| `streams` | The identical caller tuple and pair objects, preserving every exact stdout/stderr byte occurrence; omitted from the representation. |
| `pod_count` | Count of the complete initial Running Pod identities derived from the supplied selection output. |
| `pod_names` | Immutable tuple of initial Running Pod names, in frame/observation order. |
| `command_count` | Number of bound receipts and pairs, equal to `3 * pod_count + 3`. |

Pair representations omit purpose and both streams, including for refused input.
The result has no acceptance flag, pass verdict, authority or execution handle.

## Bytes, limits and credential safety

Every stdout and stderr SHA-256 is computed over its untrimmed bytes and must
match the receipt. Existing source-receipt checks also bind selection identity
hashes, version/alpha/context stream hashes and metadata projection hashes.
Whitespace, Unicode spelling, JSON escapes and newlines remain exactly retained.

| Limit | Rule |
| --- | --- |
| Each stdout or stderr | At most 1 MiB, including whitespace and diagnostics. |
| One inspection | At most 32 MiB for capture bytes plus all stdout/stderr occurrences, including repeated identical bytes. No deduplication discount. |
| JSON | Maximum container depth 30; decoded duplicate keys are forbidden at every level. |
| JSON integer lexemes | At most 380 characters including a minus sign: 380 positive digits or 379 negative digits. Parsing is independent of Python's configurable integer conversion limit. |
| Encoding | Strict UTF-8; unpaired surrogates in decoded JSON strings are forbidden. |

Both streams receive raw and decoded credential scans, including discarded
fields, JSON fragments inside diagnostics, quoted strings and embedded JSON
strings. Unicode escapes and escaped slashes cannot hide credentials. Nonempty
credential fields and credential `name`/`value` pairs are refused. Pair keys
match case-insensitively; a credential pair rejects every non-null non-string
value, including empty arrays/objects, and every nonempty string. Empty strings
and null pair values can be harmless. Direct credential fields retain their
distinct rule: empty strings, arrays/objects and null values can be harmless.
Refusals never include offending content or raw exception details.

Harmless extra fields and finite JSON numbers are supported. Nonfinite constants
and numbers overflowing a finite floating-point value are refused, including
standalone numeric documents and arrays on stderr. Ordinary diagnostic prose
that merely mentions such words or numbers is allowed. Safe nonempty
stderr, including ordinary diagnostic text and strict JSON fragments, is allowed.
Malformed, duplicate or deep recognizable JSON fragments are refused on either
stream; stdout must also satisfy the purpose's complete output grammar. An
unmatched diagnostic quote or earlier harmless object cannot hide a later
recognizable fragment. Quoted diagnostic prose need not use JSON backslash
escapes; an invalid string-token decode is ignored after raw safety checks,
while recognizable object/array fragments retain strict parsing. Literal braces
and bracket prose remain harmless. Candidate traversal advances monotonically;
the tests bound scanned character work without consulting a clock.

## Derived observations and producer compatibility

Hashing precedes trimming. Parsing follows the producer's .NET whitespace
trimming; it does not use Python's broader information-separator trimming.

| Output | Required derived meaning |
| --- | --- |
| `current-context` | Trimmed output exactly equals the pinned declared context. |
| `lifecycle-pods` | Every successive JSON value is a two-object `[metadata,status]` frame. Complete initial Running identities and order match all declared Pods. |
| `daprd-version:POD` | Trimmed output exactly equals that Pod's pinned runtime version. |
| `metadata:POD` | One strict JSON object derives the exact app ID, runtime version, scheduler addresses, actor types and enabled features, agreeing with that Pod's observation and metadata projection hash. |
| `alpha-opt-in:POD` | Trimmed output splits into exactly two lines with LF or CRLF; .NET-compatible explicit booleans match both declared alpha values. |
| `lifecycle-pods-recheck` | The complete Running identity map agrees with initial selection, independent of frame order. |

Frames may be adjacent or separated by JSON whitespace. Initial selection keeps
Running frame order, requires unique names/UIDs and rejects missing or surplus
Running identities. Each Running Pod must have its pinned app label, no non-null
deletion timestamp, exactly one `Ready=True` condition, and exactly one ready
`lifecycle` and `daprd` container with valid raw image IDs. The daprd raw image
and digest must match the declared observation. Later Pods receive identical
checks. Initial non-Running frames still need a valid phase and status arrays,
as required by the producer.

Stable rechecks bind name, UID, app label, deletion state, Ready condition and
both lifecycle/daprd container names, boolean readiness and raw image IDs.
Container order and frame order may change. Harmless annotations, timestamps,
extra conditions/containers and non-Running identities do not enter that stable
map; every field still receives credential and JSON safety checks.

Metadata accepts either `scheduler.connectedAddresses` or
`scheduler.connected_addresses`, and refuses both or neither. Arrays must be
duplicate-free, use the producer's bounded ASCII grammars and match exact
case-sensitive membership. Scheduler ports stay within 1–65535. Actor types
include the declared lifecycle actor; enabled features may be empty. The
producer sorts these arrays using a case-sensitive culture comparison. This
inspector compares membership without reading ambient culture and uses the
already checked declared array order when checking the allowlisted projection
hash. Raw array order therefore does not create a contradiction. The declared
array order is hash-bound, but this inspection does not independently establish
that the producer normalized that order for its locale. Exact membership and
projection agreement do not supply locale-normalization evidence.

Boolean parsing permits case and boundary whitespace, including .NET
`Boolean.TryParse` boundary NUL behavior. It refuses missing values, numeric
booleans and extra internal lines. Output trimming never changes stored bytes.

The offline compatibility test runs the unchanged producer against its existing
fake kubectl harness, instrumenting only that disposable transport to retain the
actual bytes. It covers one/two/three Pods, adjacent and separated frames,
Unicode trimming, safe stderr diagnostics, extra finite numbers, array sorting,
boolean spellings, true/true and false/true alpha successes, signed integer
boundaries, quoted prose with non-JSON backslashes and reordered rechecks with
incidental metadata changes.
This fixture supplies no live target or execution evidence.

## Content-free refusals

All refusals use `InterchangeFormatError` and bounded codes. New stream refusals
have null exception cause and context; existing structural, pin and command
declaration codes are unchanged.

| Boundary | Code |
| --- | --- |
| Pair purpose type | `c115-stream-purpose-required` |
| Stream immutable bytes type | `c115-stream-bytes-required` |
| Exact tuple or exact pair type | `c115-stream-tuple-required` |
| Missing, surplus, duplicate or reordered purposes | `c115-stream-sequence-mismatch` |
| Per-stream size | `c115-stream-byte-budget-exceeded` |
| Capture plus occurrence size | `c115-stream-aggregate-byte-budget-exceeded` |
| Raw SHA-256 differs | `c115-stream-hash-mismatch` |
| Invalid UTF-8 | `c115-stream-invalid-utf8` |
| Decoded unpaired surrogate | `c115-stream-unpaired-unicode-surrogate` |
| Malformed JSON | `c115-stream-invalid-json` |
| Duplicate decoded JSON keys | `c115-stream-duplicate-json-field` |
| JSON/embedded-string nesting exceeds depth 30 | `c115-stream-json-depth-exceeded` |
| Nonfinite JSON number | `c115-stream-nonfinite-json-number` |
| Integer lexeme exceeds 380 characters including minus | `c115-stream-integer-lexeme-too-long` |
| Raw or decoded credential content | `c115-stream-secret-shaped-content` |
| Output grammar, complete initial identities, observations or projection differs | `c115-stream-observation-mismatch` |
| Valid rechecked Running identities differ from initial identities | `c115-stream-recheck-mismatch` |

A correctly rehashed contradictory output can pass declaration inspection while
this retained output API refuses it. A malformed or unstable recheck can refuse
with a JSON, safety or observation code before the stable map comparison.

## Remaining acquisition, custody and registration

The match proves consistency of supplied bytes and declarations only. It does
not establish acquisition, command execution, true selection completeness on a
cluster, source/image provenance or eligibility, archive custody, freshness,
independent authority, registration, gate acceptance or C1 credit. The complete
Running comparison concerns the supplied output. A fabricated or incomplete
output that is internally consistent remains untrusted.

The API performs no filesystem, process, network, configuration or clock access.
No producer, consumer, registry or deployed refusal changes. No capture is
published and no target is contacted. Producer/verifier/command/cleanup
registration, trusted stream acquisition, source/archive custody, integrated
authority and all-gate acceptance remain separate work. Any separate owner
inspection reuses `dispatch_owner_run_files` and grants no execution authority.

## Reproducible verification

From the repository root:

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture*.py' -v
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -v
git diff --check
```

The retained output tests independently author bytes and rehash contradictions.
Every pure API case runs under counted filesystem/process/network/environment/
clock barriers, including known prebound aliases. Deliberate barrier probes
verify detection; inspector cases require zero dependency attempts. Exact
per-stream/aggregate boundaries, repeated occurrence charging, immutable results,
all later Pods, completeness and unsafe discarded fields on stderr are covered.
The affected lane retains its existing isolated fixtures and contacts no live
authority or target. Phase and review receipts belong to the bounded slice's
[specification](../../_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-18.md).
