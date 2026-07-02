# 서버 권위 기능 확장 — 설계 문서

- 날짜: 2026-07-02
- 상태: 단계별 구현 중 — Phase 1(Tutorial) 완료, Phase 2(던전 타입별 진행도) 구현 완료, Phase 3(무기 강화/합성/각성) 구현 완료, Phase 4 미착수
- 범위: Tutorial 신규 도메인, 던전 타입별 진행도 분리, 무기 강화/합성/각성, RewardTransaction 감사 로그
- 비범위: 무기/스킬 ID 전면 문자열 전환, 스킬·무기 장착 API 재설계, 다중 캐릭터/직업 전환, 전 던전 공통 Run 모델 강제

## 1. 배경

클라이언트가 정리한 요구사항(직업 선택, 재화, 던전 4종, 무기 획득/합성/강화/각성/장착, 직업별 스킬 해금/장착, 튜토리얼, 던전 클리어 보상)을 기존 서버와 비교한 결과, 다음이 이미 구현되어 있다.

- `ASP.NET Core Web API + EF Core(Npgsql) + Redis + JWT` 스택, `Domain/{Name}` 계층 구조
- 계정당 단일 직업(`Player`), 서버 권위 성장 계산(레벨/재화), `xmin` 낙관적 동시성(`PlayerResource`/`PlayerStage`/`PlayerSession`/`GoldDungeonRun`/`AccountDungeonTicket`)
- 던전 4종(`basic`/`gold`/`weapon`/`boss`) 전부 구현, 골드 던전은 `GoldDungeonRun` + 멱등 claim으로 완결
- `GET/POST /v1/player`, `POST /v1/player/loadout`, `POST /v1/player/skill/unlock`, 게임 데이터 조회 API 전부 구현

비교 과정에서 발견한 4가지 설계 충돌에 대해 사용자 결정을 받았다.

