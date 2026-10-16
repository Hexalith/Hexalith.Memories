---
title: 'Story 27.4 current-profile contract alignment'
type: 'chore'
created: '2026-10-16'
status: 'done'
route: 'oneshot'
baseline_commit: '78f875ea47d826ed7abcd8d9641a3400e4c6176d'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The approved isolated-target profile migration already exists in code, but the
current GitHub authority and declared-observation contracts still publish former
PG2 pins. Align those two current contract tables with the approved manifest and
existing consumers, and identify the dated discovery record as historical.
Preserve all runtime code, dated linkage examples, prior approvals and evidence,
Spec 14 live tasks, sprint status, Production and A41 holds. This supporting
one-shot completes only documentation alignment; it grants no target contact,
bootstrap retry, qualification acceptance, Git mutation or story completion.

</frozen-after-approval>

## Implementation Notes

2026-10-16: Read-only investigation found QT1-P selectors already migrated in
the baseline commit. The source-bound D0 discovery collector intentionally
retains the former profile for its completed twelve-query archive. The approved
bootstrap consumed its only attempt before VM launch. A separate private
recovery proposal exists without actual recovery approval; it is not executed
by this documentation slice.

Before development, three independent unittest discovery units reported 12
profile-adoption, 43 GitHub-authority and 25 capture-semantics test cases, with
zero loader errors. The profile-adoption lane already ran 12/12 successfully
in 11.339s; raw log `/tmp/story-27-4-current-profile-investigation.log`.
No new tests are needed for these reversible text corrections. Verify exact
authoritative table rows against current consumer constants, and run the two
affected consumer lanes, including their existing old-profile/target refusals.

The existing owner handoff prohibits Git/sprint mutations during preparation;
it takes precedence over the generic one-shot commit/status actions. Story
27.4 remains in-progress and all six live tasks remain unchecked. No staging,
commit, branch, push or dependency operation is part of this slice.

2026-10-16 development: Corrected only the current profile hash in
`c1-github-authority-contract.md` and the profile identity/hash/context rows in
`c1-observation-pin-inspection-contract.md`; both now link the exact approved
manifest. The D0 archive and dated production-appendix linkage samples remain
historical and byte-identical. No review layer is skipped: this one-shot
configures only Blind Hunter, running in a context-free subagent.

Executed validation (all exits 0, zero failures/errors/skips):

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_github_authority.py' -v` — 43 tests in 12.585s; `/tmp/story-27-4-authority-contract-alignment.log`.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture_semantics.py' -v` — 25 tests in 0.488s; `/tmp/story-27-4-observation-contract-alignment.log`.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 /tmp/story-27-4-validate-contract-tables.py` — exact unique rows in the named ATX sections match consumer constants and the approved manifest; leaked tool markup absent. The initial scratch checker incorrectly normalized backticks inside row labels and refused its own comparison; correcting that checker, without changing the documents, produced the passing result.

Affected scope documentation: GitHub authority and C1.15 declaration profile/
target labels only. Attached negative evidence from the two executed commands:
`GitHubAuthorityTests.test_scope_cannot_label_historical_or_inexact_profile_workload_hashes_pg2`,
`test_cross_target_tenant_session_profile_workload_and_source_denials_before_dependents`,
`CaptureSemanticTests.test_former_profile_and_target_are_historical_only_even_when_self_consistent`
and `test_each_target_substitution_is_self_consistent_before_pin_refusal` all
pass. They preserve the Story 20.2 denial-before-dependency and Story 24.3
verifier/tenant-marker boundaries. These are offline tests, not live passes.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-16 | create-story | Codex recorded the documentation-only adoption baseline. | Adoption 12 -> 12, authority 43 -> 43, semantics 25 -> 25 test cases; phase/cumulative/external +0; zero loader errors. Exact discovery command is recorded below. | matched 1/1 against 78f875ea47d826ed7abcd8d9641a3400e4c6176d using git diff --name-only 78f875ea47d826ed7abcd8d9641a3400e4c6176d plus git ls-files --others --exclude-standard; only this spec, no exclusions. |
| 2026-10-16 | dev-story | Aligned two current contract tables with the already adopted profile; consumer tests and exact section/table validation pass. | Adoption 12 -> 12, authority 43 -> 43, semantics 25 -> 25 test cases; phase/cumulative/external +0; zero loader errors. Exact discovery command below. | matched 3/3 against 78f875ea47d826ed7abcd8d9641a3400e4c6176d using git diff --name-only 78f875ea47d826ed7abcd8d9641a3400e4c6176d plus git ls-files --others --exclude-standard; no exclusions. |
| 2026-10-16 | code-review | All three Blind Hunter findings triaged and corrected; exact linked grammar and 22 focused command cases pass. No deferred finding. | Adoption 12 -> 12, authority 43 -> 43, semantics 25 -> 25 test cases; command scope mapped at review as 22 -> 22 cases. Each phase/cumulative/external delta +0; zero loader errors. Exact four-scope discovery command in Code Review Record. | matched 4/4 against 78f875ea47d826ed7abcd8d9641a3400e4c6176d using git diff --name-only 78f875ea47d826ed7abcd8d9641a3400e4c6176d plus git ls-files --others --exclude-standard; no exclusions; all other tracked paths unchanged. |

