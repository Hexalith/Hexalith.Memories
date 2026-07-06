# Story Creation Lessons

This ledger was bootstrapped automatically by `jobs/preflight-predev-hardening.py`
because this repository had no existing story-creation lessons file.

Use this file to record durable lessons for recurring BMAD story creation,
party-mode review, advanced elicitation, and code-review automation.

## L08 - Party Review vs. Elicitation

- Party-mode review is the cross-role critique and triage pass before
  development; it should produce dated trace evidence when completed.
- Advanced elicitation is a separate hardening pass after a completed
  party-mode trace exists; a recommendation to run elicitation is not itself
  completed elicitation evidence.

## L09 - Historical Broad Slices Are Anti-Templates

- Completed broad technical slices may be used only as historical context or
  dependency evidence. They must not be copied as the shape for new stories.
- When epics mark a story as historical completed scope, any reopened,
  reimplemented, or analogous work must be split into newly numbered vertical
  stories with independently observable evidence.
- Story creation must classify previous-story context as reusable pattern,
  historical reference only, or anti-template before carrying lessons forward.
- Review must flag a story or implementation that reuses a historical broad
  slice as a template, hides broad scope behind one story, or accepts internal
  classes/unit tests as sufficient proof where observable API/CLI/contract,
  trace, or integration evidence is required.
