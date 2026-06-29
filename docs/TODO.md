# TODO

## 서버 권위 전환 정리

### 배경

현재 일부 플레이어 성장 API가 인증만 확인한 뒤 클라이언트가 보낸 결과값을 그대로 DB에 저장한다.
서버 권위 게임 서버로 전환하여 레벨, 재화, 스킬, 무기, 스테이지 같은 핵심 성장값은 서버만 계산하고 변경하도록 한다.

### 기본 원칙

- 클라이언트는 성장 결과값을 직접 저장하지 않는다.
- 클라이언트는 행동 요청만 보낸다.
- 서버가 비용, 조건, 보상, 성장 결과를 계산하고 DB에 저장한다.
- 모든 상태 변경 API 응답에는 변경분과 최신 `PlayerDataResponse`를 포함한다.
- 클라이언트는 응답의 `player` 상태로 로컬 상태를 덮어쓴다.
- 플레이어 상태 조회는 `GET /v1/player`(없으면 404), 초기 생성은 `POST /v1/player`(이미 있으면 409)로 분리한다.
- 모든 성장/보상 API는 계정 ID를 JWT에서만 가져오고, 요청 body/query의 계정 식별자는 받지 않는다.
- 서버 계산에 필요한 시간은 `DateTime.UtcNow`를 직접 호출하지 않고 clock abstraction을 주입해서 테스트 가능하게 만든다.
- 재화 차감과 보상 지급은 음수 방지, overflow 방지, 동시 요청 방지를 같은 트랜잭션 안에서 처리한다.

### 비판적 리뷰

이 문서대로 바로 구현하면 깨질 가능성이 높은 지점이다. 심각도: **[BLOCKER]** 착수 전 필수 / **[HIGH]** 해당 단계 착수 전 필수 / **[MEDIUM]** 배포 전 필수.

- ~~**[BLOCKER]** 계정당 단일 직업 정책과 현재 코드의 `FindByAccountAndJobAsync(accountId, jobType)` 중심 구조가 충돌한다. → *계정과 직업 모델* 섹션 및 구현 순서 1단계에서 해결~~
- ~~**[BLOCKER]** 현재 DB unique index는 `(AccountId, JobType)`라서 같은 계정이 여러 직업을 만들 수 있다. → *마이그레이션 및 시드* 섹션 및 구현 순서 1단계에서 해결~~
- ~~**[HIGH]** 현재 Redis 플레이어 캐시도 `accountId + jobType` 키라서 단일 플레이어 정책과 맞지 않는다. → *계정과 직업 모델* 섹션 및 구현 순서 1단계에서 해결~~
- ~~**[HIGH]** 기존 `PATCH /v1/player/*` 삭제는 controller만 지우면 끝나지 않는다. request DTO, service, interface, tests, DI 등록까지 같이 정리해야 한다. → *삭제 또는 교체할 API* 섹션 및 구현 순서 2단계에서 해결~~
- ~~**[HIGH]** `POST /v1/dungeons/basic/claim`을 기본 던전 정산으로 유지하면서 `GET /v1/dungeons/basic/state`를 추가하면, 클라이언트가 어떤 주기로 state와 claim을 호출해야 하는지 계약이 필요하다. → *기본 던전* 섹션 및 구현 순서 5단계에서 해결~~
- ~~**[HIGH]** `POST /v1/player/loadout`에서 방치 보상 정산과 loadout 저장이 같은 트랜잭션이어야 하는데, unit-of-work 경계 정리 전에는 정산 성공 후 loadout 저장 실패 시 보상이 누락될 수 있다. → 구현 순서 6단계에서 해결 *(누락)*~~
- ~~**[HIGH]** 일일 골드 던전 티켓 지급 책임자가 불명확하다. `last_daily_grant_date`를 갱신하는 주체가 cron인지 lazy grant(다음 API 호출 시 자동 지급)인지 결정하지 않으면 구현이 분산된다. → 구현 순서 7단계에서 결정 *(누락)*~~
- ~~**[HIGH]** 골드 던전 run/claim은 `Random.Shared`를 그대로 쓰면 테스트가 불안정하고 재시도 멱등 응답 재현이 어렵다. → *골드 던전* 섹션 및 구현 순서 7단계에서 해결~~
- ~~**[HIGH]** Redis 캐시 삭제 실패 정책이 없으면 DB는 갱신됐는데 클라이언트는 오래된 `PlayerDataResponse`를 받을 수 있다. → *동시성 및 트랜잭션* 섹션 및 구현 순서 9단계에서 해결~~
- ~~**[MEDIUM]** 공통 응답의 `changes` 예시가 아직 티켓, 스테이지, 장착 변경, run 정보를 담지 못한다. → *응답 계약* 섹션 및 구현 순서 3단계에서 해결~~
- ~~**[MEDIUM]** 모든 상태변경 API가 전체 `PlayerDataResponse`를 반환하면 무기·스킬이 쌓일수록 응답이 과도하게 커진다. payload 크기 상한을 3단계 DTO 설계 시 함께 결정해야 한다. → 구현 순서 3단계에서 결정 *(누락)*~~
- ~~**[MEDIUM]** rate limit이 전역 fixed window면 한 유저의 던전 연타가 다른 유저에게 영향을 줄 수 있다. → *보안 및 남용 방지* 섹션 및 구현 순서 9단계에서 해결~~

