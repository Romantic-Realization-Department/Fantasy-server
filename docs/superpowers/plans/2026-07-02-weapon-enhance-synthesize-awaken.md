# 무기 강화/합성/각성 (Phase 3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 무기 강화(확정 성공, 재화 소모)/합성(동일 무기 N개 → 상위 등급 1개)/각성(복사본 + 미스릴 소모) API 3종을 추가하고, `PlayerWeapon`에 `xmin` 동시성 토큰을 도입한다.

**Architecture:** 신규 도메인 `Domain/Weapon/`에 use case별 서비스 3개(`UpgradeWeaponService`/`SynthesizeWeaponService`/`AwakenWeaponService`)와 `WeaponController`(`v1/weapons`)를 둔다. 비용은 마스터 데이터로 관리 — `WeaponData`에 컬럼 4개를 추가하고 신규 마스터 테이블 2개(`weapon_enhancement_cost`, `weapon_awaken_cost`)를 기존 `GameDataSeeder`+`GameDataCacheService` 패턴으로 시드/캐시한다. `PlayerWeapon` 갱신은 기존 `UpsertRangeAsync`(절대값 덮어쓰기, 동시성 미보호)를 쓰지 않고, **추적 조회 + 엔티티 메서드 + `UpdateAsync`** 경로를 신설해 `xmin` 충돌이 `ConflictException`(409)으로 변환되게 한다.

**Tech Stack:** ASP.NET Core Web API (.NET 10), EF Core(Npgsql), Redis(IDistributedCache), xUnit v3 + NSubstitute + FluentAssertions.

## 확정 대기 가정 (착수 전 사용자 확인 필수)

2026-07-02 질문에 응답이 없어 아래 추천안을 가정으로 진행한다. 하나라도 뒤집히면 해당 시드 값/검증 로직만 수정하면 되도록 설계했다.

| # | 항목 | 가정 |
|---|---|---|
| 1 | 강화 비용 통화 | **Gold + Scroll 혼합** — 0~4강은 Gold만, 5~9강은 Gold + 강화 스크롤 1 |
| 2 | 각성 복사본 기준 | **자신 제외 추가분 소모** — `Count >= RequiredCount + 1` 필요, `RequiredCount`만 차감 (각성 후에도 무기 1개는 항상 남음) |
| 3 | 합성 재료 | **강화/각성 레벨 무관 허용** — `Count`만 검사·차감 (레벨은 무기 종류당 1행 속성이라 복사본별 구분 자체가 불가능) |
| 4 | 에러 코드 | **HTTP 상태 + 메시지로만 구분** — Gamism.SDK 0.5.0까지 `ErrorCode` 필드가 없음을 리플렉션으로 확인(2026-07-02). 스펙의 `INSUFFICIENT_CURRENCY` 등 명칭은 문서·테스트 명명용으로만 사용 |

에러 매핑: 미보유 무기 → 404, 최대 레벨 도달/재화 부족/합성 불가/재료 부족 → 400, `xmin` 충돌 → 409(`AppDbTransactionRunner`가 자동 변환).

## Global Constraints

- 엔티티: 모든 setter `private set`, static `Create(...)` 팩토리, 클래스명에 `Entity` 접미사 금지.
- EF: `IEntityTypeConfiguration<T>` Fluent API만 사용. 컬럼명은 EF 기본값(프로퍼티명 그대로). 마스터 테이블 스키마는 `game_data`, 플레이어 테이블은 `player`.
- Controller → Service 인터페이스 → Repository 인터페이스 계층 준수. `AppDbContext`는 리포지토리만 접근.
- DTO는 positional `record`. 예외는 `Gamism.SDK.Extensions.AspNetCore.Exceptions`의 것만 throw(절대 `return NotFound()` 금지).
- 쓰기 흐름: 읽기(추적) → 검증 → 엔티티 변경 → `_transactionRunner.ExecuteAsync` 안에서 저장 → 커밋 후 `_playerRedisRepository.DeleteAsync(accountId)` (flows.md의 "update DB → Redis DEL").
- `xmin` 토큰 엔티티는 **`AsNoTracking()` 없이 추적 조회**해야 한다 (커밋 70781eb/0c09be4 회귀 방지).
- 테스트: NSubstitute 모킹, 한국어 중첩 클래스 + `BuildSut` 패턴, `IAppDbTransactionRunner` 패스스루는 필요한 테스트에서만 `.Returns(callInfo => callInfo.Arg<Func<Task>>()())` 설정. 리포지토리 테스트는 Sqlite in-memory + 파일 내 `TestAppDbContext`(프로덕션 설정 미적용 → `xmin`/`xid` 문제 없음).
- 각 태스크 종료 시 `/test`(전체 또는 필터)로 빌드+테스트 통과 확인 후 커밋.
- 기존 `PlayerWeapon.Update`/`UpsertRangeAsync`는 던전 드랍 경로가 쓰므로 삭제·변경하지 않는다.

---

### Task 1: `WeaponData` 마스터 컬럼 4개 + 시드/조회 응답 확장 + 캐시 키 v2 + 마이그레이션

**Files:**
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Entity/WeaponData.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Entity/Config/WeaponDataConfig.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Seed/GameDataSeeder.cs` (WeaponSeed 확장)
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Seed/Data/weapons.json`
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Service/GameDataCacheService.cs` (캐시 키 v2)
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Dto/Response/WeaponDataResponse.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Service/GameDataQueryService.cs`
- Modify: `Fantasy-server/Fantasy.Test/GameData/Service/GameDataQueryServiceTest.cs`
- Create: `Fantasy-server/Fantasy.Server/Migrations/{timestamp}_AddWeaponMasterColumns.cs` (자동 생성 후 Sql 추가)

**Interfaces:**
- Produces: `WeaponData`에 `MaxEnhancementLevel(long)`, `MaxAwakeningLevel(long)`, `SynthesizeRequiredCount(int?)`, `SynthesizeResultWeaponId(int?)` 프로퍼티. `Create`는 기존 6개 파라미터 뒤에 `long maxEnhancementLevel = 0, long maxAwakeningLevel = 0, int? synthesizeRequiredCount = null, int? synthesizeResultWeaponId = null` **optional 파라미터**로 추가(기존 호출부 13곳 무수정 컴파일 유지).

- [ ] **Step 1: 실패하는 테스트 작성** — `GameDataQueryServiceTest.cs`의 무기 응답 매핑 테스트에 신규 필드 검증 추가. 기존 46~47행의 `WeaponData.Create(...)` 두 건을 아래로 교체하고, 해당 테스트의 어서션에 신규 필드 확인을 추가:

```csharp
WeaponData.Create(1, "Iron Bow", WeaponGrade.C, JobType.Archer, 100, 10,
    maxEnhancementLevel: 10, maxAwakeningLevel: 3, synthesizeRequiredCount: 3, synthesizeResultWeaponId: 2),
WeaponData.Create(2, "Steel Bow", WeaponGrade.B, JobType.Archer, 150, 15,
    maxEnhancementLevel: 10, maxAwakeningLevel: 3)
```

```csharp
// 같은 테스트 메서드의 어서션 블록에 추가
result[0].MaxEnhancementLevel.Should().Be(10);
result[0].SynthesizeRequiredCount.Should().Be(3);
result[0].SynthesizeResultWeaponId.Should().Be(2);
result[1].SynthesizeRequiredCount.Should().BeNull();
```

- [ ] **Step 2: 실패 확인** — `/test GameDataQueryService` 실행. 컴파일 에러(신규 파라미터/프로퍼티 없음)로 FAIL 확인.

- [ ] **Step 3: `WeaponData` 확장**

```csharp
// WeaponData.cs — 프로퍼티 추가 + Create 확장
public long MaxEnhancementLevel { get; private set; }
public long MaxAwakeningLevel { get; private set; }
public int? SynthesizeRequiredCount { get; private set; }
public int? SynthesizeResultWeaponId { get; private set; }

public static WeaponData Create(
    int weaponId,
    string name,
    WeaponGrade grade,
    JobType jobType,
    long baseAtk,
    long atkPerEnhancement,
    long maxEnhancementLevel = 0,
    long maxAwakeningLevel = 0,
    int? synthesizeRequiredCount = null,
    int? synthesizeResultWeaponId = null) => new()
{
    WeaponId = weaponId,
    Name = name,
    Grade = grade,
    JobType = jobType,
    BaseAtk = baseAtk,
    AtkPerEnhancement = atkPerEnhancement,
    MaxEnhancementLevel = maxEnhancementLevel,
    MaxAwakeningLevel = maxAwakeningLevel,
    SynthesizeRequiredCount = synthesizeRequiredCount,
    SynthesizeResultWeaponId = synthesizeResultWeaponId
};
```

- [ ] **Step 4: `WeaponDataConfig`에 매핑 추가** (기존 `AtkPerEnhancement` 매핑 아래)

```csharp
builder.Property(w => w.MaxEnhancementLevel).IsRequired();
builder.Property(w => w.MaxAwakeningLevel).IsRequired();
builder.Property(w => w.SynthesizeRequiredCount).IsRequired(false);
builder.Property(w => w.SynthesizeResultWeaponId).IsRequired(false);
```

