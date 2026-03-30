---
description: Build-and-verify workflow. Run build then tests after any C# code change.
globs: ["**/*.cs"]
alwaysApply: false
---

## Build-and-Verify Workflow

After adding or modifying any `.cs` file, run `/test` without asking the user.

**If the build fails:** fix the build errors, then run `/test` again.

**If tests fail:**
- Production code bug → fix the production code.
- Test is outdated or needs updating to match new behavior → fix the test code.

Run `/test` again after fixing. Only consider the task complete when the build succeeds and all tests pass.