추천 구현 순서:

1. ~~계정당 단일 플레이어 모델 전환 — `FindByAccountAsync` 교체, Redis 캐시 키 단순화, `players.account_id` unique constraint 추가, 다른 jobType init 409 처리~~
2. ~~PATCH 클라이언트 쓰기 API 전부 삭제 — `PATCH /v1/player/level|resource|weapon|skill|stage` 5개 및 관련 DTO, Service, Interface, DI 등록, 테스트 함께 정리~~
3. ~~`ChangesDto` 공통 DTO 정의 — `changes` + `player` 구조, API별 타입 응답으로 기존 던전 응답 교체. `PlayerDataResponse` payload 크기 상한도 이 시점에 결정한다.~~
4. ~~DB 마이그레이션 — `account_dungeon_ticket`, `gold_dungeon_run` 테이블 추가~~
5. ~~기본 던전 API 리팩터 — 라우트 `/dungeon` → `/dungeons` 복수화, 응답 `StateChangeResponse`로 교체, `GET /v1/dungeons/basic/state` 추가~~
6. ~~Loadout + 스킬 해금 — `POST /v1/player/loadout` (변경 전 방치 보상 먼저 정산), `POST /v1/player/skill/unlock`. 방치 보상 정산과 loadout 저장을 하나의 트랜잭션으로 묶는다.~~
7. ~~골드 던전 run/claim 구조로 재작성 — `POST /v1/dungeons/gold-runs`, `POST /v1/dungeons/gold-runs/{runId}/claim` (멱등), 티켓 시스템, `IRandomProvider` 추상화. 일일 티켓 지급을 lazy grant로 구현할지 cron으로 구현할지 이 단계에서 결정한다.~~
8. ~~게임 데이터 조회 API — `GET /v1/jobs/{jobType}/skills|weapons`, `GET /v1/levels`, `GET /v1/stages`~~
9. ~~동시성, rate limit, 캐시 실패 정책 — optimistic concurrency (`xmin`), account ID 기준 rate limit partition, Redis 삭제 실패 재시도, unit-of-work 경계 정리~~

### 계정과 직업 모델

계정은 하나의 직업 캐릭터만 키울 수 있다.

