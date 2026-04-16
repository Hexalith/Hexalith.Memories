# CLI output formats (Story 7.2)

The `memories` CLI supports three output formats through the root-level global option
`--format <human|json|table>`. When the flag is omitted, the CLI defaults to **human** —
byte-for-byte identical to what Story 7.1 shipped, so every script written against the
foundation keeps working unchanged.

| Format  | Target                   | Pipe-safe?            |
| ------- | ------------------------ | --------------------- |
| `human` | Interactive terminals    | Yes                   |
| `json`  | Scripts and LLM agents   | **Yes — use this in pipelines.** |
| `table` | Interactive terminals    | No — has a separator row of hyphens. |

See also: [`cli-config.md`](cli-config.md) for endpoint/token resolution.

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
    { "id": "t-1", "displayName": "Tenant One", "status": "active", "createdAt": "2026-04-16T12:00:00Z" }
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
      { "memoryUnitId": "mu-42", "compositeScore": 0.812, "syntacticScore": 0.7, "semanticScore": 0.9, "sourceUri": "mem://case-1/mu-42", "sourceType": "file", "contentSnippet": "Customer escalation...", "annotationsCount": 0 }
    ],
    "totalCount": 1,
    "degraded": false,
    "unavailableAxes": [],
    "query": "needle",
    "explanation": {
      "caveat": "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness.",
      "axisDetails": {
        "syntactic": { "normalizationMethod": "bm25_saturation", "description": "BM25 saturation" },
        "semantic":  { "normalizationMethod": "cosine",          "description": "cosine similarity" }
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
      "author": { "value": "Jerome", "origin": "human", "confidence": 1.0 },
      "topic":  { "value": "compliance", "origin": "ai", "confidence": 0.87 }
    }
  }
}
```

## Pipe safety

- Use `--format json` for scripts and pipelines. The envelope is stable.
- `--format table` includes a separator row of hyphens and is intended for **interactive
  terminal viewing only**. Piping it into `awk`, `cut`, or similar tools requires skipping
  the separator line.
- `--format human` is stable for the commands shipped as of Story 7.2 and is pipe-friendly
  (no ANSI, no emoji), but future stories may add ANSI colour under a TTY-detection guard.
  Scripts should still prefer JSON.

## Format–command reference

| `OutputFormat` enum | CLI spelling | Notes |
| ------------------- | ------------ | ----- |
| `Human`             | `--format human` (or omit) | Default. |
| `Json`              | `--format json` | Envelope with `schemaVersion: 1`. |
| `Table`             | `--format table` | ASCII alignment via `string.PadRight`; interactive only. |

Unknown values exit with code `2` and a one-line stderr message:
`Invalid configuration at '--format': Unknown format 'xml'. Use human, json, or table.`
`--help` and `--version` always succeed regardless of an invalid `--format` value.
