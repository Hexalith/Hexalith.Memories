# Hexalith.Memories

Hexalith Memory Module with syntactic and semantic search.

## Quick start

The guided quickstart gets you from a fresh clone to a first search result in under 30 minutes on a clean machine (NFR31 — approximate; see [docs/dev/quickstart.md](docs/dev/quickstart.md#manual-nfr31-walkthrough-cadence) for the manual walkthrough cadence). The current automated coverage focuses on the wizard itself against a warm fixture; the full clean-machine walkthrough remains a manual verification step.

### 1. Prerequisites (~30s to verify)

- [Docker Desktop](https://docs.docker.com/desktop/) (WSL2 on Windows; resource allocation on macOS)
- [.NET SDK 10.0.401](https://dotnet.microsoft.com/download/dotnet/10.0) or newer
- `git` with submodules support

If any of these fail, see [docs/dev/quickstart.md](docs/dev/quickstart.md) for OS-specific setup notes.

### 2. Clone and initialize submodules (~2 min cold)

```bash
git clone <repo-url> Hexalith.Memories
cd Hexalith.Memories
git submodule update --init
```

The root-declared submodules are checked out under `references/`: `references/Hexalith.Commons`, `references/Hexalith.EventStore`, `references/Hexalith.AI.Tools`, `references/Hexalith.Tenants`, `references/Hexalith.FrontComposer`, `references/Hexalith.Builds`, and `references/Hexalith.PolymorphicSerializations`.
The non-recursive `git submodule update --init` above initializes all of them; `references/Hexalith.FrontComposer` is required to build the `Hexalith.Memories.Web` component library. Initialize only these root-declared submodules; do not use `--recursive`.

### 3. Build the solution (~5 min first time, ~30s incremental)

```bash
dotnet build Hexalith.Memories.slnx
```

### 4. Boot this repository's test stack in a dedicated terminal (~1 min first time, ~20s subsequent)

```bash
dotnet run --project src/Hexalith.Memories.AppHost
```

On first AppHost run, a session-scoped OpenBao container starts and the AppHost generates OpenBao-backed `secretstores.hashicorp.vault` Dapr components automatically; there is no local `secrets.json` file. Wait for the Aspire dashboard to show the `memories` resource as Running.

`src/Hexalith.Memories.AppHost` serves only this repository's own tests, including this walkthrough. A run of it is never evidence that Memories runs in the Platform composition. To run or debug Memories as a module, use the Platform workspace (see [Run and debug from the Platform workspace](#run-and-debug-from-the-platform-workspace)).

### 5. Install the CLI (~1 min first time, ~10s subsequent)

```bash
dotnet pack src/Hexalith.Memories.Cli -c Release -o ./artifacts
dotnet tool install -g --add-source ./artifacts Hexalith.Memories.Cli
```

### 6. Run the guided quickstart (<1 min on a warm stack)

In a second terminal (while the AppHost continues running in the first):

```bash
memories quickstart
```

The wizard verifies prerequisites, prints the stack boot command, probes server health, provisions a sample tenant, ingests a sample document, and runs a validation search. On success you'll see `Quickstart ok in <N>s across 6 steps.`

**Total** (warm cache, approximate): ~10 min. Cold-start first-time: approximately 25-30 min (network + Docker image pulls; record manual walkthrough timings in [docs/dev/quickstart-walkthrough-log.md](docs/dev/quickstart-walkthrough-log.md)).

**If a step fails**, consult [docs/dev/quickstart.md](docs/dev/quickstart.md) for the per-step failure decision tree.

## Local development stack

Running this repository's own AppHost boots the topology its tests use:

- Redis Stack on port `6379`
- FalkorDB on port `6380`
- Memories Server on port `5000`
- Dapr sidecar HTTP on port `3500`
- Dapr sidecar gRPC on port `50001`
- Aspire Dashboard opened by the AppHost

The deployment-oriented Dapr component manifests live under `deploy/dapr/components/`, including `secretstore.yaml` and `access-telemetry-secrets.yaml`, both `secretstores.hashicorp.vault` backed by OpenBao. The AppHost also attaches equivalent Dapr component resources for local development, generated using its own session-scoped OpenBao container, so the sidecar can load the state store and secret store automatically.

## Run and debug from the Platform workspace

Memories runs and debugs as a module from the [Hexalith.Platform](https://github.com/Hexalith/Hexalith.Platform) workspace, where this repository is the direct submodule `references/Hexalith.Memories`. Prerequisites:

- The Platform-pinned `hexalith-module` tool, restored with `dotnet tool restore` in the Platform root.
- Aspire CLI `13.6.1` exactly; the tool refuses any other version with `HXR015`.
- A running Docker engine.
- `HEXALITH_DAPR_HOME` set to an absolute, isolated Dapr home: Dapr CLI `1.18.0` at `tools/dapr` and runtime `1.18.2` at `.dapr/bin/{daprd,placement,scheduler}`. The Platform README shows how to prepare one from the official release assets.
- The `references/Hexalith.Memories` and `references/Hexalith.EventStore` checkouts at the gitlinks staged in the Platform repository. After a local commit here, run `git add references/Hexalith.Memories` in the Platform root before starting a run.

From the Platform root:

```bash
export HEXALITH_DAPR_HOME="$HOME/.hexalith/dapr/1.18.2"
dotnet tool run hexalith-module -- debug --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json
dotnet tool run hexalith-module -- down --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json
```

Pass the owner manifest under `references/`, never the Platform's copy in `src/Hexalith.Platform.Composition/Declarations/`. `debug` builds this checkout and `references/Hexalith.EventStore` in Debug and starts the declared selection, Memories and EventStore with no Tenants: the `memories`, `memories-access-telemetry-clock`, `memories-access-telemetry` and `eventstore` servers with their Dapr sidecars, plus Redis Stack (`memories-vectors`) and FalkorDB (`memories-graph`). It waits for each server's `/ready` endpoint and reports one `dotnet run` launcher process id per server. Each application is its launcher's child process:

```bash
ps --ppid <launcher-pid> -o pid,args
```

Attach to the child running the server's `bin/Debug/net10.0/<Assembly>` executable, for example with VS Code ("Attach to a .NET process"). Breakpoints in this checkout's sources bind. When you finish, `down` stops only that run.

## Useful endpoints

- `http://localhost:5000/health`
- `http://localhost:5000/alive`
- `http://localhost:5000/ready`

## CLI (preview)

Story 7.2 expands the `memories` .NET global tool with `--format human|json|table`, multi-axis search (`memories search query`, including syntactic, semantic, natural-language, graph, and hybrid modes), memory-unit inspection (`memories search inspect`), and the `--explain` score breakdown. Story 7.3 adds actionable error messages: every failure renders `Error: <CODE>` + message + recovery suggestion in human/table modes, or a `{ schemaVersion, command, error: { code, message, suggestion } }` envelope on stdout in JSON mode; domain errors exit `1` and plumbing exits `2`. Story 7.4 wires the `memories quickstart` guided wizard that verifies prerequisites, probes server health, provisions a sample tenant, ingests a sample document, and runs a validation search — completing in <30 minutes on a clean machine (NFR31) and in <60 seconds against a warm stack. Story 7.5 wires search + access telemetry: distributed traces (NFR28), custom Aspire Dashboard metrics (NFR29), and per-tenant audit events (FR67). See [docs/dev/telemetry.md](docs/dev/telemetry.md).

```bash
dotnet pack src/Hexalith.Memories.Cli -c Release -o ./artifacts
dotnet tool install -g --add-source ./artifacts Hexalith.Memories.Cli
memories --version
memories --format json tenant list
```

Use `--format json` for scripts and LLM agents — the envelope is versioned and stable. See:

- [CLI configuration](docs/dev/cli-config.md) — endpoint resolution, environment variables, PATH troubleshooting.
- [CLI output formats](docs/dev/cli-output-formats.md) — envelope schema, per-command examples, versioning policy.
- [Quickstart wizard](docs/dev/quickstart.md) — per-step walkthrough, failure decision tree, OS-specific notes, dry-run mode, JSON envelope reference.

## Integration Guides

- [EventStore pub/sub subscription](docs/dev/eventstore-integration.md) — zero-code DAPR pub/sub subscription for event-sourced ingestion (Story 9.1).

## Operations

- [Rate limiting — per-tenant ceilings, 429 handling, extraction gate](docs/operations/rate-limiting.md)
- [Failure recovery — failed-unit registry, re-ingestion, retry policy overrides](docs/operations/failure-recovery.md)
- [Pipeline persistence — Redis durability, restart validation, warm restart and throughput benchmark](docs/operations/pipeline-persistence.md)