- [ ] **Step 5: `WeaponSeed` 레코드·시더 매핑·시드 JSON 갱신**

```csharp
// GameDataSeeder.cs — WeaponSeed 교체
public record WeaponSeed(
    int WeaponId, string Name, WeaponGrade Grade, JobType JobType,
    long BaseAtk, long AtkPerEnhancement,
    long MaxEnhancementLevel, long MaxAwakeningLevel,
    int? SynthesizeRequiredCount, int? SynthesizeResultWeaponId);

// SeedAsync 내 weapon_data Select 교체
.Select(s => WeaponData.Create(s.WeaponId, s.Name, s.Grade, s.JobType, s.BaseAtk, s.AtkPerEnhancement,
    s.MaxEnhancementLevel, s.MaxAwakeningLevel, s.SynthesizeRequiredCount, s.SynthesizeResultWeaponId))
```

```json
// weapons.json 전체 교체 — C등급은 동일 직업 B등급으로 합성(3개), B등급은 합성 불가(A등급 마스터 부재)
[
  { "weaponId": 1001, "name": "Rusty Sword",     "grade": "C", "jobType": "Warrior", "baseAtk": 30,  "atkPerEnhancement": 5,  "maxEnhancementLevel": 10, "maxAwakeningLevel": 3, "synthesizeRequiredCount": 3,    "synthesizeResultWeaponId": 1002 },
  { "weaponId": 1002, "name": "Knight Blade",    "grade": "B", "jobType": "Warrior", "baseAtk": 70,  "atkPerEnhancement": 10, "maxEnhancementLevel": 10, "maxAwakeningLevel": 3, "synthesizeRequiredCount": null, "synthesizeResultWeaponId": null },
  { "weaponId": 2001, "name": "Short Bow",       "grade": "C", "jobType": "Archer",  "baseAtk": 35,  "atkPerEnhancement": 6,  "maxEnhancementLevel": 10, "maxAwakeningLevel": 3, "synthesizeRequiredCount": 3,    "synthesizeResultWeaponId": 2002 },
  { "weaponId": 2002, "name": "Hunter Longbow",  "grade": "B", "jobType": "Archer",  "baseAtk": 78,  "atkPerEnhancement": 11, "maxEnhancementLevel": 10, "maxAwakeningLevel": 3, "synthesizeRequiredCount": null, "synthesizeResultWeaponId": null },
  { "weaponId": 3001, "name": "Apprentice Wand", "grade": "C", "jobType": "Mage",    "baseAtk": 40,  "atkPerEnhancement": 7,  "maxEnhancementLevel": 10, "maxAwakeningLevel": 3, "synthesizeRequiredCount": 3,    "synthesizeResultWeaponId": 3002 },
  { "weaponId": 3002, "name": "Arcane Staff",    "grade": "B", "jobType": "Mage",    "baseAtk": 85,  "atkPerEnhancement": 12, "maxEnhancementLevel": 10, "maxAwakeningLevel": 3, "synthesizeRequiredCount": null, "synthesizeResultWeaponId": null }
]
```

- [ ] **Step 6: `weapon_data` 캐시 키 버전 업** — 구 캐시 JSON에는 신규 필드가 없어 역직렬화 시 0/null로 채워지므로 키를 갈아 무효화한다.

```csharp
// GameDataCacheService.cs
private const string WeaponDataKey = "game_data:weapon_data:v2";
```

- [ ] **Step 7: 조회 응답 확장**

```csharp
// WeaponDataResponse.cs 교체
public record WeaponDataResponse(
    int WeaponId,
    string Name,
    string Grade,
    string JobType,
    long BaseAtk,
    long AtkPerEnhancement,
    long MaxEnhancementLevel,
    long MaxAwakeningLevel,
    int? SynthesizeRequiredCount,
    int? SynthesizeResultWeaponId
);
```

```csharp
// GameDataQueryService.GetWeaponsByJobAsync의 Select 교체
return weapons.Select(w => new WeaponDataResponse(
    w.WeaponId,
    w.Name,
    w.Grade.ToString(),
    w.JobType.ToString(),
    w.BaseAtk,
    w.AtkPerEnhancement,
    w.MaxEnhancementLevel,
    w.MaxAwakeningLevel,
    w.SynthesizeRequiredCount,
    w.SynthesizeResultWeaponId
)).ToList();
```

- [ ] **Step 8: 마이그레이션 생성 + 기존 행 백필 SQL** — `/db-migrate add AddWeaponMasterColumns` 실행 후, 생성된 마이그레이션의 `Up()` 끝에 기존 배포 DB 행 갱신 SQL을 추가한다(시더는 행 수가 맞으면 건너뛰므로 UPDATE가 필요; 신규 DB에서는 빈 테이블에 no-op 후 시더가 값 포함 삽입).

```csharp
migrationBuilder.Sql("""
    UPDATE game_data.weapon_data SET "MaxEnhancementLevel" = 10, "MaxAwakeningLevel" = 3;
    UPDATE game_data.weapon_data SET "SynthesizeRequiredCount" = 3, "SynthesizeResultWeaponId" = 1002 WHERE "WeaponId" = 1001;
    UPDATE game_data.weapon_data SET "SynthesizeRequiredCount" = 3, "SynthesizeResultWeaponId" = 2002 WHERE "WeaponId" = 2001;
    UPDATE game_data.weapon_data SET "SynthesizeRequiredCount" = 3, "SynthesizeResultWeaponId" = 3002 WHERE "WeaponId" = 3001;
    """);
```

- [ ] **Step 9: 통과 확인** — `/test` 전체 실행(다른 `WeaponData.Create` 호출부는 optional 파라미터라 무수정 통과해야 함). PASS 확인.

- [ ] **Step 10: 커밋** — `git add` 후 `feat: WeaponData에 강화/각성/합성 마스터 컬럼 추가`

---

### Task 2: 비용 마스터 테이블 2개 (`weapon_enhancement_cost`, `weapon_awaken_cost`) + 시드 + 캐시

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/GameData/Entity/WeaponEnhancementCost.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/GameData/Entity/WeaponAwakenCost.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/GameData/Entity/Config/WeaponEnhancementCostConfig.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/GameData/Entity/Config/WeaponAwakenCostConfig.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/GameData/Seed/Data/weaponEnhancementCosts.json`
- Create: `Fantasy-server/Fantasy.Server/Domain/GameData/Seed/Data/weaponAwakenCosts.json` (csproj의 `Seed\Data\*.json` 와일드카드에 자동 포함)
- Modify: `Fantasy-server/Fantasy.Server/Global/Infrastructure/AppDbContext.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Seed/GameDataSeeder.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Repository/Interface/IGameDataRepository.cs` + `Repository/GameDataRepository.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/GameData/Service/Interface/IGameDataCacheService.cs` + `Service/GameDataCacheService.cs`
- Test: `Fantasy-server/Fantasy.Test/GameData/Service/GameDataCacheServiceTest.cs` (케이스 추가)
- Create: `Fantasy-server/Fantasy.Server/Migrations/{timestamp}_AddWeaponCostTables.cs` (자동 생성)

**Interfaces:**
- Produces: `WeaponEnhancementCost.Create(int weaponId, long enhancementLevel, long requiredGold, long requiredScroll)`, `WeaponAwakenCost.Create(int weaponId, long awakeningLevel, int requiredCount, int requiredMithril)`. `IGameDataCacheService.GetWeaponEnhancementCostAsync(int weaponId, long enhancementLevel) : Task<WeaponEnhancementCost?>`, `IGameDataCacheService.GetWeaponAwakenCostAsync(int weaponId, long awakeningLevel) : Task<WeaponAwakenCost?>`. 레벨 파라미터 의미: **현재 레벨 → 다음 레벨로 가는 비용**.

- [ ] **Step 1: 실패하는 테스트 작성** — `GameDataCacheServiceTest.cs`에 기존 무기 캐시 테스트와 같은 패턴(모킹된 `IGameDataRepository`+`IDistributedCache`)으로 추가:

```csharp
[Fact]
public async Task GetWeaponEnhancementCostAsync_캐시_미스면_리포지토리에서_읽고_해당_레벨_비용을_반환한다()
{
    _repository.GetAllWeaponEnhancementCostsAsync().Returns([
        WeaponEnhancementCost.Create(1001, 0, 100, 0),
        WeaponEnhancementCost.Create(1001, 5, 600, 1)
    ]);

    var result = await _sut.GetWeaponEnhancementCostAsync(1001, 5);

    result.Should().NotBeNull();
    result!.RequiredGold.Should().Be(600);
    result.RequiredScroll.Should().Be(1);
}

[Fact]
public async Task GetWeaponAwakenCostAsync_없는_레벨이면_null을_반환한다()
{
    _repository.GetAllWeaponAwakenCostsAsync().Returns([
        WeaponAwakenCost.Create(1001, 0, 1, 5)
    ]);

    var result = await _sut.GetWeaponAwakenCostAsync(1001, 3);

    result.Should().BeNull();
}
```

- [ ] **Step 2: 실패 확인** — `/test GameDataCacheService`. 컴파일 에러로 FAIL.

- [ ] **Step 3: 엔티티 2개 작성**

```csharp
// Entity/WeaponEnhancementCost.cs
namespace Fantasy.Server.Domain.GameData.Entity;

