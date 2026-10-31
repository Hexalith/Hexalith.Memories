---
title: Bind hosted qualification to the actual published owner libraries
status: done
created: 2026-10-31
baseline_commit: a884f72a
---

# Explicit hosted owner-library profile

The umbrella latest-package qualifier needs actual native host owner/adapter PEs
that agree with the already selected public library cohort. Independently building
source own libraries can produce a different assembly identity from the published
Contracts/API/adapters used by the parent service. Extend only the explicit
HexalithUsePublishedSdkForHostedTests profile: Server references its existing four
public own libraries as packages, DomainService references its public EventStore
and actual public adapter package. Preserve internal-only source libraries and
all default/native/publish project-reference behavior. Require an explicit actual
Memories selection at least3.0.13. This changes no catalogs, schemas, grants,
controller behavior, data or production trust.

## File Scope

Allowed files for this story:

- `Directory.Packages.props` - supply missing compatibility package version items only inside the explicit profile while preserving the shared catalog
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` - explicit published own-library/SDK profile only
- `src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj` - exact published owner and adapter PEs in the actual host output
- `tests/tooling/published_host_profile/published_host_profile_test.py` - actual MSBuild source/package/publish profile and effective-version controls
- `_bmad-output/implementation-artifacts/spec-published-host-owner-libraries.md` - bounded implementation and proof

## Verification

Evaluate actual projects in a disposable exact-owner/selected-Builds workspace,
including false/default/true/publish, bad/absent explicit version and SDK-source
selector controls. Build the genuine native bodies against actual official3.0.13
packages/publishedSDK3.129.0, verify actual restored public libraries are packages
at selected versions and output PEs match archive bytes. Verify ordinary source
edges remain. No nested initialization or default dependency updates.

## Cross-Tenant Negative Evidence

**Not triggered:** only the explicit package-reference host qualification profile
changes; existing host authentication, tenant rules, contracts and authority code
remain unchanged. Actual production/native project edges remain the default.

## Completed proof

Actual native Server and DomainService Release builds against the explicit
published profile passed with zero warnings and errors in a disposable exact
owner a884f72a workspace. Restored SDK3.129.0 and public owner3.0.13 library
version/type checks passed; all15 selected SDK/owner DLL copies match their
actual restored nupkg runtime entries and embedded source identity. Server uses
four public owner libraries; DomainService includes the published adapter PE.
Archive proof: `/tmp/memories-published-host-archive-proof.json`.

Actual ordinary Server source/default-profile Release build also passed with
zero warnings and errors using independent output directories; its actual
Contracts/EventStore/ServiceDefaults/Telemetry remain project libraries. Actual
MSBuild positive/negative/source/publish controls passed2testmethods covering
20evaluations, including bad/absent effective versions and SDK-source selection.
Logs: `/tmp/memories-published-host-{server,domainservice}-build.log`,
`/tmp/memories-native-default-source-build.log`, and
`/tmp/memories-published-host-evaluation-tests.log`.

This private host test profile requires no new public library version: all used
owner library artifacts remain the existing verified3.0.13publication. The full
umbrella runtime gate must rebuild against its final exact published SDK cohort.
