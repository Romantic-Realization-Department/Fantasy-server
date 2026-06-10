# Commit Type Guide — Fantasy Server

## feat — New capability added to the codebase

Use when creating new files is the primary change.

**Examples from this project:**

| Change | Commit message |
|---|---|
| Add LoginService.cs, LogoutService.cs | `feat: 로그인·로그아웃 서비스 추가` |
| Add AuthController.cs | `feat: Auth 컨트롤러 추가` |
| Add RefreshTokenRedisRepository.cs | `feat: 리프레시 토큰 Redis 레포지토리 추가` |
| Add new Entity class | `feat: {EntityName} 엔터티 추가` |
| Add new test class | `feat: {ServiceName} 테스트 추가` |
| Add new migration file | `feat: {MigrationName} 마이그레이션 추가` |

---

## fix — Broken behavior or missing registration/config corrected

Use when existing code is wrong, or a required wiring (DI, config key, middleware) is absent.
Adding only a DI registration line without adding the service file itself is also `fix`.

**Examples from this project:**

| Change | Commit message |
|---|---|
| Add missing `services.AddScoped<IAccountRepository, AccountRepository>()` in `Program.cs` | `fix: IAccountRepository DI 누락 수정` |
| Fix wrong prefix on Redis key | `fix: Redis 리프레시 토큰 키 prefix 수정` |
| Remove misconfigured EF Core ValueGeneration | `fix: UpdatedAt 자동 생성 설정 제거` |
| Fix typo in port value in `appsettings.json` | `fix: 개발 환경 DB 포트 수정` |
| Fix password comparison using HashPassword instead of BCrypt.Verify | `fix: 비밀번호 검증 로직 수정` |

---

## update — Existing code modified without adding a new capability

Use when modifying files that already exist — renaming, restructuring, adjusting behavior, etc.

**Examples from this project:**

| Change | Commit message |
|---|---|
| Change response type from `T` to `CommonApiResponse<T>` | `update: Auth 응답 타입을 CommonApiResponse로 변경` |
| Move JwtProvider to a different namespace | `update: JwtProvider를 Jwt 네임스페이스로 이동` |
| Update port/connection string in appsettings | `update: Docker 서비스 이름 통일 및 연결 문자열 수정` |
| Modify a property on an existing Entity | `update: Account 엔터티 수정` |
| Add a test method to an existing test class | `update: LoginService 테스트 추가` |
| Merge CI workflow steps | `update: CI 워크플로우 빌드·테스트 단계 통합` |

---

## Boundary rules

| Situation | Type |
|---|---|
| New `.cs` service/repository/controller file added | `feat` |
| New method added to an existing `.cs` file | `update` |
| DI registration line added alone, no new service file (`Program.cs`, `*Config.cs`) | `fix` |
| New service file + its DI registration added together | `feat` (same logical unit) |
| New migration file added | `feat` |
| Existing migration file corrected (column issue) | `fix` |
| New test class added | `feat` |
| Test method added to an existing test class | `update` |
| Refactoring without behavior change | `update` |

---

## When to split into multiple commits

If a branch mixes new features with unrelated bug fixes, split them:

```
# New service + its DI registration → one logical unit, commit together
git add Domain/Auth/Service/LoginService.cs Domain/Auth/Config/AuthServiceConfig.cs
git commit -m "feat: 로그인 서비스 추가"

# Separate Redis key bug fix → independent fix
git add Domain/Auth/Repository/RefreshTokenRedisRepository.cs
git commit -m "fix: 리프레시 토큰 Redis 키 prefix 수정"
```
