---
status: done
date: '2026-10-31'
baseline_commit: 5aaa3983
---

# Evaluate hosted catalog dependency guards

## Intent

The authorized latest-package/release work added the explicit hosted package
profile. Release41205728166 fails two old AppHostSecurityConfigurationTests:
one broadly assumes exactly one PackageVersion element, and one requires the
old exact Client condition text. Production/default dependency graphs remain
unchanged and compile successfully. Correct the tests without changing the SDK
floor, CPM selections, source support, public inventory or release version.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs`
- `_bmad-output/implementation-artifacts/spec-evaluate-hosted-catalog-guards.md`

## Changes

Select only the intended Hexalith.EventStore.* PackageVersion update when
checking its version authority. Evaluate the real tracked catalog in six cases:
empty/older selections require3.129.0, exact3.129.0 remains, newer3.129.1 and
3.130.0 remain, and an exact source3.128.4 selection remains authoritative. The
unrelated hosted ServiceDefaults/Telemetry entries retain exact CPM3.0.13.
Preserve existing floor-condition, import-path and provenance assertions.

Evaluate the actual Server project for package, source, hosted and publishing
conditions. Assert exactly one Client package or source edge as appropriate,
exact CPM SDK3.129.0, and no inline Version/VersionOverride. The explicit hosted
profile switches to the public SDK; publishing keeps the native source edge.
All production files are unchanged. No catalog or authorization gate is bypassed.

## Verification

A disposable ordinary source copy and exact owning Builds/Platform gitlink
archives preserve original producer outputs and initialize no nested submodules.
Release package-mode Server.Tests build passes with zero warnings/errors in
11.21s. The complete affected class passes23/23, zero errors/failures/skips,
in3.018s. Logs: `/tmp/memories-evaluated-host-fixtures-build.log` and
`/tmp/memories-evaluated-host-fixtures-tests.log`. No wider tests were run.

The patch is test-only and does not change the pending3.0.13 release candidate.
Parent owns hosted CI/release qualification and publication approval.
