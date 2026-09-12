---
review_lens: accessibility-and-recovery
reviewed: 2026-09-12
targets:
  - DESIGN.md
  - EXPERIENCE.md
verdict: revise-active-cli-claims-before-binding; future-web-gate-is-fail-closed
severity_counts:
  critical: 0
  high: 1
  medium: 3
  low: 2
---

# Accessibility and Recovery Review

## Overall verdict

**Revise the active-CLI claims before treating this migration as an implementation-conformance statement; the future-web activation contract is appropriately fail-closed.** The spines now contain a strong WCAG 2.2 AA floor, inherited FrontComposer focus/landmark behavior, non-color and equivalent-representation rules, a dated route/state/browser/AT evidence matrix, and unusually complete trust and recovery semantics. They also keep the existing Web RCL and specimen outside product scope. The remaining high-impact defect is a false blanket description of current CLI JSON/format behavior. Three medium issues could mislead accessibility, security, or activation work. None justifies a critical finding because no product web route is claimed and the PRD still records NFR37 as unverified.

Severity reflects downstream impact: **high** can break a current automation contract; **medium** can produce a materially unsafe or nonconforming implementation unless reconciled; **low** weakens traceability or wording without defeating the existing fail-closed gates.

## Scope and method

The lens compared both spines and their frontmatter sources with the live PRD, repository UX baseline, migration memlog, current CLI/contract extraction, current Web/FrontComposer extraction, and the cited implementation/tests. It checked the active CLI separately from the inactive web horizon: WCAG 2.2 AA, keyboard and focus, reflow/zoom, forced colors, reduced motion, assistive technology, terminal reading order, redirected/automation semantics, labels and accessible states, authorization/privacy, evidence-trust distinctions, destructive confirmation, progress/cancellation, and empty/degraded/offline/timeout/no-safe-axis recovery. (`DESIGN.md:1-32`; `EXPERIENCE.md:1-36`; `../../prd.md:1109-1123`, `:1196-1203`, `:1211-1215`; `../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md:5-51`; `.memlog.md:6-21`, `:23-49`; `.working/extract-protocol-implementation.md:204-263`; `.working/extract-ui-implementation.md:55-99`)

This is a contract/evidence review. It does not claim a fresh manual terminal-screen-reader run, product-route browser run, or assistive-technology pass.

## Critical findings

None.

## High findings

### A11Y-R01 — The blanket current CLI JSON contract is false for shipped export success and cancellation

**Evidence.** The current grammar says every command inherits `--format`, lists both export commands as shipped, then states without exception that JSON success emits `{ schemaVersion, command, data }`, JSON error emits `{ schemaVersion, command, error }`, and all output forms preserve the same meaning. (`EXPERIENCE.md:38-61`) Both shipped exporters deliberately ignore non-human `--format` on success and stream raw JSON, with the exception announced on stderr. (`../../../../src/Hexalith.Memories.Cli/Commands/ExportCaseCommand.cs:20-24`, `:116-135`; `../../../../src/Hexalith.Memories.Cli/Commands/ExportTenantCommand.cs:18-20`, `:104-123`) In JSON mode, cancellation is also intentionally not an envelope: stdout is empty, stderr receives `Cancelled.`, and exit code 130 carries the machine signal. (`../../../../src/Hexalith.Memories.Cli/Execution/CliCommandExecutor.cs:160-178`; `../../../../tests/Hexalith.Memories.Cli.Tests/Cli/CliCommandExecutorErrorTests.cs:219-240`)

**Downstream impact.** A script generated from the spine can attempt to parse an export as a CLI envelope or wait for a JSON cancellation object that never arrives. This breaks the active automation path and undermines the stated equivalent-semantics contract at precisely the error/recovery boundary NFR37 is meant to stabilize.

**Required fix.** Qualify the envelope rule as the contract for formatter-routed commands and document the two current exceptions at point of use: export success is the portable raw-JSON artifact and ignores `--format` with a stderr warning; cancellation currently uses stderr plus exit 130 and no JSON body. Then make an explicit product decision: either retain these versioned exceptions with automation tests, or change implementation to one canonical machine contract. Do not call byte shape identical to semantic equivalence.

## Medium findings

### A11Y-R02 — The active CLI accessibility section does not state that its evidence gate is still unverified

