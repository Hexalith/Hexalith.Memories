# Epic 23 Documentation Verification — Verification-Gap Review Prompt

Run this prompt in a separate Codex session with no prior conversation context, from
`/home/administrator/projects/hexalith/memories`:

> Invoke the `bmad-review-verification-gap` skill on the complete implementation diff
> described below. Work read-only and do not edit files.
>
> Baseline commit:
> `feac22bbc78c290f7ed8b1c2d5e1bfedf4dab133`
>
> Construct the diff as the union of:
>
> 1. All tracked changes:
>
>    ```bash
>    git diff --no-ext-diff --binary \
>      feac22bbc78c290f7ed8b1c2d5e1bfedf4dab133 -- \
>      . ':(exclude)references/**'
>    ```
>
> 2. Every untracked path returned by `git ls-files --others --exclude-standard`,
>    represented as a new-file diff:
>
>    ```bash
>    while IFS= read -r untracked_path; do
>      git diff --no-ext-diff --no-index --binary /dev/null "$untracked_path" ||
>        test "$?" -eq 1
>    done < <(git ls-files --others --exclude-standard)
>    ```
>
> Review the complete union, including tracked and untracked implementation artifacts.
> Return only concrete verification gaps with CWD-relative `path:line` evidence, the
> changed behavior or claim that could regress without reliable verification, and the
> required action. If there are no findings, return a brief clean verdict.

Paste the review findings back into the original workflow session without editing the
implementation first. The original session will deduplicate, classify, and route all
three review layers together.
