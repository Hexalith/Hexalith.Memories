---
title: Classify internal intent adapters in the release inventory
status: done
created: 2026-10-31
baseline_commit: bf2cd1a8
---

# Internal adapter release disposition

Release41187296177 passed build/unit checks and then stopped because the local internal EventStoreExtensions project declared IsPackable=true without package metadata or an inventory entry. This project describes local internal adapter enrollment; its canonical enrollment uses unpublished source SDK APIs and explicitly refuses package enrollment. It has never been one of the nine public package IDs in the authoritative inventory. Keep the internal source helper nonpackable and explicitly classify it in nonPackableProjects, preserving all nine public package IDs and the strict validator. Do not invent a public unsupported adapter package or suppress inventory enforcement.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.EventStoreExtensions/Hexalith.Memories.EventStoreExtensions.csproj` - mark the internal helper nonpackable
- `tools/release-packages.json` - classify the helper in the existing nonpackable bucket
- `_bmad-output/implementation-artifacts/spec-internal-adapter-release-disposition.md` - record disposition and verification

## Verification

Run the actual release inventory validator and its positive/negative fixtures. No production runtime code, source enrollment, tenant behavior, publication settings or package validator logic changes.

## Cross-Tenant Negative Evidence

Not triggered: only the existing internal helper's package disposition and explicit inventory classification change; no authorization, tenant routing or data behavior changes.

Actual `pwsh -NoProfile -File tools/validate-release-packages.ps1`: PASS (all nine public IDs unchanged, internal helper explicitly nonpackable). `python3 -m unittest discover -s tests/tooling/release_packages -p '*_test.py' -v`:28/28pass in76.060seconds, including malformed inventory, missing/duplicate/conditional IsPackable and dependency-boundary refusal controls. Logs `/tmp/memories-internal-adapter-inventory.log` and `/tmp/memories-internal-adapter-inventory-tests.log`. Validator and runtime code are unchanged.
