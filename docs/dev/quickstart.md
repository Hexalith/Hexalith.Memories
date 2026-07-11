<!-- Review cadence: update when server error codes change, CLI flags change, or quarterly — whichever comes first. Last reviewed: 2026-04-17. -->

# Hexalith.Memories quickstart wizard

The `memories quickstart` command walks a developer through six steps that isolate WHICH subsystem broke when a failure occurs: prerequisite verification, stack-boot hint, server-reachability probe, sample-tenant provisioning, sample-document ingestion, and a validation search. On success it prints `Quickstart ok in <N>s across 6 steps.` The wizard is non-interactive by default (ADR-7.4-002), idempotent on rerun (ADR-7.4-004), and leaves state in place on failure so you can inspect it with `memories tenant list` / `memories search query` (ADR-7.4-005).

## Overview

- **Non-interactive** — every step runs to completion, prints an outcome, and moves on. No prompts. Pipe- and script-friendly.
- **Idempotent** — rerunning `memories quickstart` on the same day against the same endpoint reuses the sample tenant (`quickstart-YYYYMMDD` in UTC). See the Shared-endpoint usage section below if you're running against a shared server.
- **Leaves state behind on failure** — lets you diagnose with `memories tenant list`, `memories search query`, `memories config show`. A successful rerun cleans up (implicitly) because the sample tenant already exists.

## Prerequisites

Mirrors the README prerequisites but with OS-specific install guides.

### .NET SDK 10.0.300