- 계정당 `Player`는 1개만 허용한다.
- `jobType`은 `POST /v1/player`(생성) 시에만 받는다. 로드(`GET /v1/player`)에는 `jobType`이 필요 없다.
- 이미 플레이어가 있는 계정이 `POST /v1/player`(생성)를 호출하면 409로 거부한다.
- 재접속 클라이언트는 직업을 몰라도 `GET /v1/player`로 로드(없으면 404)할 수 있어, 잘못된 직업 추측으로 막히지 않는다.
- 성장/보상/던전 API는 `jobType` query/body를 받지 않고, JWT 계정의 플레이어 직업을 DB에서 조회해 사용한다.
- `PlayerDataResponse.JobType`은 유지해서 클라이언트가 현재 계정의 직업을 알 수 있게 한다.
- Redis 플레이어 캐시 키는 `accountId + jobType`이 아니라 `accountId` 기준으로 단순화한다.
- DB에는 `players.account_id` unique constraint를 추가해 계정당 단일 플레이어를 강제한다.
- `IPlayerRepository.FindByAccountAndJobAsync`는 `FindByAccountAsync`로 교체한다.
- 기존 `(AccountId, JobType)` unique index는 `AccountId` unique index로 교체한다.
- 기존 데이터가 여러 직업을 가진 계정을 허용한 적이 있다면 migration 전에 정리 정책이 필요하다.

### 응답 계약

모든 상태 변경 API는 API별 타입 응답을 사용하되, `ChangesDto`와 `PlayerDataResponse`를 공통으로 포함한다.

```csharp
// 공통 변경분 DTO
public record ChangesDto(
    long Gold,
    long Exp,
    int Sp,
    int Mithril,
    int EnhancementScroll,
    int DungeonTickets,
    List<int> LevelUps,
    List<long> UnlockedSkillIds,
    List<long> AcquiredWeapons,
    int MaxStage
);

// API별 응답 예시
public record BasicDungeonClaimResponse(ChangesDto Changes, PlayerDataResponse Player);
public record GoldDungeonClaimResponse(Guid RunId, long EarnedGold, int EarnedMithril, ChangesDto Changes, PlayerDataResponse Player);
public record SkillUnlockResponse(bool WasAlreadyUnlocked, ChangesDto Changes, PlayerDataResponse Player);
```

- `changes`는 UI 연출용 변경분이며, 최종 진실은 항상 `player`다.
- 값 변화가 없는 필드는 `0` 또는 빈 배열로 반환한다.
- 멱등 성공 응답도 최신 `PlayerDataResponse`를 포함한다.
- API별 고유 데이터(runId, wasAlreadyUnlocked 등)는 각 응답 타입에 명시적으로 정의한다.

### 라우트와 버전 정책

현재 컨트롤러는 `v1/dungeon` 단수 라우트를 사용하고 있고, 신규 계획은 `v1/dungeons` 복수 라우트를 사용한다.

- 1차 전환 시 라우트 명명은 복수 리소스 기준으로 통일한다.
- 기존 `POST /v1/dungeon/gold`는 삭제하고 `POST /v1/dungeons/gold-runs` 흐름으로 교체한다.
- 기존 `POST /v1/dungeon/basic/claim`은 삭제하고 `POST /v1/dungeons/basic/claim`로 옮긴다.
- 배포 전 프로젝트이므로 deprecated alias는 두지 않는다.

### 삭제 또는 교체할 API

배포 전이므로 위험한 호환 API는 남기지 않고 삭제 또는 행동 API로 교체한다.

- `PATCH /v1/player/level` 삭제
- `PATCH /v1/player/resource` 삭제
- `PATCH /v1/player/weapon` 삭제
- `PATCH /v1/player/skill` 삭제
- `PATCH /v1/player/stage` 삭제
- `PATCH /v1/player/session/end`의 `gold`, `exp` 저장 기능 제거
- 관련 `UpdatePlayer*Request`, `UpdatePlayer*Service`, interface, controller, unit test도 함께 삭제한다.
- 단, 내부 도메인 메서드나 repository 메서드는 신규 행동 API에서 재사용할 수 있으면 이름을 바꿔 유지한다.

### 1차 신규/유지 API

- `GET /v1/player`: 플레이어 상태 조회 (없으면 404)
- `POST /v1/player`: 초기 생성 (이미 있으면 409)
- `POST /v1/player/loadout`: 장착 무기와 활성 스킬 변경
- `POST /v1/player/skill/unlock`: 스킬 해금
- `POST /v1/dungeons/basic/claim`: 방치 보상 정산
- `POST /v1/dungeons/gold-runs`: 골드 던전 시작
- `POST /v1/dungeons/gold-runs/{runId}/claim`: 골드 던전 정산
- `POST /v1/dungeons/gold-tickets/ad-reward`: 광고 시청 티켓 보상
- `GET /v1/dungeons/tickets`: 던전 티켓 상태 조회

