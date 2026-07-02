# RewardTransaction 감사 로그 (Phase 4) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 던전 보상 4종 + 무기 강화/합성/각성의 보상·소모 내역을 append-only `player.reward_transaction` 테이블에 기록한다(내부 감사·CS 대응용, 조회 API 없음).

**Architecture:** `Domain/Player/`에 `RewardTransaction` 엔티티와 `IRewardTransactionRepository`(단일 `SaveRangeAsync`)를 추가하고, 7개 서비스(`BasicDungeonClaimService`, `GoldDungeonClaimService`, `WeaponDungeonService`, `BossDungeonService`, `UpgradeWeaponService`, `SynthesizeWeaponService`, `AwakenWeaponService`)의 **기존 `IAppDbTransactionRunner.ExecuteAsync` 내부**에서 보상/소모 확정 직후 insert한다. 트랜잭션 경계는 추가하지 않는다. 소모는 음수 `Amount`로 기록한다.

**Tech Stack:** ASP.NET Core Web API (.NET 10), EF Core(Npgsql), xUnit v3 + NSubstitute + FluentAssertions.

**전제:** Phase 3 플랜(`2026-07-02-weapon-enhance-synthesize-awaken.md`)이 머지된 상태. Task 3은 Phase 3의 서비스 3개를 수정한다.

## Global Constraints

- 기록 범위(사용자 확정): 던전 보상 + 무기 강화/합성/각성 결과만. **스킬 해금 SP 소모, 레벨업 SP 지급은 기록하지 않는다.**
- append-only — 수정/삭제 메서드를 만들지 않는다. 조회 API도 만들지 않는다.
- 기록은 각 서비스의 기존 트랜잭션 내부에서 수행한다. 새 트랜잭션/경계 추가 금지.
- `Amount == 0`인 행은 기록하지 않는다 (예: 클릭 0회 골드 던전, 스크롤 비용 0인 강화).
- 멱등 재응답 경로(골드 던전 중복 claim)에서는 기록하지 않는다 — 실제 지급이 없기 때문.
- 엔티티 규칙: `private set` + static `Create`, `DateTime.UtcNow` 직접 사용(단순 발생 시각 기록).
- SourceType/RewardType 문자열은 상수 클래스로만 사용(하드코딩 금지).
- 각 태스크 종료 시 `/test`로 빌드+테스트 통과 확인 후 커밋.

---

### Task 1: `RewardTransaction` 엔티티 + 상수 + 리포지토리 + 마이그레이션

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Entity/RewardTransaction.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Entity/Config/RewardTransactionConfig.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Constant/RewardSourceTypes.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Constant/RewardTypes.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Repository/Interface/IRewardTransactionRepository.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Repository/RewardTransactionRepository.cs`
- Modify: `Fantasy-server/Fantasy.Server/Global/Infrastructure/AppDbContext.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Player/Config/PlayerServiceConfig.cs` (DI 등록)
- Test: `Fantasy-server/Fantasy.Test/Player/Repository/RewardTransactionRepositoryTests.cs`
- Create: `Fantasy-server/Fantasy.Server/Migrations/{timestamp}_AddRewardTransaction.cs` (자동 생성)

**Interfaces:**
- Produces: `RewardTransaction.Create(long playerId, string sourceType, string? sourceRefId, string rewardType, string? rewardRefId, long amount) : RewardTransaction` (Id는 `Guid.CreateVersion7()`, CreatedAt은 `DateTime.UtcNow`). `IRewardTransactionRepository.SaveRangeAsync(List<RewardTransaction> transactions) : Task` — 빈 리스트면 no-op. 상수: `RewardSourceTypes.{DungeonBasic,DungeonGold,DungeonWeapon,DungeonBoss,WeaponUpgrade,WeaponSynthesize,WeaponAwaken}`, `RewardTypes.{Gold,Mithril,Exp,Sp,EnhancementScroll,Weapon}`.

- [ ] **Step 1: 실패하는 리포지토리 테스트 작성** — Sqlite in-memory + 파일 내 `TestAppDbContext` 패턴(`PlayerTutorialRepositoryTests` 참고):

```csharp
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.Player.Constant;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fantasy.Test.Player.Repository;

