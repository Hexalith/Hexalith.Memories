# CLI output formats (Story 7.2)

The `memories` CLI supports three output formats through the root-level global option
`--format <human|json|table>`. When the flag is omitted, the CLI defaults to **human** —
byte-for-byte identical to what Story 7.1 shipped, so every script written against the
foundation keeps working unchanged.

| Format  | Target                 | Pipe-safe?                           |
| ------- | ---------------------- | ------------------------------------ |
| `human` | Interactive terminals  | Yes                                  |
| `json`  | Scripts and LLM agents | **Yes — use this in pipelines.**     |
| `table` | Interactive terminals  | No — has a separator row of hyphens. |

See also: [`cli-config.md`](cli-config.md) for endpoint/token resolution. Story 7.5 wires the
`status telemetry` subcommand — see [`telemetry.md`](telemetry.md) for its JSON envelope payload.

## JSON envelope contract — `schemaVersion: 1`

Every `--format json` response is a single JSON document with exactly three top-level fields:

```json
{
  "schemaVersion": 1,
  "command": "tenant list",
  "data": <command-specific-shape>
}
```

- **`schemaVersion`** — integer, pinned at `1` for Story 7.2. Bumps only when a field is **renamed, removed, or semantically changed**.
- **`command`** — the invoked command name (e.g., `tenant list`, `config show`, `search query`, `search inspect`).
- **`data`** — the command-specific payload; see the per-command sections below.

**Versioning policy (ADR-7.2-001):**

- Adding a new optional field is **non-breaking** — consumers stay on `schemaVersion: 1`.
  Story 7.3 will add an `error` slot; Story 7.5 may add `traceId`. Both are additive.
- Renaming, removing, or changing the semantics of a field requires bumping to
  `schemaVersion: 2`. Both versions must coexist for at least one full release cycle after
  the bump so consumers have time to migrate.
- Silently changing meaning within `schemaVersion: 1` (e.g., flipping a boolean default,
  changing an enum spelling) is never allowed — it is a bug, not an evolution.
- The `OutputFormat` enum (`human`, `json`, `table`) is itself extensible. Later stories may
  add new values like `tsv`, `yaml`, or `csv` as additive enum members without breaking
  existing formats or bumping `schemaVersion`.

## Per-command examples

### `tenant list`

**Human** (default):

```
t-1	Tenant One
t-2	Tenant Two
```

Empty list prints `No tenants found.` (Story 7.1 wording, preserved).

**JSON** — `memories --format json tenant list`:

```json
{
    "schemaVersion": 1,
    "command": "tenant list",
    "data": [
        {
            "id": "t-1",
            "displayName": "Tenant One",
            "status": "active",
            "createdAt": "2026-04-16T12:00:00Z"
        }
    ]
}
```

**Table** — `memories --format table tenant list`:

```
TENANT ID  DISPLAY NAME
-------------------------
t-1        Tenant One
t-2        Tenant Two
```

### `config show`

**Human** (default, byte-for-byte preserved from Story 7.1):

```
endpoint=http://127.0.0.1:5000/
resolvedBy=DefaultConfigurationSource
tokenConfigured=false
```

**JSON** — `memories --format json config show`:

```json
{
    "schemaVersion": 1,
    "command": "config show",
    "data": {
        "endpoint": "http://127.0.0.1:5000/",
        "resolvedBy": "DefaultConfigurationSource",
        "tokenConfigured": false
    }
}
```

The token value itself is never serialized — `tokenConfigured` is the only signal.

### `search query`

**Human without `--explain`**:

```
1. [0.812] mem://case-1/mu-42 — Customer escalation regarding invoicing discrepancy
2. [0.644] mem://case-1/mu-17 — Follow-up call notes, escalation resolved
```

**Human with `--explain`** — the caveat is printed **first**, before any result, so that
`memories search query --explain | head -N` carries the compliance guarantee:

```
Confidence scores measure query-result relevance, NOT factual accuracy or data completeness.
1. [0.812] mem://case-1/mu-42 — Customer escalation regarding invoicing discrepancy
    composite=0.812, syntactic=0.700, semantic=0.900
      (syntactic: bm25_saturation)
      (semantic: cosine)
```

**JSON with `--explain`** — `memories search query --explain --format json --tenant acme --query "needle"`:

