"""Pure inspection of the published PG2 C1.15 declared-observation pins.

Matching untrusted declarations supplies no execution, source eligibility,
custody, image provenance, registration or gate acceptance evidence.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from access_telemetry_c1_interchange import JsonSnapshot, _literal, _refuse, parse_capture


_IDENTITY_PINS = (
    ("profileId", "PG-ONPREM-2"),
    ("profileIdentity", "postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-qualification-kvm-v3"),
    ("profileSha256", "95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb"),
    ("workloadId", "adr-27.1-two-writer-500eps"),
    ("workloadSha256", "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f"),
)
_TARGET_PINS = (
    ("context", "owner@memories-qualification-27-4"),
    ("namespace", "hexalith-memories"),
    ("selector", "app.kubernetes.io/name=memories-access-telemetry"),
    ("appId", "memories-access-telemetry"),
    ("actorType", "AccessTelemetryLifecycleActor"),
)
_RUNTIME_PIN = "1.18.1"
_IMAGE_DIGEST_PINS = frozenset({
    "sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8",
    "sha256:edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b",
})
_POD_IDENTITY_OUTPUT = "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"
_METADATA_PROBE = 'if [ -z "${DAPR_API_TOKEN:-}" ]; then echo "required runtime credential unavailable" >&2; exit 72; fi; metadata="$(wget -qO- --timeout=5 --header="dapr-api-token: ${DAPR_API_TOKEN}" http://127.0.0.1:3500/v1.0/metadata)" || exit $?; case "$metadata" in *"$DAPR_API_TOKEN"*) echo "secret-shaped-output" >&2; exit 73;; esac; printf "%s" "$metadata"'
_ALPHA_PROBE = 'printf "%s\\n%s\\n" "${AccessTelemetryLifecycle__ComponentIsAlpha-__MISSING__}" "${AccessTelemetryLifecycle__AllowAlphaComponent-__MISSING__}"'


@dataclass(frozen=True, slots=True)
class C115ObservationInspection:
    """Retained declaration bytes and Pod-derived identities; no pass verdict.

    ``pod_count`` and ``pod_names`` are derived from the structurally checked
    Pods, preserving their input order. ``capture`` retains the caller's snapshot.
    """

    capture: JsonSnapshot = field(repr=False)
    pod_count: int
    pod_names: tuple[str, ...]


@dataclass(frozen=True, slots=True)
class C115CommandInspection:
    """Exact declaration snapshot and derived command data; no authority.

    Pod names and command data preserve observation/receipt order. Arguments
    are immutable tuples, including each complete fixed probe as one argument.
    ``capture`` retains the caller's identical snapshot and excludes its content
    from this result's representation.
    """

    capture: JsonSnapshot = field(repr=False)
    pod_count: int
    pod_names: tuple[str, ...]
    command_count: int
    command_purposes: tuple[str, ...]
    command_arguments: tuple[tuple[str, ...], ...] = field(repr=False)


def inspect_c115_observation_pins(snapshot: JsonSnapshot) -> C115ObservationInspection:
    """Inspect one explicit immutable capture against existing literal pins.

    Reuse the unchanged structural reader before any pin comparison. Require
    observed evidence, then compare exact strings without trimming or folding.
    Raw image prefixes and suffix agreement follow that reader; approved index
    and child digests may coexist. Dirty-development declarations are inspectable
    here, while independent source eligibility still refuses them.
    """
    capture = parse_capture(snapshot)
    value = capture.value
    _literal(value["producerStatus"], "observed")
    for name, expected in _IDENTITY_PINS:
        _literal(value[name], expected)
    for name, expected in _TARGET_PINS:
        _literal(value["target"][name], expected)
    pods = value["observations"]["pods"]
    for pod in pods:
        _literal(pod["runtimeVersion"], _RUNTIME_PIN)
        if pod["sidecarImageDigest"] not in _IMAGE_DIGEST_PINS:
            _refuse("c115-image-pin-mismatch")
    names = tuple(pod["pod"] for pod in pods)
    return C115ObservationInspection(capture, len(pods), names)


def inspect_c115_command_declarations(snapshot: JsonSnapshot) -> C115CommandInspection:
    """Compare pinned declarations to the producer's exact read-only grammar.

    Reuse pin inspection, including unchanged structural producer arguments,
    command hashes, purpose sets and chronology. Compare argv without parsing,
    executing, normalizing or consulting ambient configuration. Require context,
    selection, version/metadata/alpha for each Pod in observation order, recheck.
    A match establishes declaration consistency only, never actual execution.
    """
    observation = inspect_c115_observation_pins(snapshot)
    value = observation.capture.value
    target = value["target"]
    scope = ("--context", target["context"], "-n", target["namespace"])
    selection = scope + ("get", "pods", "-l", target["selector"], "-o", _POD_IDENTITY_OUTPUT)
    expected = [("current-context", ("config", "current-context")),
                ("lifecycle-pods", selection)]
    for name in observation.pod_names:
        execute = scope + ("exec", name, "-c")
        expected.extend((
            ("daprd-version:" + name, execute + ("daprd", "--", "/daprd", "--version")),
            ("metadata:" + name, execute + ("lifecycle", "--", "/bin/sh", "-ec", _METADATA_PROBE)),
            ("alpha-opt-in:" + name, execute + ("lifecycle", "--", "/bin/sh", "-ec", _ALPHA_PROBE)),
        ))
    expected.append(("lifecycle-pods-recheck", selection))
    commands = value["commands"]
    purposes = tuple(command["purpose"] for command in commands)
    if purposes != tuple(purpose for purpose, arguments in expected):
        _refuse("c115-command-sequence-mismatch")
    arguments = tuple(command["arguments"] for command in commands)
    if arguments != tuple(arguments for purpose, arguments in expected):
        _refuse("c115-command-arguments-mismatch")
    return C115CommandInspection(observation.capture, observation.pod_count,
                                 observation.pod_names, len(commands), purposes, arguments)