public class RewardTransactionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly RewardTransactionRepository _sut;

    public RewardTransactionRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new RewardTransactionRepository(_dbContext);
    }

    [Fact]
    public async Task SaveRangeAsync_여러_건을_한_번에_저장한다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _sut.SaveRangeAsync([
            RewardTransaction.Create(1L, RewardSourceTypes.DungeonGold, "run-1", RewardTypes.Gold, null, 500L),
            RewardTransaction.Create(1L, RewardSourceTypes.WeaponUpgrade, null, RewardTypes.Gold, null, -100L)
        ]);

        var saved = await _dbContext.RewardTransactions.ToListAsync(cancellationToken);

        saved.Should().HaveCount(2);
        saved.Should().ContainSingle(t => t.Amount == -100L && t.SourceType == "weapon_upgrade");
        saved.Should().ContainSingle(t => t.SourceRefId == "run-1" && t.Amount == 500L);
    }

    [Fact]
    public async Task SaveRangeAsync_빈_리스트면_저장하지_않는다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _sut.SaveRangeAsync([]);

        var saved = await _dbContext.RewardTransactions.ToListAsync(cancellationToken);
        saved.Should().BeEmpty();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<JobBaseStat>();
            modelBuilder.Ignore<LevelTable>();
            modelBuilder.Ignore<StageData>();
            modelBuilder.Ignore<WeaponData>();
            modelBuilder.Ignore<SkillData>();
            modelBuilder.Ignore<WeaponEnhancementCost>();
            modelBuilder.Ignore<WeaponAwakenCost>();

            modelBuilder.Entity<RewardTransaction>(entity =>
            {
                entity.ToTable("reward_transaction");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.PlayerId).IsRequired();
                entity.Property(t => t.SourceType).IsRequired();
                entity.Property(t => t.RewardType).IsRequired();
                entity.Property(t => t.Amount).IsRequired();
            });
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — `/test RewardTransactionRepository`. 컴파일 에러로 FAIL.

- [ ] **Step 3: 상수 2개 작성**

```csharp
// Constant/RewardSourceTypes.cs
namespace Fantasy.Server.Domain.Player.Constant;

public static class RewardSourceTypes
{
    public const string DungeonBasic = "dungeon_basic";
    public const string DungeonGold = "dungeon_gold";
    public const string DungeonWeapon = "dungeon_weapon";
    public const string DungeonBoss = "dungeon_boss";
    public const string WeaponUpgrade = "weapon_upgrade";
    public const string WeaponSynthesize = "weapon_synthesize";
    public const string WeaponAwaken = "weapon_awaken";
}
```

```csharp
// Constant/RewardTypes.cs
namespace Fantasy.Server.Domain.Player.Constant;

public static class RewardTypes
{
    public const string Gold = "gold";
    public const string Mithril = "mithril";
    public const string Exp = "exp";
    public const string Sp = "sp";
    public const string EnhancementScroll = "enhancement_scroll";
    public const string Weapon = "weapon";
}
```

- [ ] **Step 4: 엔티티 작성**

```csharp
// Entity/RewardTransaction.cs
namespace Fantasy.Server.Domain.Player.Entity;

public class RewardTransaction
{
    public Guid Id { get; private set; }
    public long PlayerId { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public string? SourceRefId { get; private set; }
    public string RewardType { get; private set; } = string.Empty;
    public string? RewardRefId { get; private set; }
    public long Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static RewardTransaction Create(
        long playerId,
        string sourceType,
        string? sourceRefId,
        string rewardType,
        string? rewardRefId,
        long amount) => new()
    {
        Id = Guid.CreateVersion7(),
        PlayerId = playerId,
        SourceType = sourceType,
        SourceRefId = sourceRefId,
        RewardType = rewardType,
        RewardRefId = rewardRefId,
        Amount = amount,
        CreatedAt = DateTime.UtcNow
    };
}
```

- [ ] **Step 5: EF 설정 작성**