- **Windows**: download from [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0). Verify with `dotnet --list-sdks`.
- **macOS**: `brew install --cask dotnet-sdk` or download from the link above. On Apple Silicon, ensure the arm64 build is installed (not x64 under Rosetta).
- **Linux**: follow the [Microsoft package guide](https://learn.microsoft.com/dotnet/core/install/linux) for your distribution. Verify with `dotnet --list-sdks`.

### Docker Desktop

- **Windows**: install Docker Desktop with WSL2 integration enabled. The wizard's port-availability check is best-effort on Windows — some ports may be reserved by Hyper-V / Docker Desktop without an active listener (see the Windows caveat below).
- **macOS**: allocate sufficient resources (8 GB RAM minimum) in Docker Desktop preferences.
- **Linux**: native Docker daemon OR rootless Docker both work. If you use podman-as-docker (a shell alias), the wizard's `docker ps` check will fail — use `--skip-prereq-check` and rely on the boot step's failure mode as ground truth.

### DAPR CLI (optional)

Aspire manages the DAPR sidecar for local development, so a DAPR CLI is not required. The wizard reports its presence informationally only.

## Per-step walkthrough

### [1/6] Verifying prerequisites

Runs five sub-checks: Docker daemon (`docker ps`), .NET SDK (`dotnet --list-sdks` — asserts at least one SDK version 10.0.300 or newer), port availability (5000, 6379, 6380, 3500, 50001), OS platform (informational), and DAPR CLI (soft-fail — missing is OK).

**Common failures and remediation:**

- `Docker command not found` → install Docker Desktop or ensure `docker` is on PATH.
- `Docker daemon not reachable` → start Docker Desktop; if already running, restart it.
- `No .NET SDK 10.0.300 or newer` → install the latest .NET 10 SDK from the link above.
- `Port <N> in use` → find the owner with `lsof -i :<N>` (macOS/Linux) or `netstat -ano | findstr :<N>` (Windows) and stop the conflicting process.

Skip the step with `--skip-prereq-check` when you know the environment is fine (e.g., CI with a pre-bootstrapped stack).

### [2/6] Printing stack boot command

Always prints `Run in a dedicated terminal: dotnet run --project src/Hexalith.Memories.AppHost`. Does NOT spawn the subprocess (ADR-7.4-001 — print-then-poll semantics).

### [3/6] Probing server health

Polls `GET /health` every 1 second for up to 60 seconds. On success, reports the elapsed time. On timeout, the suggestion line names the likely cause: the AppHost is not running OR it is running on a different port (Aspire Testing fixtures randomize ports).

**When step 3 times out**:

- Check the AppHost output in the other terminal for errors (look for port-conflicts, missing Docker images, or config errors).
- Open the Aspire dashboard (printed by the AppHost on startup) and check the `memories-server` resource's status + port.
- If running against a non-default port, pass `--endpoint http://localhost:<port>`.

Skip the step with `--skip-boot-check` when a fixture already bootstrapped the stack.

**Known limitation** (anti-pattern #15): `ProbeHealthAsync` checks `IsSuccessStatusCode` only — a server returning empty `200 OK` from a half-wired `/health` endpoint passes. Step 3 validates reachability, not deep backend readiness. If step 4 fails with `BACKEND_UNAVAILABLE` or `GRAPH_UNAVAILABLE`, the server booted but Redis Stack / FalkorDB did not.

### [4/6] Provisioning sample tenant

Checks whether the tenant already exists via `GET /api/v1/tenants/{id}`. If present + Active, reports `SKIP` (idempotent). Otherwise, POSTs `/api/v1/tenants` with `TenantProvisioningInput { TenantId, DisplayName }` and polls for Active status (timeout: 30 seconds).

**Tenant-id format**: validation is server-delegated — alphanumeric with hyphens. If you pass `--tenant "has spaces!"`, the server rejects with `INVALID_TENANT_ID` and the wizard renders that catalog entry.

### [5/6] Ingesting sample document

Lists cases in the tenant; reuses an existing `quickstart-default` case if present, otherwise creates one. POSTs the embedded sample document (~200 words of generic memory-system prose) to `/api/v1/ingest`. Content type is `text/plain` and the request is tagged with metadata `{"origin": "quickstart", "wizardVersion": "7.4"}`.

The sample body is embedded in the CLI binary — there is no file-IO dependency. It is deliberately generic-descriptive (no PII, credentials, internal URLs) per anti-pattern #13.

### [6/6] Running validation search

Runs a hybrid search for a sample-specific validation token embedded in the current run's sample document. The token keeps the familiar `hybrid search` phrase but adds a unique suffix per wizard run, preventing stale data from an earlier sample from satisfying the validation by accident. A hybrid result only counts as success when it also carries a syntactic match for that token, which filters out semantic nearest-neighbor noise from older samples. Retries up to 3 times with 2-second backoff to tolerate async-write settling. On success, the wizard runs a negative-match canary — a syntactic search for a guaranteed-absent opaque token — and asserts zero results. This closes the "false confidence" failure mode where a miswired pipeline could return canned results for any query.

**Failure diagnostics:**

- `Validation search returned zero token matches` → indexing may lag, or only semantic-neighbor results are returning. Retry manually with the exact validation query shown in the wizard's failure suggestion. Check server logs for ingestion errors.
- `Negative-match canary returned N results` → the lexical search path is not distinguishing match from no-match. Inspect the syntactic axis with `memories search query --tenant <id> --axis syntactic --query "quickstartnomatchcanarytokenx9f4b2p7m1" --explain`.

## OS-specific notes

### Windows

- **Port reservation caveat**: `TcpListener.Start` on `127.0.0.1:<port>` may succeed even when Docker Desktop reserves the port via Hyper-V without an active listener. The wizard's port check is best-effort — a pass is advisory; a fail is load-bearing. If step 1 passes but boot (step 3 implicitly) fails with port conflicts, check the Aspire dashboard for the actual bind port and `netstat -ano | findstr :<port>`.
- **WSL2 requirement**: Docker Desktop must be configured with WSL2 integration for Linux-container builds.

### macOS

- **Rosetta on Apple Silicon**: the .NET 10 SDK has native arm64 builds. Installing the x64 SDK under Rosetta works but the embedded-image path may slow down Redis Stack boot.
- **Docker Desktop resources**: increase CPU/RAM allocation under Settings → Resources if the stack is slow to boot.

### Linux

- **Rootless Docker**: `docker ps` still works from the user context; the quickstart's Docker check is PATH-based, not socket-probing. Rootless-specific port-binding differences apply to the AppHost, not the wizard.
- **Podman-as-docker alias**: shell-only aliases don't affect `Process.Start`. If `docker` is not resolvable via PATH, the wizard reports "Docker command not found" even though `docker ps` works in your shell. Use `--skip-prereq-check` in that case.

## `--dry-run` mode

`memories quickstart --dry-run` prints every step and the action it would perform, but makes no REST calls and writes no files. Exits `0`. Useful for:

- CI smoke-tests that validate the command is packaged without touching a real server.
- Scripted onboarding where you want to see the full step sequence before committing.

Example (human output):

```text
[1/6] Verifying prerequisites
[1/6] DRY-RUN: Would run Docker, .NET SDK, port, OS, and DAPR CLI checks.
[2/6] Printing stack boot command
[2/6] DRY-RUN: Run in a dedicated terminal: dotnet run --project src/Hexalith.Memories.AppHost
...
Quickstart ok in 0.0s across 6 steps.
```

`memories quickstart --dry-run --format json` emits a single JSON envelope with every step's `status: "dry-run"` and `overallStatus: "ok"`.

## `--format json` envelope

The wizard's envelope mirrors the 7.2 contract with a command-specific `data` payload:

```json
{
    "schemaVersion": 1,
    "command": "quickstart",
    "data": {
        "steps": [
            {
                "id": 1,
                "title": "Verifying prerequisites",
                "status": "ok",
                "durationMs": 240,
                "message": "All prerequisites satisfied.",
                "suggestion": null,
                "errorCode": null
            }
        ],
        "overallStatus": "ok",
        "elapsedMs": 42
    }
}
```

Per-step failure context is carried inside `data.steps[N].errorCode` + `message` + `suggestion` — the envelope's top-level `error` slot is NEVER populated for `quickstart` (ADR-7.4-003). Consumers read `data.overallStatus == "fail"` ⇔ any `data.steps[].status == "fail"`.

**jq patterns:**

```bash
# Pass/fail gate.
memories quickstart --format json | jq -e '.data.overallStatus == "ok"'

# Failing steps with actionable details.
memories quickstart --format json | jq '.data.steps[] | select(.status == "fail") | {step: .id, code: .errorCode, why: .message, fix: .suggestion}'

# Total elapsed time.
memories quickstart --format json | jq '.data.elapsedMs'
```

See [docs/dev/cli-output-formats.md](cli-output-formats.md) for the shared envelope contract.

## Shared-endpoint usage

The wizard defaults to tenant id `quickstart-YYYYMMDD` in UTC. This default is deliberately local-only: two developers running the wizard on the same day against the same endpoint collide on the same tenant (by design — ADR-7.4-004 idempotency). If you are running against a shared endpoint (dev/staging server, multi-tenant CI), pass `--tenant <unique-id>` (e.g., `--tenant quickstart-$(whoami)-$(date +%Y%m%d)`).

Until Phase 1.5 adds server-side per-user tenant quotas, the wizard is not rate-limited — do not expose the `memories` CLI to untrusted users on shared infrastructure. A scripted loop of `memories quickstart --tenant attack-$i` can enumerate-and-create tenants without cost. Rate limiting is a server-side Phase 1.5 concern.

## Compliance note

Story 7.4 does NOT emit structured audit events. Wizard invocations are ephemeral — the only record is the CLI's stdout/stderr. Regulated environments (SOC 2, GDPR Article 30) should defer wizard adoption until Story 7.5 lands audit telemetry (FR67) OR wrap `memories quickstart` invocations with external shell-level audit logging. See also the 7.3 compliance scope disclaimer.

## Manual NFR31 walkthrough cadence

NFR31 ("<30 minutes from clean machine to first search result") is measured quarterly on real hardware, not automated. Procedure:

1. Wipe Docker Desktop (or use a fresh machine / VM) and delete the local clone.
2. Start a stopwatch on `git clone`.
3. Run the README Quick start end-to-end.
4. Stop the stopwatch on the first `memories search query` that returns a non-empty result (after the wizard has left a sample memory unit behind).
5. Record the run in [docs/dev/quickstart-walkthrough-log.md](quickstart-walkthrough-log.md): date, machine class, total minutes, notes.

No reminder workflow is checked into this repository today; until one is added, keep the walkthrough log fresh manually during quarterly reviews and whenever the cold-start flow changes materially.

## When this is NOT the right command

If you already have the stack up and just want to explore without the provisioning steps, skip the wizard and use `dotnet run --project src/Hexalith.Memories.AppHost` + raw `curl` against the endpoints documented in [README.md](../../README.md#useful-endpoints). The wizard is optimized for first-time onboarding, not day-to-day development.

## Related reference

- [docs/dev/cli-config.md](cli-config.md) — endpoint resolution (flag > env > file > default), token transport guard, verbose diagnostics.
- [docs/dev/cli-output-formats.md](cli-output-formats.md) — shared envelope contract, exit-code table, per-command examples.
- [README.md](../../README.md) — repository-level quick start, local stack notes, and useful endpoints.
