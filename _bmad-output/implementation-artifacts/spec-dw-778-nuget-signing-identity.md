---
title: 'Qualify recovery package identity against real NuGet signing'
type: 'chore'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
baseline_commit: '067b8fac4f9d82f84f4b072e8d2b6f4f55c8c576'
review_loop_iteration: 0
context: ['{project-root}/AGENTS.md', '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-778 addresses “Verify the NuGet repository-signing ZIP-entry identity rule against real retained and nuget.org-served bytes.” Compare an authentic retained release package against the same package ID and version downloaded from NuGet. Retain enough evidence to reproduce the comparison and confirm whether repository signing changed any original payload entry.

## Boundaries & Constraints

The user-authorized work is read-only. Download release artifacts, NuGet packages and related public metadata. No release, push to registries, GitHub settings change or live recovery. Use run-with-temp for downloads and scratch outputs. Keep a concise reproducible record in the owner runbook. Do not edit the ledger/spec or commit. Preserve an unexpected mismatch and report it, since changing the package identity contract requires an owner decision.

</frozen-after-approval>

## Code Map

- `docs/dev/release-runbook.md` describes the current comparison and the outstanding signing-proof gap.
- `tools/container-publication-contract.py` implements the package_entries comparison used by recovery.
- GitHub Actions Release run 42134622308 retained artifact 13465742698 named release-packages at commit 246157a7ff8e2dcd8d356c833a77a02514490d2f. The artifact API reports it available and gives SHA-256 3765105a1c9e90e85c98555b324cd4daa0551990ceae8fbc7f8a9962c2147760. The run published v3.0.25. A newer comparable real retained artifact may be used if needed.

## Tasks & Acceptance

- [x] Retrieve exact retained and served bytes for the same real release package and record its identity in the runbook.

Given a retained package from a real release and the same NuGet package, confirm both receipt SHA-256 hashes, compare every original ZIP entry uncompressed, and list the exact added, removed or changed entries. The current comparator must agree with that independent comparison. If the inputs cannot be bound, report the concrete blocker.

## Implementation Notes

## Spec Change Log

## Review Triage Log

The replay rejects any archive checksum mismatch before unpacking and verifies both package checksums before signature validation. The local archives preserve the readback inputs after hosted retention expires.

## Verification

Compare actual bytes using both independent ZIP reads and the existing production comparator. Record package ID/version, source run, immutable artifact identity, package hashes and signature entry outcome. Save useful output outside the disposable workspace.

The v3.0.25 run retained package bytes but lacked a pre-publication package identity receipt. The v4.1.4 release retained both. The exact runbook command passed, including archive and package digests, case-sensitive NuGet SHA-512 metadata, all six original ZIP entries, recovery comparator equality, and the NuGet repository signature. An independent .NET ZipArchive read found the same sole addition, .signature.p7s. The local artifacts directory retains the exact archives and verification records.

## File List

- `docs/dev/release-runbook.md`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `57ff19d1dd376002b3c6bacae47501760c644cdc` | 1 live package, 2 independent ZIP readers and exact runbook replay | Matched 1/1 |
| 2026-11-28 | code-review | `679f0617f071dc12e61d7963ecdf91c3f3db812a` | 1 live package, 2 independent ZIP readers and exact runbook replay | Matched 1/1 |