public class WeaponEnhancementCost
{
    public int WeaponId { get; private set; }
    public long EnhancementLevel { get; private set; } // 이 레벨 → 다음 레벨 비용
    public long RequiredGold { get; private set; }
    public long RequiredScroll { get; private set; }

    public static WeaponEnhancementCost Create(int weaponId, long enhancementLevel, long requiredGold, long requiredScroll) => new()
    {
        WeaponId = weaponId,
        EnhancementLevel = enhancementLevel,
        RequiredGold = requiredGold,
        RequiredScroll = requiredScroll
    };
}
```

```csharp
// Entity/WeaponAwakenCost.cs
namespace Fantasy.Server.Domain.GameData.Entity;

public class WeaponAwakenCost
{
    public int WeaponId { get; private set; }
    public long AwakeningLevel { get; private set; } // 이 레벨 → 다음 레벨 비용
    public int RequiredCount { get; private set; }   // 자신 제외 소모 복사본 수
    public int RequiredMithril { get; private set; }

    public static WeaponAwakenCost Create(int weaponId, long awakeningLevel, int requiredCount, int requiredMithril) => new()
    {
        WeaponId = weaponId,
        AwakeningLevel = awakeningLevel,
        RequiredCount = requiredCount,
        RequiredMithril = requiredMithril
    };
}
```

- [ ] **Step 4: EF 설정 2개 작성** (복합 PK)

```csharp
// Entity/Config/WeaponEnhancementCostConfig.cs
using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class WeaponEnhancementCostConfig : IEntityTypeConfiguration<WeaponEnhancementCost>
{
    public void Configure(EntityTypeBuilder<WeaponEnhancementCost> builder)
    {
        builder.ToTable("weapon_enhancement_cost", "game_data");
        builder.HasKey(c => new { c.WeaponId, c.EnhancementLevel });
        builder.Property(c => c.WeaponId).ValueGeneratedNever();
        builder.Property(c => c.EnhancementLevel).ValueGeneratedNever();
        builder.Property(c => c.RequiredGold).IsRequired();
        builder.Property(c => c.RequiredScroll).IsRequired();
    }
}
```

```csharp
// Entity/Config/WeaponAwakenCostConfig.cs
using Fantasy.Server.Domain.GameData.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fantasy.Server.Domain.GameData.Entity.Config;

public class WeaponAwakenCostConfig : IEntityTypeConfiguration<WeaponAwakenCost>
{
    public void Configure(EntityTypeBuilder<WeaponAwakenCost> builder)
    {
        builder.ToTable("weapon_awaken_cost", "game_data");
        builder.HasKey(c => new { c.WeaponId, c.AwakeningLevel });
        builder.Property(c => c.WeaponId).ValueGeneratedNever();
        builder.Property(c => c.AwakeningLevel).ValueGeneratedNever();
        builder.Property(c => c.RequiredCount).IsRequired();
        builder.Property(c => c.RequiredMithril).IsRequired();
    }
}
```

- [ ] **Step 5: `AppDbContext`에 DbSet 추가** (`JobBaseStats` 아래)

```csharp
public DbSet<WeaponEnhancementCost> WeaponEnhancementCosts => Set<WeaponEnhancementCost>();
public DbSet<WeaponAwakenCost> WeaponAwakenCosts => Set<WeaponAwakenCost>();
```

- [ ] **Step 6: 시드 JSON 2개 작성** — 강화: C등급 골드 `(L+1)×100`/B등급 `(L+1)×200`, 5강부터 스크롤 1(가정 #1). 각성: 복사본 1/2/3개 + 미스릴 5/10/15.

```json
// Seed/Data/weaponEnhancementCosts.json
[
  { "weaponId": 1001, "enhancementLevel": 0, "requiredGold": 100,  "requiredScroll": 0 },
  { "weaponId": 1001, "enhancementLevel": 1, "requiredGold": 200,  "requiredScroll": 0 },
  { "weaponId": 1001, "enhancementLevel": 2, "requiredGold": 300,  "requiredScroll": 0 },
  { "weaponId": 1001, "enhancementLevel": 3, "requiredGold": 400,  "requiredScroll": 0 },
  { "weaponId": 1001, "enhancementLevel": 4, "requiredGold": 500,  "requiredScroll": 0 },
  { "weaponId": 1001, "enhancementLevel": 5, "requiredGold": 600,  "requiredScroll": 1 },
  { "weaponId": 1001, "enhancementLevel": 6, "requiredGold": 700,  "requiredScroll": 1 },
  { "weaponId": 1001, "enhancementLevel": 7, "requiredGold": 800,  "requiredScroll": 1 },
  { "weaponId": 1001, "enhancementLevel": 8, "requiredGold": 900,  "requiredScroll": 1 },
  { "weaponId": 1001, "enhancementLevel": 9, "requiredGold": 1000, "requiredScroll": 1 },
  { "weaponId": 1002, "enhancementLevel": 0, "requiredGold": 200,  "requiredScroll": 0 },
  { "weaponId": 1002, "enhancementLevel": 1, "requiredGold": 400,  "requiredScroll": 0 },
  { "weaponId": 1002, "enhancementLevel": 2, "requiredGold": 600,  "requiredScroll": 0 },
  { "weaponId": 1002, "enhancementLevel": 3, "requiredGold": 800,  "requiredScroll": 0 },
  { "weaponId": 1002, "enhancementLevel": 4, "requiredGold": 1000, "requiredScroll": 0 },
  { "weaponId": 1002, "enhancementLevel": 5, "requiredGold": 1200, "requiredScroll": 1 },
  { "weaponId": 1002, "enhancementLevel": 6, "requiredGold": 1400, "requiredScroll": 1 },
  { "weaponId": 1002, "enhancementLevel": 7, "requiredGold": 1600, "requiredScroll": 1 },
  { "weaponId": 1002, "enhancementLevel": 8, "requiredGold": 1800, "requiredScroll": 1 },
  { "weaponId": 1002, "enhancementLevel": 9, "requiredGold": 2000, "requiredScroll": 1 },
  { "weaponId": 2001, "enhancementLevel": 0, "requiredGold": 100,  "requiredScroll": 0 },
  { "weaponId": 2001, "enhancementLevel": 1, "requiredGold": 200,  "requiredScroll": 0 },
  { "weaponId": 2001, "enhancementLevel": 2, "requiredGold": 300,  "requiredScroll": 0 },
  { "weaponId": 2001, "enhancementLevel": 3, "requiredGold": 400,  "requiredScroll": 0 },
  { "weaponId": 2001, "enhancementLevel": 4, "requiredGold": 500,  "requiredScroll": 0 },
  { "weaponId": 2001, "enhancementLevel": 5, "requiredGold": 600,  "requiredScroll": 1 },
  { "weaponId": 2001, "enhancementLevel": 6, "requiredGold": 700,  "requiredScroll": 1 },
  { "weaponId": 2001, "enhancementLevel": 7, "requiredGold": 800,  "requiredScroll": 1 },
  { "weaponId": 2001, "enhancementLevel": 8, "requiredGold": 900,  "requiredScroll": 1 },
  { "weaponId": 2001, "enhancementLevel": 9, "requiredGold": 1000, "requiredScroll": 1 },
  { "weaponId": 2002, "enhancementLevel": 0, "requiredGold": 200,  "requiredScroll": 0 },
  { "weaponId": 2002, "enhancementLevel": 1, "requiredGold": 400,  "requiredScroll": 0 },
  { "weaponId": 2002, "enhancementLevel": 2, "requiredGold": 600,  "requiredScroll": 0 },
  { "weaponId": 2002, "enhancementLevel": 3, "requiredGold": 800,  "requiredScroll": 0 },
  { "weaponId": 2002, "enhancementLevel": 4, "requiredGold": 1000, "requiredScroll": 0 },
  { "weaponId": 2002, "enhancementLevel": 5, "requiredGold": 1200, "requiredScroll": 1 },
  { "weaponId": 2002, "enhancementLevel": 6, "requiredGold": 1400, "requiredScroll": 1 },
  { "weaponId": 2002, "enhancementLevel": 7, "requiredGold": 1600, "requiredScroll": 1 },
  { "weaponId": 2002, "enhancementLevel": 8, "requiredGold": 1800, "requiredScroll": 1 },
  { "weaponId": 2002, "enhancementLevel": 9, "requiredGold": 2000, "requiredScroll": 1 },
  { "weaponId": 3001, "enhancementLevel": 0, "requiredGold": 100,  "requiredScroll": 0 },
  { "weaponId": 3001, "enhancementLevel": 1, "requiredGold": 200,  "requiredScroll": 0 },
  { "weaponId": 3001, "enhancementLevel": 2, "requiredGold": 300,  "requiredScroll": 0 },
  { "weaponId": 3001, "enhancementLevel": 3, "requiredGold": 400,  "requiredScroll": 0 },
  { "weaponId": 3001, "enhancementLevel": 4, "requiredGold": 500,  "requiredScroll": 0 },
  { "weaponId": 3001, "enhancementLevel": 5, "requiredGold": 600,  "requiredScroll": 1 },
  { "weaponId": 3001, "enhancementLevel": 6, "requiredGold": 700,  "requiredScroll": 1 },
  { "weaponId": 3001, "enhancementLevel": 7, "requiredGold": 800,  "requiredScroll": 1 },
  { "weaponId": 3001, "enhancementLevel": 8, "requiredGold": 900,  "requiredScroll": 1 },
  { "weaponId": 3001, "enhancementLevel": 9, "requiredGold": 1000, "requiredScroll": 1 },
  { "weaponId": 3002, "enhancementLevel": 0, "requiredGold": 200,  "requiredScroll": 0 },
  { "weaponId": 3002, "enhancementLevel": 1, "requiredGold": 400,  "requiredScroll": 0 },
  { "weaponId": 3002, "enhancementLevel": 2, "requiredGold": 600,  "requiredScroll": 0 },
  { "weaponId": 3002, "enhancementLevel": 3, "requiredGold": 800,  "requiredScroll": 0 },
  { "weaponId": 3002, "enhancementLevel": 4, "requiredGold": 1000, "requiredScroll": 0 },
  { "weaponId": 3002, "enhancementLevel": 5, "requiredGold": 1200, "requiredScroll": 1 },
  { "weaponId": 3002, "enhancementLevel": 6, "requiredGold": 1400, "requiredScroll": 1 },
  { "weaponId": 3002, "enhancementLevel": 7, "requiredGold": 1600, "requiredScroll": 1 },
  { "weaponId": 3002, "enhancementLevel": 8, "requiredGold": 1800, "requiredScroll": 1 },
  { "weaponId": 3002, "enhancementLevel": 9, "requiredGold": 2000, "requiredScroll": 1 }
]
```

```json
// Seed/Data/weaponAwakenCosts.json
[
  { "weaponId": 1001, "awakeningLevel": 0, "requiredCount": 1, "requiredMithril": 5 },
  { "weaponId": 1001, "awakeningLevel": 1, "requiredCount": 2, "requiredMithril": 10 },
  { "weaponId": 1001, "awakeningLevel": 2, "requiredCount": 3, "requiredMithril": 15 },
  { "weaponId": 1002, "awakeningLevel": 0, "requiredCount": 1, "requiredMithril": 5 },
  { "weaponId": 1002, "awakeningLevel": 1, "requiredCount": 2, "requiredMithril": 10 },
  { "weaponId": 1002, "awakeningLevel": 2, "requiredCount": 3, "requiredMithril": 15 },
  { "weaponId": 2001, "awakeningLevel": 0, "requiredCount": 1, "requiredMithril": 5 },
  { "weaponId": 2001, "awakeningLevel": 1, "requiredCount": 2, "requiredMithril": 10 },
  { "weaponId": 2001, "awakeningLevel": 2, "requiredCount": 3, "requiredMithril": 15 },
  { "weaponId": 2002, "awakeningLevel": 0, "requiredCount": 1, "requiredMithril": 5 },
  { "weaponId": 2002, "awakeningLevel": 1, "requiredCount": 2, "requiredMithril": 10 },
  { "weaponId": 2002, "awakeningLevel": 2, "requiredCount": 3, "requiredMithril": 15 },
  { "weaponId": 3001, "awakeningLevel": 0, "requiredCount": 1, "requiredMithril": 5 },
  { "weaponId": 3001, "awakeningLevel": 1, "requiredCount": 2, "requiredMithril": 10 },
  { "weaponId": 3001, "awakeningLevel": 2, "requiredCount": 3, "requiredMithril": 15 },
  { "weaponId": 3002, "awakeningLevel": 0, "requiredCount": 1, "requiredMithril": 5 },
  { "weaponId": 3002, "awakeningLevel": 1, "requiredCount": 2, "requiredMithril": 10 },
  { "weaponId": 3002, "awakeningLevel": 2, "requiredCount": 3, "requiredMithril": 15 }
]
```

- [ ] **Step 7: 시더 확장** — `GameDataSeeder.cs`에 시드 레코드/로더/`AllSeeds` 필드/`SeedAsync` 블록 추가:

```csharp
public record WeaponEnhancementCostSeed(int WeaponId, long EnhancementLevel, long RequiredGold, long RequiredScroll);
public record WeaponAwakenCostSeed(int WeaponId, long AwakeningLevel, int RequiredCount, int RequiredMithril);

