# EVAL-01 peer certificate correction — proposed operation

Prepared 2026-11-16. Target: context `jpiquot@local`, namespace `openbao`,
release/StatefulSet `hexalith-keys`. Scope is EVAL-01 only. **Not approved or apply-ready.**

## Ownership and correction

`Hexalith/Hexalith.Memories` owns `deploy/openbao/values.yaml`, including the
listener Secret reference, CA path and peer-0 `retry_join` identity. The live
`ConfigMap/hexalith-keys-config`, managed under Helm release `hexalith-keys`,
declares peers 0/1/2 and their respective full DNS names. Its additional peers
are an existing owned divergence. This operation changes no HCL or Helm inputs.

Platform Operations (`jpiquot`) owns the out-of-repository `openbao-server-tls`
certificate/key and `hexalith-keys-pki` custody. No cert-manager Certificate exists
in this namespace. Bounded local and connected-source searches found consumers,
but no authoritative issuing/renewal repository or secure install/restore procedure.
`tools/production-deployment-openbao.ps1` creates an unrelated disposable CA and
single-server test deployment; it must not supply this platform's certificates.

Approved ownership-only discovery on 2026-11-17 Europe/Paris confirmed the API
endpoint is `192.168.1.30:6443`. Both Secrets report no ownerReferences and
`kubectl-create` at `2026-07-19T13:38:40Z`, followed by metadata updates. This
identifies an imperative bootstrap operation; it does not identify its caller,
script, repository or restore procedure. The existing `openbao-internal-pki`
ClusterIssuer signs application certificates through
`pki-int/sign/hexalith-memories-services`; its public CA has a different identity
from the server leaf's issuer. Do not treat it as the original server issuer.
See [approved metadata evidence](../../_bmad-output/implementation-artifacts/tests/31-1-eval-01-secret-ownership-2026-11-17.md).
The approved read exception covered only owner and manager metadata, excluding
Secret contents and annotations. It supplied no signing or live-change authority.

All three current listeners present the same public leaf, whose DER SHA-256 is
`c0c9260f14095b3d62138d27473505cf2c7bf68524efe55df1b29694ee13f884`.
Its SANs cover the Service, localhost, 127.0.0.1 and Pod-0, but omit Pods 1/2.
The proposed [SAN configuration](../../deploy/openbao/server-tls-identities.cnf)
retains every observed SAN and adds the four equivalent short/full DNS forms
for each missing peer. Integrate it into the real issuing/renewal source;
merely saving a CSR configuration here does not repair renewal.

Reissue only the shared server leaf under the **existing intended CA**. Preserve
CA bytes/trust, key custody and the existing server issuance policy. The custodian
must verify that issuance actually copies the requested SAN extensions, that the
leaf/key match, and that the signed chain verifies for all three full peer names
and existing Service names. A matching issuer CN alone proves no CA identity.
Keep each live `leader_tls_servername` equal to its own peer FQDN and keep
`leader_ca_cert_file` at `/openbao/userconfig/openbao-server-tls/ca.crt`.

## Required information before approval

The owner must identify the issuing repository/procedure and exact revision,
the intended CA's independently trusted public fingerprint, and its credential-safe
install **and restore** commands. The procedure must preserve the current valid
leaf/key pair in restricted owner custody, atomically replace the leaf/key with
unchanged `ca.crt`, preserve Secret metadata/permissions, and support restoring
the original pair. Neither old nor new Secret payloads may enter this repository,
agent context, command arguments, environment, logs, evidence or shell history.

These commands cannot yet be written honestly: there is no identified issuer,
protected input location, restore transaction or custodian receipt. Do not invent
them, export a Secret as a backup, create a replacement CA, adopt cert-manager,
or use a temporary verifier's PKI. Resolve this information gap, insert the exact
owner procedure references and commands in this plan, then obtain approval of the
complete issuance/install/reload/rollback operation **before any live mutation**.

## Rollout sequence after the complete plan is approved

1. Record approval provenance and UTC time outside the attested historical reports.
   Recheck the source revision, image IDs, public leaf fingerprints, live HCL hashes,
   readiness and active/standby labels using the recorder below with a new output
   filename. Stop on drift from the approved preflight. All three must be healthy;
   peer-1/2 hostname failures are the expected pre-change exception.
2. The custodian updates the real issuing/renewal source and executes the approved
   issuance/install transaction above. Record only its non-secret transaction ID,
   source revision, timestamp, old/new public leaf fingerprints, unchanged trusted
   CA fingerprint, key-match/chain-validation result and restore receipt. Installing
   a shared Secret can affect every mounted Pod; do not treat it as a Pod-only change.
3. Confirm Secret projection has completed using the custodian's approved procedure.
   OpenBao 2.6 supports certificate reload from its original paths on SIGHUP.
   No restart, deletion, `rollout restart`, Helm upgrade/rollback, join or Raft
   membership operation is authorized by this certificate-only plan. OnDelete and
   existing configuration drift make an unrelated restart inappropriate here.
