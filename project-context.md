# Project Context Bridge

Resolve paths from the repository root. You MUST fully load
`_bmad-output/project-context.md` as foundational context before proceeding.
That canonical file is this repository's only project-context policy source.
This bridge contains forwarding and fail-closed controls, but no implementation
policy; it MUST NOT be expanded with implementation policy.

If `_bmad-output/project-context.md` is missing, unreadable, empty, or does not
contain the exact active `Tenant isolation requires attached negative evidence`
rule under `### Testing Rules`, HALT and report the failure. Do not proceed
without valid canonical context.

Project-context generators MUST NEVER update this bridge. They may read it, but
MUST update only `_bmad-output/project-context.md`.
