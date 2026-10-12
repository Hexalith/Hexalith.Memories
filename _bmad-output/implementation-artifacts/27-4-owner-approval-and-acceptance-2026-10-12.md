# Story 27.4 owner approval and offline result acceptance

Date: 2026-10-12. Owner statement: **“I approve and accept”**.
Policy: `sole-owner-qualification/2026-10-12`. Selected project principal:
`github:user:6775094`. Assurance: `sole-owner/self-reviewed`. This is the
owner's conversation decision, not an independently authenticated receipt.

## Accepted work

The owner approves the sole-owner approach and accepts the results already
presented: the completed pure owner-run record inspector, 25 focused tests and
267 affected interchange tests, with zero failures/errors/skips. Three review
layers were triaged and their concrete findings corrected. Repository
implementation may continue under the existing delegation; repeat policy or
offline-result approval is unnecessary.

Acceptance applies to those measured offline results. Live C1, C2–C6, terminal
cleanup and publication results have not been produced and are not marked passed.
The statement does not supply missing technical observations or a concrete
mutating run record. The six Story 27.4 live tasks remain open.

## Retained evidence

Private owner-controlled archive: `/home/administrator/.local/state/hexalith/memories/access-telemetry/owner-acceptance-20261012-5c9216ec`. Its directory mode
is `0700`, retained files are `0600`, and exact-byte readback passed for all
16 source/contract/log/readiness snapshots. `acceptance.json` binds
those bytes and this owner statement. This archive makes no independent
immutability or certified retention claim.

| Retained item | SHA-256 |
| :--- | :--- |
| Acceptance record | `c509977473f5b817c107e232da2f06774cfa1b74c41337d54bb9b97317fcac40` |
| Exact approved policy snapshot | `3d873ae32ad20e649ecb142180cc4c2d296dcbafa23fae8c41dd4456f15a9f1a` |
| Full 267-test stderr | `431e213837cb8a158561b3bd461cc6ce0184f496e97dc788e0ac4315f23364e2` |

The archive contains the exact commands, focused/full logs, original preservation
receipt and new source bytes. No commit, push, dependency or tracker transition
is part of this acceptance.

## Read-only readiness observations

Following the approval, bounded read-only API queries used the existing local
`jpiquot@local` context. No Secret, credential value, log, Pod exec, mutation or
fault was requested. Each API command used `--request-timeout=10s` and an outer
20-second deadline. These observations are planning facts, not gate evidence.

| Observation | Actual result |
| :--- | :--- |
| Local API metadata | HTTPS `192.168.1.30:6443`; TLS verification enabled, embedded CA configured |
| C1 namespace lookup | `hexalith-memories`, UID `b4890577-70a6-4296-8025-d1e43de1e0e4`; exit 0 |
| Telemetry deployment projection | `memories-access-telemetry`, UID `ce58cb74-9118-414c-8a62-801802deb9ab`; desired 1, ready 1; exit 0 |
| Telemetry Pod projection | `memories-access-telemetry-cb49f44d-cscb4`, UID `e5800b59-f089-46c9-b6c7-b65e3c66c546`; Running; exit 0 |
| Dapr observed image identity | `sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8`, matches the recorded OCI index pin |
| Isolated qualification namespace lookup | `hexalith-memories-qualification` NotFound; exit 1 |

The queries ran between `2026-10-12T15:51:11Z` and `2026-10-12T15:53:26Z`.
The existing workload was already running; no replica state was changed. The
repository's Production-disabled overlay remains unchanged. These observations
do not establish that the existing namespace is non-Production or that its
lifecycle gate is enabled. Source and command snapshots are retained in the
private archive and `/tmp/story-27-4-owner-acceptance-m954shu3`.

## Next implementation work

Implement missing collectors and the complete owner-mode verifier/consumer
integration. Prepare the exact isolated-resource test plan from fresh observed
facts, including namespace creation, action budgets and owned-resource failure
restoration, before qualification mutations. The agent prepares its machine
records; the owner does not need external authority grants, fabricated evidence
or another contributor. Future result acceptance follows actual execution.