```csharp
// Entity/Config/RewardTransactionConfig.cs
using Fantasy.Server.Domain.Player.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Entity.Config;

public class RewardTransactionConfig : IEntityTypeConfiguration<RewardTransaction>
{
    public void Configure(EntityTypeBuilder<RewardTransaction> builder)
    {
        builder.ToTable("reward_transaction", "player");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.PlayerId).IsRequired();
        builder.Property(t => t.SourceType).IsRequired().HasMaxLength(30);
        builder.Property(t => t.SourceRefId).HasMaxLength(50);
        builder.Property(t => t.RewardType).IsRequired().HasMaxLength(30);
        builder.Property(t => t.RewardRefId).HasMaxLength(50);
        builder.Property(t => t.Amount).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => new { t.PlayerId, t.CreatedAt });

        builder.HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(t => t.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 6: DbSet + 리포지토리 + DI**

```csharp
// AppDbContext.cs — PlayerTutorials 아래에 추가
public DbSet<RewardTransaction> RewardTransactions => Set<RewardTransaction>();
```

```csharp
// Repository/Interface/IRewardTransactionRepository.cs
using Fantasy.Server.Domain.Player.Entity;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IRewardTransactionRepository
{
    Task SaveRangeAsync(List<RewardTransaction> transactions);
}
```

```csharp
// Repository/RewardTransactionRepository.cs
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;

namespace Fantasy.Server.Domain.Player.Repository;

public class RewardTransactionRepository : IRewardTransactionRepository
{
    private readonly AppDbContext _db;

    public RewardTransactionRepository(AppDbContext db) => _db = db;

    public async Task SaveRangeAsync(List<RewardTransaction> transactions)
    {
        if (transactions.Count == 0)
            return;

        _db.RewardTransactions.AddRange(transactions);
        await _db.SaveChangesAsync();
    }
}
```

```csharp
// PlayerServiceConfig.cs의 AddPlayerServices에 추가
services.AddScoped<IRewardTransactionRepository, RewardTransactionRepository>();
```

- [ ] **Step 7: 마이그레이션 생성** — `/db-migrate add AddRewardTransaction`. CreateTable + 인덱스 + FK만 있는지 확인.

- [ ] **Step 8: 통과 확인** — `/test RewardTransactionRepository` PASS 후 `/test` 전체 PASS.

- [ ] **Step 9: 커밋** — `feat: RewardTransaction 엔티티 및 리포지토리 추가`

---

### Task 2: 던전 서비스 4종에 기록 추가

**Files:**
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/BasicDungeonClaimService.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/GoldDungeonClaimService.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/WeaponDungeonService.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/BossDungeonService.cs`
- Test: `Fantasy-server/Fantasy.Test/Dungeon/Service/BasicDungeonClaimServiceTests.cs`, `GoldDungeonClaimServiceTest.cs`, `WeaponDungeonServiceTests.cs`, `BossDungeonServiceTests.cs` (각각 생성자 인자 추가 + 검증 케이스 추가)

**Interfaces:**
- Consumes: Task 1의 `IRewardTransactionRepository.SaveRangeAsync`, `RewardTransaction.Create`, 상수 클래스.
- 4개 서비스 모두 생성자에 `IRewardTransactionRepository rewardTransactionRepository` 주입(마지막에서 두 번째 위치 무관 — 기존 스타일대로 리포지토리 그룹 끝에 추가)하고 `private readonly` 필드 `_rewardTransactionRepository`로 보관.

- [ ] **Step 1: 실패하는 테스트 작성** — 각 서비스 테스트의 성공 경로 케이스에 검증 추가. 대표 예(각 파일에 동일 패턴 적용):