비판적 질문:

- 티켓 조회를 player 하위에 둘지 dungeon 하위에 둘지 결정해야 한다.
- 추천 답: 던전 기능의 티켓이므로 `GET /v1/dungeons/tickets`로 둔다.

### 게임 데이터 조회 API

`game-data` 같은 구현 관점 이름 대신 클라이언트가 보는 리소스 이름을 사용한다.

- `GET /v1/jobs/{jobType}/skills`: 해당 직업 스킬 트리 조회
- `GET /v1/jobs/{jobType}/weapons`: 해당 직업 무기 목록 조회
- `GET /v1/levels`: 레벨 테이블 조회
- `GET /v1/stages`: 스테이지 테이블 조회

직업 선택은 계정 최초 생성 전에만 필요하다.

- 직업 목록은 현재 클라이언트 enum을 사용하므로 `GET /v1/jobs`는 1차 범위에서 제외한다.
- 생성 이후 성장 API는 요청의 `jobType`을 신뢰하지 않고 계정의 단일 플레이어 직업을 기준으로 처리한다.

게임 데이터 조회 응답에는 클라이언트가 계산하지 않아도 되는 표시/검증 정보를 포함한다.

- 스킬: `skillId`, `jobType`, `name`, `description`, `effectType`, `effectValue`, `spCost`, `prereqSkillId`, `isActive`
- 무기: `weaponId`, `jobType`, `grade`, `name`, `attack`, `attackSpeed`, `criticalChance`
- 레벨: `level`, `requiredExp`, `rewardSp`
- 스테이지: `stage`, `monsterHp`, `goldPerSecond`, `xpPerSecond`
- 응답 DTO는 EF entity를 그대로 노출하지 않는다.
- Redis 캐시 역직렬화 문제를 막기 위해 캐시용 DTO 또는 public constructor/setter 정책을 명확히 둔다.

### 5-1 기본 던전

게임의 기본 콘텐츠다.

- 자동 전투 진행
- Gold와 XP 획득
- 진행할수록 난이도 상승
- 특정 웨이브에서 **정예 몬스터 등장**

클라이언트는 기본 던전 전투를 실시간으로 연출하지만, 최종 성장값은 서버 정산 결과를 따른다.

- 클라이언트는 전투 결과로 얻은 `gold`, `exp`, `stage`, `wave`를 직접 저장하지 않는다.
- 서버는 계정의 현재 플레이어, 장착 무기, 활성 스킬, 해금된 passive 스킬, 현재 스테이지 데이터를 기준으로 보상을 계산한다.
- 기본 던전의 서버 API는 실시간 프레임 단위 전투를 검증하지 않고, 정산 시점의 누적 진행만 검증한다.
- 정산 응답에는 변경분, 최신 `PlayerDataResponse`, 기본 던전 표시 상태를 포함한다.
- 추천 답: 1차 서버는 클라이언트 자동 전투를 "표시용 시뮬레이션"으로 보고, 서버는 경과 시간 기반 정산만 권위 있게 처리한다.

기본 던전 진행 모델:

- `StageData`는 기본 난이도와 보상 기준을 가진다.
- 난이도 상승은 stage 증가에 따른 `MonsterHp`, `MonsterAtk`, `GoldPerSecond`, `XpPerSecond` 상승으로 표현한다.
- wave는 클라이언트 연출 단위이며, 서버 저장값은 1차에서 `MaxStage`와 `LastCalculatedAt`만 유지한다.
- 정예 몬스터는 특정 wave마다 등장하는 전투 변형이다.
- 1차 서버 정산에서는 정예 몬스터를 별도 저장하지 않고, stage 보상 테이블에 평균 보상/난이도로 반영한다.
- 정예 몬스터 처치 보상을 별도로 지급하려면 `eliteWaveInterval`, `eliteHpMultiplier`, `eliteRewardMultiplier` 같은 게임 데이터 필드를 추가한다.