4. Reload one verified OpenBao process at a time, standbys first, active last.
   Preflight shows standby order **0, 2**, then active **1**; rederive the order at
   execution and stop for a revised plan if roles/identities change. PID 1 is `sh`,
   so **do not signal PID 1**. This is the proposed command for each selected Pod:

   ```bash
   reload_peer_certificate() {
     kubectl --context=jpiquot@local --request-timeout=12s -n openbao \
       exec "hexalith-keys-$1" -c openbao -- \
       env -i PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
       HOME=/nonexistent /bin/sh -eu -c '
         set --
         for entry in /proc/[0-9]*/comm; do
           read -r name < "$entry" || continue
           case "$name" in
             bao) process=${entry#/proc/}; process=${process%/comm}; set -- "$@" "$process" ;;
           esac
         done
         [ "$#" -eq 1 ] && [ "$1" -gt 1 ] || exit 70
         [ "$(readlink "/proc/$1/exe")" = /usr/bin/bao ] || exit 71
         kill -HUP "$1"
       '
   }
   ```

   Run `reload_peer_certificate 0`, then the per-peer recorder with `--peer 0`;
   require all three probes to pass and its presented leaf fingerprint to equal
   the custodian's approved new fingerprint. Confirm all three Pods remain Ready
   and no process restarted. Repeat for 2, then 1, with a fresh capture at each
   stage. Capture exact command argument arrays, UTC start/end and return codes
   for the reload commands in the new execution receipt. Do not put the above
   three reload calls in an unattended loop.
5. On a failed reload, wrong leaf, lost readiness or any Service control failure,
   stop before the next Pod. If the old leaf is still served, allow up to 180
   seconds for projection, retrying this Pod's reload/check at most every 10
   seconds. Stop at the deadline; do not advance or restart as a workaround.
6. Run the full recorder without `--peer`, using `--phase post-rollout`. Require
   **9/9 successful healthy CA-verifying status probes**, the approved new public
   leaf on every listener, unchanged intended CA receipt, unchanged runtime image
   IDs and HCL fingerprints, and no unexpected Pod/restart/role change. Append
   the exact resulting observations and approval/custodian receipts to a new
   dated evidence addendum/companion. A label or an old timestamp supplies no
   post-rollout result.

## Exact read-only verification invocation

```bash
PYTHONDONTWRITEBYTECODE=1 python3 tools/openbao/verify_peer_tls.py \
  --phase post-rollout \
  --output artifacts/story-31-1/eval-01-peer-tls-2026-11-16/post-rollout.json
```

Use a previously nonexistent output filename. For staged checks add `--peer 0`,
`--peer 2` or `--peer 1` and use distinct `after-peer-N.json` filenames. The script
returns 1 for failures, including the known preflight mismatch. Every status
command runs in a cleared environment with absent HOME and explicit CA and
`-tls-skip-verify=false`; controls change the expected TLS name to the Service
while retaining the same target, plus a separate Service-route request per source.
Only installed tools run in the Pods. Public leaf inventory uses bounded loopback
port-forwards that are terminated and awaited; its unverified handshake is only
certificate metadata, with CA identity established by the separate status probes.

## Rollback and stop conditions

Rollback is **not Helm rollback**: Helm owns no copy of this externally supplied
leaf/key. On any stop condition, freeze further reloads and have the custodian
execute the approved restore transaction to atomically reinstall the original
leaf/key and unchanged CA, retaining the failed candidate only in restricted
custody. Confirm projection, then use the same guarded reload command for already
changed Pods in reverse execution order, one Pod and verification capture at a
time. Do not signal Pods that still serve the old leaf unnecessarily.

Use the full recorder with `--phase rollback` and a new filename. Require the old
public leaf SHA-256 above on all listeners, 3/3 Ready, unchanged image/HCL identity,
and all six Service controls/routes passing. Peer-0 DNS must pass; peer-1/2 DNS
failures reproduce the known original defect, so a rollback recorder exit 1 is
expected and must be explained rather than labelled a clean remediation. If
Service verification or health cannot be restored, stop and escalate to Platform
Operations; this approval would authorize no seal/data/credential recovery action.

## Evidence and authority limits

Preserve every existing attested report and covered prefix byte-for-byte. Record
EVAL-01 technical disposition separately after actual approved execution. *(Updated 2026-11-28, Epic 31 retro A2/A5: on 2026-11-22 Jérôme Piquot closed Story 31.1 by sole-owner risk acceptance for C4b, C5b and C7 without independent countersignature; EVAL-01 remains open with Platform Operations and is registered in `deferred-work.md` as `DW-791`.)* Advisory, seal, ingress,
audit, recovery and other findings stay outside this operation. A successful peer
status check proves neither a real retry_join nor Raft failover or Production qualification.

Sources checked 2026-11-16: [OpenBao 2.6 listener reload](https://openbao.org/docs/2.6.x/configuration/listener/tcp/),
[Raft TLS identity](https://openbao.org/docs/2.6.x/configuration/storage/raft/),
and [Kubernetes Secret projection](https://kubernetes.io/docs/concepts/configuration/secret/).
