# Lifecycle and erasure review

20 November 2026 · Architecture Update gate

Reviewed revision `89106ca99d87f6f708bbaa0939c20279a9b1d1352f84bf78918281dce038f6a2`. The review traced the earlier H3, H4, H5 and M1 findings through AD-5, AD-6, AD-14, AD-16, AD-17 and AD-21. [Earlier validation](../../../architecture-validation-2026-11-20/validation-report.md) · [Architecture spine](../ARCHITECTURE-SPINE.md)

| Case | Outcome at the reviewed revision |
| --- | --- |
| H3 · No export, no backup or unused tenant | AD-16 requires fences before enumeration, affirmative scope and backup-catalogue evidence, and a final recheck. Populated targets still need a known-present sample. Missing or inaccessible evidence leaves deletion pending. |
| H4 · Worker stopped after key destruction | The erasure worker recovers the committed deletion revision, write generation, inventory and pre-shred outcomes from the platform work stream. It must authenticate again, verify lineage, reconcile the precise destruction request and verify the post-shred outcomes. Register CAS and matching identity let a lost reply or competing retry converge on one tombstone. |
| H5 · Unexpired telemetry and last-row disposal | Ordinary access ends at deletion. The principal-token map is physically purged. Platform Operations accepts the handoff and later verifies complete expiry, removes the partition and locator, and records the outcome. The handoff receipt’s prescribed durable locator exposes a residual issue detailed below. |
| M1 · Concurrent retention and erasure appends | Platform Operations is the sole writer of retention receipts to `retention-purge`. The AD-16 worker writes preparations to `erasure-work` and only terminal tombstones to the register. A retention append cannot advance the register revision. |
| Read revocation during cutover | AD-6 closes tenant read admission before committing deletion and waits for delivery to end or confirmed cancellation. A previously admitted AD-14 read lease cannot authorize another byte after revocation. Missing gate state never proves drain. |

The initial review requested three clarifications. A durable handoff receipt that holds the same tenant locator or partition alias as retained events can preserve the mapping beyond final purge. Keep the live binding exclusively in purgeable Operations custody and use an independent handoff identifier in permanent records. A partially provisioned tenant whose telemetry partition never existed needs a positive empty/inapplicable handoff and receipt without provisioning during erasure. AD-21 should also give the export index and lifecycle mirror unambiguous locations outside all three erasure and retention streams.

The failure cases above are design walkthroughs. End-to-end erasure, restart recovery, authorization denials, last-record disposal and post-shred tombstone uniqueness still require implementation evidence.


## Final disposition

**Pass.** Verified revision `3c7bf368360f37c4d77d15ed9e9ee402660e767bbb9fbe85203f813215999f0e`. AD-17 reserves the live locator and partition binding to the purgeable operator mapping. Permanent evidence uses an independent random handoff ID and omits reusable locator data. An affirmatively unprovisioned partition can take the no-record receipt path without creating a resource. AD-21 gives the export index and lifecycle mirror separate streams. These changes close the residual issues. H3, H4, H5 and M1 pass the design review. The implementation and qualification evidence identified above remains open.
