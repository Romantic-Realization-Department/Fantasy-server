# Client Integration Guide

클라이언트(게임 앱)가 Fantasy 서버를 통해 무엇을, 어떤 순서로 호출해야 하는지 정리한 문서입니다.
개별 필드 정의가 아니라 **호출 흐름과 규약**에 초점을 둡니다. 실제 요청/응답 스키마는 서버의 `/swagger`에서 확인할 수 있습니다.

## 1. 기본 규약

| 항목 | 내용 |
|---|---|
| Base 경로 | 모든 엔드포인트는 `/v1` 접두사 사용 |
| 프로토콜 | **HTTP** (배포 환경 포트 제약으로 HTTPS 미사용) |
| 인증 | JWT Bearer — 보호 엔드포인트는 `Authorization: Bearer {accessToken}` 헤더 필요 |
| 본문 형식 | `application/json` |
| API 문서 | `/swagger` (현재 무인증 공개) |
| 헬스 체크 | `GET /v1/health` — 인증 불필요, `{ status, timestamp }` 반환 |

### 응답 래퍼

모든 응답은 공통 래퍼(`CommonApiResponse`)로 감싸집니다.

```json
{
  "status":  "...",        // 성공/실패 구분 문자열 (SDK가 설정)
  "code":    200,           // HTTP 상태 코드
  "message": "로그인 성공.", // 사람이 읽는 메시지
  "data":    { }            // 실제 페이로드 (없으면 null)
}
```

- 컨트롤러가 DTO를 반환하면 `data`에 그대로 담겨 200으로 래핑됩니다.
- 회원가입처럼 생성 성공은 **201**, 본문 없는 성공은 **204**로 내려갈 수 있습니다.
- 클라이언트는 `data`만 사용하면 되고, `message`는 UI 피드백용으로 활용할 수 있습니다.

### 에러

서버는 예외를 던지면 동일한 래퍼 형태로 에러를 내려줍니다. 주요 코드:

| Code | 의미 | 대표 상황 |
|---|---|---|
| 400 | 잘못된 요청 | 유효성 위반, 티켓 부족, 비정상 클릭 수, 미해금 스킬 장착 등 |
| 401 | 인증 실패 | 로그인 실패, 토큰 만료/무효, 리프레시 토큰 재사용 감지 |
| 403 | 권한 없음 | 타인의 골드 던전 런에 접근 |
| 404 | 없음 | 플레이어 미생성(`GET /v1/player`), 스킬/스테이지 데이터 없음 |
| 409 | 충돌 | 이메일 중복, 플레이어 이미 존재(`POST /v1/player`), 광고 보상 중복 수령 |
| 429 | 요청 과다 | 레이트리밋 초과 (`Too Many Requests` 텍스트) |

### 레이트리밋

| 정책 | 한도 | 기준 | 적용 대상 |
|---|---|---|---|
| `login` | 1분에 5회 | 클라이언트 IP | `POST /v1/auth/login` |
| `game` | 1초에 30회 | 계정 ID(JWT `sub`) | `/v1/player/*`, `/v1/dungeons/*`, 게임 데이터 조회 |

429를 받으면 클라이언트는 재시도 간격을 두어야 합니다.

## 2. 전체 흐름

```
[최초]   signup → login → GET player(404) → POST player(직업 선택·생성)
[재접속] login(또는 refresh) → GET player(기존 데이터 로드)
[플레이] basic/state ↔ basic/claim · loadout · skill/unlock · weapon · boss · gold-runs · tutorials
[토큰]   accessToken 만료 → auth/refresh 로 갱신
[종료]   logout
```

핵심 원칙:

- **로그인 직후 `GET /v1/player`로 플레이어를 로드**합니다. **404면 직업 선택 후 `POST /v1/player`로 생성**합니다.
- 상태를 바꾸는 호출(`loadout`, `skill/unlock`, 던전 정산 등)은 응답에 **최신 `player` 전체 스냅샷**을 포함합니다. 클라이언트는 이를 단일 진실 소스로 삼으면 됩니다.
- 변화량은 `changes`(델타)로 함께 내려오므로 획득 연출에 사용합니다.

