# Offline Candidate Verification

Executed 2026-10-04 from source revision `5b43fe2f8a0f04dc021921a077dff1a573c2ce5e`.
These checks validate an inactive byte proposal. They grant no target eligibility,
runtime capability, security qualification or Production acceptance.

From the repository root, rerun the retained check:

```bash
PYTHONDONTWRITEBYTECODE=1 python3 _bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate/verify_candidate.py --repo-root .
```

The same check first ran successfully against the staged candidate, and was
repeated after copying these files into the planning package. It verifies:

- All eighteen base64 archives against their exact sizes and SHA-256 hashes.
- PostgreSQL, OpenBao and Dapr index-to-platform-to-config descriptor chains;
  the old PostgreSQL index's unique Linux/amd64 child; and the chart content/config chain.
- Canonical profile and mutation-manifest hashes, all three proposed workload
  file hashes, OpenBao values/render and all twelve current source bindings.
- The retained historical profile equals the current constructor; capability and
  500 events/s workload requirements are unchanged; Production source remains disabled.

The original check passed four nonzero groups. Review hardening on 2026-10-04
adds explicit failures that remain active under Python optimization, strict
duplicate-rejecting JSON, exact envelope/version/alias validation and canonical/
receipt-bound archive chains. It also verifies the separately retained exact
[historical C1.15 packet bytes](historical-c1-15-packet-bytes.json), their secret
safety, original hash and runtime/image observations without reading the original
workstation path. The original immutable packet is unchanged. The hardened check
passes five nonzero groups in normal and optimized package replay. Six focused
verifier tests cover malformed envelopes, duplicate JSON, internally consistent
alternative image/chart chains, receipt drift and retained-packet/hash/observation
failures in both normal and `python -O` execution:

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p candidate_profile_verification_test.py -v
```

The check fails explicitly on any mismatch.
It has no network, process execution, credentials or target access.

The parent independently decoded the retained chart into a new temporary
directory and reran `helm template` and strict `helm lint` using the checksum-bound
Helm 3.22.0 binary. Both exited 0 with empty stderr. The new raw render matched
`16001617572d803bc9b53731a54c8b564e3c6b5a3d9b28b34a727dad9d435303`.
Exact commands/results are in [offline-render-replay.json](offline-render-replay.json).
Original acquisition commands remain in [commands-and-results.json](commands-and-results.json).

To replay the render elsewhere, decode `openbao-0.29.6.tgz` from
[registry-source-evidence.json](registry-source-evidence.json), verify its retained
hash, then run checksum-verified Helm 3.22.0:

```bash
helm template hexalith-keys /tmp/openbao-0.29.6.tgz --namespace openbao --values _bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate/openbao-values.candidate.yaml --kube-version 1.34.9
helm lint /tmp/openbao-0.29.6.tgz --values _bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate/openbao-values.candidate.yaml --kube-version 1.34.9 --strict
```

Preserve raw stdout bytes before hashing. Lint stdout includes its temporary
chart path, so its textual hash is path-dependent. No files were rendered into
active deployment resources and no Helm release or cluster was contacted.

The complete final tooling suite passed 55 methods in 833.295 seconds, including
all six candidate-verifier methods. Both standalone checker modes passed all five
groups again. [Exact final raw logs and receipts](../final-verification-evidence.json)
retain the execution evidence and final verifier source hash. This does not
change the candidate profile hash or its pending-adoption state.