public static IReadOnlyList<WeaponEnhancementCostSeed> LoadWeaponEnhancementCostSeeds() => Load<WeaponEnhancementCostSeed>("weaponEnhancementCosts.json");
public static IReadOnlyList<WeaponAwakenCostSeed> LoadWeaponAwakenCostSeeds() => Load<WeaponAwakenCostSeed>("weaponAwakenCosts.json");

// AllSeeds 레코드에 두 필드 추가 + LoadAllSeeds()에 두 로더 추가
public record AllSeeds(
    IReadOnlyList<JobBaseStatSeed> JobBaseStats,
    IReadOnlyList<LevelSeed> Levels,
    IReadOnlyList<StageSeed> Stages,
    IReadOnlyList<SkillSeed> Skills,
    IReadOnlyList<WeaponSeed> Weapons,
    IReadOnlyList<WeaponEnhancementCostSeed> WeaponEnhancementCosts,
    IReadOnlyList<WeaponAwakenCostSeed> WeaponAwakenCosts);

// SeedAsync 내 weapon_data 블록 아래에 추가
SeedTable(db.WeaponEnhancementCosts, seeds.WeaponEnhancementCosts
    .Select(s => WeaponEnhancementCost.Create(s.WeaponId, s.EnhancementLevel, s.RequiredGold, s.RequiredScroll))
    .ToList(), await db.WeaponEnhancementCosts.CountAsync(), "weapon_enhancement_cost", logger);

SeedTable(db.WeaponAwakenCosts, seeds.WeaponAwakenCosts
    .Select(s => WeaponAwakenCost.Create(s.WeaponId, s.AwakeningLevel, s.RequiredCount, s.RequiredMithril))
    .ToList(), await db.WeaponAwakenCosts.CountAsync(), "weapon_awaken_cost", logger);
```

- [ ] **Step 8: 리포지토리·캐시 확장**

```csharp
// IGameDataRepository.cs에 추가
Task<List<WeaponEnhancementCost>> GetAllWeaponEnhancementCostsAsync();
Task<List<WeaponAwakenCost>> GetAllWeaponAwakenCostsAsync();

// GameDataRepository.cs에 추가 (기존 GetAll 패턴과 동일 — AsNoTracking)
public async Task<List<WeaponEnhancementCost>> GetAllWeaponEnhancementCostsAsync()
    => await _db.WeaponEnhancementCosts.AsNoTracking().ToListAsync();

public async Task<List<WeaponAwakenCost>> GetAllWeaponAwakenCostsAsync()
    => await _db.WeaponAwakenCosts.AsNoTracking().ToListAsync();
```

```csharp
// IGameDataCacheService.cs에 추가
Task<WeaponEnhancementCost?> GetWeaponEnhancementCostAsync(int weaponId, long enhancementLevel);
Task<WeaponAwakenCost?> GetWeaponAwakenCostAsync(int weaponId, long awakeningLevel);

// GameDataCacheService.cs — 캐시 키 상수 + 공개 메서드 + private 로더 (기존 GetAllWeaponDatasAsync 패턴 복제)
private const string WeaponEnhancementCostKey = "game_data:weapon_enhancement_cost";
private const string WeaponAwakenCostKey = "game_data:weapon_awaken_cost";

public async Task<WeaponEnhancementCost?> GetWeaponEnhancementCostAsync(int weaponId, long enhancementLevel)
{
    var all = await GetAllWeaponEnhancementCostsAsync();
    return all.FirstOrDefault(c => c.WeaponId == weaponId && c.EnhancementLevel == enhancementLevel);
}

public async Task<WeaponAwakenCost?> GetWeaponAwakenCostAsync(int weaponId, long awakeningLevel)
{
    var all = await GetAllWeaponAwakenCostsAsync();
    return all.FirstOrDefault(c => c.WeaponId == weaponId && c.AwakeningLevel == awakeningLevel);
}

