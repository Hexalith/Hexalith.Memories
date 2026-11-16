# Story 31.1 — EVAL-01 peer TLS preparation addendum

**Prepared:** 2026-11-16T21:56:24.499583+00:00. **Author:** Codex, AI assistant.
**Source revision:** `341ae091bc9a94bfab0c7741314419713188ae8d`.
**Disposition:** Correction prepared; rollout blocked on unidentified issuing/install/restore source.
**Live rollout:** No approval recorded; no issuance, configuration/Secret mutation, reload or restart executed.

This is new technical evidence for EVAL-01 only. It preserves the attested reports and their covered
prefixes. It supplies no independent sign-off. C4b/C5b/C7 remain pending / not complete, without
completion dates; Story 31.1 remains in-progress. Other technical findings remain open.

## Ownership and proposed correction

The tracked listener and retry_join input is `Hexalith/Hexalith.Memories/deploy/openbao/values.yaml`.
It declares peer-0 only; deployed `openbao/ConfigMap/hexalith-keys-config` declares all three peer
FQDNs with matching `leader_tls_servername` and explicit CA paths. This existing divergence remains
unchanged. Platform Operations (`jpiquot`) owns the externally supplied TLS/PKI Secrets. No
cert-manager Certificate exists in the namespace. Bounded local and connected-source searches
found consumers but no authoritative issuing/renewal source or safe install/restore procedure.
The disposable production verifier's generated CA is unrelated and must not be used.

The proposed [SAN configuration](../../../deploy/openbao/server-tls-identities.cnf) preserves all
ten observed SANs and adds eight short/full DNS entries for Pods 1/2. It must be integrated into
the real issuing procedure under the **same intended CA**. Saving this public configuration alone
is not a durable issuing-source correction. Keep the current per-peer TLS names and CA verification.

The [operation plan](../../../docs/operations/openbao-peer-tls-eval-01.md) records staged reloads,
stop conditions and rollback to the original leaf/key through protected custody. It contains the
exact guarded reload and read-only verification commands. Its issuance/install/restore portion
cannot be made executable until the owner identifies that procedure, its exact revision/commands,
restore receipt and independently trusted public CA fingerprint. The complete plan must be
approved before live action. No live permission is inferred from this evidence request.

## Fresh preflight results

Target: `jpiquot@local`, namespace `openbao`, release `hexalith-keys`.
Window: **2026-11-16T21:53:53.150018+00:00–2026-11-16T21:54:00.836191+00:00**.

```bash
PYTHONDONTWRITEBYTECODE=1 python3 tools/openbao/verify_peer_tls.py --phase preflight --output artifacts/story-31-1/eval-01-peer-tls-2026-11-16/preflight-final.json
```

Exit **1**, expected for the reproduced defect. Exact subprocess argument arrays, UTC start/end,
return codes, projected observations and source hashes are retained in the [JSON companion](31-1-eval-01-peer-tls-addendum-2026-11-16.json).

| Source / target peer | Own peer DNS | Same target, Service TLS-name control | Service-route control |
|---|---|---|---|
| Pod-0 | exit 0; healthy | exit 0; healthy | exit 0; healthy |
| Pod-1 | exit 1; x509 hostname mismatch | exit 0; healthy | exit 0; healthy |
| Pod-2 | exit 1; x509 hostname mismatch | exit 0; healthy | exit 0; healthy |

Every status command clears the environment, sets absent HOME, explicitly uses the installed
`/openbao/userconfig/openbao-server-tls/ca.crt` and `-tls-skip-verify=false`, with no implicit OpenBao
credential. CA material remains inside the installed CLI. No Secret payload or mounted certificate,
private key, token, seal material or audit content was read. Existing Kubernetes transport
credentials authenticate kubectl in the normal way; they are not copied into OpenBao commands.

All three listeners present the same public certificate DER SHA-256:
`c0c9260f14095b3d62138d27473505cf2c7bf68524efe55df1b29694ee13f884`.
Issuer: `CN=Hexalith Keys Internal CA, O=Hexalith`; expiry: `2027-08-20 13:38:39 UTC`.
Its SANs omit Pods 1/2. Public-certificate inventory uses unverified network handshakes solely
for metadata. It supplies no CA authentication by itself; separate direct status probes verify
against the installed CA. All three loopback port-forwards were terminated and awaited.

Every runtime imageID is
`quay.io/openbao/openbao@sha256:11fd73a2102cda9c55d5d881a8c3210303146a7ec1e8ac76f526e175c6d24641`;
verified status reports `2.6.2`. Chart label is `openbao-0.29.5`, not a measured chart digest.
The declared HCL and both observed files on every Pod
(`/openbao/config/extraconfig-from-values.hcl`, `/tmp/storageconfig.hcl`) share SHA-256
`9340daddbfb65afdab4e9400762ff0cd6f858003c61f983a10b0edfe8fcc87db`.
Pod/config identities stayed stable across the probe window. File equality does not prove
process-loaded configuration. The observed active Pod is 1; standbys are 0/2. Separate process
captures resolve `/usr/bin/bao` as PID 12 on each Pod, with `sh` at PID 1. The first process
projection assumed `/bin/bao`; the corrected capture and proposed reload guard use the measured
path. No signal was sent.

## Local validation and preservation

`run-with-temp bash artifacts/story-31-1/eval-01-peer-tls-2026-11-16/verify-prepared.sh`
exited **0**: all 18 intended DNS/IP entries verify in a synthetic certificate, wrong-host and
wrong-CA controls fail as intended, recorder syntax parses and checked failure projections discard
synthetic sensitive output. Disposable synthetic keys stayed below wrapper TMPDIR and were
removed. These checks supply no deployed-CA or platform qualification. No .NET/product code changed;
prior documentation-test results remain historical and are not presented as newly executed tests.

All **14 existing source/report/tracking inputs** in the companion retain their initial
SHA-256 hashes. The owner-attested assistant report also matches its companion's current-report
hash. Existing checkpoint cells, story/sprint/epic state and prior report bytes were not edited.
The local preparation captures and verification log are under
`artifacts/story-31-1/eval-01-peer-tls-2026-11-16/`; the companion embeds the final preflight and
corrected process observations so the checked-in evidence does not depend on ignored local files.

## Pending approved execution

The post-rollout evidence field is **null / not executed**, not a passing placeholder. After
custody is identified and the complete exact plan is approved and executed, record actual approval
and custodian transaction references, exact mutation commands and timestamps, old/new public leaf
identity and unchanged trusted CA fingerprint. Run the full recorder again with
`--phase post-rollout` and a new output filename; require 9/9 successful healthy CA-verifying probes
and the approved certificate on all three listeners. Bind the resulting image/configuration identity,
record failures and any rollback, and publish that as a new dated execution addendum without
rewriting this preflight or any attested report. Actual retry_join/failover, other findings and the
authentic independent C4b/C5b/C7 evaluations remain separate obligations.