Discovery command (run from the repository root):

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 - <<'PY'
import unittest
for directory, pattern in [('tests/tooling/access_telemetry_lifecycle', 'test_pg_onprem_2_adoption.py'), ('tests/tooling/access_telemetry_c1_interchange', 'test_github_authority.py'), ('tests/tooling/access_telemetry_c1_interchange', 'test_capture_semantics.py')]:
    loader = unittest.TestLoader()
    suite = loader.discover(directory, pattern=pattern)
    print(directory, pattern, suite.countTestCases(), 'test cases; loader errors', len(loader.errors))
PY
```

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-26.md`

- `docs/operations/c1-github-authority-contract.md`
- `docs/operations/c1-observation-pin-inspection-contract.md`

- `docs/operations/c1-command-declaration-inspection-contract.md`

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| :------ | :------ | :----------------------- |
| R1: File List contains only creation artifact. | low; patched | Reviewer read the initial creation snapshot. Development independently added both contract paths and a 3/3 reconciliation before the review returned. Final scope also includes the linked grammar correction, reconciled 4/4. |
| R2: Verification results are not recorded. | low; patched | Reviewer read the initial snapshot. The independently appended development record contains exact commands, measured results and the table comparison. Final review evidence also records the command-grammar lane. |
| R3: Linked command grammar still selects the former context. | low; patched | Confirmed exact SCOPE in its current command section contradicted the consumer and updated pin table. Changed that literal to the current KVM context and dated the supersession; preserved historical archives. No runtime/API change. |

All configured review work is complete. No unresolved or deferred finding remains.

## Code Review Record

2026-10-16: Root checked all three Blind Hunter findings against the current
files and consumer code. The first two referred to the earlier creation snapshot
and were corrected during development; the third required the simple linked
command-document correction. The frozen intent is unchanged. The added grammar
edit is directly necessary for the corrected table's linked API instructions.
The grammar discovery unit is mapped at review: `test_capture_commands.py`
had 22 cases before and after its text-only correction; phase/cumulative/external
+0. Other units remain adoption 12, authority 43 and semantics 25 test cases,
all with zero loader errors. No test, runtime or dependency file changed.

The table-validation script now also extracts the exact grammar section's sole
SCOPE array and compares its context/namespace against the consumer constants.
It passes with exact unique table rows and no leaked tool markup. The checker
is a temporary read-only verification artifact, not a new test or public API.

Read-only recovery inspection: `python3 /tmp/story-27-4-validate-recovery-proposal.py`
passes for the prepared private proposal SHA-256
`50321897837eec8ca816dd5f66ccf370e98045fbc78ac0e9812304efc2292b20`,
commands, ten bound artifacts, retained result/probe/full-read receipts and
retained OS metadata. The pinned LF private manifest and current CRLF repository
copy differ only in line endings; their raw hashes are deliberately distinct.
The initial scratch check selected the repository copy for that private raw pin
and refused; selecting the actual retained artifact corrected the check without
changing either manifest or proposal. Twelve original guest reads remain fixed,
`data.raw` is absent, and recovery status has no actual decision or executable
authority. No OS full reread, target query, disk write or guest launch occurred;
execution still requires fresh full OS/content/resource/ACL/authority checks.
Standing policy and target-profile approvals remain effective; the consumed
bootstrap attempt cannot be replayed.

Final command-grammar evidence:
`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_capture_commands.py' -v`
— 22 tests in 44.937s, exit 0, zero failures/errors/skips. Raw log:
`/tmp/story-27-4-command-contract-alignment.log`. Named denial evidence:
`CaptureCommandTests.test_wrong_cluster_namespace_selector_projection_and_container_refuse`
and `test_every_argument_including_later_pods_rehashed_substitution_refuses`
pass, preserving target/namespace attribution refusal for the linked grammar.
Across the four separate executed lanes, 102 test cases passed; none was added.

Exact final discovery command (all four units unchanged, zero loader errors):

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 - <<'PYDISCOVERY'
import unittest
for directory, pattern in [('tests/tooling/access_telemetry_lifecycle', 'test_pg_onprem_2_adoption.py'), ('tests/tooling/access_telemetry_c1_interchange', 'test_github_authority.py'), ('tests/tooling/access_telemetry_c1_interchange', 'test_capture_semantics.py'), ('tests/tooling/access_telemetry_c1_interchange', 'test_capture_commands.py')]:
    loader = unittest.TestLoader()
    suite = loader.discover(directory, pattern=pattern)
    print(directory, pattern, suite.countTestCases(), 'test cases; loader errors', len(loader.errors))
PYDISCOVERY
```

Root reviewed the complete final documentation diff and the supporting spec.
The exact four-path union matches File List with no exclusions. Markdown CRLF
and `git diff --check` pass. No broader test run is needed: only current prose
literals changed; code, tests, protected history and runtime configuration are
byte-identical to the baseline. The configured review is complete, and the
parent story's sprint/A41/Production holds and six live tasks stay unchanged.

The explicit four-path readiness gate passed in in-progress status with final
line `Story review readiness validation passed.` The one-shot's own completion
records only this reviewed documentation slice; it does not synchronize Story
27.4 to review/done, commit work or reopen the consumed operational budget.
