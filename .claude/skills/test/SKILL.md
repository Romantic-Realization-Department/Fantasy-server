---
name: test
description: Builds Fantasy.Server then runs all tests in Fantasy.Test. Reports pass/fail results. e.g. /test CreateAccountService
argument-hint: [ClassName or MethodName (optional)]
allowed-tools: Bash(dotnet build:*), Bash(dotnet test:*)
context: fork
---

Build the server project, then run the test suite and report results.

## Steps

### Step 1 — Build

```bash
dotnet build Fantasy.Server/Fantasy.Server.csproj
```

- Build **fails**: list each error with its file path and line number, explain the likely cause, then stop.
- Build **succeeds**: continue to Step 2.

### Step 2 — Run Tests

If `$ARGUMENTS` is empty, run all tests:

```bash
dotnet test Fantasy.Test/Fantasy.Test.csproj --no-build
```

If `$ARGUMENTS` is provided, filter by class or method name:

```bash
dotnet test Fantasy.Test/Fantasy.Test.csproj --no-build --filter "FullyQualifiedName~$ARGUMENTS"
```

### Step 3 — Report Results

Report in this format:

```
Test Results

Passed:  N
Failed:  N
Skipped: N
```

If all tests pass, add: "All tests passed."

If any tests fail, list each failure:

```
Failed Tests:

- {FullyQualifiedTestName}
  Error: {exception type and message}
  Location: {file path and line number if available}
```

Do not truncate error messages. If the failure output contains an inner exception, include it.
