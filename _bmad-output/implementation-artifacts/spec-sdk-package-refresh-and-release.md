---
title: 'Refresh the Memories SDK and dependencies and qualify a release'
type: 'chore'
created: '2026-11-11'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '9a959b22383c32779da8930dee5a0e5260603b36'
context:
  - AGENTS.md
---

<frozen-after-approval reason="User authorized synchronization, dependency updates, CI repairs and release without intermediate human validation">

## Intent

**Problem:** Memories has pending review documents, outdated root submodule pointers and an older SDK/package cohort. The latest push CI has a failing browser lane, preventing an exact-source release.

**Approach:** Preserve pending work, synchronize the nine root dependencies, select the latest stable SDK and compatible package cohort, repair evidenced CI failures, validate and push, then dispatch the existing guarded release and verify every manifest package on NuGet.

## Boundaries & Constraints

**Always:** Work in each owning repository; preserve existing changes; use Conventional Commits and commitlint before and after commits. Root SDK becomes 11.0.100 while preserving net10.0/C#14 and existing package/image inventories. Keep central package ownership in Builds and its reviewed compatibility exceptions. Update safe remaining compatible package selections through Builds with its revision-bound audit procedures; avoid consumer version overrides. Push any submodule content commits before recording their pointers. Keep current Builds qualification profile and test constant aligned with its gitlink. Preserve historical publisher approval pins and evidence. Release remains CI-only, bound to exact-source successful push CI, production approval controls and NuGet OIDC. User authorization replaces optional conversational checkpoints; required external human approval cannot be forged or bypassed.

**Never:** Initialize or update nested submodules; load skills in references; force-push; disable tests or approval controls; publish from the workstation; retarget the framework merely to consume incompatible packages; modify unrelated open Story 30.4 action items.

</frozen-after-approval>

## Code Map

- `global.json`: older 10.0.403 SDK pin; 11.0.100 is installed and verified against Microsoft release metadata.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: sole version authority; current dd68a5ef cohort supplies Aspire13.6.5, Dapr1.19.1, bunit2.15.2, OpenTelemetry core1.21 and System.CommandLine3. The Builds README/Tools README document catalog, exception and revision-bound audit validation. Retain compatibility pins with documented reasons; remaining candidate patches include Test SDK18.12 and OTel instrumentation1.21.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`: explicit Aspire SDK13.6.4 must match catalog13.6.5; no AppHost API changes intended.
- `tools/builds-publication-qualification.json`, `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py`: buildsSha/PINNED_SHA track current gitlink; historical release pins remain81c88b34d09d34260b92d7f0928dbbc1c7446baa.
- `.github/workflows/ci.yml`: exact-source CI, source-debug, tooling, individual unit/integration suites and deployment probes. Dapr toolchain1.18 is stale against shared1.19.1. Browser job130592650549 failed in run43041060983; logs currently HTTP502 and no browser artifacts. Diagnose before changing expected outcomes.
- `tests/Hexalith.Memories.Web.E2E/package.json`: Node>=24; @types/node24.19.2 is latest compatible24.x; preserve that floor. Root npm dependencies are already latest and semantic-release25.0.9 plus alias has a reviewed security exception.
- `tools/release-packages.json`: canonical ten-package inventory; `release.yml` and release runbook define dispatch and protected production flow.

## Tasks & Acceptance

**Execution:**
- [x] Preserve the three pre-existing review document edits in a validated commit and integrate origin/main.
- [x] Synchronize all nine root-declared submodules without nested initialization; no pre-existing submodule commits or dirty files were found.
- [ ] `global.json`, AppHost project, qualification profile and test -- align SDK and immutable current dependency selection.
- [ ] Builds catalog and audit, only if needed -- update remaining compatible safe versions and validate the owner procedures; report deliberate compatibility exceptions.
- [ ] `.github/workflows/ci.yml` and affected code/projects -- repair evidenced SDK/package/browser/toolchain failures, retaining existing test intent and runtime diagnostic dependencies.
- [ ] Run restore/build, relevant individual test projects, release/package fixtures, npm configuration verification and package validation; record exact blockers and commands.
- [ ] Commit/push owner changes with commitlint, verify remote submodule availability, push main and observe successful CI.
- [ ] Dispatch existing release after exact-source CI succeeds; if production waits for human approval, report the run and required action without bypassing it. Verify package publication for any completed release.

**Acceptance Criteria:**
- Given the refreshed graph, when the repository is built and tested, then existing contract checks pass with SDK11.0.100 and net10.0 package compatibility.
- Given synchronized dependencies, when main is pushed, then all recorded submodule commits are remotely available and required CI passes on that exact SHA.
- Given green exact-source CI, when guarded release is dispatched, then existing protection is retained and all ten expected NuGet versions are verified after successful publication, or the concrete external blocker is reported.

## Implementation Notes

User requested no intermediate human validation; the existing required production reviewer is an external release gate. Initial root docs preserved by9a959b22 atop13fdb88a. Root submodules were fast-forwarded only, no nested initialization.

## Spec Change Log

## Review Triage Log

## Verification

Use SDK version, serialized solution build, individually invoked test projects, affected Python tooling suites, npm semantic-release verifier, package validator, GitHub Actions exact-head results and NuGet flat-container endpoints. Preserve command output in task-owned temporary logs and summarize decisive results here.

Implementation handoff: the implementation agent owns configuration/code edits and local validation. The parent owns commits, pushes, remote CI observation and release dispatch after implementation and review. Do not perform remote mutations in the implementation handoff. For submodule catalog edits, leave audit refresh pending the parent-owned catalog commit if its revision binding requires a commit first.
