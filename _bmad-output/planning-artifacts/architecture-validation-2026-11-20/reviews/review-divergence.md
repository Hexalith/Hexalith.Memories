# Independent architecture consistency review

**Verdict — revise.** Two high-priority decisions affect when the product may report a completed projection or combine results from separate retrieval axes. Three medium findings affect pagination and the bytes sent to consumers. This review examined the November 20 working-tree spine at SHA-256 `e97c37a46cc0d5c557621d4f6cc075a7f22a8ff1f3a752dd6118d10ff93a2c28`. [Reviewed spine](../../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md)

### High · Acknowledgements can outlive a repair invalidation

AD-3 compares the source version, schema generation and embedding epoch. Consider an axis that wrote its projection successfully and queued its acknowledgement. The projection is subsequently lost. The coordinator records `reprojectionRequired`, then receives the delayed acknowledgement for precisely the same tuple. One coordinator could count that success toward repair completion. Another could require proof of a write or inspection after invalidation. In the first case the other two acknowledgements can restore `indexed` while the lost axis is still absent. ETag/CAS orders checkpoint writes. A coordinator can read the latest ETag and still apply an acknowledgement produced earlier. The erasure generation also stays constant through ordinary repair. See spine lines 115–119 and 490.

**Proposed correction.** Give each invalidation a repair generation or equivalent checkpoint token. Bind all three repair acknowledgements to it. Verify that delivering pre-invalidation successes after the invalidation leaves status, counters and case activity in progress until post-invalidation evidence arrives.

### High · A query can span tenant epoch cutover

AD-14 gives one actor authority to resolve the active schema generation and embedding epoch at each instant. A syntactic adapter can read the old pair immediately before activation and a vector adapter the new pair immediately afterward. Both can truthfully report AD-22’s required boolean that they read the active pair. AD-10 selects once but only sees the reported facts. Another implementation can bind a common pair to the whole request and refuse the mismatch. AD-14’s cutover fence protects command acceptance. AD-6 closes the query gate for tenant deletion. Neither specifies how an admitted multi-axis query proves that all its answers belong to the same pair. See spine lines 141–143, 167, 193–195 and 278.

**Proposed correction.** Capture a concrete pair for the request, require adapters to return the pair actually read, and define how a cutover between axis reads affects that result. A test that pauses one adapter across activation should establish whether the server retries, safely degrades, or fails. Verify that it never combines different pairs.

### Medium · The public rank contribution has two plausible scales

AD-9 defines `1 / (10 + rank)` and separately normalizes the composite. AD-12 requires every surface to preserve the per-axis contribution. A new consumer can publish `1/11` for a rank-one hit while the existing V1 response publishes `1.0`. Both can compute the same final ranking. The existing server tests explicitly expect `1.0`. See spine lines 161 and 179. [Fusion calculation](../../../../src/Hexalith.Memories.Server/Search/FusionEngine.cs) · [Rank-one V1 assertion](../../../../tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs)

**Proposed correction.** Name the raw term used in the weighted sum and the top-rank-normalized value carried by existing V1 per-axis fields. Include rank-one and rank-two examples with one raw wire fixture across presentations.

### Medium · Offset pagination has no stability boundary across requests

AD-9 promises that consecutive pages concatenate to the head of a single ordering. A new high-scoring unit can enter the same active epoch between two requests. The second offset can then repeat the last result of the first page. Another implementation could retain the first ordering and return its original second page. Final sorting and fixed per-axis depth are identical in both. The AD-3 managed-source check verifies the publication window within a read. See spine lines 119 and 161. The current hybrid service applies its offset after retrieving and ranking the candidates for that request. [Hybrid search](../../../../src/Hexalith.Memories.Server/Search/HybridSearchService.cs)

**Proposed correction.** State whether the guarantee covers stable inputs only or binds pages to one versioned result set. If it covers writes between requests, specify the continuation identity, expiration and recovery behavior. Exercise insertion or deletion between two requests.

### Medium · Set-valued reasons lack a canonical array order

AD-12 requires byte-identical packets, fixes object property order in a registry and describes packet reasons as a set. Two producers can discover the same two reasons in opposite orders. Both preserve the set and every property rule, yet their JSON arrays differ byte for byte. AD-22 orders neither the reasons nor their encoding. See spine lines 179 and 278.

**Proposed correction.** Give reasons a canonical order and deduplicate them before emission. Compare the extracted packet bytes for the same pair of reasons supplied in opposite input orders.

### Existing rules that already settle adjacent questions

AD-22 explicitly gives zero denominator weight to safe axes with no hits. AD-9 separates the fixed candidate depth from the caller’s offset. AD-3 already rejects acknowledgements for stale source versions and distinguishes projected visibility from completion. The two high findings depend specifically on ordinary repair at the same tuple and on a cutover during one logical query.

## Standalone validation rerun

The working-tree spine still hashes to `e97c37a46cc0d5c557621d4f6cc075a7f22a8ff1f3a752dd6118d10ff93a2c28`. The rerun checked the decision log, surrounding lifecycle rules, the existing fusion tests and the offset implementation. All five findings persist. The three medium findings have narrower consequences than a failing runtime test.

| Finding | Disposition | Counterevidence and remaining decision |
| --- | --- | --- |
| H1 · Repair acknowledgements | Confirmed high | An ETag can reject a writer holding the pre-loss checkpoint revision. AD-3 also permits the coordinator to reload the current ETag and consume an older success for the same tuple. The acknowledgement needs a link to work verified after invalidation. |
| H2 · Cross-axis cutover | Confirmed high | Synchronous invalidation gives all callers a consistent active pair **at one instant**. AD-6 drains product reads before deletion and AD-14 fences command acceptance during cutover. Neither rule binds the query’s separate axis reads across that cutover. A shared read barrier or pair identity with a defined mismatch outcome would settle it. |
| M2 · Rank contribution | Confirmed medium, specification clarification | The decision log calls `1/(10+rank)` the raw term. The shipped tests establish rank-one public score `1.0`. Preserve the V1 behavior by naming both scales in AD-9/12. There is no evidence here of an incorrect shipped composite. |
| M3 · Pagination | Confirmed medium, scope clarification | For stable inputs, fusion before paging and an ordinal tie-break settle the ordering. A write between requests can move an item across an offset in the very same epoch. FR22 permits pagination without choosing a snapshot contract. The smallest correction would state the stable-input condition if that is the intended guarantee. |
| M4 · Reason arrays | Confirmed medium, canonical encoding | AD-12 fixes property order and prohibits a surface reordering a received packet. Two producers can create the same unordered reason set and serialize arrays in different initial orders. Canonicalize at the producer or require every producer to use a shared serializer that does so. |

[Decision log](../../architecture/architecture-memories-2026-09-09/.memlog.md) · [Product requirements](../../prd.md) · [Fusion tests](../../../../tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs) · [Offset implementation](../../../../src/Hexalith.Memories.Server/Search/HybridSearchService.cs)
