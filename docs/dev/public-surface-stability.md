<!-- Review cadence: update when a consumer-facing host project is renamed, its assembly name / root namespace / PackageId changes, or quarterly — whichever comes first. Last reviewed: 2026-06-24. -->

# Public-surface stability contract — host project names (Story 18.1)

Downstream Aspire AppHosts reference `Hexalith.Memories` by **generated project symbols** and consume the
published MCP package by **PackageId**. Those names are therefore a public contract: renaming a project,
its assembly, its root namespace, or its NuGet `PackageId` silently breaks consumers even though no C# member
signature changed. This document records the contract and the breaking-change rule that protects it.

Origin: MEM-1 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27). A clean clone with
root submodules initialised must build the full `.slnx` without submodule-drift surprises; that requires the
two consumer-facing host projects below to keep resolving under stable names.

## Contract — consumer-facing host projects

| Project | Project name | Assembly name | Root namespace | PackageId | Aspire metadata symbol |
| :------ | :----------- | :------------ | :------------- | :-------- | :--------------------- |
| Server | `Hexalith.Memories.Server` | `Hexalith.Memories.Server` | `Hexalith.Memories.Server` | — (not packed, `IsPackable=false`) | `Projects.Hexalith_Memories_Server` |
| Mcp | `Hexalith.Memories.Mcp` | `Hexalith.Memories.Mcp` | `Hexalith.Memories.Mcp` | `Hexalith.Memories.Mcp` | `Projects.Hexalith_Memories_Mcp` |

Neither csproj sets an explicit `<AssemblyName>` or `<RootNamespace>`, so the effective assembly name and
root namespace **default to the project (csproj base) name**. The contract is therefore "these defaults must
not change" — do not add overriding tags and do not rename the projects. `Hexalith.Memories.Mcp` sets an
explicit `<PackageId>Hexalith.Memories.Mcp</PackageId>`; that value is part of the contract because it is the
published NuGet package consumers depend on.

## The guarantee (breaking-change rule)

These names are a **stability contract** for downstream AppHosts. Any future rename of a project, its assembly
name, its root namespace, or (for Mcp) its `PackageId` is a **breaking change** and requires a
breaking-change note. For `Hexalith.Memories.Mcp` specifically — it is the published package — the rename
must also carry a semantic-release `BREAKING CHANGE:` footer so the version bumps major; a quiet rename would
strand every consumer pinned to the old package id.

This mirrors the additive-only posture documented elsewhere (cf. ADR-7.2-001 in
[cli-output-formats.md](./cli-output-formats.md): adding a new optional field is non-breaking;
renaming / removing / changing semantics is breaking).

## Why the Aspire symbol is load-bearing

The `Aspire.AppHost.Sdk` (pinned at `Aspire.AppHost.Sdk/13.3.3`) generates, for **each**
`<ProjectReference>` in the AppHost csproj, a `public class Projects.<SanitizedName> : Aspire.Hosting.IProjectMetadata`,
where every `.` in the referenced project name becomes `_`:

```text
Hexalith.Memories.Server  ──►  Projects.Hexalith_Memories_Server
Hexalith.Memories.Mcp     ──►  Projects.Hexalith_Memories_Mcp
```

Because the project name is the sole input to that sanitisation, **renaming the project silently changes the
generated symbol** — a downstream `builder.AddProject<Projects.Hexalith_Memories_Server>(...)` then fails to
compile with no member-level API change to point at. That implicit coupling is exactly why a rename is a
breaking change rather than a refactor.

## Automated enforcement

The symbol-resolution half of this contract is enforced by a compile-time guard test:
[`tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostProjectResolutionTests.cs`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostProjectResolutionTests.cs).
It names both `Projects.Hexalith_Memories_Server` and `Projects.Hexalith_Memories_Mcp`, so the
`Hexalith.Memories.IntegrationTests` assembly fails to build the moment either symbol stops resolving, asserts
each generated `ProjectPath` still ends with the expected `.csproj`, and asserts the generated symbol *shape*
(`Projects.Hexalith_Memories_*`, the dots→underscores derivation above). The test runs in the default
(no-Docker) test lane — it is a plain `[Fact]` and does not provision containers.

The assembly-name and root-namespace half is enforced by a sibling runtime guard test:
[`tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs).
It reflects over a stable public type from each assembly and asserts the assembly simple name is
`Hexalith.Memories.Server` / `Hexalith.Memories.Mcp` and the root namespace prefix still holds — so a project
rename or an added `<AssemblyName>` / `<RootNamespace>` override fails the build's test lane (also no Docker).

Only the **PackageId** half remains review-enforced — it is a pack-time NuGet property and is not reflectable
from a built assembly. Any PR that renames the Mcp `PackageId` must add a breaking-change note and a
`BREAKING CHANGE:` footer for the published package.

## References

- Story 18.1 — AppHost project-resolution guard and public-surface stability contract.
- MEM-1 — Parties consumer integration intake (Sprint Change Proposal 2026-05-27): symbols resolve at
  `src/Hexalith.Memories.AppHost/Program.cs:151` / `:226`; the reported EventStore redis-parameter "drift" was
  a stale submodule pin (see [eventstore-integration.md](./eventstore-integration.md) §1.2.1).
- [experimental-apis.md](./experimental-apis.md) — companion stability surface (member-level
  `[Experimental]` diagnostics); this document covers host project/assembly/namespace names instead.
- [eventstore-integration.md](./eventstore-integration.md) — stable EventStore wiring surface and the
  no-redis-parameter finding.
