---
status: done
date: '2026-10-31'
route: dispatch
---

# Align finite campaign fixtures with their historical source

## Scope and authorization

The user authorized CI repairs and release without another human checkpoint.
The parent directs fixing Memories' actual b7947472 lifecycle C1 job failure,
while preserving the seven-workload/nine-Pod finite campaign, exact owner source
selection, capacity reservations, bounds and real authority rejection checks.
This change edits three Python fixture files and records this implementation
spec. It changes no production source, chart, allocation, dependency selector,
workflow, operational approval or historical qualification status.

## Observed failure and source decision

Run 41188585055/job 124500470432 fails 16 of 139 lifecycle fixtures. The completed
log records campaign-capacity-application-graph-drifted at the selected Platform
ad99f7e41871af751120795e1278f79bb6dedf32 configure_application guard.
That source deliberately retains seven workloads and nine Pods, while its current
reference graph has eight workloads after adding memories/domain. Platform's
bound_fixture creates historical seven-workload values; its own capacity tests
pair them with campaign_source_fixture, a matching isolated source/chart snapshot.
Memories omitted that source fixture and reopened the current eight-workload
chart. Admission correctly refused those mixed inputs.

## Implementation

- Reuse Platform's existing historical source/chart fixture inside a temporary
  repository workspace. Ordinary copied owner tools and reporter bytes retain
  real owner Git source metadata through a local shared clone with no checkout.
  No original checkout or nested submodule is initialized or updated.
- Select that workspace consistently for profile loading, real consumer CLI
  subprocesses and authority verification. The shared OwnerFixture accepts an
  explicit repository root; existing callers retain their original default.
- Assert seven workloads, nine Pods, fixed application resource ceilings,
  97GiB primary allocation and 4,600,500 cumulative records.
- Explicitly reject the actual current eight-workload owner graph through both
  the unchanged capacity guard and actual profile loading, even after chart and
  outside profile digests are refreshed. Existing negative allocation, target,
  source, authority, expiry and cleanup tests remain active.

## Scoped verification

A disposable workspace at /tmp/memories-c1-qualification-tf3298br/Memories uses
ordinary files from the exact owner-selected Platform commit, without nested
initialization. The original fixture failure reproduces before this patch.

- test_campaign_profile.py: 12/12 pass in 2.609s, including two new bounded graph
  controls and the real frozen CLI selection/refusal path.
- test_campaign_review_fixes.py: 6/6 pass in 3.694s, including source authentication,
  original admission, foreign-scope cleanup refusal and parent failure cleanup.
- test_owner_authority.py: 25/25 pass in 14.695s, including unchanged default-root
  behavior and private custody, revocation, source, target and session controls.
- The exact selected Platform test_campaign_capacity.py: 19/19 pass in 1.089s,
  including competing allocation, resource insufficiency, stale measurements,
  workload growth, foreign backup domain and current graph rejection controls.
- All 62 cases pass without skips. Focused logs are
  /tmp/memories-c1-campaign-profile-final.log,
  /tmp/memories-c1-campaign-review-final.log,
  /tmp/memories-c1-owner-authority.log and
  /tmp/memories-c1-platform-capacity.log.
- git diff --check passes. No commit, push, runtime campaign or .NET build was
  performed by this agent for this repair.

## ServiceDefaults publication follow-up

The release inventory includes Hexalith.Memories.ServiceDefaults; the publisher
loops over the validated inventory without an intentional compatibility skip.
The completed release log records the exact ServiceDefaults 3.0.11 filename
published. A bounded public retry now returns HTTP 200 for its actual NuGet
archive, whose embedded id/version/source are
Hexalith.Memories.ServiceDefaults/3.0.11/b7947472e80871110216648b69bd181ddfb5f0e8.
Its retained public artifact proof is
/tmp/memories-servicedefaults-3.0.11-public.json. No repush was attempted.


## File Scope

Allowed files for this story:

- `tests/tooling/access_telemetry_lifecycle/test_campaign_profile.py` - bind the existing finite fixture to its matching isolated source and add drift controls
- `tests/tooling/access_telemetry_lifecycle/test_campaign_review_fixes.py` - use the same bounded historical source in review fixtures
- `tests/tooling/access_telemetry_lifecycle/test_owner_authority.py` - accept the explicit isolated repository root in the shared test fixture
- `_bmad-output/implementation-artifacts/spec-ci-finite-campaign-fixtures.md` - record source decision and exact scoped evidence