```json
{
    "schemaVersion": 1,
    "command": "search query",
    "data": {
        "results": [
            {
                "memoryUnitId": "mu-42",
                "compositeScore": 0.812,
                "syntacticScore": 0.7,
                "semanticScore": 0.9,
                "sourceUri": "mem://case-1/mu-42",
                "sourceType": "file",
                "contentSnippet": "Customer escalation...",
                "annotationsCount": 0
            }
        ],
        "totalCount": 1,
        "degraded": false,
        "unavailableAxes": [],
        "query": "needle",
        "explanation": {
            "caveat": "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness.",
            "axisDetails": {
                "syntactic": {
                    "normalizationMethod": "bm25_saturation",
                    "description": "BM25 saturation"
                },
                "semantic": {
                    "normalizationMethod": "cosine",
                    "description": "cosine similarity"
                }
            }
        }
    }
}
```

> ⚠ **Combined-flag reminder:** `data.explanation` is populated only when **both**
> `--explain` **and** `--format json` are set. `--format json` alone returns the envelope
> with `explanation: null` (omitted under `WhenWritingNull`). Passing `--explain` alone
> formats the explanation to human/table output but does not produce JSON.

**Table with `--explain`**:

```
RANK  COMPOSITE  SYNTACTIC  SEMANTIC  GRAPH  URI
--------------------------------------------------
1     0.812      0.700      0.900     -      mem://case-1/mu-42
Confidence scores measure query-result relevance, NOT factual accuracy or data completeness.
```

Degraded searches (`HybridSearchResult.Degraded == true`) prepend a one-line notice:
`Note: search degraded — axes unavailable: graph`. Story 7.3 replaces this bridge surface
with actionable recovery suggestions.

### `search inspect`

**Human** — metadata origin uses lowercase `[human]` / `[ai]` prefixes, matching the
`MetadataOrigin` enum's camelCase JSON serialization. Plain ASCII only — pipe-friendly.

```
id=mu-42
tenantId=acme
caseId=case-1
sourceUri=mem://case-1/mu-42
ingestedBy=user@acme
ingestedAt=2026-04-16T15:30:00.0000000+00:00
status=Indexed
metadata:
  author = Jerome      [human, confidence=1.00]
  topic  = compliance  [ai, confidence=0.87]
```

Empty metadata prints `metadata: (none)` on a single line.

**JSON** — `memories --format json search inspect --tenant acme --case case-1 --id mu-42`:

```json
{
    "schemaVersion": 1,
    "command": "search inspect",
    "data": {
        "id": "mu-42",
        "tenantId": "acme",
        "caseId": "case-1",
        "metadata": {
            "author": {
                "value": "Jerome",
                "origin": "human",
                "confidence": 1.0
            },
            "topic": {
                "value": "compliance",
                "origin": "ai",
                "confidence": 0.87
            }
        }
    }
}
```

### `quickstart`

The guided-wizard command (Story 7.4) returns a step-array payload instead of a single entity. Each step carries its own status, `durationMs`, and (on failure) an error code + actionable suggestion. Per-step failure context lives inside `data.steps[N]` — the envelope's top-level `error` slot is NEVER populated for `quickstart` (ADR-7.4-003).

**JSON** — `memories --format json quickstart --dry-run`:

```json
{
    "schemaVersion": 1,
    "command": "quickstart",
    "data": {
        "steps": [
            {
                "id": 1,
                "title": "Verifying prerequisites",
                "status": "dry-run",
                "durationMs": 0,
                "message": "Would run Docker, .NET SDK, port, OS, and DAPR CLI checks.",
                "suggestion": null,
                "errorCode": null
            }
        ],
        "overallStatus": "ok",
        "elapsedMs": 2
    }
}
```

See [quickstart.md](quickstart.md) for the per-step walkthrough, failure decision tree, and `jq` patterns.

## Pipe safety

- Use `--format json` for scripts and pipelines. The envelope is stable.
- `--format table` includes a separator row of hyphens and is intended for **interactive
  terminal viewing only**. Piping it into `awk`, `cut`, or similar tools requires skipping
  the separator line.
- `--format human` is stable for the commands shipped as of Story 7.2 and is pipe-friendly
  (no ANSI, no emoji), but future stories may add ANSI colour under a TTY-detection guard.
  Scripts should still prefer JSON.

## Format–command reference

| `OutputFormat` enum | CLI spelling               | Notes                                                    |
| ------------------- | -------------------------- | -------------------------------------------------------- |
| `Human`             | `--format human` (or omit) | Default.                                                 |
| `Json`              | `--format json`            | Envelope with `schemaVersion: 1`.                        |
| `Table`             | `--format table`           | ASCII alignment via `string.PadRight`; interactive only. |

Unknown values exit with code `2` and a one-line stderr message:
`Invalid configuration at '--format': Unknown format 'xml'. Use human, json, or table.`
`--help` and `--version` always succeed regardless of an invalid `--format` value.