```csharp
// BasicDungeonClaimServiceTests — 보상 지급 성공 케이스에 추가
await _rewardTransactionRepository.Received(1).SaveRangeAsync(
    Arg.Is<List<RewardTransaction>>(list =>
        list.Any(t => t.SourceType == RewardSourceTypes.DungeonBasic && t.RewardType == RewardTypes.Gold && t.Amount > 0) &&
        list.Any(t => t.SourceType == RewardSourceTypes.DungeonBasic && t.RewardType == RewardTypes.Exp && t.Amount > 0)));

// GoldDungeonClaimServiceTest — claim 성공 케이스에 추가 (SourceRefId = runId 문자열)
await _rewardTransactionRepository.Received(1).SaveRangeAsync(
    Arg.Is<List<RewardTransaction>>(list =>
        list.Any(t => t.SourceType == RewardSourceTypes.DungeonGold && t.RewardType == RewardTypes.Gold
            && t.SourceRefId == runId.ToString())));

// GoldDungeonClaimServiceTest — 멱등(이미 claim) 케이스에 추가
await _rewardTransactionRepository.DidNotReceive().SaveRangeAsync(Arg.Any<List<RewardTransaction>>());

// WeaponDungeonServiceTests — 클리어+드랍 케이스에 추가 (RewardRefId = weaponId 문자열)
await _rewardTransactionRepository.Received(1).SaveRangeAsync(
    Arg.Is<List<RewardTransaction>>(list =>
        list.All(t => t.SourceType == RewardSourceTypes.DungeonWeapon) &&
        list.Any(t => t.RewardType == RewardTypes.Weapon && t.Amount == 1)));

// BossDungeonServiceTests — 클리어 케이스에 추가
await _rewardTransactionRepository.Received(1).SaveRangeAsync(
    Arg.Is<List<RewardTransaction>>(list =>
        list.Any(t => t.RewardType == RewardTypes.Mithril && t.Amount == 1) &&
        list.Any(t => t.RewardType == RewardTypes.Exp)));
```

각 테스트 파일의 `BuildSut`에 `IRewardTransactionRepository? rewardTxRepo = null` 파라미터와 `rewardTxRepo ??= Substitute.For<IRewardTransactionRepository>();`를 추가하고 생성자 호출에 전달한다. 미클리어/예외 경로 테스트는 수정 불필요(생성자 인자만 컴파일 수정).

- [ ] **Step 2: 실패 확인** — `/test Dungeon`. 컴파일 에러로 FAIL.

- [ ] **Step 3: `BasicDungeonClaimService` 수정** — 생성자 주입 후, 기존 트랜잭션 블록을 다음으로 교체:

```csharp
var rewardTransactions = new List<RewardTransaction>();
if (reward.EarnedGold > 0)
    rewardTransactions.Add(RewardTransaction.Create(
        player.Id, RewardSourceTypes.DungeonBasic, null, RewardTypes.Gold, null, reward.EarnedGold));
if (reward.EarnedXp > 0)
    rewardTransactions.Add(RewardTransaction.Create(
        player.Id, RewardSourceTypes.DungeonBasic, null, RewardTypes.Exp, null, reward.EarnedXp));

await _transactionRunner.ExecuteAsync(async () =>
{
    await _playerRepository.UpdateAsync(player);
    await _playerResourceRepository.UpdateAsync(resource);
    await _playerStageRepository.UpdateAsync(stage);
    await _rewardTransactionRepository.SaveRangeAsync(rewardTransactions);
});
```

(using 추가: `Fantasy.Server.Domain.Player.Constant;`, `Fantasy.Server.Domain.Player.Entity;` — `Player` 타입 충돌 시 기존 파일들처럼 `PlayerEntity` alias 사용.)

- [ ] **Step 4: `GoldDungeonClaimService` 수정** — `run.Claim(...)` 호출 뒤에 목록 구성, 트랜잭션 블록에 저장 추가. 멱등 조기 반환 경로(`run.IsClaimed`)는 수정하지 않는다:

```csharp
var rewardTransactions = new List<RewardTransaction>();
if (earnedGold > 0)
    rewardTransactions.Add(RewardTransaction.Create(
        claimPlayer.Id, RewardSourceTypes.DungeonGold, run.Id.ToString(), RewardTypes.Gold, null, earnedGold));
if (mithrilDropped)
    rewardTransactions.Add(RewardTransaction.Create(
        claimPlayer.Id, RewardSourceTypes.DungeonGold, run.Id.ToString(), RewardTypes.Mithril, null, 1));

await _transactionRunner.ExecuteAsync(async () =>
{
    await _playerResourceRepository.UpdateAsync(claimResource);
    await _goldDungeonRunRepository.UpdateAsync(run);

    if (isNewProgress)
        await _playerDungeonProgressRepository.SaveAsync(progress);
    else
        await _playerDungeonProgressRepository.UpdateAsync(progress);

    await _rewardTransactionRepository.SaveRangeAsync(rewardTransactions);
});
```

