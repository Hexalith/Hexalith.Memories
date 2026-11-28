export default {
  extends: ['@commitlint/config-conventional'],
  // Apply the ordinary rules to merge/revert/version-shaped messages too.
  defaultIgnores: false,
  plugins: [
    {
      rules: {
        // Epic 27 item 69 / Epic 31 retro A7: reject bare story/epic/maintenance placeholder subjects
        // (e.g., "fix: story 31.1", "fix: stpry 31.1", "fix: epic 30", "fix: submodules", "fix: sweep").
        'no-bare-story-subject': ({ subject }) => {
          const trimmed = (subject ?? '').trim();
          const barePattern = /^(?:(?:context\s+)?(?:story|stpry|epic)\s+\d+(?:[.-]\d+)?|submodules?|sweep|wip|temp|misc|releasefix:.*)$/i;
          return [
            !barePattern.test(trimmed),
            'subject must describe the actual change rather than a bare story/epic number or placeholder (e.g. "story 31.1", "submodules", "sweep")',
          ];
        },
      },
    },
  ],
  // Sprint Change Proposal 2026-07-05 (CI Package Boundary / Fluent Pin): the team's
  // conventional-commit subjects are written in sentence-case (e.g. "feat: Add ..."),
  // change-proposal bodies routinely exceed the default line cap, and descriptive headers can
  // exceed 100 characters. Subject casing and body line length are unrestricted, while headers
  // retain a 150-character cap. Type/scope/format enforcement from config-conventional is retained.
  // The shared AI baseline (AGENTS.md / CLAUDE.md / .github/copilot-instructions.md) requires a
  // type that accurately describes the work and permits `chore` for general maintenance, so
  // retain it in the explicit config-conventional type-enum.
  rules: {
    'type-enum': [
      2,
      'always',
      ['build', 'chore', 'ci', 'docs', 'feat', 'fix', 'perf', 'refactor', 'revert', 'style', 'test'],
    ],
    'subject-case': [0],
    'body-max-line-length': [0],
    'header-max-length': [2, 'always', 150],
    'no-bare-story-subject': [2, 'always'],
  },
};
