---
title: Bind publisher fixtures to the catalog adapter inventory
status: done
created: 2026-10-31
baseline_commit: ef5c134bbee7ec708f354ce1bc3e508ee6105ab7
---

# Publisher catalog adapter fixture

CI 41192867419/job 124513240913 fails two publisher cases and errors in two
more because the fixture creates nine archives while the actual strict public
manifest requires ten, including Hexalith.Memories.EventStoreExtensions. Add the
real catalog adapter to the fixture. Preserve production publisher/validator
behavior, existing positive, failure, partial-publication and secret-redaction
assertions. Add a control that removes precisely the adapter and proves refusal
before any dotnet push, with nine archives truthfully recorded as not attempted.

## File Scope

Allowed files for this story:

- `tests/tooling/publish_nuget/publish_nuget_test.py` - generate the actual ten-package inventory and prove missing adapter refusal
- `_bmad-output/implementation-artifacts/spec-publisher-catalog-adapter-fixture.md` - record bounded repair and focused evidence

## Verification

Run only the publisher fixture module, which uses fake dotnet and GitHub commands
and the actual unchanged PowerShell publisher/validator. No packages are pushed.
Validate the exact Conventional Commit with the owner-pinned commitlint and real
File Scope, tenant-evidence and readiness hooks before committing.

## Cross-Tenant Negative Evidence

Not triggered: this change touches publisher test fixtures only. Runtime tenant
routing, authority, catalog policy, production publication and data are unchanged.

Actual focused evidence: `python3 -m unittest discover -s tests/tooling/publish_nuget -p "*_test.py"` passes all31cases, including allfour formerly failing cases and the new missing-adapter refusal. Log: `/tmp/memories-ef5-publisher-fixture-tests.log`. Production scripts and manifest are unchanged.