기본 던전 API:

- `POST /v1/dungeons/basic/claim`: 기본 던전 누적 보상 정산
- `GET /v1/dungeons/basic/state`: 기본 던전 표시 상태 조회

클라이언트 호출 계약:

- 앱 시작 또는 복귀 시 `GET /v1/player`로 최신 플레이어 상태를 받고, 404면 `POST /v1/player`로 생성한다.
- 기본 던전 화면 진입 시 `GET /v1/dungeons/basic/state`로 전투 표시 기준값을 받는다.
- 앱 백그라운드 복귀, 일정 주기, loadout 변경 직전에는 `POST /v1/dungeons/basic/claim`을 호출한다.
- claim 성공 후 클라이언트는 서버 `player`와 `basicDungeonState`로 로컬 전투 상태를 재동기화한다.
- 추천 답: claim 자동 호출 주기는 30~60초 이상으로 제한하고, 너무 잦은 claim은 rate limit 또는 204/변경 없음으로 처리한다.

`GET /v1/dungeons/basic/state` 응답 후보:

- `stage`
- `lastCalculatedAt`
- `serverNow`
- `maxOfflineSeconds`
- `combatPower`
- `goldPerSecond`
- `xpPerSecond`
- `eliteWaveInterval`

정예 몬스터 결정이 필요한 질문:

- 정예 몬스터가 단순히 더 강한 몬스터인지, 별도 보상을 주는 이벤트인지 결정한다.
- 추천 답: 1차에서는 별도 보상 없이 클라이언트 연출과 평균 보상 테이블로 처리하고, 별도 드랍은 2차로 미룬다.
- 단, 클라이언트가 이미 정예 처치 보상 UI를 보여준다면 1차에서도 서버 보상 계약을 추가해야 한다.

### 방치 보상 정산

- `PlayerStage.LastCalculatedAt` 이후 경과 시간만 정산한다.
- 최대 누적 시간은 8시간으로 제한한다.
- 정산 성공 시 `LastCalculatedAt = now`로 갱신한다.
- 클라이언트가 보낸 `gold`, `exp`, `stage` 값은 받지 않는다.
- 장착 무기와 활성 스킬 변경은 다음 정산부터 반영한다.
- `POST /v1/player/loadout`은 변경 전에 현재 장착 상태 기준으로 미정산 방치 보상을 먼저 정산한다.
- `elapsedSeconds <= 0`이어도 최신 `PlayerDataResponse`를 반환한다.
- 정산 중 레벨업으로 지급되는 SP는 같은 트랜잭션에서 반영한다.
- 현재 구현처럼 한 번에 한 스테이지만 클리어시키는지, 경과 시간 동안 여러 스테이지를 진행시키는지 명확히 결정한다.
- 추천 답: 방치 보상은 1차에서 "현재 최고 스테이지 기준 보상 누적"으로 단순화하고, 자동 스테이지 진행은 별도 기능으로 분리한다.
- 기본 던전 클라이언트 연출의 wave/정예 몬스터 진행은 정산 결과와 충돌하면 서버 응답으로 덮어쓴다.
- `LastCalculatedAt` 갱신 시점은 정산 계산에 사용한 `now`와 동일해야 한다.
- 트랜잭션 재시도 시 같은 elapsed window가 중복 보상되지 않도록 row concurrency를 적용한다.

`POST /v1/player/loadout` 처리 순서:

1. 현재 장착 상태 기준으로 방치 보상 정산
2. 무기 소유 여부 검증
3. 스킬 해금 여부 및 계정 플레이어 직업 일치 검증
4. `LastWeaponId`, `ActiveSkills` 저장
5. 변경분과 최신 `PlayerDataResponse` 반환

### 스킬 트리와 해금

