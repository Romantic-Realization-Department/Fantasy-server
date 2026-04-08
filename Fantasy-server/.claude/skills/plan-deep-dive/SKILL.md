---
name: plan-deep-dive
description: Conduct an in-depth structured interview with the user to uncover non-obvious requirements, tradeoffs, and constraints, then produce a detailed implementation spec file.
argument-hint: [feature description or task goal]
allowed-tools: AskUserQuestion, Write, Bash(mkdir:*)
---

Use `$ARGUMENTS` as the primary description of the feature or task. Start the interview based on it and expand into detailed requirements.

If `$ARGUMENTS` is provided, treat it as the initial feature idea and begin the interview immediately without asking for clarification of the topic itself.

Interview me in detail using the AskUserQuestionTool about anything non-obvious: technical implementation, UI & UX, concerns, tradeoffs, constraints, edge cases, etc. Be very in-depth and continue interviewing me continually until it's complete.

Once the interview is complete and you have enough information to write the spec:

1. Derive a concise `feature-name` in kebab-case based on the full interview context. Do not rely solely on `$ARGUMENTS` — use it only as an initial hint. Examples: `dungeon-system`, `auth-refresh`, `player-inventory`.

2. Create the plans directory if it doesn't exist:
   ```bash
   mkdir -p .claude/plans
   ```

3. Write the full spec to `.claude/plans/{feature-name}-plan.md`. Use structured markdown with headings (`#`, `##`, `###`), lists, and tables where appropriate.

4. Copy the same content verbatim to `.claude/current_plan.md` (overwrite — do not summarize). This file always reflects the most recently created plan.

5. Tell the user:
   - The plan was saved to `.claude/plans/{feature-name}-plan.md`
   - `.claude/current_plan.md` was updated
   - They can run `/review-plan` to send this plan to Codex for review

<task>$ARGUMENTS</task>