private async Task<List<WeaponEnhancementCost>> GetAllWeaponEnhancementCostsAsync()
{
    var json = await _cache.GetStringAsync(WeaponEnhancementCostKey);
    if (json is not null)
        return JsonSerializer.Deserialize<List<WeaponEnhancementCost>>(json)!;

    var data = await _repository.GetAllWeaponEnhancementCostsAsync();
    await _cache.SetStringAsync(WeaponEnhancementCostKey, JsonSerializer.Serialize(data),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
    return data;
}

private async Task<List<WeaponAwakenCost>> GetAllWeaponAwakenCostsAsync()
{
    var json = await _cache.GetStringAsync(WeaponAwakenCostKey);
    if (json is not null)
        return JsonSerializer.Deserialize<List<WeaponAwakenCost>>(json)!;

    var data = await _repository.GetAllWeaponAwakenCostsAsync();
    await _cache.SetStringAsync(WeaponAwakenCostKey, JsonSerializer.Serialize(data),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
    return data;
}
```

- [ ] **Step 9: 마이그레이션 생성** — `/db-migrate add AddWeaponCostTables`. 생성물에 두 테이블 CreateTable만 있는지 확인.

- [ ] **Step 10: 통과 확인** — `/test GameDataCacheService` PASS, 이어서 `/test` 전체 PASS.

- [ ] **Step 11: 커밋** — `feat: 무기 강화/각성 비용 마스터 테이블 및 시드 추가`

---

### Task 3: `PlayerWeapon` — 도메인 메서드 + `xmin` + 단건 리포지토리 경로

**Files:**
- Modify: `Fantasy-server/Fantasy.Server/Domain/Player/Entity/PlayerWeapon.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Player/Entity/Config/PlayerWeaponConfig.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Player/Repository/Interface/IPlayerWeaponRepository.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Player/Repository/PlayerWeaponRepository.cs`
- Test: `Fantasy-server/Fantasy.Test/Player/Repository/PlayerWeaponRepositoryTests.cs` (케이스 추가)
- Create: `Fantasy-server/Fantasy.Server/Migrations/{timestamp}_AddPlayerWeaponConcurrencyToken.cs` (자동 생성)

**Interfaces:**
- Produces: `PlayerWeapon.Enhance()`, `AddCount(long amount)`, `ConsumeCount(long amount)`, `Awaken()` 인스턴스 메서드. `IPlayerWeaponRepository.FindByPlayerIdAndWeaponIdAsync(long playerId, int weaponId) : Task<PlayerWeapon?>`(**추적 조회** — `AsNoTracking` 금지), `SaveAsync(PlayerWeapon weapon) : Task`, `UpdateAsync(PlayerWeapon weapon) : Task`. 기존 `FindAllByPlayerIdAsync`/`UpsertRangeAsync`는 변경 없음.

- [ ] **Step 1: 실패하는 테스트 작성** — `PlayerWeaponRepositoryTests.cs`에 추가:

```csharp
[Fact]
public async Task FindByPlayerIdAndWeaponIdAsync_없으면_null을_반환한다()
{
    var result = await _sut.FindByPlayerIdAndWeaponIdAsync(1L, 999);

    result.Should().BeNull();
}

[Fact]
public async Task SaveAsync_저장한_무기를_단건_조회할_수_있다()
{
    await _sut.SaveAsync(PlayerWeapon.Create(1L, 1001, 3L, 0L, 0L));

    var result = await _sut.FindByPlayerIdAndWeaponIdAsync(1L, 1001);

    result.Should().NotBeNull();
    result!.Count.Should().Be(3L);
}

[Fact]
public async Task UpdateAsync_엔티티_메서드로_변경한_값이_반영된다()
{
    var weapon = PlayerWeapon.Create(1L, 1001, 5L, 0L, 0L);
    await _sut.SaveAsync(weapon);

    weapon.ConsumeCount(3L);
    weapon.Enhance();
    weapon.Awaken();
    weapon.AddCount(1L);
    await _sut.UpdateAsync(weapon);

    var result = await _sut.FindByPlayerIdAndWeaponIdAsync(1L, 1001);
    result!.Count.Should().Be(3L);
    result.EnhancementLevel.Should().Be(1L);
    result.AwakeningCount.Should().Be(1L);
}
```

- [ ] **Step 2: 실패 확인** — `/test PlayerWeaponRepository`. 컴파일 에러로 FAIL.

- [ ] **Step 3: 엔티티 메서드 추가** — 기존 `Update(...)` 아래에:

```csharp
public void Enhance() => EnhancementLevel += 1;

public void AddCount(long amount) => Count += amount;

public void ConsumeCount(long amount) => Count -= amount;

public void Awaken() => AwakeningCount += 1;
```

- [ ] **Step 4: `PlayerWeaponConfig`에 xmin 추가** — `HasOne<Player>` 블록 아래(`PlayerDungeonProgressConfig`와 동일):

```csharp
builder.Property<uint>("xmin")
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

- [ ] **Step 5: 리포지토리 확장**

```csharp
// IPlayerWeaponRepository.cs에 추가
Task<PlayerWeapon?> FindByPlayerIdAndWeaponIdAsync(long playerId, int weaponId);
Task SaveAsync(PlayerWeapon weapon);
Task UpdateAsync(PlayerWeapon weapon);

// PlayerWeaponRepository.cs에 추가 — xmin 토큰 유실 방지를 위해 추적 조회(AsNoTracking 금지)
public async Task<PlayerWeapon?> FindByPlayerIdAndWeaponIdAsync(long playerId, int weaponId)
    => await _db.PlayerWeapons
        .FirstOrDefaultAsync(w => w.PlayerId == playerId && w.WeaponId == weaponId);

public async Task SaveAsync(PlayerWeapon weapon)
{
    _db.PlayerWeapons.Add(weapon);
    await _db.SaveChangesAsync();
}

public async Task UpdateAsync(PlayerWeapon weapon)
{
    _db.PlayerWeapons.Update(weapon);
    await _db.SaveChangesAsync();
}
```

- [ ] **Step 6: 마이그레이션 생성** — `/db-migrate add AddPlayerWeaponConcurrencyToken`. xmin은 Npgsql 시스템 컬럼이라 생성 SQL이 migration history insert 외에 사실상 없음(무해) — 기존 `PlayerDungeonProgress` 때와 동일.

- [ ] **Step 7: 통과 확인** — `/test PlayerWeaponRepository` PASS. (파일 내 `TestAppDbContext`는 프로덕션 Config를 적용하지 않으므로 Sqlite에서 xmin 문제 없음. 만약 해당 파일 `TestAppDbContext`가 `base.OnModelCreating`을 호출하는 구조면 `PlayerWeapon` 모델을 파일 내에서 재정의해 xmin을 제외한다.)

- [ ] **Step 8: 커밋** — `feat: PlayerWeapon xmin 동시성 토큰 및 단건 갱신 리포지토리 추가`

---

### Task 4: `UpgradeWeaponService` (강화)

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Dto/Response/WeaponUpgradeResponse.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/Interface/IUpgradeWeaponService.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/UpgradeWeaponService.cs`
- Test: `Fantasy-server/Fantasy.Test/Weapon/Service/UpgradeWeaponServiceTest.cs`

**Interfaces:**
- Consumes: Task 1의 `WeaponData.MaxEnhancementLevel`, Task 2의 `GetWeaponEnhancementCostAsync(int, long)`, Task 3의 `FindByPlayerIdAndWeaponIdAsync`/`UpdateAsync`/`Enhance()`.
- Produces: `IUpgradeWeaponService.ExecuteAsync(int weaponId) : Task<WeaponUpgradeResponse>`, `WeaponUpgradeResponse(int WeaponId, long EnhancementLevel, ChangesDto Changes, PlayerDataResponse Player)`.

- [ ] **Step 1: DTO/인터페이스 작성**

```csharp
// Dto/Response/WeaponUpgradeResponse.cs
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Dto.Response;

public record WeaponUpgradeResponse(
    int WeaponId,
    long EnhancementLevel,
    ChangesDto Changes,
    PlayerDataResponse Player
);
```

```csharp
// Service/Interface/IUpgradeWeaponService.cs
using Fantasy.Server.Domain.Weapon.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Service.Interface;

public interface IUpgradeWeaponService
{
    Task<WeaponUpgradeResponse> ExecuteAsync(int weaponId);
}
```

- [ ] **Step 2: 실패하는 테스트 작성** — `Fantasy.Test/Weapon/Service/UpgradeWeaponServiceTest.cs`:

```csharp
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Weapon.Service;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Weapon.Service;

public class UpgradeWeaponServiceTest
{
    private static UpgradeWeaponService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IGameDataCacheService? cache = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null)
    {
        playerRepo ??= Substitute.For<IPlayerRepository>();
        resourceRepo ??= Substitute.For<IPlayerResourceRepository>();
        stageRepo ??= Substitute.For<IPlayerStageRepository>();
        sessionRepo ??= Substitute.For<IPlayerSessionRepository>();
        weaponRepo ??= Substitute.For<IPlayerWeaponRepository>();
        skillRepo ??= Substitute.For<IPlayerSkillRepository>();
        redisRepo ??= Substitute.For<IPlayerRedisRepository>();
        cache ??= Substitute.For<IGameDataCacheService>();
        txRunner ??= Substitute.For<IAppDbTransactionRunner>();
        userProvider ??= Substitute.For<ICurrentUserProvider>();

        return new UpgradeWeaponService(
            playerRepo, resourceRepo, stageRepo, sessionRepo,
            weaponRepo, skillRepo, redisRepo, cache, txRunner, userProvider);
    }

    private static (IPlayerRepository, IPlayerResourceRepository, IPlayerStageRepository,
        IPlayerSessionRepository, IPlayerWeaponRepository, IPlayerSkillRepository,
        IGameDataCacheService, ICurrentUserProvider) BuildHappyPathMocks(
        PlayerResource resource, PlayerWeapon playerWeapon)
    {
        var playerRepo = Substitute.For<IPlayerRepository>();
        var resourceRepo = Substitute.For<IPlayerResourceRepository>();
        var stageRepo = Substitute.For<IPlayerStageRepository>();
        var sessionRepo = Substitute.For<IPlayerSessionRepository>();
        var weaponRepo = Substitute.For<IPlayerWeaponRepository>();
        var skillRepo = Substitute.For<IPlayerSkillRepository>();
        var cache = Substitute.For<IGameDataCacheService>();
        var userProvider = Substitute.For<ICurrentUserProvider>();

        userProvider.GetAccountId().Returns(1L);
        playerRepo.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
        resourceRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(resource);
        stageRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
        sessionRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1001).Returns(playerWeapon);
        weaponRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([playerWeapon]);
        skillRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
        cache.GetWeaponDataAsync(1001).Returns(WeaponData.Create(
            1001, "Rusty Sword", WeaponGrade.C, JobType.Warrior, 30, 5,
            maxEnhancementLevel: 10, maxAwakeningLevel: 3));
        cache.GetWeaponEnhancementCostAsync(1001, 0).Returns(WeaponEnhancementCost.Create(1001, 0, 100, 0));

        return (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider);
    }

    [Fact]
    public async Task ExecuteAsync_플레이어가_없으면_NotFoundException이_발생한다()
    {
        var userProvider = Substitute.For<ICurrentUserProvider>();
        var playerRepo = Substitute.For<IPlayerRepository>();
        userProvider.GetAccountId().Returns(1L);
        playerRepo.FindByAccountAsync(1L).Returns((PlayerEntity?)null);
        var sut = BuildSut(playerRepo: playerRepo, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_미보유_무기면_NotFoundException이_발생한다()
    {
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(PlayerResource.Create(1L), PlayerWeapon.Create(1L, 1001, 1L, 0L, 0L));
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1001).Returns((PlayerWeapon?)null);
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_최대_강화_레벨이면_BadRequestException이_발생한다()
    {
        var weapon = PlayerWeapon.Create(1L, 1001, 1L, 10L, 0L);
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(PlayerResource.Create(1L), weapon);
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_재화가_부족하면_BadRequestException이_발생한다()
    {
        // PlayerResource.Create는 Gold 0으로 시작 — 비용 100을 못 냄
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(PlayerResource.Create(1L), PlayerWeapon.Create(1L, 1001, 1L, 0L, 0L));
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_성공하면_골드가_차감되고_강화_레벨이_오른다()
    {
        var resource = PlayerResource.Create(1L);
        resource.UpdateGold(500L);
        var weapon = PlayerWeapon.Create(1L, 1001, 1L, 0L, 0L);
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(resource, weapon);
        var txRunner = Substitute.For<IAppDbTransactionRunner>();
        txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        var redisRepo = Substitute.For<IPlayerRedisRepository>();
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo,
            redisRepo: redisRepo, cache: cache, txRunner: txRunner, userProvider: userProvider);

        var result = await sut.ExecuteAsync(1001);

        result.EnhancementLevel.Should().Be(1L);
        result.Changes.Gold.Should().Be(-100L);
        resource.Gold.Should().Be(400L);
        weapon.EnhancementLevel.Should().Be(1L);
        await resourceRepo.Received(1).UpdateAsync(resource);
        await weaponRepo.Received(1).UpdateAsync(weapon);
        await redisRepo.Received(1).DeleteAsync(1L);
    }
}
```

- [ ] **Step 3: 실패 확인** — `/test UpgradeWeaponService`. 컴파일 에러로 FAIL.

- [ ] **Step 4: 서비스 구현**

```csharp
// Service/UpgradeWeaponService.cs
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Weapon.Dto.Response;
using Fantasy.Server.Domain.Weapon.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Weapon.Service;

public class UpgradeWeaponService : IUpgradeWeaponService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public UpgradeWeaponService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IGameDataCacheService gameDataCacheService,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _gameDataCacheService = gameDataCacheService;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<WeaponUpgradeResponse> ExecuteAsync(int weaponId)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var weaponData = await _gameDataCacheService.GetWeaponDataAsync(weaponId)
            ?? throw new NotFoundException("무기 데이터를 찾을 수 없습니다.");

        var playerWeapon = await _playerWeaponRepository.FindByPlayerIdAndWeaponIdAsync(player.Id, weaponId);
        if (playerWeapon is null || playerWeapon.Count < 1)
            throw new NotFoundException("보유하지 않은 무기입니다.");

        if (playerWeapon.EnhancementLevel >= weaponData.MaxEnhancementLevel)
            throw new BadRequestException("이미 최대 강화 레벨입니다.");

        var cost = await _gameDataCacheService.GetWeaponEnhancementCostAsync(weaponId, playerWeapon.EnhancementLevel)
            ?? throw new NotFoundException("강화 비용 데이터를 찾을 수 없습니다.");

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        if (resource.Gold < cost.RequiredGold || resource.EnhancementScroll < cost.RequiredScroll)
            throw new BadRequestException("재화가 부족합니다.");

        resource.UpdateGold(resource.Gold - cost.RequiredGold);
        resource.UpdateChangeData(resource.EnhancementScroll - cost.RequiredScroll, null, null);
        playerWeapon.Enhance();

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerResourceRepository.UpdateAsync(resource);
            await _playerWeaponRepository.UpdateAsync(playerWeapon);
        });

        await _playerRedisRepository.DeleteAsync(accountId);

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

        var changes = new ChangesDto(
            Gold: -cost.RequiredGold,
            Exp: 0,
            Sp: 0,
            Mithril: 0,
            EnhancementScroll: -cost.RequiredScroll,
            DungeonTickets: 0,
            LevelUps: [],
            UnlockedSkillIds: [],
            AcquiredWeaponIds: [],
            MaxStage: 0
        );

        return new WeaponUpgradeResponse(weaponId, playerWeapon.EnhancementLevel, changes, playerResponse);
    }
}
```

- [ ] **Step 5: 통과 확인** — `/test UpgradeWeaponService` PASS.

- [ ] **Step 6: 커밋** — `feat: 무기 강화 서비스 추가`

---

### Task 5: `SynthesizeWeaponService` (합성)

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Dto/Response/WeaponSynthesizeResponse.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/Interface/ISynthesizeWeaponService.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/SynthesizeWeaponService.cs`
- Test: `Fantasy-server/Fantasy.Test/Weapon/Service/SynthesizeWeaponServiceTest.cs`