## 3. 인증 / 계정

| 메서드 | 경로 | 인증 | 설명 |
|---|---|---|---|
| POST | `/v1/account/signup` | ✗ | 회원가입. `email`(≤50, 이메일형식), `password`(8~20). 이메일 중복 시 409 |
| POST | `/v1/auth/login` | ✗ | 로그인. `email`,`password` → 토큰 발급. 레이트리밋 `login` |
| POST | `/v1/auth/refresh` | ✗ | 토큰 갱신. `refreshToken` → 새 토큰 세트 |
| POST | `/v1/auth/logout` | ✓ | 로그아웃. 서버의 리프레시 토큰 폐기 |
| DELETE | `/v1/account` | ✓ | 계정 삭제. 본문 `password` 재확인. 플레이어 데이터·토큰 모두 제거 |

### 토큰 사용 규칙

로그인/갱신 응답의 `data`:

```json
{ "accessToken": "...", "refreshToken": "...", "accessTokenExpiresAt": 1735660800 }
```

- `accessToken`: 보호 API 호출 시 `Authorization: Bearer` 헤더에 사용. 기본 수명 **15분**.
- `accessTokenExpiresAt`: Unix epoch(초). 클라이언트는 이 시각 이전에 갱신을 준비합니다.
- `refreshToken`: 수명 **30일**. **회전식(rotating)** 입니다.
  - `refresh` 호출 시마다 **새 refreshToken이 발급되고 이전 토큰은 무효화**됩니다.
  - 응답으로 받은 새 토큰을 반드시 저장하고 다음 갱신에 사용해야 합니다.
  - 이미 사용한(이전) 토큰을 다시 보내면 **재사용 감지로 401** 처리됩니다 → 재로그인 필요.

## 4. 플레이어

모든 경로 `/v1/player/*` — 인증 필요, 레이트리밋 `game`.

| 메서드 | 경로 | 본문 | 설명 |
|---|---|---|---|
| GET | `/v1/player` | (없음) | 플레이어 로드 (없으면 404) |
| POST | `/v1/player` | `{ jobType }` | 플레이어 생성 (이미 있으면 409) |
| POST | `/v1/player/loadout` | `{ weaponId?, activeSkills[] }` | 장착 무기·액티브 스킬 저장 |
| POST | `/v1/player/skill/unlock` | `{ skillId }` | 스킬 해금 (SP 소모) |

- **GET /v1/player**: 현재 플레이어를 로드(200). 아직 생성 전이면 **404** → 클라이언트는 직업 선택 화면으로 유도합니다.
- **POST /v1/player**: `jobType`으로 신규 생성(201). 이미 플레이어가 있으면 **409**. 재접속 로드에는 GET을 사용하므로 직업을 보낼 필요가 없습니다.
- **loadout**: `weaponId`는 보유 무기여야 하고, `activeSkills`는 **해금된 액티브 스킬**만 허용(패시브·미해금·중복 → 400). 저장과 함께 방치 보상 정산이 함께 일어나 `changes`에 골드/경험치/레벨업이 포함될 수 있습니다.
- **skill/unlock**: 선행 스킬 해금 + SP 충분 + 해당 직업 스킬이어야 함. 이미 해금된 스킬이면 `wasAlreadyUnlocked=true`로 멱등 응답.

## 5. 게임 데이터(레퍼런스)

인증 필요, 레이트리밋 `game`. 정적 테이블이므로 **클라이언트가 1회 조회 후 캐시**하는 것을 권장합니다.

| 메서드 | 경로 | 설명 |
|---|---|---|
| GET | `/v1/jobs/{jobType}/skills` | 직업별 스킬 테이블 |
| GET | `/v1/jobs/{jobType}/weapons` | 직업별 무기 테이블 |
| GET | `/v1/levels` | 레벨별 필요 경험치·보상 SP |
| GET | `/v1/stages` | 스테이지별 몬스터 HP·초당 골드/경험치 |

