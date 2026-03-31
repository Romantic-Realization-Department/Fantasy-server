# Example: Feature Branch PR (feature → develop)

## Branch context

- Current branch: `feat/auth-api`
- Base branch: `develop`

## Suggested PR titles (3 options)

1. `feature: JWT 기반 로그인·로그아웃 API 추가`
2. `feature: Auth 도메인 로그인·로그아웃 엔드포인트 구현`
3. `feature: 로그인·리프레시토큰·로그아웃 서비스 추가`

## Completed PR body example

---

## 📚작업 내용

- LoginService 구현 — 이메일·비밀번호 검증 및 JWT 발급
- LogoutService 구현 — Redis 리프레시 토큰 삭제
- RefreshTokenRedisRepository 추가 — Redis key: `refresh:{accountId}`, TTL 30일
- AuthController 추가 — `POST /v1/auth/login`, `POST /v1/auth/logout`
- 로그인 엔드포인트에 `"login"` RateLimit 정책 적용

## ◀️참고 사항

액세스 토큰 만료 시간은 `appsettings.json`의 `Jwt:AccessTokenExpirationMinutes` 값을 사용합니다.
로그아웃은 리프레시 토큰만 삭제하며, 액세스 토큰은 만료 전까지 유효합니다.

## ✅체크리스트

> `[ ]`안에 x를 작성하면 체크박스를 체크할 수 있습니다.

- [x] 현재 의도하고자 하는 기능이 정상적으로 작동하나요?
- [x] 변경한 기능이 다른 기능을 깨뜨리지 않나요?


> *추후 필요한 체크리스트는 업데이트 될 예정입니다.*

---

## Writing rules

- **작업 내용 bullets**: group by meaningful change, not by raw commit
- **참고 사항**: configuration notes, before/after comparisons, etc. Use `"."` if nothing to add
- Keep the total body under 2500 characters
- All text content in Korean (keep section header emojis as-is)
- No emojis in body text — section headers only