**Evidence.** The phase matrix and general qualifier correctly say the CLI is active but incomplete and that normative behavior is target contract unless current status says otherwise. (`EXPERIENCE.md:24-36`) At point of use, however, the heading `Active CLI` and present-tense requirements do not repeat the evidence status or name NFR37's required verification. (`EXPERIENCE.md:243-252`; `.memlog.md:16`) The authoritative source explicitly classifies NFR37 as **not started / current-contract evidence absent** and requires narrow-terminal, redirected-output, no-color, duplication, timeout/cancellation, secret-sanitization, semantic-parity, and keyboard-only walkthrough evidence. (`../../prd.md:1113-1123`, `:1211-1215`) Existing focused tests prove token redaction and cancellation behavior, but repository search exposes no NFR37 evidence suite or recorded full command-tree walkthrough; that is partial implementation evidence, not closure. (`../../../../tests/Hexalith.Memories.Cli.Tests/Cli/TokenRedactionTests.cs:17-40`, `:160-180`; `../../../../tests/Hexalith.Memories.Cli.Tests/Cli/CliCommandExecutorErrorTests.cs:219-240`)

**Downstream impact.** A story or release reviewer can read the correct target rules as a claim that the current primary surface already conforms, masking missing narrow-terminal, non-interactive, cross-format, and manual keyboard evidence.

**Required fix.** Add a point-of-use delivery note: NFR37 is binding now but unverified as of 2026-09-12. Carry its exact evidence checklist and require a dated artifact/owner/disposition before claiming active-CLI conformance. Preserve the existing target behavior; change only the evidence claim.

### A11Y-R03 — Credential entry and authentication recovery disagree on the safe configuration name

**Evidence.** The CLI table exposes literal `--token <value>` while the privacy floor only says secrets must not enter copied output, accessible names, diagnostics, or suggestions; it does not preserve the shipped warning that argv is visible in shell history. (`EXPERIENCE.md:38-40`, `:204-210`, `:245-252`) Current help tells users to prefer `HEXALITH_MEMORIES_API_TOKEN`, and that is the environment variable the resolver actually reads. (`../../../../src/Hexalith.Memories.Cli/Commands/CliGlobalOptions.cs:20-25`; `../../../../src/Hexalith.Memories.Cli/Configuration/EnvironmentVariableConfigurationSource.cs:8-15`, `:56-62`) The current HTTP 401 recovery suggestion instead names nonexistent `HEXALITH_MEMORIES_TOKEN`. (`../../../../src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs:542-554`) Output-value redaction itself has useful coverage. (`../../../../tests/Hexalith.Memories.Cli.Tests/Cli/TokenRedactionTests.cs:17-40`, `:145-180`)

**Downstream impact.** A user following an authentication recovery message can set the wrong variable, while examples derived only from the spine can normalize putting credentials in shell history. This is both a recovery failure and an avoidable privacy hazard.

**Required fix.** At the global-option definition, state that `--token` is supported but visible in argv/history and that `HEXALITH_MEMORIES_API_TOKEN` is the preferred noninteractive source. Correct the 401 suggestion to that exact name and add a recovery test that resolves the suggested variable. Keep literal secrets out of examples and artifacts.

### A11Y-R04 — Destructive-dialog focus ownership is one layer too broad

