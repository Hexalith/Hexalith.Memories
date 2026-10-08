"""Pure inspection of the published PG2 C1.15 declared-observation pins.

Matching untrusted declarations supplies no execution, source eligibility,
custody, image provenance, registration or gate acceptance evidence.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from access_telemetry_c1_interchange import JsonSnapshot, _literal, _refuse, parse_capture


_IDENTITY_PINS = (
    ("profileId", "PG-ONPREM-2"),
    ("profileIdentity", "postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2"),
    ("profileSha256", "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"),
    ("workloadId", "adr-27.1-two-writer-500eps"),
    ("workloadSha256", "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f"),
)
_TARGET_PINS = (
    ("context", "jpiquot@local"),
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


@dataclass(frozen=True, slots=True)
class C115ObservationInspection:
    """Retained declaration bytes and Pod-derived identities; no pass verdict.

    ``pod_count`` and ``pod_names`` are derived from the structurally checked
    Pods, preserving their input order. ``capture`` retains the caller's snapshot.
    """

    capture: JsonSnapshot = field(repr=False)
    pod_count: int
    pod_names: tuple[str, ...]


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