> 이 엔드포인트들은 enum 값을 **문자열 이름**(`"Warrior"`, `"C"`, `"AtkPercent"`)으로 반환합니다.

## 6. 던전

모든 경로 `/v1/dungeons/*` — 인증 필요, 레이트리밋 `game`.

### 6.1 기본(방치) 던전

| 메서드 | 경로 | 설명 |
|---|---|---|
| GET | `/v1/dungeons/basic/state` | 방치 상태 스냅샷 (읽기 전용) |
| POST | `/v1/dungeons/basic/claim` | 방치 보상 정산 |

- `state`는 `stage`, `lastCalculatedAt`, `serverNow`, `maxOfflineSeconds`(28800초 = **8시간**), `combatPower`(DPS), `goldPerSecond`, `xpPerSecond`를 반환합니다.
- 클라이언트는 `serverNow`와 `lastCalculatedAt`을 기준으로 누적 보상을 **로컬에서 표시**하고, 실제 지급은 `claim`으로 받습니다.
- `claim`은 경과 시간(최대 8시간으로 캡)만큼 골드/경험치를 지급하고, DPS가 현재 스테이지를 클리어할 수 있으면 `maxStage`를 +1 올린 뒤 `lastCalculatedAt`을 현재로 리셋합니다. 앱 포그라운드 진입 시 호출을 권장합니다.

### 6.2 무기 던전 / 보스 던전 (즉시 전투)

| 메서드 | 경로 | 설명 |
|---|---|---|
| POST | `/v1/dungeons/weapon` | 무기 파밍. 클리어(`DPS×30 ≥ 몬스터HP`) 시 확률 드랍 |
| POST | `/v1/dungeons/boss` | 보스 전투. 보스HP = 일반 몬스터HP×5 |

- 무기 던전 드랍 확률(클리어 시): B등급 20%, C등급 70%, 강화 스크롤 30%.
- 보스 클리어 시: 미스릴 +1, 경험치 = `스테이지 초당경험치 × 10`, **A등급 무기 확정 드랍**, 레벨업 가능. 미클리어면 보상 없음.
- **독립 진행도**: 무기/보스 던전은 각자 자기 진행도(`HighestClearedStage`)를 갖습니다. 기본(방치) 던전의 `maxStage`와 **무관하게 스테이지 1부터 독립적으로** 진행하며, 클리어 시 +1 올라갑니다(신규 유저·기존 유저 모두 1부터 시작).
- 응답에 `cleared`, 드랍 정보와 함께 `highestClearedStage`(현재 진행도)가 포함됩니다. 미클리어 시에도 현재 진행도가 그대로 반환됩니다.

### 6.3 골드 던전 (클릭형, 티켓 소모)

| 메서드 | 경로 | 설명 |
|---|---|---|
| GET | `/v1/dungeons/tickets` | 티켓 현황 조회 |
| POST | `/v1/dungeons/gold-tickets/ad-reward` | 광고 보상 티켓 +1 (하루 1회) |
| POST | `/v1/dungeons/gold-runs` | 런 시작 (티켓 1장 소모) |
| POST | `/v1/dungeons/gold-runs/{runId}/claim` | 런 결과 정산 |

티켓 규칙:

- 매일(**KST 기준 날짜**) 첫 접근 시 자동으로 **3장 충전**됩니다(최초 요청 시점에 충전).
- 광고 보상으로 하루 1회 +1 가능. 같은 날 두 번 시도하면 **409**.

런 흐름:

1. `gold-runs` 호출 → 티켓 1장 소모, 런 생성. 응답: `runId`, `startedAt`, `durationSeconds`(30), `expiresAt`, `maxClicks`(15×30 = **450**). 티켓이 없으면 **400**.
2. 30초 동안 클라이언트가 클릭 수를 집계.
3. `gold-runs/{runId}/claim`에 `{ clicks }` 전송. 서버 검증:
   - 런 소유자 불일치 → 403
   - `expiresAt` 초과(시작+30초+여유 30초) → 400
   - `clicks > maxClicks` 또는 **경과 시간 대비 과다 클릭** → 400 (어뷰징 방지)