- [ ] **Step 5: `WeaponDungeonService` 수정** — `cleared` 블록의 트랜잭션 직전에 목록 구성:

```csharp
var rewardTransactions = droppedWeapons
    .Select(w => RewardTransaction.Create(
        player.Id, RewardSourceTypes.DungeonWeapon, null, RewardTypes.Weapon, w.WeaponId.ToString(), 1))
    .ToList();
if (droppedScrolls > 0)
    rewardTransactions.Add(RewardTransaction.Create(
        player.Id, RewardSourceTypes.DungeonWeapon, null, RewardTypes.EnhancementScroll, null, droppedScrolls));
```

트랜잭션 블록 마지막에 `await _rewardTransactionRepository.SaveRangeAsync(rewardTransactions);` 추가.

- [ ] **Step 6: `BossDungeonService` 수정** — 트랜잭션 직전에 목록 구성:

```csharp
var rewardTransactions = new List<RewardTransaction>
{
    RewardTransaction.Create(player.Id, RewardSourceTypes.DungeonBoss, null, RewardTypes.Mithril, null, BossMithrilReward)
};
if (earnedXp > 0)
    rewardTransactions.Add(RewardTransaction.Create(
        player.Id, RewardSourceTypes.DungeonBoss, null, RewardTypes.Exp, null, earnedXp));
if (droppedWeapon is not null)
    rewardTransactions.Add(RewardTransaction.Create(
        player.Id, RewardSourceTypes.DungeonBoss, null, RewardTypes.Weapon, droppedWeapon.WeaponId.ToString(), 1));
```

트랜잭션 블록 마지막에 `await _rewardTransactionRepository.SaveRangeAsync(rewardTransactions);` 추가.

- [ ] **Step 7: 통과 확인** — `/test Dungeon` PASS 후 `/test` 전체 PASS.

- [ ] **Step 8: 커밋** — `feat: 던전 보상에 RewardTransaction 기록 추가`

---

### Task 3: 무기 강화/합성/각성 서비스 3종에 소모/결과 기록 추가

**Files:**
- Modify: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/UpgradeWeaponService.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/SynthesizeWeaponService.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Weapon/Service/AwakenWeaponService.cs`
- Test: `Fantasy-server/Fantasy.Test/Weapon/Service/UpgradeWeaponServiceTest.cs`, `SynthesizeWeaponServiceTest.cs`, `AwakenWeaponServiceTest.cs` (BuildSut 인자 추가 + 검증 케이스 추가)

**Interfaces:**
- Consumes: Task 1의 리포지토리/상수. 3개 서비스 생성자에 `IRewardTransactionRepository` 주입.

- [ ] **Step 1: 실패하는 테스트 작성** — 각 성공 케이스에 검증 추가:

```csharp
// UpgradeWeaponServiceTest — 성공 케이스 (골드 100 소모, 스크롤 0이라 골드 1건만)
await _rewardTransactionRepository.Received(1).SaveRangeAsync(
    Arg.Is<List<RewardTransaction>>(list =>
        list.Count == 1 &&
        list[0].SourceType == RewardSourceTypes.WeaponUpgrade &&
        list[0].RewardType == RewardTypes.Gold &&
        list[0].Amount == -100L));

// SynthesizeWeaponServiceTest — 성공 케이스 (재료 -3, 결과 +1)
await _rewardTransactionRepository.Received(1).SaveRangeAsync(
    Arg.Is<List<RewardTransaction>>(list =>
        list.Any(t => t.SourceType == RewardSourceTypes.WeaponSynthesize
            && t.RewardType == RewardTypes.Weapon && t.RewardRefId == "1001" && t.Amount == -3L) &&
        list.Any(t => t.SourceType == RewardSourceTypes.WeaponSynthesize
            && t.RewardType == RewardTypes.Weapon && t.RewardRefId == "1002" && t.Amount == 1L)));

