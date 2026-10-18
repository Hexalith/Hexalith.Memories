---
title: 'Repair the Story 27.4 packaged validation child assembly boundary'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Repair HXP020 in the owning Builds source used by failed run `f482f16095f3027867cec221b0a35220`. Trace actual referenced/resolved assembly paths and identities, make the minimal owning correction, add a regression executing the packaged AppHost `--validate-platform` child, and verify offline. Preserve the failed run, workspace, diagnostics and retained/unverified resource verdict. Update the existing evidence handoff and prepare a concrete separately approved new-run proposal. Do not run functional-retention, start workloads, clean resources, publish packages, upgrade dependencies, attribute this failure to HXW010, close A41 or complete Story 27.4. This bounded blocker repair does not synchronize the parent story/sprint status.

</frozen-after-approval>

## Implementation Notes

- Investigation: the actual owner is `/home/administrator/projects/hexalith/platform/references/Hexalith.Builds`, clean detached HEAD `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`; the Memories reference is a distinct checkout and will not be edited. External Platform has unrelated dirty planning/evidence paths; preserve them.
- Actual model SHA `c50613998a55e69318ba6c877b0400ad9d9af96f44f37f1ad6406adf5a3c5087` references Tooling4.31.2.0, matching root CLI Tooling SHA8285cbea. Packaged AppHost references Tooling1.0.0.0 and carries that lower identity. `Tools/build-g4-tool-packages.ps1` versions only pack, after an unversioned solution build. `RunTopology.ComposePlatform` uses the default assembly context shared with the host.
- Small reversible footprint: propagate the same version properties to restore/build/pack; add an offline packaged-child fixture/gate and hook it before successful inventory. No intent gaps, irreversible operations or dependency changes. Use newly created private artifact directories; never overwrite the failed installation/model/plan.
- Verification: record red old-package child failure and actual host binding; run corrected official local build/pack and packaged-child regression, focused existing validation tests, whitespace and preservation hashes. Local diagnostic packages remain unsigned, unpublished, release-ineligible. Leave source changes uncommitted under the task's preservation scope.
- Private preservation baseline: `/home/administrator/.local/state/hexalith/memories/access-telemetry/loader-fix-20261018/preservation-before.json` hashes63 failed-run files. `evidence-before.md` preserves all original report bytes for append-only comparison.

- Final outcome: the requested version now reaches restore/build/pack; the official builder runs the packaged validation-child regression before inventory. The old package still fails child3/0x80131040; the fixed package and exact retained model pass child0. Five focused xUnit cases and63 artifact scenarios pass; wrapper controls prove consistent version arguments and inventory refusal after loader failure.
- Review corrections isolate fixture MSBuild imports even under the checkout; preserve partial timeout logs/structured failure; bound execution, termination and collection waits to one deadline; add a BeforeStartEvent sentinel; emit receipt success after all assertions. Deliberate one-second timeout refused, retained output and reported incomplete diagnostics/unknown exit rather than fabricating termination. Current fixture regression was rerun after corrections.
- Source remains uncommitted in the actual owning Builds checkout. No Platform source/dependency change, primary Builds gitlink update, workload, functional test, cleanup, publication, A41 edit or story/sprint completion occurred. The root evidence file preserves all original body bytes. New-run proposal is unapproved and expires if its proposed admission window lapses.

## Review Triage Log

| Finding | Verdict and disposition |
| --- | --- |
| Timeout loses logs/receipt | Medium, patched: stream directly to retained log files; finally writes a failed/timed-out receipt. Forced-delay control retains composition stdout and both log files. |
| Unbounded termination/drain waits | Medium, patched: execution reserves collection time, every wait uses the same remaining deadline, asynchronous stream copies accept cancellation. No unbounded WaitForExit/GetResult remains. Timeout reports incomplete diagnostics and unknown exit truthfully. |
| Default output inherits checkout build configuration | High, patched: disable Directory.Build.props/targets and Directory.Packages.props discovery for the fixture; copy only source/project files. In-repository output control passes with cleared feeds. |
| Readiness absence does not prove no startup | Low, patched: fixture subscribes to BeforeStartEvent, writes a sentinel and throws before resource startup; successful validation asserts sentinel absent. Exact-model probe separately retains zero runtime-tool trap calls; no broader live proof is claimed. |
| Wrapper version and loader-refusal controls missing | Medium, patched: existing gate controls now execute a sandbox copy of the real wrapper, capture all four dotnet calls and force post-pack loader refusal. All four artifacts remain, inventory absent, versions equal; lane passes. |
| Receipt outcome and publication-proof expansion | Low for receipt ambiguity, patched: result remains failed until every contract/startup assertion passes, including child-exit0 assertion failures. Publication expansion rejected as unsupported by a reachable bypass: the official wrapper runs the loader gate before either inventory mode and full qualification invokes that wrapper; existing publisher requires immutable clean qualified source. These uncommitted diagnostic artifacts remain release-ineligible. No publisher acceptance is claimed or authorized. |