- 스킬 트리는 단일 선행 스킬만 허용한다.
- 분기는 여러 스킬이 같은 `PrereqSkillId`를 바라보는 방식으로 표현한다.
- 다중 선행 조건은 1차 범위에서 제외한다.
- 서버는 계정 플레이어의 `JobType`, `SpCost`, `PrereqSkillId`, 이미 해금 여부를 검증한다.
- 이미 해금된 스킬은 멱등 성공으로 처리한다.
- 응답에는 `wasAlreadyUnlocked`와 최신 `PlayerDataResponse`를 포함한다.
- `ActiveSkills`에 들어갈 수 있는 스킬 개수 상한을 서버에서 검증한다.
- active 스킬과 passive 스킬을 구분해 passive 스킬은 장착 요청에서 거부한다.
- 해금되지 않은 스킬, 다른 직업 스킬, 존재하지 않는 스킬은 모두 명확한 400/404 정책을 둔다.
- `ActiveSkills` 배열의 중복 skill id를 거부한다.
- 현재 장착 스킬 목록에서 삭제된/비활성화된 게임 데이터가 있으면 loadout 저장 전에 정리한다.

`POST /v1/player/skill/unlock` 검증 순서:

1. 플레이어 존재 확인
2. `skillId`가 계정 플레이어 직업의 스킬인지 확인
3. 이미 해금된 스킬이면 성공 응답
4. 선행 스킬이 있으면 해금 여부 확인
5. `resource.Sp >= SkillData.SpCost` 확인
6. SP 차감
7. `PlayerSkill` 저장
8. 캐시 삭제
9. 최신 `PlayerDataResponse` 반환

### 무기 강화

무기 강화 규칙은 아직 명확하지 않으므로 1차 범위에서 보류한다.

- `POST /v1/player/weapon/enhance`는 아직 만들지 않는다.
- 기존 `PATCH /v1/player/weapon`은 삭제한다.
- 무기 획득은 던전과 보스 보상처럼 서버 계산 보상으로만 발생한다.
- `PlayerWeapon.EnhancementLevel`, `AwakeningCount` 필드는 남겨두되 현재는 변경하지 않는다.

### 골드 던전

기획:

- 광산 컨셉 미니게임
- 제한 시간 30~60초
- 화면 클릭으로 광석 채굴
- 클릭 수에 따라 Gold 획득
- 매우 낮은 확률로 Mithril 드랍

서버 권위 구조:

- 골드 던전은 클릭 기반으로 유지한다.
- 단일 요청으로 `clicks`, `durationSeconds`를 받는 구조는 사용하지 않는다.
- 서버가 run 세션을 만들고, 클라이언트는 claim 시 `runId`와 `clicks`만 제출한다.
- 서버는 소유자, 제한 시간, 만료 여부, 중복 claim, 클릭 상한을 검증한다.
- 보상 계산과 Mithril 드랍 판정은 서버가 수행한다.
- 정산 응답에는 변경분과 최신 `PlayerDataResponse`를 포함한다.
- `runId`는 예측 불가능한 UUID/ULID로 만들고 계정 소유권 검증을 필수로 한다.
- claim은 멱등하게 처리한다. 이미 claim된 run은 같은 보상을 재지급하지 않고 기존 결과와 최신 player를 반환하거나 409로 거부한다.
- 추천 답: 네트워크 재시도 UX를 고려해 같은 run의 중복 claim은 보상 재지급 없이 멱등 성공으로 처리한다.
- `maxClicks`는 `durationSeconds * 서버 설정 CPS 상한`으로 저장하고 claim 시 저장된 값을 기준으로 검증한다.
- `clicks < 0`, 비정상적으로 이른 claim, 만료 후 claim, 시작하지 않은 run claim의 에러 정책을 테스트로 고정한다.
- 보상 난수는 `IRandomProvider` 또는 domain RNG abstraction으로 분리해 테스트에서 고정한다.
- claim 성공 시 산출된 `earnedGold`, `earnedMithril`은 run row에 저장해 중복 claim 응답을 재현한다.

`POST /v1/dungeons/gold-runs` 처리:

1. 계정 골드 던전 티켓 1장 차감
2. `runId`, `startedAt`, `durationSeconds`, `expiresAt`, `maxClicks` 생성
3. 진행 중 run 저장
4. run 정보와 최신 `PlayerDataResponse` 반환

