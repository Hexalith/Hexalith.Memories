# Story 30.6 clean-base dry run

On 2026-11-15, two detached worktrees were created beneath the private `run-with-temp` TMPDIR:
Memories at `fad5ec5efb286a1b4589869a354895acd3aaf55c`, and its declared Builds gitlink at
`e2bbbd0893f586e4d447ef8358ff747ea3d34435`. `git status --porcelain` returned empty before
qualification. No dependency was initialized, updated or re-pinned. Both worktrees were removed
after the foreground child finished.

Executed from that clean Memories tree:

```bash
python3 -B tools/qualify-builds-publication.py --output-directory /home/administrator/projects/hexalith/memories/_bmad-output/implementation-artifacts/30-6-cutover/qualifier
```

After execution, raw qualifier files were moved from the `qualifier` output directory to this
evidence directory without changing their bytes; `.log` output was retained as `qualification.txt`.
This keeps the delivered evidence within the approved single-level artifact scope.

Exit 0. All six checkpoint results pass, each with `bindings.source.dirty: false` and source SHA
`fad5ec5efb286a1b4589869a354895acd3aaf55c`. Complete-set dry run binds ten manifest package IDs
and all five image project/repository mappings. The synthetic version is **0.0.302**; it is not a
real next-release version. The profile's qualification SHA is the recorded root Builds gitlink;
the current caller's independent publication execution pin remains `ae77d569`. The job used real
pinned qualification helpers against fake network, registry, Docker and .NET seams.

| Checkpoint | Owner | Exact artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Complete-set dry run | Memories Release Engineer | `complete-set-dry-run.json`, SHA-256 `75ffb7df7b5155176302edd8ca2e44a749d0ebd9bdb253db3dd83404dc1ae72d` | reviewed 2026-11-16 | pass; offline only |
| Inventory drift refusals | Memories Release Engineer | `set-drift.json` | reviewed 2026-11-16 | pass; offline only |
| Phase verification | Memories Release Engineer | `phase-verification.json` | reviewed 2026-11-16 | pass; offline only |
| Partial publication | Memories Release Engineer | `partial-publication.json` | reviewed 2026-11-16 | pass; offline only |
| Recovery | Memories Release Engineer | `recovery.json` | reviewed 2026-11-16 | pass; offline only |
| Evidence upload | Memories Release Engineer | `evidence-upload.json` | reviewed 2026-11-16 | pass; offline only |
| Before-snapshot / next version absent | Memories Release Engineer | `identity-before.json`, `identity-before.json.sha256`, `identity-before-capture.txt` | reviewed 2026-11-16 | complete on 2026-11-15 at 23:59:49 Europe/Paris; candidate 4.0.1 absent in every domain |

`qualification.txt` retains the exact six result hashes. `approval.md` remains
pending; this dry run supplies no owner approval, live release, NuGet OIDC, registry authorization
or final-code publication credit.

The initial required before-snapshot attempt refused at
`registry memories:0.0.0-preview.0.474: not an image index`. GitHub and NuGet reads completed,
but the tool wrote no partial evidence. A historical single-image manifest cannot satisfy the
then-frozen requirement that every image tag bind an index digest. Owner-approved D6 resolved that shape on 2026-11-15: recognize existing manifests and indexes
while retaining exact digest/media-type identities. The original refusal remains historical evidence.
The targeted v4.0.0 readback in `live-release-readback.json` is expressly **not** a full snapshot.

## D6 before-snapshot completion

The fresh capture completed on 2026-11-15 at 22:59:49 UTC (23:59:49 Europe/Paris) with exit 0.
`identity-before.json` and its SHA-256 receipt cover 196 `v*` tags, 183 Releases and 1,700
assets, every version/listed state of the ten package IDs, and 463 tags across the five
image repositories. The registry entries comprise 418 Docker schema-2 single-image manifests
and 45 OCI indexes. Every top-level digest was recomputed from its original response bytes
and matched the registry header; both digest and media type are retained.

Candidate **4.0.1** is absent from the tag refs, Release tags, all ten package inventories and
all five image repositories. This is a before-state observation, not a version selection or
authorization to dispatch. The qualifier's synthetic 0.0.302 version remains offline-only.