// AwakenWeaponServiceTest — 성공 케이스 (복사본 -1, 미스릴 -5)
await _rewardTransactionRepository.Received(1).SaveRangeAsync(
    Arg.Is<List<RewardTransaction>>(list =>
        list.Any(t => t.SourceType == RewardSourceTypes.WeaponAwaken
            && t.RewardType == RewardTypes.Weapon && t.RewardRefId == "1001" && t.Amount == -1L) &&
        list.Any(t => t.SourceType == RewardSourceTypes.WeaponAwaken
            && t.RewardType == RewardTypes.Mithril && t.Amount == -5L)));
```

- [ ] **Step 2: 실패 확인** — `/test Weapon`. FAIL.

- [ ] **Step 3: `UpgradeWeaponService` 수정** — 트랜잭션 직전에 목록 구성, 트랜잭션 블록 마지막에 저장:

```csharp
var rewardTransactions = new List<RewardTransaction>();
if (cost.RequiredGold > 0)
    rewardTransactions.Add(RewardTransaction.Create(
        player.Id, RewardSourceTypes.WeaponUpgrade, null, RewardTypes.Gold, weaponId.ToString(), -cost.RequiredGold));
if (cost.RequiredScroll > 0)
    rewardTransactions.Add(RewardTransaction.Create(
        player.Id, RewardSourceTypes.WeaponUpgrade, null, RewardTypes.EnhancementScroll, weaponId.ToString(), -cost.RequiredScroll));

await _transactionRunner.ExecuteAsync(async () =>
{
    await _playerResourceRepository.UpdateAsync(resource);
    await _playerWeaponRepository.UpdateAsync(playerWeapon);
    await _rewardTransactionRepository.SaveRangeAsync(rewardTransactions);
});
```

- [ ] **Step 4: `SynthesizeWeaponService` 수정** — 동일 패턴:

```csharp
var rewardTransactions = new List<RewardTransaction>
{
    RewardTransaction.Create(player.Id, RewardSourceTypes.WeaponSynthesize, null,
        RewardTypes.Weapon, weaponId.ToString(), -requiredCount),
    RewardTransaction.Create(player.Id, RewardSourceTypes.WeaponSynthesize, null,
        RewardTypes.Weapon, resultWeaponId.ToString(), 1)
};
```

트랜잭션 블록 마지막에 `await _rewardTransactionRepository.SaveRangeAsync(rewardTransactions);` 추가.

- [ ] **Step 5: `AwakenWeaponService` 수정** — 동일 패턴:

```csharp
var rewardTransactions = new List<RewardTransaction>
{
    RewardTransaction.Create(player.Id, RewardSourceTypes.WeaponAwaken, null,
        RewardTypes.Weapon, weaponId.ToString(), -cost.RequiredCount),
    RewardTransaction.Create(player.Id, RewardSourceTypes.WeaponAwaken, null,
        RewardTypes.Mithril, null, -cost.RequiredMithril)
};
```

트랜잭션 블록 마지막에 `await _rewardTransactionRepository.SaveRangeAsync(rewardTransactions);` 추가.

- [ ] **Step 6: 통과 확인** — `/test Weapon` PASS 후 `/test` 전체 PASS.

- [ ] **Step 7: 커밋** — `feat: 무기 강화/합성/각성에 RewardTransaction 기록 추가`

---

### Task 4: 문서 갱신 + 최종 검증

**Files:**
- Modify: `docs/TODO.md`
- Modify: `docs/superpowers/specs/2026-07-02-server-authority-feature-expansion-design.md` (상태 줄)
- (client-integration-guide.md는 변경 없음 — 클라이언트에 노출되는 API/응답 변화가 없는 내부 감사 기능)

- [ ] **Step 1: 스펙 상태 갱신** — 설계 문서 4행 상태를 `Phase 1~4 전체 구현 완료`로 갱신.

- [ ] **Step 2: TODO.md 갱신** — 서버 권위 전환 관련 후속 항목 중 이번에 해소된 것이 있으면 ~~strikethrough~~ 처리.

- [ ] **Step 3: 최종 검증** — `/test` 전체 PASS. 7개 서비스의 보상/소모 경로마다 `RewardTransaction`이 정확히 1회 `SaveRangeAsync`로 기록되고(중복 없음), 멱등 재응답·미클리어·예외 경로에서는 기록되지 않음을 테스트로 확인.

- [ ] **Step 4: 커밋** — `chore: RewardTransaction 문서 갱신`
