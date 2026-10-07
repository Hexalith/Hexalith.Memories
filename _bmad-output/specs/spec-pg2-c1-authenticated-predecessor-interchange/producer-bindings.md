# Producer bindings and semantic artifact validation

## Current inventory, not a registration

| Gate | Current source / ownership evidence | Interchange eligibility |
| --- | --- | --- |
| C1.15 / PG2 | Prepared `tools/verify-access-telemetry-c1.ps1`; v2 capture, `tools/access-telemetry-c1-profile.ps1` and loaded `tools/access-telemetry-c1-component-backend.ps1`; sixteen approved inputs. Historical Story 27.21 is done for PG1 only; PG2 renewal has no registered successor. | Candidate source binding only. Requires an approved successor registration, authenticated producer execution and current independent disposition; no accepting entry exists here. |
| C1.16 / PG2 | Registered/done Story 27.22; identity collector above plus `tools/verify-access-telemetry-c1-linkage.ps1`. Existing identity and linkage envelopes are v1. Independent full-set linkage acceptance covers the recorded closed window. | Story registration is real; a schema-compatible authenticated interchange binding and session eligibility have not been demonstrated. Preserve acceptance; hold accepting entry until P5/P6. |
| C1.1–C1.14, C1.17–C1.25 | Twenty-three held/unregistered gate-owner definitions. | No implemented source/schema/verifier entry inferred from draft story numbers or filenames. Missing entry refuses. |

The existing `STORY_27_4_PRODUCERS` registry covers C0/C2-C6/terminal machinery;
it is **not** a C1.1–C1.25 producer registry. An unrelated valid Git blob,
including a C2 producer or `.editorconfig`, cannot be used as a C1 binding.

This proposed consumer initially has **no demonstrated accepting C1 entry**.
A denial/inventory table is not successor registration. Supported accepting
entries arrive only through their separately approved ownership transactions.

## Closed registry entry

Proposed exact entry fields:

`gate`, `profileId`, `registeredStory`, `registrationReceipt`, `producerPath`,
`helperPaths`, `inputPaths`, `captureSchema`, `verifierPath`, `verifierSchema`,
`commandContract`, `cleanupRequired`, `reviewRolePolicy`.

`gate` is one literal C1 identifier; profile is `PG-ONPREM-2`. Story and source
paths are canonical repository-relative paths. Helpers/inputs are distinct,
lexically ordered arrays with complete required membership. Schema identifiers
and verifier path identify real implemented contracts. `registrationReceipt` is
`Ref` to the actual approved registration evidence, whose supported authority
interpretation is P1/P5. `commandContract` and `reviewRolePolicy` are `Ref` to
approved closed grammar/role-policy artifacts. `cleanupRequired` is boolean.
Their contents and source revisions must be verified, not guessed from names.

Hash J1 of the entry as `registryEntrySha256`; hash J1 of the ordered entries
as `registrySha256`. Every entry must resolve to the approved root-owned story,
implemented producer/verifier, reviewed helper/configuration set and required
done owner state. Planning prose and a `done` string alone do not authenticate
approval. No wildcard entry, generic collector fallback, pending registration
or fixture registry is eligible in deployed use.

Per-capture source checks require the full authorized commit and **every** used
producer/helper/config input. Recompute canonical Git blob hashes and separately
verify executed/read-byte receipts against retained approved source snapshots.
Reject unsupported Git filters, untracked/dirty/mismatching sources, omitted or
extra helpers, substituted approved-looking paths and unapproved source revisions.
Source/registry/verifier drift invalidates the binding rather than inheriting
approval from a former version. Git SHA identifies bytes, not reviewer authority.

## Verifier sequence and atomic output

1. Require approved authority, policy and accepting registry inputs. Refuse unknown schema/gate/profile before any launch. Initialize bounded local snapshot/aggregate budgets; no collector is executed.
2. Resolve each reference beneath approved external custody, reject every alias/symlink component, and safely open regular files without following links. Address ancestor-symlink/path replacement races through descriptor-relative safe traversal or an equivalently reviewed snapshot boundary. Hash and parse the same bounded bytes.
3. Validate exact capture schema and semantic observations with its registered verifier; bind profile/workload/session/target, producer invocation, full source set and all command receipts. Recompute counts from permitted observations and enforce required command cardinality/order rather than trusting arbitrary positive counts.
4. Verify authenticated producer execution/session and separate disposition. Compare all embedded facts to the capture, registered binding and expected record; independently check role, independence, time/expiry and revocation.
5. For mutating gates, validate actual owned-resource cleanup receipts against the approved baseline and session scope. Refuse missing, failed, partial or unrelated cleanup. Read-only status must be proved by the command contract, not caller choice.
6. Construct accepted-gate records from these verified inputs. Require exactly the twenty-five distinct literal gates; distinct paths/hashes across per-gate capture/disposition/accepted records; one eligible profile/workload/target/session. Shared supporting source snapshots may be reused and budgeted once; gate evidence cannot.
7. Freeze manifest bytes; verify two real independent bundle-approval receipts against that exact manifest. Validate any asserted predecessor summaries by deriving them again. Recheck current session/authority/revocation at publication and use.
8. Publish the final predecessor only to a fresh exclusive path, with immutable finalization. Failure publishes no `passed` gate/predecessor. Retained captures and decisions are never overwritten or rewritten. A bounded secret-safe rejection report may be retained separately without gate credit.

Session/policy/registry missing data is a hard refusal. A caller cannot opt out of
artifact, authority, cleanup or freshness checks. Retained historical inspection
has a separate result type that cannot satisfy a downstream authorization input.

Apply field-aware secret checks. The Python generic secret-key filter currently
rejects key aliases containing `authorization`, `secret` or `token`; v2 legitimately
uses fixed credential-variable names in command arguments. Reusing primitives
requires a closed field schema and decoded value checks, never a global secret
filter bypass. Do not retain credential values or unsafe raw command output.

## Consumer enforcement boundary

Every C2-C4 launcher and offline/terminal consumer that reads C1 must validate the
new complete interchange. Enumerate entry points in P7; no downgrade through
the existing `_validate_predecessor`, `--input` path, `successors` alternative or
`require_current_freshness=False` may authorize execution. The consumer must
revalidate the exact retained manifest, referenced bytes, authority and current
session before its first target/dependency call. Denial tests install a forbidden
dependency and prove zero calls.

That enforcement permits only the consumer's separately approved scope.
It does not mark all successors done, grant C2-C4 execution, satisfy post-evidence
C5/C6, mutate A41, or remove the Production disabled configuration.
