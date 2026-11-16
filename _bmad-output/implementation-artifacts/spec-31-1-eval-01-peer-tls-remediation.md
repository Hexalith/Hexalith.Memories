---
title: 'Story 31.1 EVAL-01 peer TLS hostname remediation'
type: 'bugfix'
created: '2026-11-16'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The shared deployed certificate covers Pod-0 and the Service, but not
Pod-1/Pod-2. Live retry_join verifies those missing peer identities, producing the
EVAL-01 CA-verifying hostname failures.

**Approach:** Integrate the prepared SAN additions into the real certificate
issuing source under the existing CA. Approve a complete secure install/rollback
plan, reload the certificate without restarting the platform, then record exact
fresh peer and Service verification against the resulting profile.

## Boundaries & Constraints

**Always:** Limit work to EVAL-01. Preserve all attested reports, covered prefixes,
secret-safety rules and peer-specific CA verification. Keep C4b/C5b/C7 pending
without completion dates and Story 31.1 in-progress until authentic independent
sign-off. Approval must precede live issuance/install/reload. The owner must
identify the authoritative renewal source and protected install/restore procedure.

**Never:** Read Secret payloads, keys, credentials or audit contents; invent owner
approval or independent sign-off; replace the CA; use the disposable verifier PKI;
weaken hostname verification; upgrade images/chart; reconcile unrelated live HCL
drift; restart Pods or change Raft membership as a certificate-only workaround.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Corrected identity | Existing CA signs all intended names | All peer DNS and Service checks pass | Reject wrong leaf/CA or dropped existing SAN |
| Install/reload failure | Wrong leaf, stale projection or unhealthy control | Stop before next Pod | Restore original leaf/key through owner custody |
| Incomplete authority | Issuing/restore source or approval absent | Prepare local artifacts only | No live mutation |

</frozen-after-approval>

## Open Questions

- What repository/path/revision or Platform Operations procedure authoritatively
  issues and renews `openbao-server-tls`, and what are its exact credential-safe
  install/restore commands and intended public CA fingerprint? Options: identify
  the existing owner procedure (complete and approve the executable plan) / retain
  the proposal locally until the owner supplies it (no live rollout). No private
  key, token or Secret payload is requested.

## Code Map

- `deploy/openbao/values.yaml`: Memories-owned inactive candidate; only peer-0
  retry_join, listener and CA paths. Preserve bytes; live HCL peers 1/2 are an
  already documented divergence, not a reason to deploy the broader candidate.
- `docs/operations/openbao.md`: existing ownership and secret-read restrictions;
  out-of-repository Platform Operations TLS input, no tracked producer.
- `deploy/openbao/server-tls-identities.cnf`: proposed public SAN policy preserving
  all ten observed SANs and adding eight Pod-1/2 DNS names; not an installed issuer.
- `docs/operations/openbao-peer-tls-eval-01.md`: proposed rollout/rollback, exact
  guarded child-process HUP and verification commands; incomplete custodian step
  is explicitly blocked. PID 1 is a shell; OpenBao is its `/usr/bin/bao` child.
- `tools/openbao/verify_peer_tls.py`: read-only argument/timestamp/result recorder;
  nine CA-verifying probes, public leaf metadata, image IDs and HCL fingerprints.
- `_bmad-output/implementation-artifacts/tests/31-1-security-evidence-refresh-2026-11-16.{md,json}`:
  starting evidence, preserved unchanged along with prior attested evaluations.
- `tools/production-deployment-openbao.ps1`: unrelated disposable CA/test topology;
  do not reuse it for the deployed Raft platform.

## Tasks & Acceptance

**Execution:**

- [x] `deploy/openbao/server-tls-identities.cnf` — prepare the public SAN change.
- [x] `tools/openbao/verify_peer_tls.py` — prepare and run the read-only preflight recorder.
- [ ] `docs/operations/openbao-peer-tls-eval-01.md` — resolve the owner issuing and
  restore source, bind exact custodian commands and obtain complete-plan approval.
- [ ] Owner issuing source — apply approved durable SAN policy and credential-safe
  leaf replacement, retaining protected rollback custody; then staged reloads.
- [ ] `_bmad-output/implementation-artifacts/tests/31-1-eval-01-peer-tls-addendum-2026-11-16.{md,json}`
  — retain preparation evidence and append authentic post-rollout results and receipts.

**Acceptance Criteria:**

- Given the original certificate, when preflight runs, then peer-1/2 hostname
  mismatches and successful Service controls are retained without sensitive output.
- Given identified custody and approved exact rollout/rollback, when the owner
  deploys the corrected leaf, then each peer verifies its own DNS name against
  the intended unchanged CA, every Service control passes, and image/HCL identity
  remains within the approved profile.
- Given actual approved execution, when evidence is published, then commands,
  timestamps, approval/custodian provenance and measured results are recorded
  separately while original reports and independent-review states retain their bytes.

## Implementation Notes

Preparation is authorized by the request. No live mutation or rollout approval
is inferred. Intent gap: external issuing and restore source. Irreversibles:
certificate issuance/install and process reload. Footprint: public SAN config,
read-only evidence tool, operation plan, this spec and separate evidence addendum;
the actual issuing-source edit cannot be located yet.

2026-11-17 Europe/Paris: user approved the proposed narrow Secret ownership
metadata read. Both Secrets have no ownerReferences and show `kubectl-create`
at `2026-07-19T13:38:40Z`; no signing/restore procedure was discovered. The API
endpoint is already `192.168.1.30:6443`. The existing ClusterIssuer serves the
application PKI, whose public CA identity differs from the server issuer. See
`tests/31-1-eval-01-secret-ownership-2026-11-17.{md,json}` relative to this spec.
This permission grants neither Secret-content access nor a live rollout. The
issuing-source question and complete-plan approval remain open.

## Spec Change Log

## Review Triage Log

## Verification

- `PYTHONDONTWRITEBYTECODE=1 python3 tools/openbao/verify_peer_tls.py --phase preflight --output <new-evidence-path>`
  — original peer failures expected; no sensitive payload retained.
- `run-with-temp bash artifacts/story-31-1/eval-01-peer-tls-2026-11-16/verify-prepared.sh`
  — synthetic certificate parsing/name and negative controls, recorder syntax and
  failure-output safety; synthetic key material stays below wrapper TMPDIR.
- After approved execution, run the full recorder as specified by the operation plan;
  expect nine successful healthy CA-verifying probes and approved public leaf identity.