`POST /v1/dungeons/gold-runs/{runId}/claim` 처리:

1. run 존재 여부 확인
2. 계정 소유자 일치 확인
3. 이미 claim 되었는지 확인
4. 제한 시간과 만료 여부 확인
5. `clicks <= maxClicks` 확인
6. Gold와 Mithril 보상 계산
7. 보상 지급 및 run claim 처리
8. 최신 `PlayerDataResponse` 반환

### 골드 던전 티켓

- 계정당 직업이 하나이므로 티켓은 계정 단위로 관리한다.
- 기본 티켓은 KST 기준 하루 3장 지급한다.
- 광고 보상은 KST 기준 하루 1회, 티켓 1장을 추가 지급한다.
- 골드 던전 시작 시 티켓 1장을 차감한다.
- DB 저장은 UTC timestamp/date를 사용한다.
- "오늘" 판정은 `Asia/Seoul` 날짜 기준으로 계산한다.
- 서버 재시작과 무관하게 계정별 `last_daily_grant_date`, `daily_ad_reward_claimed_date`로 판정한다.

추천 테이블:

- `account_dungeon_ticket`
  - `id`
  - `account_id`
  - `dungeon_type`
  - `ticket_count`
  - `last_daily_grant_date`
  - `daily_ad_reward_claimed_date`

직업별 티켓 확장은 1차 범위에서 제외한다. 계정당 단일 직업 정책이 바뀌면 별도 migration으로 전환한다.

추가 추천 테이블:

- `gold_dungeon_run`
  - `id`
  - `account_id`
  - `started_at`
  - `duration_seconds`
  - `expires_at`
  - `max_clicks`
  - `claimed_at`
  - `claimed_clicks`
  - `earned_gold`
  - `earned_mithril`

### 동시성 및 트랜잭션

- `PlayerResource`, `PlayerStage`, `PlayerSession`, `AccountDungeonTicket`, `GoldDungeonRun`에 optimistic concurrency token을 추가할지 결정한다.
- 추천 답: 재화/티켓/run에는 `xmin` 또는 `row_version` 기반 optimistic concurrency를 1차에 넣는다.
- 모든 보상 지급, 비용 차감, 티켓 차감, run claim은 트랜잭션 하나로 묶는다.
- repository 내부에서 각각 `SaveChangesAsync`를 호출하는 현재 구조는 복합 갱신에서 중간 저장이 발생하므로 unit-of-work 경계를 재정리한다.
- Redis 캐시 삭제는 DB commit 이후 수행한다. 삭제 실패 시 재시도/로그 정책을 둔다.
- 현재 `AppDbTransactionRunner` 안에서 repository별 `SaveChangesAsync`가 여러 번 호출되므로 "트랜잭션은 하나지만 저장 시점은 여러 번"이다. 신규 구현에서는 aggregate 변경 후 마지막에 한 번 저장하는 경계를 만든다.
- PostgreSQL을 기준으로 하면 `xmin` concurrency token을 우선 검토한다. 다른 DBMS 확장 가능성을 중요하게 보면 명시적 `row_version` 컬럼을 둔다.

### 보안 및 남용 방지

- ~~game API rate limit은 전역 fixed window가 아니라 account ID 기준 partition으로 적용한다.~~ → *`RateLimitConfig`의 `game` 정책 (account `sub` claim 기준)*
- ~~로그인 전 API는 IP 기준 partition을 사용한다.~~ → *`RateLimitConfig`의 `login` 정책 (IP 기준)*
- claim 계열 API는 같은 account에 대해 짧은 시간 중복 호출될 수 있으므로 rate limit과 멱등성 정책을 함께 설계한다.
- ~~gold dungeon 클릭 수 검증은 `clicks <= maxClicks`만으로 충분하지 않다. 너무 이른 claim과 run 시작/종료 시간을 같이 검증한다.~~ → *경과 시간 기반 클릭 상한 검증 추가 (`TimeProvider` 주입)*
- 서버 권위 전환 후에도 클라이언트 조작 가능 값이 요청 DTO에 남아 있는지 route/request audit을 한다.