**Interfaces:**
- Consumes: `WeaponData.SynthesizeRequiredCount/SynthesizeResultWeaponId`(Task 1), `FindByPlayerIdAndWeaponIdAsync`/`SaveAsync`/`UpdateAsync`/`ConsumeCount`/`AddCount`(Task 3).
- Produces: `ISynthesizeWeaponService.ExecuteAsync(int weaponId) : Task<WeaponSynthesizeResponse>`, `WeaponSynthesizeResponse(int ConsumedWeaponId, int AcquiredWeaponId, ChangesDto Changes, PlayerDataResponse Player)`.

- [ ] **Step 1: DTO/인터페이스 작성**

```csharp
// Dto/Response/WeaponSynthesizeResponse.cs
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Dto.Response;

public record WeaponSynthesizeResponse(
    int ConsumedWeaponId,
    int AcquiredWeaponId,
    ChangesDto Changes,
    PlayerDataResponse Player
);
```

```csharp
// Service/Interface/ISynthesizeWeaponService.cs
using Fantasy.Server.Domain.Weapon.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Service.Interface;

public interface ISynthesizeWeaponService
{
    Task<WeaponSynthesizeResponse> ExecuteAsync(int weaponId);
}
```

- [ ] **Step 2: 실패하는 테스트 작성** — `SynthesizeWeaponServiceTest.cs`. Task 4의 `BuildSut`/`BuildHappyPathMocks` 구조를 이 파일에도 동일하게 작성하되(`UpgradeWeaponService` → `SynthesizeWeaponService`, cache 설정만 교체), cache 기본 설정은:

```csharp
cache.GetWeaponDataAsync(1001).Returns(WeaponData.Create(
    1001, "Rusty Sword", WeaponGrade.C, JobType.Warrior, 30, 5,
    maxEnhancementLevel: 10, maxAwakeningLevel: 3,
    synthesizeRequiredCount: 3, synthesizeResultWeaponId: 1002));
```

핵심 테스트 케이스 (전체 코드는 위 구조에 맞춰 작성):

