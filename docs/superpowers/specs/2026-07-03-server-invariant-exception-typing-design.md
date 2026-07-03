# 서버 불변식 예외 타입화 설계

작성일: 2026-07-03

## 1. 배경 / 문제

현재 "찾을 수 없음" 실패가 의미와 무관하게 뒤섞여 있다.

1. **500으로 새는 요청 경로**: `GetPlayerService`가 player 하위 데이터(재화/스테이지/세션) 누락을 `InvalidOperationException`(→ 500)으로 던진다.
2. **같은 상황의 상태코드 불일치**: 동일한 "player 하위 데이터 누락"을 다른 서비스들(Loadout/Weapon/Dungeon/SkillUnlock)은 `NotFoundException`(→ 404)으로 던진다.

### 검증된 사실

`CreatePlayerService.ExecuteAsync`는 player + 재화 + 스테이지 + 세션을 **하나의 트랜잭션으로 함께 생성**한다 (`CreatePlayerService.cs:55-70`). 넷은 원자적으로 커밋/롤백된다.

➡️ **player가 존재하면 재화/스테이지/세션은 반드시 존재한다.** 이들이 없으면 데이터 손상 = 서버 불변식 위반이며, 클라가 고칠 수 없으므로 **의미상 5xx**다.
- 따라서 `GetPlayerService`의 500은 사실 옳았고, 나머지 서비스의 404가 잘못됐다.

마찬가지로 시드된 **마스터 데이터**(스테이지 데이터, 직업 기본 스탯, 강화/각성 비용) 조회 실패도 서버 시딩/설정 문제이므로 5xx다. 현재는 404로 처리 중이다.

## 2. 결정 (승인됨)

- 상태코드는 **500으로 통일**한다.
- `ExpectedException`을 상속한 **두 개의 전용 예외 타입**을 만든다. 둘 다 500이지만 로그/telemetry에서 구분된다.
  - `PlayerStateException` — player 하위 데이터(재화/스테이지/세션) 불변식 위반
  - `GameDataMissingException` — 시드된 마스터/게임 데이터 조회 실패
- 위치: `Global/Exceptions/` (신설, 복수형 — SDK의 `...Exceptions` 네임스페이스 관례와 일치)
- 범위에 **Swagger 문서 재분류**와 **테스트 수정**을 포함한다.

## 3. 신규 예외 타입

SDK 확인 결과 `ExpectedException : System.Exception`은 `ctor(HttpStatusCode statusCode, string message)`를 가지며, 기존 `NotFoundException` 등은 이를 상속해 `ctor(string message)`로 상태코드를 고정한다. 동일 패턴을 따른다.

```csharp
// Global/Exceptions/PlayerStateException.cs
using System.Net;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Global.Exceptions;

public class PlayerStateException : ExpectedException
{
    public PlayerStateException(string message)
        : base(HttpStatusCode.InternalServerError, message) { }
}
```

```csharp
// Global/Exceptions/GameDataMissingException.cs
using System.Net;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Global.Exceptions;

public class GameDataMissingException : ExpectedException
{
    public GameDataMissingException(string message)
        : base(HttpStatusCode.InternalServerError, message) { }
}
```

메시지는 기존 문구를 그대로 유지한다(클라 노출돼도 민감정보 아님, 디버깅에 유용).

## 4. 분류 규칙

| 판별 | 처리 |
|---|---|
| 조회 키가 **클라 입력**에서 오고 없을 수 있음 (weaponId, skillId, runId, player 미생성) | **클라 4xx 유지** |
| player가 존재하는데 그 **하위 행**(재화/스테이지/세션)이 없음 | `PlayerStateException` (500) |
| **시드된 마스터 데이터**(스테이지/직업 기본 스탯/비용 테이블) 조회 실패 | `GameDataMissingException` (500) |

### 유지 (클라 4xx, 변경 없음)

- 404 `"플레이어(를/ 데이터를) 찾을 수 없습니다."` — player 행 자체 없음(미생성)
- 404 `"무기 데이터를 찾을 수 없습니다."` — 잘못된 weaponId(클라 입력)
- 404 `"보유하지 않은 무기입니다."`, `"골드 던전 런을 찾을 수 없습니다."`, `"존재하지 않는 스킬입니다."`
- 400/401/403/409 전부 유지

## 5. 마이그레이션 사이트 (전수)

### → `PlayerStateException` (재화/스테이지/세션 하위 데이터)

| 파일 | 라인 |
|---|---|
| `Player/Service/GetPlayerService.cs` | 54, 56, 58 (기존 `InvalidOperationException`) |
| `Player/Service/LoadoutService.cs` | 63, 66, 69 |
| `Player/Service/SkillUnlockService.cs` | 68, 70, 72, 88, 103, 105 |
| `Weapon/Service/UpgradeWeaponService.cs` | 76, 103, 106 |
| `Weapon/Service/SynthesizeWeaponService.cs` | 109, 112, 115 |
| `Weapon/Service/AwakenWeaponService.cs` | 79, 106, 109 |
| `Dungeon/Service/WeaponDungeonService.cs` | 78, 81, 84 |
| `Dungeon/Service/BossDungeonService.cs` | 81, 84, 87 |
| `Dungeon/Service/BasicDungeonStateService.cs` | 52, 55 |
| `Dungeon/Service/BasicDungeonClaimService.cs` | 61, 64, 67 |
| `Dungeon/Service/GoldDungeonRunService.cs` | 61, 64, 67 |
| `Dungeon/Service/GoldDungeonClaimService.cs` | 85, 88, 91, 137, 140, 143 |

