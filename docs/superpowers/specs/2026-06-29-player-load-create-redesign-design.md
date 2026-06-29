# Player 존재/생성/로드 흐름 재설계 — 설계 문서

- 날짜: 2026-06-29
- 상태: 승인됨 (구현 전)
- 범위: `POST /v1/player/init` 흐름만. `loadout`, `skill/unlock`은 변경하지 않음.

## 1. 배경 / 문제

현재 `POST /v1/player/init`(`InitPlayerService`)이 **조회·생성·충돌**을 한 엔드포인트에서 겸한다.

- 캐시/DB에 플레이어 존재(같은 직업) → 로드(200)
- 없음 → `jobType`으로 생성(201)
- 다른 직업으로 존재 → `ConflictException`(409), **데이터 미반환·복구 경로 없음**

세 가지 모호함이 발생한다.

1. 하나의 `POST`가 "생성"과 "로드"를 동시에 담당해 의미가 흐릿하다.
2. `jobType`이 항상 필수라, 단순 로드 상황(재접속)에서도 클라이언트가 직업을 알고 보내야 한다.
3. "플레이어가 있는지/어떤 직업인지"만 조회할 수단이 없다. 새 기기에서 잘못된 `jobType`을 보내면 409로 막히고 복구 흐름이 모호하다.

PR #28 리뷰(gemini-code-assist)에서도 동일 지점을 지적했다.

## 2. 목표 / 비목표

**목표**
- 조회(읽기)와 생성(쓰기)을 의미가 분명한 별도 엔드포인트로 분리한다.
- `jobType`은 생성할 때만 요구한다.
- 플레이어 존재 여부를 명확한 상태 코드(200/404)로 확인할 수 있게 한다.
- 직업 불일치 409 모호함을 제거한다.

**비목표**
- `loadout`, `skill/unlock` 엔드포인트는 건드리지 않는다.
- 하위호환/마이그레이션은 고려하지 않는다. 클라이언트는 출시 전이라 계약을 자유롭게 교체한다.

## 3. API 계약

| 메서드 | 경로 | 본문 | 성공 | 실패 |
|---|---|---|---|---|
| GET | `/v1/player` | 없음 | **200** + `PlayerDataResponse` | **404** (플레이어 미생성) |
| POST | `/v1/player` | `{ jobType }` | **201** + `PlayerDataResponse` | **409** (이미 존재) |

- 둘 다 `[Authorize]` + 레이트리밋 `game` 유지.
- `POST /v1/player/init`은 제거한다.

**클라이언트 흐름**

```
로그인 → GET /v1/player
  ├ 200 → 플레이어 로드, 바로 게임 진입
  └ 404 → 직업 선택 화면 → POST /v1/player { jobType } → 201 진입
```

"플레이어 없음"은 GET에서 **404**(`NotFoundException`)로 표현한다. SDK 예외 패턴과 일치하고 클라이언트 분기가 명확하다.

## 4. 코드 구조

기존 `InitPlayerService`를 use-case 컨벤션("인터페이스 1개 + `ExecuteAsync` 1개")에 맞춰 둘로 분리한다.

### 4.1 서비스

- **`IGetPlayerService` / `GetPlayerService`** — `Task<PlayerDataResponse> ExecuteAsync()`
  - Redis 우선 조회 → 히트 시 반환.
  - 미스 → DB `FindByAccountAsync` → `null`이면 `NotFoundException` throw → 있으면 응답 빌드 후 캐시 SET, 반환.
  - 기존 `InitPlayerService`의 로드 분기를 재사용하되 직업 불일치(409) 검사는 제거한다.

- **`ICreatePlayerService` / `CreatePlayerService`** — `Task<PlayerDataResponse> ExecuteAsync(CreatePlayerRequest)`
  - DB `FindByAccountAsync` → 이미 존재하면 `ConflictException` throw.
  - 없으면 기존 생성 분기(트랜잭션으로 Player·Resource·Stage·Session 생성, weapons/skills 조회, 응답 빌드)를 그대로 수행하고 캐시 SET 후 반환.

### 4.2 DTO

- `InitPlayerRequest(JobType)` → **`CreatePlayerRequest(JobType)`** 로 이름 변경. (`[Required] JobType`)
- 응답 `PlayerDataResponse`는 변경 없음.

### 4.3 컨트롤러 (`PlayerController`)

- `IInitPlayerService` 주입 제거, `IGetPlayerService`·`ICreatePlayerService` 주입.
- `[HttpGet]` → `Get()` : `PlayerDataResponse` 반환(plain DTO → `ApiResponseWrapperFilter`가 200 래핑).
- `[HttpPost]` → `Create([FromBody] CreatePlayerRequest)` : `CommonApiResponse.Created("플레이어가 생성되었습니다.", data)` 반환(201).
- `(data, isNew)` 튜플 분기 제거.

### 4.4 DI (`PlayerServiceConfig`)

- `IInitPlayerService` 등록 제거.
- `IGetPlayerService`/`GetPlayerService`, `ICreatePlayerService`/`CreatePlayerService` 를 `AddScoped`로 등록.

## 5. 제거 대상

- `POST /v1/player/init` 라우트
- `IInitPlayerService` / `InitPlayerService`
- `InitPlayerRequest` (→ `CreatePlayerRequest`로 이름 변경)
- `(PlayerDataResponse Data, bool IsNew)` 튜플 패턴

## 6. 테스트

- `InitPlayerServiceTests` 제거, 다음으로 분리:
  - **`GetPlayerServiceTest`**
    - 캐시 히트 → 캐시 데이터 반환, DB 미조회.
    - 캐시 미스 + 플레이어 존재 → 로드 후 캐시 SET.
    - 플레이어 없음 → `NotFoundException`.
  - **`CreatePlayerServiceTest`**
    - 플레이어 없음 → 생성 후 데이터 반환.
    - 플레이어 이미 존재 → `ConflictException`.
- `PlayerControllerRouteTests` → `GET /v1/player`, `POST /v1/player`로 갱신, `init` 라우트 제거.
- 기존 컨벤션 준수: NSubstitute 모킹, FluentAssertions, AAA 구조.

## 7. 문서 업데이트 (`docs/client-integration-guide.md`)

새 플레이어 흐름에 맞춰 갱신한다.

- 2장 전체 흐름도: `player/init` → `GET /v1/player` / `POST /v1/player`.
- 4장 플레이어 표/설명: GET 로드(200/404), POST 생성(201/409)으로 교체. 기존 init 멱등 설명 삭제.
- 에러 표(34행대): 409 의미를 "플레이어 이미 존재"로, "다른 직업 플레이어 존재" 항목 제거. 404에 "플레이어 미생성" 보강.

## 8. PR #28 처리

PR #28(클라이언트 연동 가이드 문서)은 **보류**한다. 위 7번 문서 업데이트를 이 재설계 구현 작업에 포함하여, 새 플레이어 흐름이 반영된 문서로 정리한다.

## 9. 검증

- `/test`로 빌드 + 전체 테스트 통과 확인.
- 클라이언트 흐름(로그인 → GET 404 → POST 201 → GET 200) 수동 확인 권장.