4. 보상: 클릭당 **10골드**, 2% 확률로 미스릴 1.
5. 이미 정산된 런을 다시 호출하면 **동일 보상으로 멱등 응답**(중복 지급 없음).
6. claim 응답에 `highScore`(골드 던전 역대 최고 획득 골드)가 포함됩니다. 이번 획득 골드가 기존 최고 기록을 넘으면 갱신되고, 넘지 못하면 기존 값이 유지됩니다. 멱등 재호출 시에도 저장된 최고 기록을 반환합니다.

## 7. 튜토리얼

모든 경로 `/v1/tutorials/*` — 인증 필요, 레이트리밋 `game`.

| 메서드 | 경로 | 본문 | 설명 |
|---|---|---|---|
| GET | `/v1/tutorials` | (없음) | 완료한 튜토리얼 ID 목록 조회 |
| POST | `/v1/tutorials/{tutorialId}/complete` | (없음) | 튜토리얼 완료 기록 |

- 튜토리얼은 **순수 기록용**입니다. 완료 여부가 다른 API의 동작을 제한하지 않으며, 재화/성장 상태도 바꾸지 않습니다(응답에 `changes`/`player` 미포함).
- `tutorialId`는 서버 화이트리스트에 있는 값만 허용됩니다. 목록 외 ID는 **400**.

| tutorialId |
|---|
| `tutorial_first_game_start` |
| `tutorial_first_dungeon` |
| `tutorial_first_upgrade` |

- **complete**: 성공 시 `{ tutorialId, wasAlreadyCompleted, completedAt }` 반환. 이미 완료한 튜토리얼을 다시 호출하면 `wasAlreadyCompleted=true`로 **멱등 성공**(중복 기록 없음).
- **GET**: `{ completedTutorialIds: [...] }` 반환. 재접속 시 이 목록으로 튜토리얼 표시 상태를 동기화합니다(`GET /v1/player` 응답에는 포함되지 않음).
- 플레이어 미생성 상태에서 호출하면 **404** → 먼저 `POST /v1/player`로 생성해야 합니다.

## 8. 공통 데이터 구조

### PlayerDataResponse (`player`)

상태 변경 응답들이 공통으로 포함하는 플레이어 전체 스냅샷:

```
jobType, level, maxStage, lastWeaponId?, activeSkills[],
gold, exp, enhancementScroll, mithril, sp,
weapons[ { weaponId, count, enhancementLevel, awakeningCount } ],
skills[  { skillId, isUnlocked } ]
```

### ChangesDto (`changes`)

이번 호출로 발생한 변화량(델타). 연출/토스트용:

```
gold, exp, sp, mithril, enhancementScroll, dungeonTickets,
levelUps[], unlockedSkillIds[], acquiredWeaponIds[], maxStage
```

### Enum

| Enum | 값 (순서값) |
|---|---|
| JobType | `Warrior`(0), `Archer`(1), `Mage`(2) |
| WeaponGrade | `C`(0), `B`(1), `A`(2), `S`(3) |
| SkillEffectType | `AtkFlat`, `AtkPercent`, `HpFlat`, `HpPercent`, `CritRate`, `CritDmg`, `CooldownReduce`, `ElementalBoost` |

> 게임 데이터 조회 엔드포인트(5장)는 enum을 **문자열 이름**으로 반환합니다.
> 플레이어/던전 응답의 enum 직렬화 형태(이름 vs 순서값)는 `/swagger`의 실제 응답으로 확인 후 매핑하세요.

## 9. 시간 처리

- 서버 시각은 모두 **UTC**입니다. 응답의 시각 필드(`serverNow`, `lastCalculatedAt`, `expiresAt` 등)는 UTC 기준입니다.
- 단, 일일 리셋(던전 티켓·광고 보상)은 **KST(UTC+9) 날짜** 기준으로 판정됩니다.
- 클라이언트는 로컬 시계 대신 서버가 내려준 시각(`serverNow`)을 기준으로 동기화하는 것을 권장합니다.
