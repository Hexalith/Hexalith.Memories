# Hexalith.Memories

Hexalith Memory Module with syntactic and semantic search.

## Quick start

The guided quickstart gets you from a fresh clone to a first search result in under 30 minutes on a clean machine (NFR31 — approximate; see [docs/dev/quickstart.md](docs/dev/quickstart.md#manual-nfr31-walkthrough-cadence) for the manual walkthrough cadence). The current automated coverage focuses on the wizard itself against a warm fixture; the full clean-machine walkthrough remains a manual verification step.

### 1. Prerequisites (~30s to verify)

- [Docker Desktop](https://docs.docker.com/desktop/) (WSL2 on Windows; resource allocation on macOS)
- [.NET SDK 10.0.300](https://dotnet.microsoft.com/download/dotnet/10.0) or newer
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

### 4. Boot the local stack in a dedicated terminal (~1 min first time, ~20s subsequent)

```bash
dotnet run --project src/Hexalith.Memories.AppHost
```

On first AppHost run, a gitignored local `secrets.json` placeholder is created automatically if it does not already exist. Wait for the Aspire dashboard to show the `memories` resource as Running.

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

Running the AppHost boots the local development topology:

- Redis Stack on port `6379`
- FalkorDB on port `6380`
- Memories Server on port `5000`
- Dapr sidecar HTTP on port `3500`
- Dapr sidecar gRPC on port `50001`
- Aspire Dashboard opened by the AppHost

The deployment-oriented Dapr component manifests live under `deploy/dapr/components/`. The AppHost also attaches equivalent Dapr component resources for local development so the sidecar can load the state store and secret store automatically.

## Useful endpoints

- `http://localhost:5000/health`
- `http://localhost:5000/alive`
- `http://localhost:5000/ready`

## CLI (preview)

Story 7.2 expands the `memories` .NET global tool with `--format human|json|table`, three-axis search (`memories search query`), memory-unit inspection (`memories search inspect`), and the `--explain` score breakdown. Story 7.3 adds actionable error messages: every failure renders `Error: <CODE>` + message + recovery suggestion in human/table modes, or a `{ schemaVersion, command, error: { code, message, suggestion } }` envelope on stdout in JSON mode; domain errors exit `1` and plumbing exits `2`. Story 7.4 wires the `memories quickstart` guided wizard that verifies prerequisites, probes server health, provisions a sample tenant, ingests a sample document, and runs a validation search — completing in <30 minutes on a clean machine (NFR31) and in <60 seconds against a warm stack. Story 7.5 wires search + access telemetry: distributed traces (NFR28), custom Aspire Dashboard metrics (NFR29), and per-tenant audit events (FR67). See [docs/dev/telemetry.md](docs/dev/telemetry.md).

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
