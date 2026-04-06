---
description: When to use AskUserQuestion — ambiguous requirements, design decisions, destructive changes, test scope, and post-investigation uncertainty.
globs: []
alwaysApply: true
---

# When to Use AskUserQuestion

Before proceeding, use `AskUserQuestion` in the following situations. Do NOT make assumptions — ask first.

## 1. Ambiguous Requirements

The request is too vague to implement safely without interpretation.

**Triggers:**
- The task omits scope, behavior, or acceptance criteria (e.g., "add room feature", "fix the auth issue")
- Multiple interpretations of the request are plausible
- The request implies a user-facing behavior change but doesn't specify the expected result

**Ask:** What exact behavior is expected? What are the success/failure conditions?

## 2. Design Decisions

Two or more valid implementation approaches exist and the choice affects the architecture.

**Triggers:**
- Choosing between a new service vs. extending an existing one
- Deciding whether logic belongs in the service layer vs. the domain entity
- Adding a new domain vs. extending an existing domain
- Determining whether to use Redis cache or DB for a given piece of state

**Ask:** Which approach do you prefer? (Briefly describe the trade-offs.)

## 3. Destructive or High-Impact Changes

The change modifies existing data, removes behavior, or has wide blast radius.

**Triggers:**
- Modifying an existing entity property (rename, type change, removal)
- Dropping or altering a DB column (even in Phase 1 deprecation)
- Changing a shared global service (`IPasswordHasher`, `ISessionService`, etc.)
- Removing or changing an existing API contract (route, request/response shape)

**Ask:** Confirm the intended change and whether a migration or data backfill is needed.

## 4. Test Scope

It is unclear how much test coverage to write for a given change.

**Triggers:**
- A new service is added — should all branches be tested, or only happy path?
- A bug fix — should a regression test be added?
- A refactor — should existing tests be updated or rewritten?

**Ask:** Which test cases should be covered? Should negative/edge cases be included?

## 5. Uncertainty After Investigation

After reading the relevant code and rules, the correct path is still unclear.

**Triggers:**
- Conflicting signals between existing code and the rules files
- The existing implementation doesn't match the documented pattern
- A rule file doesn't cover the specific scenario

**Ask:** Describe what was found and what options exist — ask which to follow.

---

## How to Ask

- Ask all questions in a **single** `AskUserQuestion` call — do not ask one at a time.
- Keep questions concrete and numbered.
- If relevant, briefly describe the trade-off so the user can make an informed choice.
- Do not ask about things already answered in the rules files or derivable from the code.
