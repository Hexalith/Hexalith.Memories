# Sprint change proposal · Three decisions for the next Memories sprint

**Approved 21 November 2026 · Moderate scope · Memories `45fb2308`, McpCli `8042275f`.** Sprint planning found three open decisions after the repository and its ten direct submodules were synced to main. The Administrator approved the recommended course with “apply recommended”. The operational handoff and results are recorded below. The search disposition, reviewer appointments and storage allowance need their respective owners’ decisions. Phase 1 is no-go. The Administrator retains the single schedule reset. [Release decision](prd.md#release-decision-record)

## 1. Epic 32 and Story 35.7 · Make first search possible from the shared CLI

**Recommendation.** Start with one user outcome. A user who can read a case can find a known item through the shared CLI. Use the existing McpCli query mechanism and the Memories search implementation. Memories owns the decorated query, handler and authorization. McpCli owns its enrollment and presentation.

**How it works.** The two owners first map the manual journey (tenant, case, ingest, search) to actual target operations, using one short list of available operations and gaps. For L03 they identify the proposed query and handler, how the caller’s tenant and user authority reach the handler, and which search semantics the operation supports. Compare that proposal with the approved L03 inventory, including its result envelope, empty and degraded results, and visibility rules. Record any scope difference and obtain an amended owner disposition before registering a delivery story. The first implementation proof is a permitted search for a known item and a denied cross-tenant search through the same CLI. Keep broader parity and retirement evidence with the owner inventory. Once the whole manual path works, Story 35.7 times a clean-machine run through the first correct result. Its target is under 30 minutes. [Inventory](../../references/Hexalith.McpCli/_bmad-output/implementation-artifacts/memories-migration-inventory-v1.1.0.md) · [Story 35.7](epics.md#story-357-time-the-clean-machine-onboarding-path)

**Benefit.** This exposes a missing authorization or routing contract early and reuses the search behavior already maintained by Memories. The journey list also exposes an upstream gap in case creation or ingest before anyone schedules the walkthrough.

**Cost and alternative.** This requires coordination across two repositories. A successful search alone cannot qualify onboarding. Running the legacy wizard is cheap and still useful for diagnosing the stack, but L06 was withdrawn and its run cannot count toward G3. A redesign of the whole CLI would increase scope before the first operation is usable.

## 2. Epics 33 and 35 · Secure independent judgments before scoring G1

**Recommendation.** Have the Administrator recruit two external human reviewers now. A short availability confirmation and a shared grading sheet are sufficient to organize the work.

**How it works.** Record each reviewer’s name, review window, and confirmation that they neither curated the corpus nor implemented the evaluated behavior. Before confirming the window, give them the expected workload. The protocol uses at least 50 topics. For each topic they grade the deduplicated pool from both top-ten lists plus preregistered references. Jérôme and both reviewers each grade every candidate 0–3, independently and blind to arm and rank. Compute all three pairwise agreement values. Freeze complete labels only once each unweighted Cohen’s κ reaches 0.6, then run the comparison against BM25 plus semantic search using the existing PRD thresholds. Corpus and tooling preparation can proceed while reviewers are being recruited. [Grading protocol](addendum.md) · [Reviewer independence](prd.md#open-questions)

**Benefit.** Independent judgments make it possible to distinguish gains that users would recognize from a system doing well on its authors’ examples. Confirming calendar capacity early exposes a schedule risk while there is still time to act.

**Cost and alternative.** Human grading takes time, and disagreements can require another version of the labels. With exactly 50 topics and disjoint top tens, each grader already faces 1,000 judgments before any extra reference items. If reviewers are unavailable, use the eight-topic suite for diagnostic feedback, leave G1 open, and defer the release-date decision. Extra automated scoring cannot supply the missing independent judgments.

## 3. Story 34.58 · Decide what one tenant may retain

**Recommendation.** Use one owner-approved byte cap for the tenant content store for the supported deployment profile. Start with retained content under AD-4. Keep the choice of charging logical payload bytes or physical storage explicit. Logical payload bytes are easier for a tenant to predict. Physical backend consumption still matters for sizing the service.

**How it works.** The Administrator and capacity owner record three things in the PRD: what counts, the limit in bytes or GiB, and the workload and headroom behind it. Reuse a representative ingest run and the existing capacity procedure. Measure retained AD-4 content for ordinary and large inputs, repeat a submission, and compare the footprint with the storage available at the intended tenant count. Record physical overhead separately. DW-735 measures ingestion ownership records and can share the workload. A count of units or ownership keys cannot determine the content byte allowance. Once the owner selects the value, scope 34.58 to enforcing that cap. A boundary example should show a write up to the cap succeeding, a write that would exceed it being refused, a retry charging once, and another tenant using its own allowance. [Capacity procedure](../../docs/operations/capacity-planning.md) · [Architecture decisions AD-4 and AD-18](architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md)

**Benefit.** Operators can explain a refusal and reproduce the boundary with a small test budget. One limit is inexpensive to configure and avoids introducing billing tiers or a new quota service.

**Cost and alternative.** A fixed allowance can reject a legitimate large tenant or promise more than the deployment can hold. Start with an existing approved service allowance if the owner can supply one, then validate it against the workload. Without one, measure before choosing the value. Bootstrapping from a Kubernetes volume size or the 100K-unit benchmark would confuse backend capacity with a product promise.

## Applied corrections and handoff

| Artifact | Previous instruction | Correction or decision |
| --- | --- | --- |
| Epic 32 and its coverage map | Wait for McpCli 4.4/4.5 and inventory approval. | Both stories are done. The active dependency is an eligible, owner-approved and delivered operation. L03 is deferred and L06 withdrawn. |
| Readiness report and Epic 34 review history | The report ended by sending the next reviewer back to 34.9. | Mark the October instruction as historical. Retain the reviewed order beginning 34.33 → 34.1 → 34.8. All three outstanding decisions are visible together. |
| Epic requirements and UX onboarding | The December 1 decision appeared current and compatibility verbs described the critical path. | Adopt the PRD’s missed-checkpoint disposition and Story 35.7’s manual, enrolled CLI journey. |
| PRD and architecture | G3, the G1 protocol and AD-18 already specify the target behavior. | The PRD records the approved logical-byte accounting rule. The numeric cap awaits sizing against the deployment profile. Story registration follows the existing owner and readiness gates. |

**Order of work.** Start the reviewer outreach and the cross-repository search mapping together. The capacity owner can measure tenant content independently. The first two planning tasks are small, while search delivery depends on the identity contract and reviewer time depends on the candidate pool. Use those observations before forecasting a release. The product owner rechecks each decision in the [readiness report](implementation-readiness.md). Once all planning holds are addressed, rerun epic validation and sprint planning. Qualification runs belong to their respective stories.

## Approval and execution · 21 November 2026

- **Epic 32.** The [G3 preflight and L03 decision request](../../docs/dev/quickstart-walkthrough-log.md#phase-1-shared-cli-preflight--21-november-2026) maps the four operations, their source evidence and the open disposition. McpCli’s production package manifest and its current operation list establish which modules are enrolled. The Memories and McpCli owners can review the case-scoped search proposal against the immutable L03 inventory and NFR10.
- **Story 34.58.** The [PRD decision](prd.md#open-questions) adopts logical-byte accounting. The [capacity sample](../../docs/operations/capacity-planning.md#tenant-content-store-measurement--21-november-2026) records observed ordinary, large-input and retry footprints. The owner still needs deployment inputs to select a sustainable cap.
- **G1.** The Administrator owns recruitment. Both external reviewer slots are open. Record each name, confirmation of independence and agreed grading window in PRD open question 2 when the reviewer accepts. The invitation below is ready for an individual recipient.

### G1 reviewer invitation

> Would you independently grade search results for the Hexalith.Memories Phase 1 benchmark? For each topic, you’ll read candidate items in blinded order and grade how well each answers the question, from 0 (irrelevant) to 3 (direct, sufficient evidence). The benchmark covers at least 50 real topics. At 50 topics, the two top-ten lists can contribute up to 1,000 distinct topic–item pairs before extra references. We’ll confirm the actual count before you commit. Please confirm whether you helped curate the evaluation corpus or implement the retrieval behavior, and which dates you could complete grading. Each reviewer submits their own judgments before seeing anyone else’s.

Give each reviewer a separate copy of the same blinded pool. Use this CSV header and populate one row per deduplicated topic–unit pair before sending it.

```csv
topic_id,topic_prompt,candidate_unit_id,evidence_text,grade_0_to_3
```

Keep reviewer identity and pool version with each submission. Keep system identity, rank and the other reviewers’ grades out of those copies. The Administrator checks completeness and the three pairwise agreement values before freezing the labels. [G1 grading protocol](addendum.md)

## Evidence checked

Commands run from the Memories root. Set `P=_bmad-output/planning-artifacts` and `M=references/Hexalith.McpCli/_bmad-output/implementation-artifacts`.

| Claim | Re-runnable check | Verdict |
| --- | --- | --- |
| “Stories 4.4 and 4.5 are still backlog” | `rg -e '^  4-4-' -e '^  4-5-' "$M/sprint-status.yaml"` | **corrected**. Both rows are done. The correction is recorded at Epic 32. |
| “L03 search is deferred and L06 withdrawn” | `rg -e '^. L03 ' -e '^. L06 ' "$M/memories-migration-inventory-v1.1.0.md"` and `jq -r .decision "$M/evidence/story-5-10/approval.json"` | **confirmed**. The receipt says approved. Both rows are excluded. |
| “No walkthrough has been recorded yet” | `rg 'No walkthrough' docs/dev/quickstart-walkthrough-log.md` | **confirmed** in the committed manual walkthrough log. |
| “Two independent external reviewers remain unnamed” | `rg -F 'Two independent external reviewers remain unnamed' "$P/prd.md"` | **confirmed** in the release record. The Administrator must supply current identities and availability. |
| “Story 34.58 awaits a per-tenant storage budget” | `rg -F 'Story 34.58 awaits a per-tenant storage budget' "$P/prd.md"` | **confirmed**. The approved plan supplies no byte allowance. |
| “The reviewed order begins 34.33 → 34.1 → 34.8” | `rg -e approvedExecutionPrefix -e recordedExecutionOrder "$P/epics.md"` | **confirmed** against the prefix and complete order. |
| “December 1 is unreachable” | `rg -F '2026-12-01 — unreachable' "$P/prd.md"` | **confirmed**. The recorded decision has no replacement date. |