### 마이그레이션 및 시드

- ~~`players.account_id` unique constraint 추가~~ → *`PlayerSinglePerAccount` 마이그레이션*
- ~~`players`의 기존 `(account_id, job_type)` unique index 제거~~ → *`PlayerSinglePerAccount` 마이그레이션*
- ~~`account_dungeon_ticket`, `gold_dungeon_run` 테이블 migration 추가~~ → *`AddDungeonTicketAndGoldRun` 마이그레이션*
- ~~게임 데이터 조회 API에서 필요한 표시 필드가 부족하면 `SkillData`, `WeaponData`에 이름/설명 필드 추가 여부 결정~~ → *추가하지 않음으로 결정 (클라이언트가 표시 텍스트 보유)*
- ~~seed 경로를 배포 이미지에서 실행 가능한 방식으로 정리~~ → *임베디드 JSON + `GameDataSeeder`로 구성, `docs/game-data-seeding.md` 참고*
- ~~seed 데이터 중 스킬 선행 관계가 순환하지 않는지 검증 테스트 추가~~ → *`SkillSeedDataTests`*
- ~~KST 일일 보상 판정을 위해 저장 컬럼 타입을 `date`로 둘지 UTC timestamp로 둘지 확정~~ → *`DateOnly`(`date` 컬럼) + `Asia/Seoul` 판정으로 구현됨*
- 추천 답: 판정 기준 날짜는 `LocalDate` 의미의 `date` 컬럼으로 저장하고, 계산은 서버에서 `Asia/Seoul` 기준으로 수행한다.

### 테스트 체크리스트

- ~~계정당 플레이어 1개만 생성되는지, 다른 직업 재초기화가 409로 거부되는지 테스트~~
- ~~`IPlayerRepository`가 `jobType` 없이 계정 플레이어를 찾는지 테스트~~
- ~~Redis 플레이어 캐시 키가 `accountId` 기준으로 바뀌었는지 테스트~~
- ~~삭제된 `PATCH /v1/player/*` 라우트가 더 이상 노출되지 않는지 controller/route 테스트~~ → *`PlayerControllerRouteTests` (리플렉션 기반)*
- ~~방치 보상 0초, 음수 시간, 8시간 초과, 레벨업 포함 정산 테스트~~
- ~~기본 던전 state가 서버 시간, 전투력, 초당 보상, 정예 웨이브 기준을 반환하는지 테스트~~
- ~~loadout 변경 전에 미정산 보상이 이전 장착 상태 기준으로 지급되는지 테스트~~
- ~~스킬 해금 성공, SP 부족, 선행 스킬 부족, 중복 해금, 타 직업 스킬 거부 테스트~~
- ~~골드 던전 시작 시 티켓 차감, 티켓 부족, 일일 지급, 광고 보상 1일 1회 테스트~~
- ~~골드 던전 claim 소유자 불일치, 클릭 상한 초과, 만료, 중복 claim 테스트~~
- ~~동시 claim/동시 보상 정산에서 재화가 중복 지급되지 않는지 통합 테스트~~ → *`GoldDungeonClaimServiceTest` / `BasicDungeonClaimServiceTests`의 충돌 시 캐시 미갱신 검증. xmin→ConflictException 변환 자체는 Postgres 전용이라 단위 테스트에서 미재현*
- ~~Redis 캐시 hit/miss와 캐시 DTO 역직렬화 테스트~~

### 기존 Findings 후속 작업

1. ~~클라이언트가 핵심 성장 값을 직접 쓰는 API 제거~~
2. ~~게임 데이터 Redis 캐시의 private setter JSON 역직렬화 문제 수정~~
3. ~~재화/보상 갱신의 read-modify-write 동시성 문제 해결~~
4. ~~회원 삭제 시 플레이어 데이터와 refresh token 정리~~
5. ~~rate limit을 전역이 아니라 IP 또는 account 기준으로 partition~~
6. ~~배포 DB migration 및 seed 경로 추가~~
