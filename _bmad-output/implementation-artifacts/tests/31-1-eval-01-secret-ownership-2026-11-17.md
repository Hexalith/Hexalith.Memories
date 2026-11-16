# Story 31.1 — EVAL-01 approved Secret ownership inspection

Prepared 2026-11-17 Europe/Paris; execution timestamps are UTC on 2026-11-16.
Author: Codex, AI assistant. Source revision:
`341ae091bc9a94bfab0c7741314419713188ae8d`.

The user answered **“yes”** to the request to inspect only owner and manager metadata
on `openbao-server-tls` and `hexalith-keys-pki`, excluding contents and annotations.
That approval is limited to these reads. It grants no signing, Secret modification,
certificate reload, deployment or independent sign-off authority.

## Executed result

Target: context `jpiquot@local`, API endpoint `https://192.168.1.30:6443`, namespace
`openbao`. Exact commands, UTC start/end, return codes and selected output appear
in the [JSON companion](31-1-eval-01-secret-ownership-2026-11-17.json).

| Secret | UTC probe window | Exit | Owner references | Reported field managers |
|---|---|---|---|---|
| `openbao-server-tls` | 23:47:33.741768–23:47:33.871077, 2026-11-16 | 0 | none returned | `kubectl-create`, `kubectl-patch`, `kubectl-annotate`, `kubectl-label` |
| `hexalith-keys-pki` | 23:47:33.871112–23:47:34.070972, 2026-11-16 | 0 | none returned | `kubectl-create`, `kubectl-annotate`, `kubectl-label` |

Both report `kubectl-create Update 2026-07-19T13:38:40Z`. The TLS Secret's patch
has the same timestamp. Annotation and label updates report `13:38:41Z` that day.
The annotation **values** were not read. The selected fields exclude `.data`,
`.stringData`, annotations, private keys, credential values and field ownership
payloads. No signing or token operation was attempted.

These results identify an imperative bootstrap operation rather than a reported
controller owner. `kubectl-create` may have been invoked by a human or automation;
the manager name does not establish the caller, source repository, issuing script,
key custody or rollback procedure. No undocumented provenance is inferred.

## Consequence for remediation

The cluster's existing application issuer and public PKI identity are documented in
the [preceding discovery addendum](31-1-eval-01-cluster-source-discovery-2026-11-17.md).
They do not establish how to reissue the current OpenBao listener leaf under its
intended unchanged CA. Kubernetes ownership metadata does not contain that procedure.

The [SAN proposal](../../../deploy/openbao/server-tls-identities.cnf) and
[rollout/rollback plan](../../../docs/operations/openbao-peer-tls-eval-01.md) remain
prepared. The plan and draft spec now record this discovery; their prior bytes were
saved under `artifacts/story-31-1/eval-01-cluster-source-discovery-2026-11-17/` before
editing. The actual authoritative issuing source and protected install/restore
procedure still need to be supplied by the original CA custodian. No certificate
issuance, live mutation or rollout occurred, and no post-rollout result is claimed.

All eighteen earlier evidence/source/tracking files inventoried in the companion
retain their bytes, including the attested reports and both preceding addenda.
C4b/C5b/C7 remain pending / not complete without completion dates. Story 31.1 stays
in-progress; EVAL-01 remains unresolved pending an approved, executed correction.
