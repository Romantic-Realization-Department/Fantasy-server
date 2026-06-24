# Test Filter Patterns — Fantasy Server

## How filtering works

```bash
dotnet test --filter "FullyQualifiedName~{value}"
```

The `~` operator matches any fully qualified test name that **contains** the value as a substring.

Fully qualified name format:
```
{Namespace}.{OuterClass}+{InnerClass}.{MethodName}
```

Example:
```
Fantasy.Test.Account.Service.CreateAccountServiceTests+이메일이_존재하지_않을_때.회원가입_요청_시_계정이_저장된다
```

**Korean class and method names work as-is. No escaping required.**

---

## Current test classes in this project

| `/test` argument | What runs |
|---|---|
| _(no argument)_ | All tests |
| `CreateAccountServiceTests` | All CreateAccountService tests |
| `DeleteAccountServiceTests` | All DeleteAccountService tests (currently empty) |
| `LoginServiceTests` | All LoginService tests |
| `이메일이_존재하지_않을_때` | Email-not-found scenario (CreateAccount) |
| `이미_사용중인_이메일일_때` | Duplicate email scenario (CreateAccount) |
| `유효한_자격증명일_때` | Valid credentials scenario (Login) |
| `존재하지_않는_이메일일_때` | Email-not-found scenario (Login) |
| `잘못된_비밀번호일_때` | Wrong password scenario (Login) |

---

## Pattern examples

### Filter by outer class — runs all tests for a service
```
/test CreateAccountServiceTests
```
→ Matches all of `Fantasy.Test.Account.Service.CreateAccountServiceTests+*.*`

### Filter by Korean inner class — runs all tests in a scenario
```
/test 이메일이_존재하지_않을_때
```
→ Matches all methods inside that inner class

### Filter by method name — runs a single test
```
/test 회원가입_요청_시_비밀번호가_해싱된다
```
→ Matches the single method by name substring

### Filter by domain
```
/test LoginService
```
→ Matches all of `Fantasy.Test.Auth.Service.LoginServiceTests+*.*`

### Filter by namespace — runs an entire domain
```
/test Fantasy.Test.Account
```
→ Matches everything under the Account namespace
