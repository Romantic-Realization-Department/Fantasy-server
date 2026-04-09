---
name: plan-deep-dive
description: Conduct an in-depth structured interview with the user to uncover non-obvious requirements, tradeoffs, and constraints, then produce a detailed implementation spec file.
argument-hint: [instructions]
allowed-tools: Read, Write
context: fork
---

Follow the user instructions and conduct a structured, in-depth interview to uncover non-obvious requirements, constraints, and tradeoffs before writing an implementation spec.

Use the conversation to ask targeted questions about:

- technical scope and constraints
- product and UX expectations
- edge cases and failure handling
- rollout, migration, and operational risks
- testing and validation expectations

Avoid obvious or redundant questions. Keep drilling down until the requirements are concrete enough to implement safely.

After the interview is complete, write the resulting spec to a file and summarize the key decisions and open questions.

<instructions>$ARGUMENTS</instructions>