```csharp
[Fact]
public async Task ExecuteAsync_합성_불가_무기면_BadRequestException이_발생한다()
{
    // cache.GetWeaponDataAsync(1002)가 synthesizeRequiredCount: null인 WeaponData 반환하도록 설정
    // sut.ExecuteAsync(1002) → BadRequestException
}

[Fact]
public async Task ExecuteAsync_재료가_부족하면_BadRequestException이_발생한다()
{
    // PlayerWeapon.Create(1L, 1001, 2L, 0L, 0L) — 필요 3개, 보유 2개
    // → BadRequestException
}

[Fact]
public async Task ExecuteAsync_결과_무기가_없으면_새로_생성된다()
{
    var material = PlayerWeapon.Create(1L, 1001, 3L, 5L, 1L); // 강화/각성돼 있어도 재료 허용 (가정 #3)
    // weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1002).Returns((PlayerWeapon?)null);
    // txRunner 패스스루 설정

    var result = await sut.ExecuteAsync(1001);

    material.Count.Should().Be(0L);
    result.AcquiredWeaponId.Should().Be(1002);
    result.Changes.AcquiredWeaponIds.Should().BeEquivalentTo([1002]);
    await weaponRepo.Received(1).SaveAsync(Arg.Is<PlayerWeapon>(w =>
        w.WeaponId == 1002 && w.Count == 1L && w.EnhancementLevel == 0L && w.AwakeningCount == 0L));
    await weaponRepo.Received(1).UpdateAsync(material);
}

[Fact]
public async Task ExecuteAsync_결과_무기를_이미_보유하면_Count가_1_증가한다()
{
    var material = PlayerWeapon.Create(1L, 1001, 5L, 0L, 0L);
    var existing = PlayerWeapon.Create(1L, 1002, 2L, 4L, 0L);
    // weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1002).Returns(existing);

    var result = await sut.ExecuteAsync(1001);

    material.Count.Should().Be(2L);
    existing.Count.Should().Be(3L);
    existing.EnhancementLevel.Should().Be(4L); // 기존 강화 레벨 유지
    await weaponRepo.Received(1).UpdateAsync(existing);
}
```

- [ ] **Step 3: 실패 확인** — `/test SynthesizeWeaponService`. FAIL.

- [ ] **Step 4: 서비스 구현** — 생성자는 Task 4와 동일한 10개 의존성(코드 구조 동일). `ExecuteAsync`:

```csharp
public async Task<WeaponSynthesizeResponse> ExecuteAsync(int weaponId)
{
    var accountId = _currentUserProvider.GetAccountId();

    var player = await _playerRepository.FindByAccountAsync(accountId)
        ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

    var weaponData = await _gameDataCacheService.GetWeaponDataAsync(weaponId)
        ?? throw new NotFoundException("무기 데이터를 찾을 수 없습니다.");

    if (weaponData.SynthesizeRequiredCount is null || weaponData.SynthesizeResultWeaponId is null)
        throw new BadRequestException("합성할 수 없는 무기입니다.");

    var requiredCount = weaponData.SynthesizeRequiredCount.Value;
    var resultWeaponId = weaponData.SynthesizeResultWeaponId.Value;

    var material = await _playerWeaponRepository.FindByPlayerIdAndWeaponIdAsync(player.Id, weaponId);
    if (material is null || material.Count < 1)
        throw new NotFoundException("보유하지 않은 무기입니다.");

    if (material.Count < requiredCount)
        throw new BadRequestException("합성 재료가 부족합니다.");

    material.ConsumeCount(requiredCount);

    var result = await _playerWeaponRepository.FindByPlayerIdAndWeaponIdAsync(player.Id, resultWeaponId);
    var isNewResult = result is null;
    if (result is null)
        result = PlayerWeapon.Create(player.Id, resultWeaponId, 1, 0, 0);
    else
        result.AddCount(1);

    await _transactionRunner.ExecuteAsync(async () =>
    {
        await _playerWeaponRepository.UpdateAsync(material);

        if (isNewResult)
            await _playerWeaponRepository.SaveAsync(result);
        else
            await _playerWeaponRepository.UpdateAsync(result);
    });

    await _playerRedisRepository.DeleteAsync(accountId);

    var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
        ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

    var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
        ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

    var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
        ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

    var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
    var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

    var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

    var changes = new ChangesDto(
        Gold: 0,
        Exp: 0,
        Sp: 0,
        Mithril: 0,
        EnhancementScroll: 0,
        DungeonTickets: 0,
        LevelUps: [],
        UnlockedSkillIds: [],
        AcquiredWeaponIds: [resultWeaponId],
        MaxStage: 0
    );

    return new WeaponSynthesizeResponse(weaponId, resultWeaponId, changes, playerResponse);
}
```

- [ ] **Step 5: 통과 확인** — `/test SynthesizeWeaponService` PASS.

- [ ] **Step 6: 커밋** — `feat: 무기 합성 서비스 추가`

---

### Task 6: `AwakenWeaponService` (각성)

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Dto/Response/WeaponAwakenResponse.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/Interface/IAwakenWeaponService.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/AwakenWeaponService.cs`
- Test: `Fantasy-server/Fantasy.Test/Weapon/Service/AwakenWeaponServiceTest.cs`

**Interfaces:**
- Consumes: `WeaponData.MaxAwakeningLevel`(Task 1), `GetWeaponAwakenCostAsync(int, long)`(Task 2), `ConsumeCount`/`Awaken`/`UpdateAsync`(Task 3).
- Produces: `IAwakenWeaponService.ExecuteAsync(int weaponId) : Task<WeaponAwakenResponse>`, `WeaponAwakenResponse(int WeaponId, long AwakeningCount, ChangesDto Changes, PlayerDataResponse Player)`.

- [ ] **Step 1: DTO/인터페이스 작성**

```csharp
// Dto/Response/WeaponAwakenResponse.cs
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Dto.Response;

public record WeaponAwakenResponse(
    int WeaponId,
    long AwakeningCount,
    ChangesDto Changes,
    PlayerDataResponse Player
);
```

```csharp
// Service/Interface/IAwakenWeaponService.cs
using Fantasy.Server.Domain.Weapon.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Service.Interface;

public interface IAwakenWeaponService
{
    Task<WeaponAwakenResponse> ExecuteAsync(int weaponId);
}
```

- [ ] **Step 2: 실패하는 테스트 작성** — `AwakenWeaponServiceTest.cs`. Task 4와 동일한 `BuildSut` 구조. cache 기본 설정:

```csharp
cache.GetWeaponDataAsync(1001).Returns(WeaponData.Create(
    1001, "Rusty Sword", WeaponGrade.C, JobType.Warrior, 30, 5,
    maxEnhancementLevel: 10, maxAwakeningLevel: 3));
cache.GetWeaponAwakenCostAsync(1001, 0).Returns(WeaponAwakenCost.Create(1001, 0, 1, 5));
```

핵심 테스트 케이스:

```csharp
[Fact]
public async Task ExecuteAsync_최대_각성이면_BadRequestException이_발생한다()
{
    // PlayerWeapon.Create(1L, 1001, 5L, 0L, 3L) — AwakeningCount 3 == MaxAwakeningLevel 3
}

[Fact]
public async Task ExecuteAsync_복사본이_부족하면_BadRequestException이_발생한다()
{
    // RequiredCount 1 → Count >= 2 필요. PlayerWeapon.Create(1L, 1001, 1L, 0L, 0L)이면 400
}

[Fact]
public async Task ExecuteAsync_미스릴이_부족하면_BadRequestException이_발생한다()
{
    // PlayerResource.Create(1L)은 Mithril 0 — RequiredMithril 5 미달
    // PlayerWeapon.Create(1L, 1001, 2L, 0L, 0L)
}

[Fact]
public async Task ExecuteAsync_성공하면_복사본과_미스릴이_차감되고_각성이_오른다()
{
    var resource = PlayerResource.Create(1L);
    resource.UpdateChangeData(null, 10L, null); // Mithril 10
    var weapon = PlayerWeapon.Create(1L, 1001, 2L, 0L, 0L);
    // txRunner 패스스루 설정

    var result = await sut.ExecuteAsync(1001);

    weapon.Count.Should().Be(1L);          // 자신 제외 1개 차감 (가정 #2)
    weapon.AwakeningCount.Should().Be(1L);
    resource.Mithril.Should().Be(5L);
    result.Changes.Mithril.Should().Be(-5L);
    await resourceRepo.Received(1).UpdateAsync(resource);
    await weaponRepo.Received(1).UpdateAsync(weapon);
}
```

- [ ] **Step 3: 실패 확인** — `/test AwakenWeaponService`. FAIL.

- [ ] **Step 4: 서비스 구현** — 생성자는 Task 4와 동일한 10개 의존성. `ExecuteAsync`:

```csharp
public async Task<WeaponAwakenResponse> ExecuteAsync(int weaponId)
{
    var accountId = _currentUserProvider.GetAccountId();

    var player = await _playerRepository.FindByAccountAsync(accountId)
        ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

    var weaponData = await _gameDataCacheService.GetWeaponDataAsync(weaponId)
        ?? throw new NotFoundException("무기 데이터를 찾을 수 없습니다.");

    var playerWeapon = await _playerWeaponRepository.FindByPlayerIdAndWeaponIdAsync(player.Id, weaponId);
    if (playerWeapon is null || playerWeapon.Count < 1)
        throw new NotFoundException("보유하지 않은 무기입니다.");

    if (playerWeapon.AwakeningCount >= weaponData.MaxAwakeningLevel)
        throw new BadRequestException("이미 최대 각성 레벨입니다.");

    var cost = await _gameDataCacheService.GetWeaponAwakenCostAsync(weaponId, playerWeapon.AwakeningCount)
        ?? throw new NotFoundException("각성 비용 데이터를 찾을 수 없습니다.");

    if (playerWeapon.Count < cost.RequiredCount + 1)
        throw new BadRequestException("각성 재료가 부족합니다.");

    var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
        ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

    if (resource.Mithril < cost.RequiredMithril)
        throw new BadRequestException("재화가 부족합니다.");

    playerWeapon.ConsumeCount(cost.RequiredCount);
    playerWeapon.Awaken();
    resource.UpdateChangeData(null, resource.Mithril - cost.RequiredMithril, null);

    await _transactionRunner.ExecuteAsync(async () =>
    {
        await _playerResourceRepository.UpdateAsync(resource);
        await _playerWeaponRepository.UpdateAsync(playerWeapon);
    });

    await _playerRedisRepository.DeleteAsync(accountId);

    var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
        ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

    var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
        ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

    var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
    var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

    var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

    var changes = new ChangesDto(
        Gold: 0,
        Exp: 0,
        Sp: 0,
        Mithril: -cost.RequiredMithril,
        EnhancementScroll: 0,
        DungeonTickets: 0,
        LevelUps: [],
        UnlockedSkillIds: [],
        AcquiredWeaponIds: [],
        MaxStage: 0
    );

    return new WeaponAwakenResponse(weaponId, playerWeapon.AwakeningCount, changes, playerResponse);
}
```

- [ ] **Step 5: 통과 확인** — `/test AwakenWeaponService` PASS.

- [ ] **Step 6: 커밋** — `feat: 무기 각성 서비스 추가`

---

### Task 7: `WeaponController` + DI 등록 + 라우트 테스트

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Controller/WeaponController.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Weapon/Config/WeaponServiceConfig.cs`
- Modify: `Fantasy-server/Fantasy.Server/Program.cs`
- Test: `Fantasy-server/Fantasy.Test/Weapon/Controller/WeaponControllerRouteTests.cs`