### → `GameDataMissingException` (마스터 데이터)

| 파일 | 라인 | 데이터 |
|---|---|---|
| `Dungeon/Service/IdleRewardSettler.cs` | 46, 49 | 스테이지 데이터, 직업 기본 스탯 |
| `Dungeon/Service/WeaponDungeonService.cs` | 90, 115 | 직업 기본 스탯, 스테이지 데이터 |
| `Dungeon/Service/BossDungeonService.cs` | 93, 117 | 직업 기본 스탯, 스테이지 데이터 |
| `Dungeon/Service/BasicDungeonStateService.cs` | 61, 64 | 스테이지 데이터, 직업 기본 스탯 |
| `Weapon/Service/UpgradeWeaponService.cs` | 73 | 강화 비용 데이터 |
| `Weapon/Service/AwakenWeaponService.cs` | 73 | 각성 비용 데이터 |

> 주의: `"플레이어 스테이지 데이터"`(하위 행 → PlayerState)와 `"스테이지 데이터"`(마스터 → GameDataMissing)는 문구로 구분된다. 라인별로 정확히 대조할 것.

각 서비스에서 `using Fantasy.Server.Global.Exceptions;`를 추가한다. 사용하지 않게 된 `NotFoundException` 등의 using은 다른 곳에서 여전히 쓰이면 유지한다.

## 6. Swagger 문서 재분류

컨트롤러의 `[ApiError]` 중, 5장에서 500으로 재분류된 메시지에 해당하는 항목을 **삭제**한다. 500은 클라 계약이 아니므로 `[ApiError]`로 문서화하지 않는다. 진짜 4xx만 남긴다.

예시(재분류 후):
- `POST /v1/player/loadout` → 404 `"플레이어 데이터를 찾을 수 없습니다."` + 400 ×5 (재화/스테이지/세션/스테이지데이터/직업기본스탯 404 제거)
- `POST /v1/weapons/{id}/upgrade` → 404 `"플레이어 데이터"`, `"무기 데이터"`, `"보유하지 않은 무기"` + 400 ×2 (강화비용/재화/스테이지/세션 404 제거)
- `GET /v1/dungeons/basic/state` → 404 `"플레이어 데이터를 찾을 수 없습니다."` 만 (나머지 5개 제거)
- `POST /v1/dungeons/gold-runs/{runId}/claim` → 404 `"골드 던전 런"`, `"플레이어 데이터"` + 403 + 400 ×3 (재화/스테이지/세션 404 제거)

## 7. 범위 밖 (변경하지 않음)

- **엔티티 방어 가드**: `Dungeon/Entity/AccountDungeonTicket.cs:29` `"티켓이 부족합니다"` (`InvalidOperationException`). 서비스(`GoldDungeonRunService`)가 먼저 `BadRequestException`으로 검사하므로 도달 불가한 방어 코드이며, 엔티티를 HTTP 예외 타입에 의존시키지 않기 위해 그대로 둔다.
- **시작/설정 검증**: `JwtConfig`, `JwtProvider`, `DatabaseConfig`, `RedisConfig`, `AppDbContextFactory`, `GameDataSeeder`의 `InvalidOperationException`. 앱 부팅 단계 실패이므로 500/부팅 실패가 맞다.

## 8. 테스트

- 기존 테스트 중 위 사이트에 대해 `NotFoundException`(또는 `InvalidOperationException`)을 단언하는 케이스가 있으면 새 타입으로 갱신한다.
- 내부 불변식 경로(하위 데이터 누락)는 대부분 단위 테스트가 없을 것으로 예상되나, `/test` 결과에 따라 test-fixer로 처리한다.

## 9. 검증

1. `/test` — Build 성공 + 전체 통과 (233개 기준, 예외 타입 변경 반영)
2. 인프로세스 Swagger harness 재실행 — 재분류된 엔드포인트에서 500 메시지가 4xx 문서에서 사라지고 진짜 4xx만 남는지 확인

## 10. 성공 기준

- [ ] `PlayerStateException`, `GameDataMissingException` 생성 (`Global/Exceptions/`)
- [ ] 5장 전 사이트가 정확히 마이그레이션됨 (라인 대조)
- [ ] `GetPlayerService`의 `InvalidOperationException` 3곳 제거
- [ ] 재분류된 `[ApiError]` 삭제, 진짜 4xx만 유지
- [ ] Build + 전체 테스트 통과
- [ ] Swagger harness로 재분류 확인
