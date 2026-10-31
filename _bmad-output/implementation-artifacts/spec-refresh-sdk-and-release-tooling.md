# Refresh SDK and release tooling

Status: ready-for-dev

The operator requests SDK and compatible package updates with submodule synchronization on 2026-10-31. Pin SDK 10.0.403, latest compatible commitlint/GitHub plugin and browser test tools. Preserve the exact semantic-release 25.0.8 and cosmiconfig 9.0.2 pins required by the existing release-configuration verifier. No runtime domain code changes.

## File Scope

Allowed files for this story:

- `README.md` - align SDK prerequisite

- `global.json` - dependency tooling update
- `package.json` - dependency tooling update
- `package-lock.json` - dependency tooling update
- `tests/Hexalith.Memories.Web.E2E/package.json` - dependency tooling update
- `tests/Hexalith.Memories.Web.E2E/package-lock.json` - dependency tooling update
- `_bmad-output/implementation-artifacts/spec-refresh-sdk-and-release-tooling.md` - dependency tooling update

## Verification

Run exact pinned commitlint, the canonical semantic-release configuration verifier and browser typecheck; evaluate the Contracts project SDK without initializing nested submodules.
