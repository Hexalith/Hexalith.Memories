# Sprint Change Proposal — 2026-08-31

## Story 28.1: EventStore Owner-Approved Identity vs. Mandated SDK Toolchain Mismatch

## 1. Issue Summary

Story 28.1 ("Adopt Owner-Approved EventStore Runtime Identity") was selected and began implementation against `spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md`. The spec pins to EventStore Story 1.20's exact owner-approved proof identity:

- Source SHA: `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`
- Package version: `999.1.20-proof.fa2d1c9910f8`
- Approved package hash manifest: `4271ddc76411780591423ab024b776cd34a2abccf1cc2dac03a245e141dbe0bc`

During implementation:

- **Task 1 (source-SHA pin)** succeeded cleanly: `references/Hexalith.EventStore` gitlink checked out to `fa2d1c99...`, Debug/source build (`-p:UseHexalithProjectReferences=true`) passes.
- **Task 3 (AppHost `eventstore` resource)** succeeded cleanly: added via `AddHexalithEventStoreGatewayProject()`, sidecar wired to the existing `statestore`/`pubsub` resources (mirroring the `memories` resource pattern), no duplicate Dapr components. Verified in isolation (34-project Debug build, 72/74 `Deployment` contract tests pass — the 2 failures trace to the package blocker below, not the AppHost change).
- **Task 2 (package identity)** could not be satisfied: the approved package bytes are not on `nuget.org` (404), not cached locally, and the proof packet's Azure evidence-blob URL 404s without credentials this environment doesn't hold. Repacking the EventStore submodule fresh at the pinned SHA succeeds mechanically but produces bytes whose SHA-256 does **not** match the approved hash manifest, because the proof packet was sealed under .NET SDK `10.0.302`/ASP.NET `10.0.10`, while this project's mandated toolchain (`global.json`, confirmed in `_bmad-output/project-context.md:20`) is SDK `10.0.400`. A rebuild under the mandated SDK is not byte-identical to the sealed proof artifact — this is a structural incompatibility, not a fetch/config error.
- **Task 4** (full-stack proof test) is blocked behind Task 2; not attempted.
- Investigating a workaround (bump `references/Hexalith.Builds` to a commit that already exposes the proof version) found none exists: `Hexalith.Builds` commit `8f32f12` did this once (2026-07-27 10:09) and was deliberately reverted by `bb02cdc` four hours later (2026-07-27 14:09, `"fix(deps): restore published EventStore package pin"`); `Hexalith.Builds` has since advanced through many unrelated fixes to `HexalithEventStoreVersion=3.100.0`. No current commit carries both the proof pin and today's other fixes.

**Category:** Technical limitation discovered during implementation (toolchain/artifact reproducibility), not a requirements misunderstanding.

### Epic AC Verification

Verified 2026-08-31 against `references/Hexalith.EventStore` HEAD and `references/Hexalith.Builds` HEAD (pre-correction).

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "EventStore Story 1.20 now records `final_decision: available`, `authorize_consumer_migration: true`, a 40-hex `tested_runtime_sha`, named owner approval, and the approved package version and SHA-256 inventory." (epics.md Epic 28 Activation state) | Existence/behavioral | `grep -E "^(final_decision\|authorize_consumer_migration\|tested_runtime_sha\|approved_package_version\|approved_package_hash_manifest_sha256):" references/Hexalith.EventStore/_bmad-output/implementation-artifacts/1-20-owner-approved-parity-closure-proof-packet.md` | `final_decision: available`, `authorize_consumer_migration: true`, `tested_runtime_sha: fa2d1c99...`, `approved_package_version: 999.1.20-proof.fa2d1c9910f8`, hash manifest present | `confirmed` — still current |
| Implicit AC3 premise: a `Hexalith.Builds` gitlink can expose the approved package version for restore | Existence | `git -C references/Hexalith.Builds log --all --oneline -S "999.1.20-proof.fa2d1c9910f8" -- Props/Directory.Packages.props` | Only `8f32f12` ever set it, reverted 4h later by `bb02cdc`; current HEAD has `HexalithEventStoreVersion=3.100.0` | `corrected` — no current commit exposes it; requires a new commit (see Section 4) |
| Implicit AC3 premise: the approved package hash is reproducible by rebuilding the approved source | Behavioral | Repack `references/Hexalith.EventStore` at `fa2d1c99...` via its own `tools/pack-release-packages.py`, compare SHA-256 to `4271ddc7...` manifest | Hashes diverge; proof packet built under SDK `10.0.302`, this project mandates SDK `10.0.400` (`_bmad-output/project-context.md:20`) | `corrected` — exact-hash reproduction is not achievable under this project's mandated toolchain |