| # | 충돌 지점 | 결정 |
|---|---|---|
| 1 | 무기/스킬 ID: 문자열 vs 기존 int | 신규 기능(튜토리얼 등)만 안정적 문자열 ID 사용, 기존 무기/스킬 int 구조 유지 |
| 2 | 던전 진행도: 기본 던전 종속 vs 던전별 독립 | 던전 타입별 독자 `HighestClearedStage`/`HighScore` 도입 |
| 3 | 보상 이력: 전 던전 공통 Run+Ledger vs 현행 유지 | `RewardTransaction` 로그만 얇게 추가, 기존 Run/즉시재계산 전략 유지 |
| 4 | 직업 모델: 단일 vs 다중 캐릭터 | 계정당 단일 직업 유지 (PR #29 결정 유지) |

이어서 세부 인터뷰로 각 항목의 구체 사양을 확정했다. 이 문서는 그 결과를 구현 가능한 설계로 정리한다.

## 2. 확정된 설계 결정 요약

| 영역 | 결정 |
|---|---|
| 튜토리얼 게이팅 | 순수 기록용. 다른 API 로직을 제한하지 않음 |
| 튜토리얼 ID 검증 | 서버 화이트리스트로만 허용, 목록 외 400 |
| 무기 강화 성공 판정 | 확정 성공 — 자원이 충분하면 항상 성공, 실패 개념 없음 |
| 무기 강화 비용 | `EnhancementLevel`별 마스터 데이터 테이블로 관리 |
| 무기 강화 최대 레벨 | 무기별 마스터 데이터 컬럼 |
| 무기 합성 메커니즘 | 동일 무기 N개 → 다음 등급 무기 1개 |
| 합성 필요 개수(N) | 무기별 마스터 데이터 컬럼 |
| 무기 각성 소모 자원 | 동일 무기 복사본(Count) + 미스릴, 둘 다 소모 |
| 각성 비용 | `AwakeningLevel`별 마스터 데이터 테이블(무기 개수 + 미스릴 개수) |
| 무기/보스 던전 진행도 | 완전 독립 — 각자 1부터 시작, 자기 진행도가 난이도 기준 |
| 무기/보스 던전 난이도 테이블 | 기존 `StageData` 공유, 자기 진행도 인덱스로 조회(신규 테이블 없음) |
| 골드 던전 HighScore | 단일 run 최고 획득 골드 |
| RewardTransaction 기록 범위 | 던전 보상 + 무기 강화/합성/각성 결과만 (스킬 해금 SP 소모 등은 제외) |
| 스킬/무기 장착 API | 현행 `POST /player/loadout` 배열 통째 교체 유지, 슬롯 단위 PUT/DELETE 신설 안 함 |
| 구현 순서 | 단계별 PR: Tutorial → 던전 타입별 진행도 → 무기 강화/합성/각성 → RewardTransaction |

## 3. Phase 1 — Tutorial 도메인

### 3.1 데이터 모델

새 도메인 `Domain/Tutorial/`. 튜토리얼은 이름/설명 등 표시 데이터를 서버가 갖지 않으므로(클라이언트가 표시 텍스트 보유, 기존 스킬/무기 조회 API와 동일한 원칙) 별도 마스터 테이블 없이 **코드 상수 화이트리스트**로 검증한다.

```csharp
// Domain/Tutorial/Constant/TutorialIds.cs
public static class TutorialIds
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        "tutorial_first_game_start",
        "tutorial_first_dungeon",
        "tutorial_first_upgrade"
    };
}
```

```csharp
// Domain/Tutorial/Entity/PlayerTutorial.cs
public class PlayerTutorial
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public string TutorialId { get; private set; } = string.Empty;
    public DateTime CompletedAt { get; private set; }

    public static PlayerTutorial Create(long playerId, string tutorialId) => new()
    {
        PlayerId = playerId,
        TutorialId = tutorialId,
        CompletedAt = DateTime.UtcNow
    };
}
```

- 테이블: `tutorial.player_tutorial` — `(player_id, tutorial_id)` 유니크 인덱스.
- 신규 목록 추가는 코드 배포로만 가능(마스터 데이터 테이블이 아니므로). 튜토리얼 종류가 자주 바뀔 예정이면 Phase 착수 전에 재확인 필요.

### 3.2 API

| 메서드 | 경로 | 본문 | 성공 | 실패 |
|---|---|---|---|---|
| POST | `/v1/tutorials/{tutorialId}/complete` | 없음 | 200 | 400(목록에 없는 ID) |
| GET | `/v1/tutorials` | 없음 | 200 | - |

- `[Authorize]` + `[EnableRateLimiting("game")]`.
- 이미 완료된 튜토리얼을 다시 호출하면 멱등 성공(중복 insert 없음).
- 재화/성장에 영향이 없으므로 응답에 `ChangesDto`/`PlayerDataResponse`를 포함하지 않는다(다른 상태 변경 API와 다른 점 — 튜토리얼은 성장 상태를 바꾸지 않기 때문).
- **`PlayerDataResponse`는 건드리지 않는다.** 재접속 시 튜토리얼 상태 동기화는 `GET /v1/tutorials`(전용 조회, 기존 `GET /v1/dungeons/tickets`와 동일한 "소규모 상태 조회" 패턴)로 분리한다. `PlayerDataResponse`에 필드를 추가하면 이를 생성하는 7개 서비스(GetPlayerService/CreatePlayerService/LoadoutService/SkillUnlockService/BasicDungeonClaimService/GoldDungeonRunService/GoldDungeonClaimService)를 전부 수정해야 하는데, 튜토리얼은 성장 상태와 무관한 순수 기록이라 그 정도 파급을 정당화하지 못한다.

```csharp
public record TutorialCompleteResponse(string TutorialId, bool WasAlreadyCompleted, DateTime CompletedAt);
public record CompletedTutorialsResponse(List<string> CompletedTutorialIds);
```

### 3.3 서비스 로직

**`ICompleteTutorialService`**

1. `accountId`를 `ICurrentUserProvider`에서 가져온다.
2. `player = FindByAccountAsync(accountId)` — 없으면 `NotFoundException`.
3. `tutorialId`가 `TutorialIds.All`에 없으면 `BadRequestException`.
4. 기존 완료 기록 조회 — 있으면 `WasAlreadyCompleted = true`로 멱등 응답.
5. 없으면 `PlayerTutorial.Create` 후 저장, `WasAlreadyCompleted = false` 응답.

**`IGetCompletedTutorialsService`**

1. `accountId`를 `ICurrentUserProvider`에서 가져온다.
2. `player = FindByAccountAsync(accountId)` — 없으면 `NotFoundException`.
3. `FindAllByPlayerIdAsync(player.Id)` 조회 후 `TutorialId` 목록만 추려 반환.

### 3.4 테스트

- `CompleteTutorialServiceTest`: 정상 완료, 중복 완료(멱등), 목록에 없는 ID(400), 플레이어 없음(404).
- `GetCompletedTutorialsServiceTest`: 완료 기록 있음/없음, 플레이어 없음(404).
- `TutorialControllerRouteTests`: 라우트 존재 확인(기존 `PlayerControllerRouteTests` 패턴 재사용).

## 4. Phase 2 — 던전 타입별 진행도 ✅ 구현 완료 (2026-07-02)

### 4.1 `DungeonType`

구조적 분류값이므로 `JobType`과 동일하게 C# enum + `.HasConversion<string>()`로 DB에는 이름으로 저장한다(기존 `conventions.md` 규칙 그대로 적용, 신규 문자열 ID 원칙과는 무관 — 이건 컨텐츠 ID가 아니라 고정된 4종 카테고리).

```csharp
public enum DungeonType { Basic, Gold, Weapon, Boss }
```

### 4.2 `PlayerDungeonProgress` 엔티티

기존 `PlayerStage`(기본 던전 전용, `LastCalculatedAt` 기반 방치 정산)는 **그대로 둔다** — 방치 정산 메커니즘은 "경과 시간 누적"이라 이번 "던전별 최고 스테이지/최고 점수" 개념과 목적이 다르다. 신규 테이블은 골드/무기/보스 3종만 다룬다.

```csharp
public class PlayerDungeonProgress
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public DungeonType DungeonType { get; private set; }
    public long HighestClearedStage { get; private set; }
    public long HighScore { get; private set; }
    public DateTime? LastClearedAt { get; private set; }

    public static PlayerDungeonProgress Create(long playerId, DungeonType dungeonType) => new()
    {
        PlayerId = playerId,
        DungeonType = dungeonType,
        HighestClearedStage = 1,
        HighScore = 0,
        LastClearedAt = null
    };

    public void ClearStage(long stage)
    {
        if (stage > HighestClearedStage) HighestClearedStage = stage;
        LastClearedAt = DateTime.UtcNow;
    }

    public void UpdateHighScore(long score)
    {
        if (score > HighScore) HighScore = score;
        LastClearedAt = DateTime.UtcNow;
    }
}
```

- `ClearStage`/`UpdateHighScore`는 `Player.UpdateLevel`/`UpdateExp`와 동일하게 외부 시간 파라미터 없이 엔티티 내부에서 직접 `DateTime.UtcNow`를 찍는다. `TimeProvider` 주입은 경과 시간 계산(골드 던전 만료 판정 등)에만 예약되어 있고, 단순 "이 이벤트가 언제 발생했는지 기록"하는 타임스탬프는 엔티티가 직접 처리하는 것이 기존 컨벤션이다.

- 테이블: `dungeon.player_dungeon_progress` — `(player_id, dungeon_type)` 유니크 인덱스.
- `xmin` 동시성 토큰 적용(다른 Player 하위 테이블과 동일).

### 4.3 던전별 반영 방식

| 던전 | 갱신 필드 | 난이도 조회 |
|---|---|---|
| Weapon | `HighestClearedStage`만 (자기 진행도 기준 클리어 시 +1) | 기존 `StageData`를 **자기 `HighestClearedStage`**로 조회 |
| Boss | `HighestClearedStage`만 | 기존 `StageData`를 **자기 `HighestClearedStage`**로 조회, 보스 HP = 해당 stage `MonsterHp × 5` (기존 로직 유지) |
| Gold | `HighScore`만 (해당 run `EarnedGold`가 기존 `HighScore`보다 크면 갱신) | 해당 없음(클릭 기반, StageData 미사용) |
| Basic | 변경 없음 — 기존 `PlayerStage.MaxStage` 유지 | 기존과 동일 |

- `WeaponDungeonService`/`BossDungeonService`는 현재 `PlayerStage.MaxStage`를 난이도 기준으로 참조하고 있었으나(`docs/TODO.md` 확인 결과), 이 Phase부터는 각자 `PlayerDungeonProgress` 행을 참조하도록 변경한다.
- `GoldDungeonClaimService`가 claim 처리 시 `PlayerDungeonProgress(Gold)`를 함께 갱신(같은 트랜잭션).

### 4.4 API/응답 확장

신규 GET 엔드포인트를 추가하지 않고, 기존 응답 DTO에 필드를 확장한다(API 표면 최소화).

- `WeaponDungeonResponse`, `BossDungeonResponse`에 `HighestClearedStage` 추가.
- `GoldDungeonClaimResponse`에 `HighScore` 추가.
- `DungeonTicketResponse`는 건드리지 않는다 — 이 엔드포인트는 계정 기준으로만 동작하고 Player 조회가 없으므로, 새로 Player 의존성을 넣지 않기로 결정(2026-07-02 확정).

### 4.5 확인 필요 사항 — 확정됨 (2026-07-02)

- 무기/보스 던전이 "독자 진행도"로 바뀌면 기존에 기본 던전 스테이지에 종속되어 있던 난이도 곡선이 사실상 초기화(둘 다 1부터 시작)되는 셈이라 밸런싱에 영향을 준다는 점을 사용자에게 명시적으로 확인했다.
- 결정: **백필하지 않는다.** 기존 유저 포함 전원 지연 생성(lazy-create)으로 1부터 시작한다. 기존 `AccountDungeonTicket`/`GoldDungeonRun`과 동일한 지연 생성 패턴을 따르며, 별도 데이터 마이그레이션/백필 스크립트를 두지 않는다. 이는 앞서 확정한 "완전 독립, 각자 1부터 시작" 원칙과 일치한다.

## 5. Phase 3 — 무기 강화/합성/각성 ✅ 구현 완료 (2026-07-02)

### 5.1 마스터 데이터 확장

`WeaponData`에 컬럼 추가:

```csharp
public long MaxEnhancementLevel { get; private set; }
public long MaxAwakeningLevel { get; private set; }
public int? SynthesizeRequiredCount { get; private set; }   // null = 합성 불가(최고 등급)
public int? SynthesizeResultWeaponId { get; private set; }  // null = 합성 불가(최고 등급)
```

신규 마스터 테이블 2개(`GameData` 도메인 소속):

```csharp
// game_data.weapon_enhancement_cost — PK(WeaponId, EnhancementLevel)
public class WeaponEnhancementCost
{
    public int WeaponId { get; private set; }
    public long EnhancementLevel { get; private set; } // 이 레벨 → 다음 레벨 비용
    public long RequiredGold { get; private set; }
    public long RequiredScroll { get; private set; }
}

// game_data.weapon_awaken_cost — PK(WeaponId, AwakeningLevel)
public class WeaponAwakenCost
{
    public int WeaponId { get; private set; }
    public long AwakeningLevel { get; private set; } // 이 레벨 → 다음 레벨 비용
    public int RequiredCount { get; private set; }
    public int RequiredMithril { get; private set; }
}
```

기존 `GameDataSeeder`/`GameDataCacheService` 패턴을 그대로 따라 시드 JSON + Redis 캐시로 관리한다.

### 5.2 API

| 메서드 | 경로 | 본문 | 실패 |
|---|---|---|---|
| POST | `/v1/weapons/{weaponId}/upgrade` | 없음 | 404 미보유, 400 최대 레벨 도달·재화 부족 |
| POST | `/v1/weapons/{weaponId}/synthesize` | 없음 | 404 미보유, 400 재료 부족·합성 불가 무기 |
| POST | `/v1/weapons/{weaponId}/awaken` | 없음 | 404 미보유, 400 최대 각성 도달·복사본/미스릴 부족 |

무기 장착(`equip`)은 기존 `POST /player/loadout`으로 계속 처리하므로 신규 엔드포인트를 만들지 않는다. 동시 요청으로 인한 충돌은 409(§5.4). 실패 사유는 위 표기처럼 문서상 설명 라벨일 뿐 실제 응답은 HTTP 상태 코드 + 한글 메시지로만 구분된다(§5.5 ④ — 머신 리더블 `ErrorCode` 필드 미도입).

### 5.3 처리 순서

**강화(upgrade)**

1. 소유 무기 확인(`PlayerWeapon` 존재) → 없으면 `WEAPON_NOT_OWNED`.
2. 현재 `EnhancementLevel`이 `WeaponData.MaxEnhancementLevel` 이상이면 `MAX_UPGRADE_REACHED`.
3. `WeaponEnhancementCost(weaponId, 현재레벨)` 조회 → `RequiredGold`/`RequiredScroll` 확인.
4. 재화 부족 시 `INSUFFICIENT_CURRENCY`.
5. 재화 차감 + `EnhancementLevel + 1` — 하나의 트랜잭션.
6. `RewardTransaction`(소모 방향) 기록.

**합성(synthesize)**

1. 소유 무기 확인, `SynthesizeRequiredCount`/`SynthesizeResultWeaponId`가 null이면 합성 불가 응답.
2. `PlayerWeapon.Count >= SynthesizeRequiredCount` 확인 → 부족 시 400.
3. `Count -= SynthesizeRequiredCount`, 결과 무기(`SynthesizeResultWeaponId`) `PlayerWeapon`이 없으면 생성(Count 1) / 있으면 `Count += 1`.
4. 결과 무기의 강화/각성 레벨은 0에서 시작(기존 재료 무기의 레벨을 승계하지 않음).
5. `RewardTransaction` 기록.

**각성(awaken)**

1. 소유 무기 확인, 현재 `AwakeningCount`가 `MaxAwakeningLevel` 이상이면 `MAX_UPGRADE_REACHED`.
2. `WeaponAwakenCost(weaponId, 현재각성레벨)` 조회 → `RequiredCount`(복사본)와 `RequiredMithril` 확인.
3. `PlayerWeapon.Count >= RequiredCount + 1`(자기 자신 제외 복사본 소모 기준 확정 필요, 아래 참고) 및 `PlayerResource.Mithril >= RequiredMithril` 확인.
4. `Count -= RequiredCount`, `Mithril -= RequiredMithril`, `AwakeningCount + 1`.
5. `RewardTransaction` 기록.

### 5.4 동시성

`PlayerWeapon`에 `xmin` 동시성 토큰이 없으므로 이 Phase에서 반드시 추가한다(강화/합성/각성이 모두 같은 행을 동시 변경할 수 있는 경합 지점). 토큰 충돌(`DbUpdateConcurrencyException`)은 기존 `AppDbTransactionRunner` 정책 그대로 `ConflictException`(409)으로 매핑한다 — 머신 리더블 에러코드는 도입하지 않는다(§5.5 ④ 참고).

### 5.5 확인 필요 사항 — 확정됨 (2026-07-02)

Phase 3 구현 중 실제 코드로 확정된 가정 4건:

1. **강화 비용 통화 구성** — Gold만/Scroll만/혼합인지 확정 필요했음.
   결정: **Gold + Scroll 혼합.** 시드 데이터 기준 `EnhancementLevel` 0~4는 Gold만, 5부터는 Gold + 강화 스크롤 1을 함께 소모한다.
2. **각성 복사본 소모 기준** — 자기 자신 포함 총 보유수인지, 자기 자신 제외 추가 보유분인지 확정 필요했음(3번 단계의 `+1` 가정 검증).
   결정: **자기 자신 제외 추가 보유분 기준.** `PlayerWeapon.Count >= RequiredCount + 1`을 검증하고 `RequiredCount`만 차감한다 — 대상 무기 1개는 각성 후에도 항상 남는다.
3. **합성 재료 무기의 강화 상태 허용 여부** — 강화된 무기를 재료로 써도 되는지 확정 필요했음.
   결정: **강화/각성 상태와 무관하게 허용.** `Count`만 검사하고 `EnhancementLevel`/`AwakeningCount`는 검사하지 않는다.
4. **에러 코드 표현 방식**(§7 공통 사항에서 이어짐) — `ErrorCode` 필드 도입 가능 여부 확인 필요했음.
   결정: **도입하지 않는다.** `Gamism.SDK.Core`/`Gamism.SDK.Extensions.AspNetCore` 0.4.0(현재 참조 버전)·0.5.0(최신 캐시 버전) 모두 `CommonApiResponse`/`ExpectedException` 계열에 `ErrorCode`류 필드가 없음을 리플렉션으로 직접 확인했다. HTTP 상태 코드 + 한글 메시지로만 에러를 구분하는 기존 방식을 유지한다.

## 6. Phase 4 — RewardTransaction 감사 로그

### 6.1 엔티티

```csharp
public class RewardTransaction
{
    public Guid Id { get; private set; }
    public long PlayerId { get; private set; }
    public string SourceType { get; private set; } = string.Empty; // "dungeon_basic","dungeon_gold","dungeon_weapon","dungeon_boss","weapon_upgrade","weapon_synthesize","weapon_awaken"
    public string? SourceRefId { get; private set; }               // gold-run runId 등, 없으면 null
    public string RewardType { get; private set; } = string.Empty; // "gold","mithril","exp","sp","enhancement_scroll","weapon"
    public string? RewardRefId { get; private set; }               // 무기 보상일 때 weaponId(문자열화)
    public long Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

- 테이블: `player.reward_transaction`, append-only(수정/삭제 없음).
- 소모(강화/합성/각성 자원 차감)도 같은 테이블에 음수 `Amount`로 기록해 지급/소모를 한 흐름으로 조회 가능하게 한다.
- 별도 조회 API는 만들지 않는다(내부 감사·CS 대응 목적). 필요 시 추후 관리자 전용 조회 엔드포인트를 별도 논의한다.

### 6.2 기록 시점

각 서비스의 기존 트랜잭션(`IAppDbTransactionRunner.ExecuteAsync`) 내부에서 보상/소모 확정 직후 insert한다. 별도 트랜잭션을 열지 않는다(트랜잭션 경계 추가 금지 — 기존 원칙 준수).

### 6.3 비범위

스킬 해금 SP 소모, 레벨업 SP 지급 등은 이번 범위에서 기록하지 않는다(사용자 결정).

## 7. 공통 사항

- **에러 코드 문자열화 — 확정됨 (2026-07-02)**: 현재 예외는 HTTP status + 한글 메시지만 갖고 있다(`Gamism.SDK`의 `BadRequestException` 등). `Gamism.SDK.Core`/`Gamism.SDK.Extensions.AspNetCore` 0.4.0(현재 참조 버전)·0.5.0(최신 캐시 버전) 모두 `CommonApiResponse`/`ExpectedException` 계열에 `ErrorCode` 필드가 없음을 리플렉션으로 확인했다. `ErrorCode` 필드 도입은 SDK 확장이 필요해 이번 범위에서 보류하고, 1차는 HTTP 상태 코드 + 한글 메시지로만 에러를 구분한다(§5.5 ④).
- **동시성**: `Player`, `PlayerSkill`에는 아직 `xmin`이 없다(권장 사항으로만 유지, 이번 범위에는 포함하지 않음). `PlayerWeapon`은 Phase 3에서 `xmin` 동시성 토큰 추가를 완료했다(§5.4).
- **문서**: 각 Phase 완료 시 `docs/client-integration-guide.md`와 `docs/TODO.md`를 갱신한다(기존 컨벤션).
- **테스트/빌드 검증**: 기존 프로젝트 규칙(`verify.md`) 그대로 — `.cs` 변경 후 `/test` 실행, 실패 시 원인 파악 후 재실행.

## 8. 구현 순서

1. **Phase 1** — Tutorial 도메인 신규
2. **Phase 2** — 던전 타입별 `HighestClearedStage`/`HighScore` (`PlayerDungeonProgress`)
3. **Phase 3** — 무기 강화/합성/각성 API + `PlayerWeapon` 동시성 토큰
4. **Phase 4** — `RewardTransaction` 감사 로그

각 Phase는 독립된 PR로 진행하며, 다음 Phase 착수 전 위에 명시한 "확인 필요 사항"을 재확인한다.

## 9. 검증

- 각 Phase 완료 시 `/test`로 빌드 + 전체 테스트 통과 확인.
- Phase 1: 튜토리얼 완료(`POST /v1/tutorials/{tutorialId}/complete`) → `GET /v1/tutorials` 재조회 시 반영되는지 수동 확인.
- Phase 2: 무기/보스 던전 반복 클리어 시 `HighestClearedStage`가 독립적으로 증가하는지, 골드 던전 최고 run이 `HighScore`에 반영되는지 확인.
- Phase 3: 강화/합성/각성 각각 자원 부족·최대 레벨 도달·미소유 무기 케이스 테스트 — 완료. 동시 요청 `xmin` 충돌의 `ConflictException`(409) 매핑은 기존 `AppDbTransactionRunner` 공통 로직(골드/기본 던전과 동일 경로)이므로 별도 단위 테스트로 재현하지 않는다 — Postgres 전용이라 단위 테스트에서 미재현되는 기존 한계와 동일(`docs/TODO.md` 테스트 체크리스트 참고).
- Phase 4: 위 3개 Phase의 모든 보상/소모 경로에서 `RewardTransaction` 행이 정확히 1개씩 생성되는지(중복 없음) 확인.