## Error envelope (added in Story 7.3)

Every `--format json` error is emitted as a stable envelope on **stdout** (per ADR-7.3-002 —
errors share the single structural channel rather than duplicating onto stderr). By default,
stderr stays empty in JSON mode on error; when `--verbose` is enabled, diagnostic lines still go
to stderr while the primary error surface remains the stdout envelope. Shape:

```json
{
    "schemaVersion": 1,
    "command": "tenant list",
    "error": {
        "code": "TENANT_NOT_FOUND",
        "message": "Tenant 'acme' does not exist.",
        "suggestion": "Run 'memories tenant list' to see available tenants."
    }
}
```

Rules:

- On error, the envelope contains `schemaVersion`, `command`, and `error` only. `data` is
  **absent** (suppressed via `JsonIgnoreCondition.WhenWritingNull`), so
  `env.data === undefined`.
- In JSON mode, `--verbose` remains a debugging aid on **stderr**. Only the primary error payload
  moves to stdout.
- On success, the envelope contains `schemaVersion`, `command`, and `data` only. `error` is
  absent. The mutual-exclusivity invariant `data == null ⇔ error != null` is enforced at
  construction.
- Adding `error` is additive per ADR-7.2-001 → `schemaVersion` stays at `1`; existing
  consumers are unaffected.
- Human and `table` formats render errors as a multi-line stderr block:
  `Error: <CODE>` then `  <message>` then `  Suggestion: <suggestion>`.

### Synthetic transport codes

When the CLI cannot reach the server (no response → no `ErrorResponse.Code`), it assigns a
synthetic code so script consumers still get a stable identifier to switch on (ADR-7.3-001):

| Code                 | Trigger                                      | Exit |
| -------------------- | -------------------------------------------- | ---- |
| `CONNECTION_REFUSED` | `HttpRequestException` / `SocketException`   | 2    |
| `REQUEST_TIMEOUT`    | `TaskCanceledException` (not user-cancelled) | 2    |
| `TLS_ERROR`          | `AuthenticationException`                    | 2    |
| `INVALID_ENDPOINT`   | `UriFormatException` / unparseable config    | 2    |
| `INVALID_CONFIG`     | `InvalidConfigurationException`              | 2    |
| `UNEXPECTED_ERROR`   | Outermost unhandled exception                | 2    |
| `HTTP_<status>`      | Non-JSON 4xx/5xx body from server            | 2    |

### Recommended script patterns for `--format json`

**1. Exit-code-first (recommended):**

```sh
memories tenant list --format json | jq '.data' || { echo "command failed"; exit 1; }
```

Check `$?` before parsing — works regardless of success/error envelope shape.

**2. Presence-detection with `jq -e`:**

```sh
memories tenant list --format json | jq -e '.data // (.error | error)'
```

`jq -e` exits non-zero when the predicate is null; `.data // (.error | error)` returns `data`
if present, otherwise raises a jq error containing the `error` envelope. One-line,
`pipefail`-safe.

**3. Anti-pattern (avoid):**

```sh
set -o pipefail
memories tenant list --format json | jq '.data[]' > out.txt
```

On error, jq sees `.error` only, `.data[]` returns null/iterates nothing, and `pipefail`
surfaces the CLI exit code (`1`) as if it were a parse failure. Use patterns 1 or 2 instead.

## Exit codes

| Code  | Meaning                                                            | Example trigger                                                                                                                                            |
| ----- | ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `0`   | Success (includes empty results and degraded-with-partial-results) | `memories search query ...` returned 0 rows but the call itself succeeded                                                                                  |
| `1`   | Domain error — server returned a structured `ErrorResponse`        | `TENANT_NOT_FOUND`, `CASE_NOT_FOUND`, `INVALID_INPUT`, `MEMORY_UNIT_NOT_FOUND`, …                                                                          |
| `2`   | Plumbing — transport, config, or server-infrastructure failure     | Connection refused, TLS handshake failure, `DAPR_UNAVAILABLE`, `BACKEND_UNAVAILABLE`, `ALL_BACKENDS_UNAVAILABLE`, unparseable `--format` value, `HTTP_503` |
| `130` | User cancellation (Ctrl-C / SIGINT)                                | `memories search query ...` interrupted                                                                                                                    |

Cross-reference: [`CliExitCodes.cs`](../../src/Hexalith.Memories.Cli/Execution/CliExitCodes.cs)
and [`ErrorMessageCatalog.cs`](../../src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs).