Per `_bmad/custom/epic-ac-verification.md`: these are `corrected` claims that change scope and a ratified decision (Epic 28's anti-drift AC), so they are escalated here for a human decision rather than absorbed — this proposal *is* that escalation.

## 2. Impact Analysis

**Epic Impact:** Epic 28 / Story 28.1 cannot close as literally written — the exact-hash package-identity AC assumes a reproducibility condition that does not hold. No other epic's stories are invalidated. Epic 31 / Story 31.2 remains correctly sequenced to start after Epic 28's dependency-identity work lands (epic-28-context.md Cross-Story Dependencies) — this only affects *when* that lands, not the sequencing rule itself.

**Story Impact:** Only Story 28.1. Tasks 1 and 3 are unaffected by this correction and should be kept as-is (they don't depend on the package-hash issue). Task 2's acceptance criterion needs amendment; Task 4 depends on Task 2's resolution.

**Artifact Conflicts:**
- `epics.md` — Story 28.1's package-identity AC (Given/When/Then #3) and the Epic 28 "Activation state" note need amendment to state the SDK-driven exception explicitly, per `_bmad/custom/epic-ac-verification.md`'s rule that a `corrected` claim must correct the source planning artifact in the same change.
- `spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md` — frozen intent needs a human-renegotiated update (Boundaries & Constraints, Ask-First resolution) plus a Spec Change Log entry.
- `references/Hexalith.Builds` — a new local commit is needed (see Section 4); `PRD.md` and `architecture.md` need no changes (no EventStore-version pinning lives there — verified by grep).
- No CI/deployment/test-strategy artifacts beyond what's already tracked in the spec's Code Map.

## 3. Recommended Approach

**Selected: Option 1 — Direct Adjustment (narrow amendment), not a full retreat to "latest."**

Your instruction was "bump to latest .NET 10.0.400, latest packages and submodules." I want to flag a distinction before locking this in: a **full** interpretation (track EventStore's current default-branch tip and whatever NuGet version is newest) would silently recreate the exact drift Epic 28 exists to close — the epic's own AC explicitly forbids "a current tag, repository HEAD, or unapproved package version" as sufficient authorization, and that's precisely the state Memories was in before this story started (submodule at `1194dfe5`, packages at `3.100.0`, neither owner-approved).

The **narrow** interpretation — which I'm recommending — keeps the actual owner-approved anchor (source SHA `fa2d1c99...`, which Task 1 already adopted correctly and doesn't need to change) and fixes only the one thing that's genuinely broken: the package-identity AC's requirement that restored bytes match a hash sealed under an SDK this project no longer runs. Instead of chasing an unreproducible byte-for-byte match, Release/package mode packages the **same approved source SHA**, rebuilt under this project's own mandated SDK `10.0.400`, published to an isolated local feed pinned by the hash of *that* rebuild — with a dated correction note in `epics.md` explaining the SDK-driven divergence from the original proof packet. This keeps every part of Epic 28's anti-drift intent intact (still one specific, source-traceable, owner-approved identity — not "whatever's newest") while unblocking the toolchain mismatch, which is outside Memories' control (the proof packet is sealed/immutable in the EventStore repo; only EventStore's owners could reseal it under a newer SDK, and that's out of scope here).

- Effort: Low (amend two epics.md AC blocks + spec frozen block + one `Hexalith.Builds` commit already drafted).
- Risk: Low-Medium — the rebuilt package is no longer byte-identical to the sealed proof artifact, so this is a real, documented deviation from "exact hash," not a technicality. It remains traceable to the approved source SHA, which is the load-bearing part of "owner-approved."
- Rollback (Option 2) is not viable — nothing to roll back; Tasks 1/3 are correct and worth keeping.
- MVP scope (Option 3) is unaffected — Epic 28 is explicitly Post-MVP Operational Readiness track (epics.md:424), not gating MVP.

**If you actually want the full interpretation** (drop the owner-approval anchor entirely and track EventStore's latest tip / latest published package, accepting that Story 28.1 then no longer satisfies Epic 28's stated purpose and the epic should probably be re-scoped or retired instead), say so explicitly in your approval below — I did not choose that path by default because it silently defeats the epic, and Epic AC Verification policy requires escalating rather than absorbing a correction that changes a ratified decision.

## 4. Detailed Change Proposals

### 4.1 `epics.md` — Epic 28 Activation state (~line 5073-5078)

**OLD:**
> **Activation state (2026-08-02):** EventStore Story 1.20 now records `final_decision: available`, `authorize_consumer_migration: true`, a 40-hex `tested_runtime_sha`, named owner approval, and the approved package version and SHA-256 inventory. The external gate is satisfied; Epic 28 and Story 28.1 remain backlog pending explicit selection. A current tag, repository HEAD, or unapproved package version remains insufficient for implementation.

**NEW (append a dated correction paragraph, keep the existing text unmodified above it):**
> **Correction (2026-08-31):** Story 1.20's proof packet seals package hashes under .NET SDK `10.0.302`; Memories mandates SDK `10.0.400` (`global.json`). Rebuilding the approved source SHA under Memories' mandated SDK is not byte-identical to the sealed hash — this is a toolchain-generation artifact, not an authorization gap. Package-identity adoption is satisfied by packaging the approved source SHA under Memories' current mandated SDK via an isolated local feed, pinned to the rebuild's own hash, rather than the original SDK-10.0.302 hash. The source-SHA anchor (`fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`) remains the binding authorization; "latest tag/HEAD/unapproved version" remains insufficient exactly as before.

### 4.2 `epics.md` — Story 28.1 package-mode AC (~line 5099-5104)

**OLD:**
> **Given** Story 1.20 names the approved 14-package version and hashes,
> **When** Release/package mode restores from an isolated cache,
> **Then** `Hexalith.EventStore.Client`, `Hexalith.EventStore.Aspire`, and every resolved `Hexalith.EventStore*` asset use that exact version, fetched package bytes match the approved hashes, no EventStore project reference enters the Release asset graph, and the selected `Hexalith.Builds` gitlink already exposes that version.

**NEW:**
> **Given** Story 1.20 names the approved source SHA and Memories' mandated SDK (`10.0.400`) differs from the SDK that sealed Story 1.20's package hashes (`10.0.302`),
> **When** Release/package mode restores from an isolated cache,
> **Then** `Hexalith.EventStore.Client`, `Hexalith.EventStore.Aspire`, and every resolved `Hexalith.EventStore*` asset are packaged from that exact approved source SHA rebuilt under Memories' mandated SDK, published only to an isolated local feed pinned by the rebuild's own SHA-256 manifest, no EventStore project reference enters the Release asset graph, and the selected `Hexalith.Builds` gitlink exposes that rebuilt version pin.

### 4.3 `spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md`

- Update `<frozen-after-approval>` **Always** bullet 2 to match the amended AC above (rebuild-under-mandated-SDK, not exact-hash-of-sealed-artifact).
- Remove the now-resolved **Ask First** bullet on package sourcing; keep the Dapr sidecar bullet (already resolved and implemented).
- Append a **Spec Change Log** entry: what triggered the change (SDK-reproducibility finding), what was amended, and a `KEEP` note that Tasks 1 and 3 are correct as implemented and must not be redone.

### 4.4 `references/Hexalith.Builds`

Local commit `069d1168d88b53a4854bed1f6f52685b2283c6e2` (already made, unpushed) pins `HexalithEventStoreVersion` to the proof version string on top of current HEAD. Under this correction its commit message/intent should instead reflect "rebuild under mandated SDK" rather than "the proof version bytes" — will amend once this proposal is approved, alongside actually producing and publishing the rebuilt package bytes to an isolated local feed.

## 5. Implementation Handoff

- **Scope classification: Major** — this corrects a ratified epic acceptance criterion (ties to `_bmad/custom/epic-ac-verification.md`'s escalation requirement), even though the mechanical edit is small.
- **Handoff:** Back to the `bmad-build` Story 28.1 implementation subagent once this proposal is approved — it already holds full investigation context (Code Map, prior verification runs) and can apply the amended AC directly: rewrite the `Hexalith.Builds` commit intent, produce the rebuilt package feed, re-run Task 2/3/4 verification.
- **Success criteria:** `epics.md` and the spec's frozen block carry the corrected AC text with a dated correction note (not a silent edit); `dotnet build` (both Debug/source and Release/package) succeeds under SDK `10.0.400`; Task 4's full-stack proof test is attempted against the rebuilt identity.