**Interfaces:**
- Consumes: Task 4~6의 서비스 인터페이스 3개.
- Produces: `POST /v1/weapons/{weaponId}/upgrade`, `POST /v1/weapons/{weaponId}/synthesize`, `POST /v1/weapons/{weaponId}/awaken`.

- [ ] **Step 1: 실패하는 라우트 테스트 작성** — `TutorialControllerRouteTests` 패턴:

```csharp
using System.Reflection;
using Fantasy.Server.Domain.Weapon.Controller;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Fantasy.Test.Weapon.Controller;

public class WeaponControllerRouteTests
{
    private static readonly MethodInfo[] Actions = typeof(WeaponController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    [Fact]
    public void WeaponController는_POST만_노출한다()
    {
        var httpMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct();

        httpMethods.Should().BeEquivalentTo(["POST"]);
    }

    [Fact]
    public void POST_경로_템플릿은_upgrade_synthesize_awaken이다()
    {
        var templates = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Select(a => a.Template);

        templates.Should().BeEquivalentTo(
            ["{weaponId:int}/upgrade", "{weaponId:int}/synthesize", "{weaponId:int}/awaken"]);
    }
}
```

- [ ] **Step 2: 실패 확인** — `/test WeaponControllerRoute`. FAIL.

- [ ] **Step 3: 컨트롤러 구현**

```csharp
// Controller/WeaponController.cs
using Fantasy.Server.Domain.Weapon.Dto.Response;
using Fantasy.Server.Domain.Weapon.Service.Interface;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Weapon.Controller;

[ApiController]
[Route("v1/weapons")]
[Authorize]
[EnableRateLimiting("game")]
public class WeaponController : ControllerBase
{
    private readonly IUpgradeWeaponService _upgradeWeaponService;
    private readonly ISynthesizeWeaponService _synthesizeWeaponService;
    private readonly IAwakenWeaponService _awakenWeaponService;

    public WeaponController(
        IUpgradeWeaponService upgradeWeaponService,
        ISynthesizeWeaponService synthesizeWeaponService,
        IAwakenWeaponService awakenWeaponService)
    {
        _upgradeWeaponService = upgradeWeaponService;
        _synthesizeWeaponService = synthesizeWeaponService;
        _awakenWeaponService = awakenWeaponService;
    }

    [HttpPost("{weaponId:int}/upgrade")]
    public async Task<CommonApiResponse<WeaponUpgradeResponse>> Upgrade([FromRoute] int weaponId)
    {
        var result = await _upgradeWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 강화가 완료되었습니다.", result);
    }

    [HttpPost("{weaponId:int}/synthesize")]
    public async Task<CommonApiResponse<WeaponSynthesizeResponse>> Synthesize([FromRoute] int weaponId)
    {
        var result = await _synthesizeWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 합성이 완료되었습니다.", result);
    }

    [HttpPost("{weaponId:int}/awaken")]
    public async Task<CommonApiResponse<WeaponAwakenResponse>> Awaken([FromRoute] int weaponId)
    {
        var result = await _awakenWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 각성이 완료되었습니다.", result);
    }
}
```

- [ ] **Step 4: DI 등록**

```csharp
// Config/WeaponServiceConfig.cs
using Fantasy.Server.Domain.Weapon.Service;
using Fantasy.Server.Domain.Weapon.Service.Interface;

namespace Fantasy.Server.Domain.Weapon.Config;

public static class WeaponServiceConfig
{
    public static IServiceCollection AddWeaponServices(this IServiceCollection services)
    {
        services.AddScoped<IUpgradeWeaponService, UpgradeWeaponService>();
        services.AddScoped<ISynthesizeWeaponService, SynthesizeWeaponService>();
        services.AddScoped<IAwakenWeaponService, AwakenWeaponService>();
        return services;
    }
}
```

```csharp
// Program.cs — using 추가 + AddDungeonServices() 아래에
using Fantasy.Server.Domain.Weapon.Config;
...
builder.Services.AddWeaponServices();
```

- [ ] **Step 5: 통과 확인** — `/test` 전체 PASS.

- [ ] **Step 6: 커밋** — `feat: WeaponController 및 DI 등록 추가`

---

### Task 8: 문서 갱신 + 최종 검증

**Files:**
- Modify: `docs/client-integration-guide.md`
- Modify: `docs/TODO.md`
- Modify: `docs/superpowers/specs/2026-07-02-server-authority-feature-expansion-design.md` (상태 줄)

- [ ] **Step 1: client-integration-guide.md 갱신** — "6. 던전" 뒤·"7. 튜토리얼" 앞이 아닌, 던전과 튜토리얼 사이 순서를 유지한 채 새 섹션 "7. 무기 강화/합성/각성"을 삽입하고 이후 섹션 번호(튜토리얼→8, 공통 데이터 구조→9, 시간 처리→10)를 조정. 내용: 3개 엔드포인트 표, 강화(확정 성공·Gold/Scroll 소모·무기별 최대 10강), 합성(C등급 3개→동일 직업 B등급 1개, 결과 무기는 강화/각성 0에서 시작, B등급 합성 불가), 각성(자신 제외 복사본+미스릴 소모, 최대 3회), 실패 시 상태 코드(404 미보유/400 조건 미달/409 동시 요청 충돌), `changes`의 음수 델타(소모) 안내, 게임 데이터 조회(`GET /v1/jobs/{jobType}/weapons`)에 신규 필드 4개 추가된 점.

- [ ] **Step 2: TODO.md 갱신** — "무기 강화" 섹션의 "1차 범위에서 보류" 문구 아래에 Phase 3에서 `POST /v1/weapons/{weaponId}/upgrade|synthesize|awaken`으로 구현됐음을 반영(보류 항목 ~~strikethrough~~ 처리).

- [ ] **Step 3: 스펙 상태 갱신** — 설계 문서 4행의 상태를 `Phase 3(무기 강화/합성/각성) 구현 완료`로 갱신하고, §5.5에 확정된 가정 4건(또는 사용자 확인 결과)을 기록.

- [ ] **Step 4: 최종 검증** — `/test` 전체 PASS 확인. 무기/보스 던전·loadout 등 기존 테스트 회귀 없는지 확인.

- [ ] **Step 5: 커밋** — `chore: 무기 강화/합성/각성 문서 갱신`

---

## 비고 (범위 외 발견 사항)

- **A등급 무기 마스터 부재**: `BossDungeonService`는 A등급 확정 드랍 로직이 있으나 `weapons.json`에 A등급 무기가 없어 현재 아무것도 드랍되지 않는다. 이 플랜은 그 상태를 유지한다(B등급 합성 불가 처리). A/S등급 무기 추가는 별도 컨텐츠 작업이며, 이때 `GameDataSeeder`가 행 수 불일치 시 삽입을 건너뛰는 정책(수동 운영 확인 필요)도 함께 다뤄야 한다.
- **비용 테이블 클라이언트 조회 API 없음**: 클라이언트가 강화/각성 비용을 미리 표시하려면 조회 API가 필요하지만 스펙 비범위라 만들지 않는다. 필요 시 후속 논의.
- **`Player`/`PlayerSkill` xmin**: 스펙이 "권장"으로만 명시 — 이번 범위에 포함하지 않는다.