**Evidence.** The component table says `FcDestructiveConfirmationDialog` owns focus, Escape, confirm/cancel, and plain-text rendering, while the general ownership rule correctly leaves live focus with the product host and the activation matrix requires start/end focus evidence. (`EXPERIENCE.md:105-129`, `:222-232`, `:254-260`) The current Memories wrapper renders the FrontComposer dialog body directly. (`../../../../src/Hexalith.Memories.Web/Components/Interaction/MemoriesActionConfirmation.razor:8-16`) FrontComposer documents that the body is meant to be opened through `IDialogService`; its own class supplies safe autofocus, Escape and lifecycle callbacks, but the dialog context is null when rendered standalone. (`../../../../references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor:5-28`; `../../../../references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.cs:10-28`, `:42-92`) Current tests explicitly defer real trap/return and AT behavior to a live product route. (`../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17FocusContractTests.cs:22-28`, `:121-142`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs:183-209`)

**Downstream impact.** A host can mount the specimen-shaped wrapper inline and believe destructive-action focus containment and return are complete, even though no service/provider dialog lifecycle or live focus evidence exists.

**Required fix.** Split the ownership statement precisely: Memories owns domain copy and callbacks; `FcDestructiveConfirmationDialog` owns its dialog body, safe autofocus, Escape and callbacks; the product host must launch it through the shell-provided Fluent `IDialogService`/provider lifecycle and prove initial focus, trap, Escape, cancel default, and return on the activated route. Keep destructive consequences, rollback boundary, and tenant/case/target naming as already specified.

## Low findings

### A11Y-R05 — The frontmatter does not identify the implementation evidence behind current-state claims

**Evidence.** Both source lists name the PRD, legacy artifacts, September validation, and repository UX baseline, but not the current protocol/UI extracts or a durable implementation evidence register. (`DESIGN.md:1-11`; `EXPERIENCE.md:1-11`) Yet the spines make exact current grammar, component-inventory, conformance-specimen, and activation-gap claims whose evidence lives in `.working/extract-protocol-implementation.md:204-263`, `.working/extract-ui-implementation.md:9-39`, `:55-99`, and the current seven-item Web gap registry. (`../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs:155-210`)

**Downstream impact.** Reviewers can reproduce the intended UX lineage but not efficiently revalidate delivery facts after code changes; known axe, target-size, overflow, product-route, browser, touch-device, and live-AT gaps can drift out of the activation review.

**Required fix.** Add a stable implementation-evidence source or explicit activation dependency that resolves to the protocol extract, UI extract, and fail-closed gap registry. Do not elevate the extracts above the PRD for product intent; use them only for delivery facts and make activation disposition all currently open gaps.

### A11Y-R06 — “Remain keyboard-only” describes exclusivity rather than operability

**Evidence.** The CLI accessibility list says prompts, confirmations, help, and errors “remain keyboard-only.” (`EXPERIENCE.md:245-252`) The source requirement instead says users can complete those interactions “by keyboard alone,” which establishes keyboard operability without excluding pointer, voice, paste, or assistive input. (`../../prd.md:1211-1215`)

**Downstream impact.** The phrase can be copied into acceptance criteria as a prohibition on other input modalities rather than an accessibility floor.

**Required fix.** Use “fully operable by keyboard alone” or “keyboard operable without requiring a pointer,” while preserving other supported input and assistive technologies.

## Verified strengths and evidence notes

- **The web floor is now complete enough to gate activation.** WCAG 2.2 AA, keyboard completion, visible/unobscured focus, names/descriptions, non-color state, grid semantics, ordered graph equivalence, reduced motion, forced colors, SC 2.5.8, 200% text resize, 400% zoom/320-CSS-pixel reflow, supported browser/AT, expected announcements, focus endpoints, dated artifacts, and defect/waiver disposition are explicit. Automated component/axe evidence cannot substitute for manual AT evidence. (`DESIGN.md:177-193`, `:231-241`; `EXPERIENCE.md:254-268`; `../../prd.md:1196-1203`)
- **Future-versus-current claims are otherwise honest.** The Web RCL is called a 19-component non-product conformance inventory; routing, authorization, loading, notifications, and live focus remain unimplemented host work, and NFR32/NFR35 stay inactive until an approved product capability exists. (`DESIGN.md:203-229`; `EXPERIENCE.md:24-36`, `:254-260`; `.working/extract-ui-implementation.md:91-99`)
- **FrontComposer inheritance is explicit and avoids duplicate accessibility mechanics.** The spines inherit `FrontComposerShell` and FC-A11Y, while the shell supplies skip links, one focusable `main`, theme/density watchers, connection/pending-command status, and Fluent providers. (`DESIGN.md:12-18`, `:171-181`; `EXPERIENCE.md:228`, `:254-260`; `../../../../references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:31-52`, `:118-185`)
- **Accessible state and trust distinctions are excellent.** Relevance is not fact; evidence strength, freshness, projection completion, authorization and degradation remain separate; every axis can be available-with-hits, available-with-no-hits, unavailable, excluded, or collectively no-safe-axis; restricted responses contain no evidence. (`EXPERIENCE.md:16-22`, `:97-103`, `:131-146`, `:163-173`, `:204-220`; `DESIGN.md:231-240`)
- **Recovery closure is broad and explicit.** The surface matrix covers empty classifications, omission, partial/degraded, no-safe-axis, tenant denial, timeout/network, failed stages, erasure verification, rollback and safe retry. Offline operation is deliberately out of scope; unreachable service is a recoverable error, not silent offline mode. (`EXPERIENCE.md:175-202`, `:222-241`, `:270-400`)
- **Progress and destructive safety have a strong target contract.** Long operations disclose admission, queue delay, state, last update, duration certainty, affected capability, cancellation/dismissal limits, timeout, completion/failure and safe retry; destructive/scope-expanding work names scope, target, consequence and rollback, with cancel as safe default and dry-run where supported. (`EXPERIENCE.md:148-173`, `:222-228`; `.memlog.md:28-31`)
- **Authorization and privacy fail closed.** Claims authorize requested tenant scope; app identity, display metadata, channel credentials and case membership do not. Unauthorized responses disclose no restricted evidence, and telemetry is sanitized and explicitly not a tamper-evident audit trail. (`EXPERIENCE.md:40`, `:74`, `:97-103`, `:204-210`; `.memlog.md:13`, `:26-30`)

## Severity counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 3 |
| Low | 2 |

## Resolution check

**Checked 2026-09-12 against the patched spines.** Narrow verification re-read the exact CLI-status and accessibility sections, compared export/cancellation wording with their current command/executor tests, checked the configured token variable and 401 recovery text, and rechecked the dialog/provider and implementation-evidence mappings. No broad test suite was rerun because the changes are documentary and the remaining issue is directly visible in one constant.

- **A11Y-R01 — Resolved.** The two export rows now state that success is the raw portable JSON artifact and that non-human `--format` is ignored with a stderr warning. The formatter-routed envelope rule is explicitly qualified, cancellation is documented as stderr plus exit 130 with no JSON body, and cross-form semantic parity is correctly identified as the unclosed NFR37 target rather than byte-shape identity. (`EXPERIENCE.md:48-71`; `../../../../src/Hexalith.Memories.Cli/Commands/ExportCaseCommand.cs:20-24`, `:116-135`; `../../../../src/Hexalith.Memories.Cli/Commands/ExportTenantCommand.cs:18-20`, `:104-123`; `../../../../tests/Hexalith.Memories.Cli.Tests/Cli/CliCommandExecutorErrorTests.cs:219-240`)
- **A11Y-R02 — Resolved.** Point-of-use maturity now names NFR37 as not started with no current-contract evidence, release governance keeps it inside the no-go G6 boundary, and the Active CLI section requires the dated artifact, owner, disposition, exact automated dimensions, and command-tree keyboard walkthrough before conformance is claimed. (`EXPERIENCE.md:32-46`, `:253-264`)
- **A11Y-R03 — Partially resolved; one medium implementation recovery defect remains open.** The spine now warns that `--token` is visible in process arguments/history, prefers the exact `HEXALITH_MEMORIES_API_TOKEN` source, bans literal secrets in examples/artifacts, and explicitly marks the current 401 wording as debt. (`EXPERIENCE.md:48-50`) The shipped error catalogue still recommends nonexistent `HEXALITH_MEMORIES_TOKEN`, while help and the resolver use `HEXALITH_MEMORIES_API_TOKEN`; the recovery path remains wrong until that constant and a resolving recovery test are corrected. (`../../../../src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs:547-554`; `../../../../src/Hexalith.Memories.Cli/Commands/CliGlobalOptions.cs:20-25`; `../../../../src/Hexalith.Memories.Cli/Configuration/EnvironmentVariableConfigurationSource.cs:8-15`)
- **A11Y-R04 — Resolved.** Action Confirmation now assigns each layer precisely and requires host launch through the shell-provided Fluent `IDialogService`/provider lifecycle plus activated-route proof of initial focus, trap, cancellation, and return. The machine-readable visual binding carries the same activation boundary. (`DESIGN.md:119-125`; `EXPERIENCE.md:119-139`)
- **A11Y-R05 — Resolved.** Both frontmatter blocks add a separate `implementation-evidence` registry covering the UI/protocol extracts, conformance allowlist, and validation inventory; the activation rule consumes the live gap registry instead of duplicating its contents. (`DESIGN.md:12-19`; `EXPERIENCE.md:12-19`, `:266-272`)
- **A11Y-R06 — Resolved.** The CLI rule now requires operation “by keyboard alone” while explicitly preserving other supported input and assistive technologies. (`EXPERIENCE.md:253-264`)

### Revised open-severity counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 0 |
| Medium | 1 |
| Low | 0 |
