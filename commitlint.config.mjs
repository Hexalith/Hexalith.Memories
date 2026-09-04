export default {
  extends: ['@commitlint/config-conventional'],
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
  },
};
