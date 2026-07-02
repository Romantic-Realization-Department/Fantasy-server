# 던전 타입별 진행도 (Phase 2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 무기/보스/골드 던전에 각자 독립된 `HighestClearedStage`(무기·보스) 또는 `HighScore`(골드)를 추가해, 세 던전이 더 이상 기본 던전의 `PlayerStage.MaxStage`에 종속되지 않고 자기 진행도를 갖도록 한다.

**Architecture:** 신규 `PlayerDungeonProgress` 엔티티(`(player_id, dungeon_type)` 유니크)를 추가하고, `WeaponDungeonService`/`BossDungeonService`는 난이도 조회를 `PlayerStage.MaxStage` 대신 자기 `PlayerDungeonProgress.HighestClearedStage`로 바꾸며, `GoldDungeonClaimService`는 claim 시 `HighScore`를 갱신한다. 신규 레코드는 지연 생성(lazy-create)하며 별도 백필 마이그레이션은 두지 않는다.

**Tech Stack:** ASP.NET Core Web API, EF Core(Npgsql), xUnit v3 + NSubstitute + FluentAssertions.

## Global Constraints

- 엔티티: 모든 setter `private set`, static `Create(...)` 팩토리. 단순 이벤트 타임스탬프(`LastClearedAt`)는 `Player.UpdateLevel`과 동일하게 엔티티 메서드 내부에서 직접 `DateTime.UtcNow`를 찍는다 — 외부에서 시간을 파라미터로 받지 않는다.
- EF: `IEntityTypeConfiguration<T>` Fluent API만 사용. 테이블은 `dungeon.player_dungeon_progress`(스키마는 도메인 폴더명과 동일하게 `dungeon`). 컬럼명은 EF 기본값(C# 프로퍼티명 그대로, 예: `HighestClearedStage`)을 따르고 별도 `.HasColumnName(...)`을 지정하지 않는다 — 이 저장소의 기존 엔티티 전부가 이 방식이다.
- `DungeonType` enum은 `.HasConversion<string>()`으로 DB에 문자열 저장(`JobType`/`WeaponGrade`와 동일 패턴).
- `xmin` 동시성 토큰을 다른 Player 하위 테이블(`PlayerStage`, `AccountDungeonTicket`, `GoldDungeonRun`)과 동일하게 적용한다. `AppDbTransactionRunner`가 `DbUpdateConcurrencyException`을 `ConflictException`으로 자동 변환하므로, 서비스 코드에서 별도 예외 처리는 필요 없다 — `_transactionRunner.ExecuteAsync(...)` 안에서 저장하기만 하면 된다.
- 리포지토리: 얇은 CRUD만 노출한다(`FindByPlayerIdAndDungeonTypeAsync`, `SaveAsync`, `UpdateAsync`). find-or-create 판단은 서비스가 하며(신규/기존 여부를 서비스가 알고 있어야 `SaveAsync` vs `UpdateAsync`를 선택할 수 있음), Phase 1의 `CompleteTutorialService`와 동일한 패턴을 따른다.
- 신규 유저와 기존 유저 모두 `PlayerDungeonProgress`를 지연 생성한다(1부터 시작). 별도 데이터 백필 스크립트를 만들지 않는다 — 2026-07-02 사용자 확정 사항.
- `DungeonTicketResponse`는 건드리지 않는다 — `HighScore`는 `GoldDungeonClaimResponse`에만 추가한다 — 2026-07-02 사용자 확정 사항.
- 테스트: `NSubstitute`로 리포지토리 인터페이스만 모킹. `IAppDbTransactionRunner`의 패스스루(`.Returns(callInfo => callInfo.Arg<Func<Task>>()())`)는 트랜잭션 본문이 실제로 실행되어야 하는 테스트 클래스에서만 명시적으로 설정한다(이 저장소의 기존 컨벤션 — `BuildSut` 기본값에는 넣지 않는다). Arrange/Act/Assert 사이 빈 줄.
- 각 태스크가 끝나면 `/test <필터>`로 빌드+테스트를 확인한다.

---

### Task 1: `DungeonType` enum + `PlayerDungeonProgress` 엔티티 + EF 설정 + 마이그레이션

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Enum/DungeonType.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Entity/PlayerDungeonProgress.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Entity/Config/PlayerDungeonProgressConfig.cs`
- Modify: `Fantasy-server/Fantasy.Server/Global/Infrastructure/AppDbContext.cs`
- Create: `Fantasy-server/Fantasy.Server/Migrations/{timestamp}_AddPlayerDungeonProgress.cs` (자동 생성)

**Interfaces:**
- Produces: `DungeonType { Basic, Gold, Weapon, Boss }`. `PlayerDungeonProgress.Create(long playerId, DungeonType dungeonType) : PlayerDungeonProgress`, 프로퍼티 `Id(long)`, `PlayerId(long)`, `DungeonType(DungeonType)`, `HighestClearedStage(long, 기본값 1)`, `HighScore(long, 기본값 0)`, `LastClearedAt(DateTime?)`. 인스턴스 메서드 `ClearStage(long stage)`, `UpdateHighScore(long score)`. `AppDbContext.PlayerDungeonProgresses : DbSet<PlayerDungeonProgress>`.

- [ ] **Step 1: `DungeonType` enum 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Dungeon/Enum/DungeonType.cs
namespace Fantasy.Server.Domain.Dungeon.Enum;

public enum DungeonType
{
    Basic,
    Gold,
    Weapon,
    Boss
}
```

- [ ] **Step 2: 엔티티 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Dungeon/Entity/PlayerDungeonProgress.cs
using Fantasy.Server.Domain.Dungeon.Enum;

namespace Fantasy.Server.Domain.Dungeon.Entity;

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

- [ ] **Step 3: EF Fluent 설정 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Dungeon/Entity/Config/PlayerDungeonProgressConfig.cs
using Fantasy.Server.Domain.Dungeon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Dungeon.Entity.Config;

public class PlayerDungeonProgressConfig : IEntityTypeConfiguration<PlayerDungeonProgress>
{
    public void Configure(EntityTypeBuilder<PlayerDungeonProgress> builder)
    {
        builder.ToTable("player_dungeon_progress", "dungeon");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.PlayerId)
            .IsRequired();

        builder.Property(p => p.DungeonType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.HighestClearedStage)
            .IsRequired()
            .HasDefaultValue(1L);

        builder.Property(p => p.HighScore)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(p => p.LastClearedAt)
            .IsRequired(false);

        builder.HasIndex(p => new { p.PlayerId, p.DungeonType })
            .IsUnique();

        builder.HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
```

- [ ] **Step 4: `AppDbContext`에 `DbSet` 등록**

`Fantasy-server/Fantasy.Server/Global/Infrastructure/AppDbContext.cs`의 `using Fantasy.Server.Domain.Dungeon.Entity;`는 이미 존재하므로 추가 using 불필요. `AccountDungeonTickets`/`GoldDungeonRuns` 선언 바로 아래에 추가:

```csharp
    public DbSet<PlayerDungeonProgress> PlayerDungeonProgresses => Set<PlayerDungeonProgress>();
```

- [ ] **Step 5: 빌드 확인**

Run: `/test PlayerDungeonProgress`
Expected: 아직 테스트가 없으므로 "일치하는 테스트 없음" 메시지와 함께 **빌드는 성공**해야 한다.

- [ ] **Step 6: 마이그레이션 생성**

Run: `/db-migrate add AddPlayerDungeonProgress`
Expected: `Fantasy-server/Fantasy.Server/Migrations/`에 `{timestamp}_AddPlayerDungeonProgress.cs`/`.Designer.cs`가 생성되고, `Up()`에 `player_dungeon_progress` 테이블 생성(컬럼: `Id`, `PlayerId`, `DungeonType`(text), `HighestClearedStage`(bigint, default 1), `HighScore`(bigint, default 0), `LastClearedAt`(nullable timestamp), `xmin`) + `(PlayerId, DungeonType)` unique index + `player.player`로의 FK cascade가 포함되어 있는지 확인한다.

- [ ] **Step 7: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Dungeon/Enum Fantasy-server/Fantasy.Server/Domain/Dungeon/Entity/PlayerDungeonProgress.cs Fantasy-server/Fantasy.Server/Domain/Dungeon/Entity/Config/PlayerDungeonProgressConfig.cs Fantasy-server/Fantasy.Server/Global/Infrastructure/AppDbContext.cs Fantasy-server/Fantasy.Server/Migrations
git commit -m "feat: PlayerDungeonProgress 엔티티 및 마이그레이션 추가"
```

---

### Task 2: `IPlayerDungeonProgressRepository` / `PlayerDungeonProgressRepository` + DI 등록

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Repository/Interface/IPlayerDungeonProgressRepository.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Repository/PlayerDungeonProgressRepository.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Config/DungeonServiceConfig.cs`
- Test: `Fantasy-server/Fantasy.Test/Dungeon/Repository/PlayerDungeonProgressRepositoryTests.cs`

**Interfaces:**
- Consumes: `PlayerDungeonProgress.Create(long, DungeonType)`, `AppDbContext.PlayerDungeonProgresses` (Task 1).
- Produces: `IPlayerDungeonProgressRepository.FindByPlayerIdAndDungeonTypeAsync(long playerId, DungeonType dungeonType) : Task<PlayerDungeonProgress?>`, `SaveAsync(PlayerDungeonProgress progress) : Task`, `UpdateAsync(PlayerDungeonProgress progress) : Task`.

- [ ] **Step 1: 실패하는 리포지토리 테스트 작성**

```csharp
// Fantasy-server/Fantasy.Test/Dungeon/Repository/PlayerDungeonProgressRepositoryTests.cs
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fantasy.Test.Dungeon.Repository;

public class PlayerDungeonProgressRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly PlayerDungeonProgressRepository _sut;

    public PlayerDungeonProgressRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new PlayerDungeonProgressRepository(_dbContext);
    }

    [Fact]
    public async Task FindByPlayerIdAndDungeonTypeAsync_존재하지_않으면_null을_반환한다()
    {
        var result = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Weapon);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_저장한_레코드를_FindByPlayerIdAndDungeonTypeAsync로_조회할_수_있다()
    {
        await _sut.SaveAsync(PlayerDungeonProgress.Create(1L, DungeonType.Weapon));

        var result = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Weapon);

        result.Should().NotBeNull();
        result!.HighestClearedStage.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_변경한_값이_반영된다()
    {
        var progress = PlayerDungeonProgress.Create(1L, DungeonType.Gold);
        await _sut.SaveAsync(progress);

        progress.UpdateHighScore(500L);
        await _sut.UpdateAsync(progress);

        var result = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Gold);
        result!.HighScore.Should().Be(500L);
    }

    [Fact]
    public async Task 다른_DungeonType은_독립적으로_저장된다()
    {
        await _sut.SaveAsync(PlayerDungeonProgress.Create(1L, DungeonType.Weapon));
        await _sut.SaveAsync(PlayerDungeonProgress.Create(1L, DungeonType.Boss));

        var weapon = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Weapon);
        var boss = await _sut.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Boss);

        weapon.Should().NotBeNull();
        boss.Should().NotBeNull();
        weapon!.DungeonType.Should().Be(DungeonType.Weapon);
        boss!.DungeonType.Should().Be(DungeonType.Boss);
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

            modelBuilder.Entity<PlayerDungeonProgress>(entity =>
            {
                entity.ToTable("player_dungeon_progress");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedOnAdd();
                entity.Property(p => p.PlayerId).IsRequired();
                entity.Property(p => p.DungeonType).HasConversion<string>().IsRequired();
                entity.Property(p => p.HighestClearedStage).IsRequired();
                entity.Property(p => p.HighScore).IsRequired();
                entity.HasIndex(p => new { p.PlayerId, p.DungeonType }).IsUnique();
            });
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: `/test PlayerDungeonProgressRepositoryTests`
Expected: FAIL — `PlayerDungeonProgressRepository`/`IPlayerDungeonProgressRepository`가 없어 컴파일 에러.

- [ ] **Step 3: 인터페이스 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Dungeon/Repository/Interface/IPlayerDungeonProgressRepository.cs
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;

namespace Fantasy.Server.Domain.Dungeon.Repository.Interface;

public interface IPlayerDungeonProgressRepository
{
    Task<PlayerDungeonProgress?> FindByPlayerIdAndDungeonTypeAsync(long playerId, DungeonType dungeonType);
    Task SaveAsync(PlayerDungeonProgress progress);
    Task UpdateAsync(PlayerDungeonProgress progress);
}
```

- [ ] **Step 4: 구현 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Dungeon/Repository/PlayerDungeonProgressRepository.cs
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Dungeon.Repository;

public class PlayerDungeonProgressRepository : IPlayerDungeonProgressRepository
{
    private readonly AppDbContext _db;

    public PlayerDungeonProgressRepository(AppDbContext db) => _db = db;

    public async Task<PlayerDungeonProgress?> FindByPlayerIdAndDungeonTypeAsync(long playerId, DungeonType dungeonType)
        => await _db.PlayerDungeonProgresses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.DungeonType == dungeonType);

    public async Task SaveAsync(PlayerDungeonProgress progress)
    {
        await _db.PlayerDungeonProgresses.AddAsync(progress);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(PlayerDungeonProgress progress)
    {
        _db.PlayerDungeonProgresses.Update(progress);
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: 통과 확인**

Run: `/test PlayerDungeonProgressRepositoryTests`
Expected: PASS (4 tests)

- [ ] **Step 6: DI 등록**

`Fantasy-server/Fantasy.Server/Domain/Dungeon/Config/DungeonServiceConfig.cs`의 `services.AddScoped<IGoldDungeonRunRepository, GoldDungeonRunRepository>();` 바로 아래 줄에 추가:

```csharp
        services.AddScoped<IPlayerDungeonProgressRepository, PlayerDungeonProgressRepository>();
```

- [ ] **Step 7: 전체 빌드 확인**

Run: `/test`
Expected: 전체 빌드 성공 + 전체 테스트 통과(회귀 없음).

- [ ] **Step 8: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Dungeon/Repository Fantasy-server/Fantasy.Server/Domain/Dungeon/Config/DungeonServiceConfig.cs Fantasy-server/Fantasy.Test/Dungeon/Repository
git commit -m "feat: PlayerDungeonProgressRepository 추가 및 DI 등록"
```

---

### Task 3: `WeaponDungeonService` — 독립 진행도 적용

**Files:**
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/WeaponDungeonResponse.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/WeaponDungeonService.cs`
- Modify: `Fantasy-server/Fantasy.Test/Dungeon/Service/WeaponDungeonServiceTests.cs`

**Interfaces:**
- Consumes: `IPlayerDungeonProgressRepository`(Task 2), 기존 `IAppDbTransactionRunner`(신규로 이 서비스에 주입).
- Produces: `WeaponDungeonResponse(bool Cleared, List<DroppedWeaponInfo> DroppedWeapons, long DroppedScrolls, long HighestClearedStage)`.

**중요한 구조 변경:** 기존 코드는 드랍(`droppedWeapons.Count > 0 || droppedScrolls > 0`)이 있을 때만 저장 블록을 실행했다. 이 태스크부터는 **클리어만 해도**(드랍이 전혀 없어도) `HighestClearedStage`가 갱신되어야 하므로, 저장 조건을 `if (cleared)`로 바꾼다. 단, Redis 캐시 무효화는 `PlayerDataResponse`에 포함되는 무기/재화가 실제로 바뀌었을 때만 필요하므로 기존처럼 드랍 여부에 조건부로 남긴다. 이 서비스는 지금까지 `IAppDbTransactionRunner`가 없었으므로(무기 던전만 유일하게 여러 리포지토리를 트랜잭션 없이 갱신하던 예외였다) 이번에 처음 주입한다 — 신규로 추가하는 세 번째 쓰기(진행도 갱신)를 다른 형제 서비스처럼 트랜잭션으로 묶기 위한 자연스러운 정리다.

- [ ] **Step 1: `WeaponDungeonResponse`에 필드 추가**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/WeaponDungeonResponse.cs
namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record WeaponDungeonResponse(
    bool Cleared,
    List<DroppedWeaponInfo> DroppedWeapons,
    long DroppedScrolls,
    long HighestClearedStage
);
```

- [ ] **Step 2: 실패하는 서비스 테스트 작성**

`Fantasy-server/Fantasy.Test/Dungeon/Service/WeaponDungeonServiceTests.cs`의 `BuildSut` 헬퍼를 아래로 교체(새 파라미터 2개 추가, 기존 파라미터는 순서·이름 그대로 유지):

```csharp
    private static WeaponDungeonService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IGameDataCacheService? cache = null,
        IPlayerDungeonProgressRepository? progressRepo = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null,
        ICombatStatCalculator? calculator = null)
    {
        playerRepo ??= Substitute.For<IPlayerRepository>();
        resourceRepo ??= Substitute.For<IPlayerResourceRepository>();
        stageRepo ??= Substitute.For<IPlayerStageRepository>();
        sessionRepo ??= Substitute.For<IPlayerSessionRepository>();
        weaponRepo ??= Substitute.For<IPlayerWeaponRepository>();
        skillRepo ??= Substitute.For<IPlayerSkillRepository>();
        redisRepo ??= Substitute.For<IPlayerRedisRepository>();
        cache ??= Substitute.For<IGameDataCacheService>();
        progressRepo ??= Substitute.For<IPlayerDungeonProgressRepository>();
        txRunner ??= Substitute.For<IAppDbTransactionRunner>();
        userProvider ??= Substitute.For<ICurrentUserProvider>();
        calculator ??= new CombatStatCalculator();

        return new WeaponDungeonService(
            playerRepo, resourceRepo, stageRepo, sessionRepo,
            weaponRepo, skillRepo, redisRepo, cache,
            progressRepo, txRunner, userProvider, calculator);
    }
```

상단 using에 추가:

```csharp
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
```

기존 `전투력이_부족해서_클리어_실패할_때`, `전투력이_충분해서_클리어_성공할_때`, `DPS_곱하기_30이_몬스터HP와_정확히_같을_때` 세 클래스는 전부 `_gameDataCacheService.GetStageDataAsync(1)`을 이미 스텁하고 있으므로(진행도가 없을 때 기본값 1을 조회한다는 이번 설계와 일치) 그대로 컴파일·통과해야 한다. 여기에 신규 테스트 클래스를 추가한다:

```csharp
    public class 진행도가_있고_클리어에_성공할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IGameDataCacheService _gameDataCacheService = Substitute.For<IGameDataCacheService>();
        private readonly IPlayerDungeonProgressRepository _progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
        private readonly IAppDbTransactionRunner _transactionRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 진행도가_있고_클리어에_성공할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            // 기존 진행도: HighestClearedStage = 5
            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Weapon);
            existingProgress.ClearStage(5);
            _progressRepository.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Weapon)
                .Returns(existingProgress);

            // 몬스터 HP = 1 → 항상 클리어
            var stageData = StageData.Create(5, monsterHp: 1, monsterAtk: 1, xpPerSecond: 5, goldPerSecond: 10);
            _gameDataCacheService.GetStageDataAsync(5).Returns(stageData);
            _gameDataCacheService.GetJobBaseStatAsync(JobType.Warrior)
                .Returns(JobBaseStat.Create(JobType.Warrior, 1000, 100, 0, 1.5, 10, 10));
            _gameDataCacheService.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);
            _gameDataCacheService.GetWeaponDataByGradeAsync(Arg.Any<WeaponGrade>()).Returns([]);

            _transactionRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 자기_진행도_기준으로_스테이지를_조회한다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository, cache: _gameDataCacheService,
                progressRepo: _progressRepository, txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _gameDataCacheService.Received(1).GetStageDataAsync(5);
        }

        [Fact]
        public async Task HighestClearedStage가_1_증가한다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository, cache: _gameDataCacheService,
                progressRepo: _progressRepository, txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.HighestClearedStage.Should().Be(6);
        }

        [Fact]
        public async Task 진행도_UpdateAsync가_호출된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository, cache: _gameDataCacheService,
                progressRepo: _progressRepository, txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _progressRepository.Received(1).UpdateAsync(Arg.Any<PlayerDungeonProgress>());
        }

        [Fact]
        public async Task 드랍이_없으면_Redis_캐시가_무효화되지_않는다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository, cache: _gameDataCacheService,
                progressRepo: _progressRepository, txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _playerRedisRepository.DidNotReceive().DeleteAsync(Arg.Any<long>());
        }
    }

    public class 진행도가_없을_때_신규_생성후_클리어에_성공하면
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IGameDataCacheService _gameDataCacheService = Substitute.For<IGameDataCacheService>();
        private readonly IPlayerDungeonProgressRepository _progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
        private readonly IAppDbTransactionRunner _transactionRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 진행도가_없을_때_신규_생성후_클리어에_성공하면()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            _progressRepository.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Weapon)
                .Returns((PlayerDungeonProgress?)null);

            var stageData = StageData.Create(1, monsterHp: 1, monsterAtk: 1, xpPerSecond: 5, goldPerSecond: 10);
            _gameDataCacheService.GetStageDataAsync(1).Returns(stageData);
            _gameDataCacheService.GetJobBaseStatAsync(JobType.Warrior)
                .Returns(JobBaseStat.Create(JobType.Warrior, 1000, 100, 0, 1.5, 10, 10));
            _gameDataCacheService.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);
            _gameDataCacheService.GetWeaponDataByGradeAsync(Arg.Any<WeaponGrade>()).Returns([]);

            _transactionRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task HighestClearedStage가_2로_반환된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                cache: _gameDataCacheService, progressRepo: _progressRepository,
                txRunner: _transactionRunner, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.HighestClearedStage.Should().Be(2);
        }

        [Fact]
        public async Task 진행도_SaveAsync가_호출된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                cache: _gameDataCacheService, progressRepo: _progressRepository,
                txRunner: _transactionRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _progressRepository.Received(1).SaveAsync(Arg.Any<PlayerDungeonProgress>());
        }
    }
```

- [ ] **Step 3: 실패 확인**

Run: `/test WeaponDungeonServiceTests`
Expected: FAIL — `WeaponDungeonService` 생성자 시그니처 불일치 및 `WeaponDungeonResponse` 필드 불일치로 컴파일 에러.

- [ ] **Step 4: `WeaponDungeonService` 구현**

`Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/WeaponDungeonService.cs` 전체를 아래로 교체:

```csharp
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class WeaponDungeonService : IWeaponDungeonService
{
    private const int BGradeDropRatePercent = 20;
    private const int CGradeDropRatePercent = 70;
    private const int ScrollDropRatePercent = 30;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly IPlayerDungeonProgressRepository _playerDungeonProgressRepository;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ICombatStatCalculator _calculator;

    public WeaponDungeonService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IGameDataCacheService gameDataCacheService,
        IPlayerDungeonProgressRepository playerDungeonProgressRepository,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider,
        ICombatStatCalculator calculator)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _gameDataCacheService = gameDataCacheService;
        _playerDungeonProgressRepository = playerDungeonProgressRepository;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
        _calculator = calculator;
    }

    public async Task<WeaponDungeonResponse> ExecuteAsync()
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var jobType = player.JobType;

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var jobStat = await _gameDataCacheService.GetJobBaseStatAsync(player.JobType)
            ?? throw new NotFoundException("직업 기본 스탯 데이터를 찾을 수 없습니다.");

        WeaponData? weaponData = null;
        long weaponEnhancement = 0;
        if (session.LastWeaponId.HasValue)
        {
            weaponData = await _gameDataCacheService.GetWeaponDataAsync(session.LastWeaponId.Value);
            var equippedWeapon = weapons.FirstOrDefault(w => w.WeaponId == session.LastWeaponId.Value);
            weaponEnhancement = equippedWeapon?.EnhancementLevel ?? 0;
        }

        var jobSkillData = await _gameDataCacheService.GetSkillDataByJobAsync(player.JobType);
        var unlockedPassiveSkills = skills
            .Where(s => s.IsUnlocked)
            .Select(s => jobSkillData.FirstOrDefault(sd => sd.SkillId == s.SkillId))
            .Where(sd => sd is not null && !sd.IsActive)
            .Select(sd => (Skill: sd!, IsPassive: true));

        var combatStat = _calculator.Calculate(player.Level, jobStat, weaponData, weaponEnhancement, unlockedPassiveSkills);

        var progress = await _playerDungeonProgressRepository.FindByPlayerIdAndDungeonTypeAsync(player.Id, DungeonType.Weapon);
        var isNewProgress = progress is null;
        var currentStage = progress?.HighestClearedStage ?? 1;

        var stageData = await _gameDataCacheService.GetStageDataAsync(currentStage)
            ?? throw new NotFoundException("스테이지 데이터를 찾을 수 없습니다.");

        var dps = _calculator.CalculateDps(combatStat);
        var cleared = dps * 30 >= stageData.MonsterHp;

        var droppedWeapons = new List<DroppedWeaponInfo>();
        long droppedScrolls = 0;

        if (cleared)
        {
            // B등급 드랍 시도
            if (Random.Shared.Next(0, 100) < BGradeDropRatePercent)
            {
                var bWeapons = await _gameDataCacheService.GetWeaponDataByGradeAsync(WeaponGrade.B);
                var bJobWeapons = bWeapons.Where(w => w.JobType == jobType).ToList();
                if (bJobWeapons.Count > 0)
                {
                    var dropped = bJobWeapons[Random.Shared.Next(bJobWeapons.Count)];
                    droppedWeapons.Add(new DroppedWeaponInfo(dropped.WeaponId, dropped.Name, dropped.Grade));
                }
            }
            // C등급 드랍 시도
            if (Random.Shared.Next(0, 100) < CGradeDropRatePercent)
            {
                var cWeapons = await _gameDataCacheService.GetWeaponDataByGradeAsync(WeaponGrade.C);
                var cJobWeapons = cWeapons.Where(w => w.JobType == jobType).ToList();
                if (cJobWeapons.Count > 0)
                {
                    var dropped = cJobWeapons[Random.Shared.Next(cJobWeapons.Count)];
                    droppedWeapons.Add(new DroppedWeaponInfo(dropped.WeaponId, dropped.Name, dropped.Grade));
                }
            }

            // 스크롤 드랍 시도
            if (Random.Shared.Next(0, 100) < ScrollDropRatePercent)
                droppedScrolls = 1;

            progress ??= PlayerDungeonProgress.Create(player.Id, DungeonType.Weapon);
            progress.ClearStage(currentStage + 1);

            var weaponChanges = droppedWeapons
                .Select(w =>
                {
                    var existing = weapons.FirstOrDefault(pw => pw.WeaponId == w.WeaponId);
                    return new WeaponChangeItem(w.WeaponId, (existing?.Count ?? 0) + 1,
                        existing?.EnhancementLevel ?? 0, existing?.AwakeningCount ?? 0);
                })
                .ToList();

            await _transactionRunner.ExecuteAsync(async () =>
            {
                if (weaponChanges.Count > 0)
                    await _playerWeaponRepository.UpsertRangeAsync(player.Id, weaponChanges);

                if (droppedScrolls > 0)
                {
                    resource.UpdateChangeData(resource.EnhancementScroll + droppedScrolls, null, null);
                    await _playerResourceRepository.UpdateAsync(resource);
                }

                if (isNewProgress)
                    await _playerDungeonProgressRepository.SaveAsync(progress);
                else
                    await _playerDungeonProgressRepository.UpdateAsync(progress);
            });

            if (droppedWeapons.Count > 0 || droppedScrolls > 0)
                await _playerRedisRepository.DeleteAsync(accountId);
        }

        var highestClearedStage = progress?.HighestClearedStage ?? currentStage;
        return new WeaponDungeonResponse(cleared, droppedWeapons, droppedScrolls, highestClearedStage);
    }
}
```

- [ ] **Step 5: 통과 확인**

Run: `/test WeaponDungeonServiceTests`
Expected: PASS (기존 6개 + 신규 6개 = 12개)

- [ ] **Step 6: 전체 빌드 확인**

Run: `/test`
Expected: 전체 통과. 다른 파일에서 `new WeaponDungeonResponse(...)`를 호출하는 곳이 없는지 확인(컨트롤러는 서비스 반환값을 그대로 전달하므로 수정 불필요).

- [ ] **Step 7: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/WeaponDungeonResponse.cs Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/WeaponDungeonService.cs Fantasy-server/Fantasy.Test/Dungeon/Service/WeaponDungeonServiceTests.cs
git commit -m "feat: 무기 던전에 독립 진행도(HighestClearedStage) 적용"
```

---

### Task 4: `BossDungeonService` — 독립 진행도 적용

**Files:**
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/BossDungeonResponse.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/BossDungeonService.cs`
- Modify: `Fantasy-server/Fantasy.Test/Dungeon/Service/BossDungeonServiceTests.cs`

**Interfaces:**
- Consumes: `IPlayerDungeonProgressRepository`(Task 2), 기존 `IAppDbTransactionRunner`(이미 주입되어 있음 — 새로 추가하지 않음).
- Produces: `BossDungeonResponse(bool Cleared, long EarnedMithril, DroppedWeaponInfo? DroppedWeapon, long EarnedXp, List<LevelUpResult> LevelUps, long HighestClearedStage)`.

**Weapon과의 차이:** `BossDungeonService`는 이미 `_transactionRunner`를 갖고 있고, 클리어 시(`!cleared`로 조기 반환하지 않는 한) 트랜잭션이 항상 실행되므로 조건 재구성이 필요 없다 — 기존 트랜잭션 람다 안에 진행도 저장 호출만 추가하면 된다.

- [ ] **Step 1: `BossDungeonResponse`에 필드 추가**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/BossDungeonResponse.cs
using Fantasy.Server.Domain.LevelUp.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record BossDungeonResponse(
    bool Cleared,
    long EarnedMithril,
    DroppedWeaponInfo? DroppedWeapon,
    long EarnedXp,
    List<LevelUpResult> LevelUps,
    long HighestClearedStage
);
```

- [ ] **Step 2: 실패하는 서비스 테스트 작성**

`Fantasy-server/Fantasy.Test/Dungeon/Service/BossDungeonServiceTests.cs`의 `BuildSut` 헬퍼를 아래로 교체(새 파라미터 1개 추가):

```csharp
    private static BossDungeonService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IGameDataCacheService? cache = null,
        ILevelUpService? levelUpService = null,
        IPlayerDungeonProgressRepository? progressRepo = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null,
        ICombatStatCalculator? calculator = null)
    {
        playerRepo ??= Substitute.For<IPlayerRepository>();
        resourceRepo ??= Substitute.For<IPlayerResourceRepository>();
        stageRepo ??= Substitute.For<IPlayerStageRepository>();
        sessionRepo ??= Substitute.For<IPlayerSessionRepository>();
        weaponRepo ??= Substitute.For<IPlayerWeaponRepository>();
        skillRepo ??= Substitute.For<IPlayerSkillRepository>();
        redisRepo ??= Substitute.For<IPlayerRedisRepository>();
        cache ??= Substitute.For<IGameDataCacheService>();
        levelUpService ??= Substitute.For<ILevelUpService>();
        progressRepo ??= Substitute.For<IPlayerDungeonProgressRepository>();
        txRunner ??= Substitute.For<IAppDbTransactionRunner>();
        userProvider ??= Substitute.For<ICurrentUserProvider>();
        calculator ??= new CombatStatCalculator();

        return new BossDungeonService(
            playerRepo, resourceRepo, stageRepo, sessionRepo,
            weaponRepo, skillRepo, redisRepo, cache,
            levelUpService, progressRepo, txRunner, userProvider, calculator);
    }
```

상단 using에 추가:

```csharp
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
```

기존 세 클래스(`플레이어가_없을_때`, `전투력이_부족해서_클리어_실패할_때`, `전투력이_충분해서_클리어_성공할_때`, `DPS_곱하기_30이_보스HP와_정확히_같을_때`)는 모두 `_gameDataCacheService.GetStageDataAsync(1)`을 스텁하므로 그대로 컴파일·통과해야 한다. 신규 테스트 클래스를 추가한다:

```csharp
    public class 진행도가_있고_클리어에_성공할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IGameDataCacheService _gameDataCacheService = Substitute.For<IGameDataCacheService>();
        private readonly ILevelUpService _levelUpService = Substitute.For<ILevelUpService>();
        private readonly IPlayerDungeonProgressRepository _progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
        private readonly IAppDbTransactionRunner _transactionRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 진행도가_있고_클리어에_성공할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Boss);
            existingProgress.ClearStage(3);
            _progressRepository.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Boss)
                .Returns(existingProgress);

            // 보스 HP = 1 * 5 = 5 → DPS(100) > 5 → 클리어
            var stageData = StageData.Create(3, monsterHp: 1, monsterAtk: 1, xpPerSecond: 5, goldPerSecond: 10);
            _gameDataCacheService.GetStageDataAsync(3).Returns(stageData);
            _gameDataCacheService.GetJobBaseStatAsync(JobType.Warrior)
                .Returns(JobBaseStat.Create(JobType.Warrior, 1000, 100, 0, 1.5, 10, 10));
            _gameDataCacheService.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);
            _gameDataCacheService.GetWeaponDataByGradeAsync(WeaponGrade.A).Returns([]);
            _levelUpService.ExecuteAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<long>())
                .Returns([]);
            _transactionRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 자기_진행도_기준으로_보스_스테이지를_조회한다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository, cache: _gameDataCacheService,
                levelUpService: _levelUpService, progressRepo: _progressRepository,
                txRunner: _transactionRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _gameDataCacheService.Received(1).GetStageDataAsync(3);
        }

        [Fact]
        public async Task HighestClearedStage가_1_증가한다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository, cache: _gameDataCacheService,
                levelUpService: _levelUpService, progressRepo: _progressRepository,
                txRunner: _transactionRunner, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.HighestClearedStage.Should().Be(4);
        }

        [Fact]
        public async Task 진행도_UpdateAsync가_호출된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository, sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository, skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository, cache: _gameDataCacheService,
                levelUpService: _levelUpService, progressRepo: _progressRepository,
                txRunner: _transactionRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _progressRepository.Received(1).UpdateAsync(Arg.Any<PlayerDungeonProgress>());
        }
    }

    public class 클리어_실패시_진행도가_변하지_않는다
    {
        [Fact]
        public async Task 기존_HighestClearedStage가_그대로_반환된다()
        {
            var playerRepository = Substitute.For<IPlayerRepository>();
            var resourceRepository = Substitute.For<IPlayerResourceRepository>();
            var stageRepository = Substitute.For<IPlayerStageRepository>();
            var sessionRepository = Substitute.For<IPlayerSessionRepository>();
            var weaponRepository = Substitute.For<IPlayerWeaponRepository>();
            var skillRepository = Substitute.For<IPlayerSkillRepository>();
            var gameDataCacheService = Substitute.For<IGameDataCacheService>();
            var progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
            var currentUserProvider = Substitute.For<ICurrentUserProvider>();

            currentUserProvider.GetAccountId().Returns(1L);
            playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Boss);
            existingProgress.ClearStage(3);
            progressRepository.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Boss)
                .Returns(existingProgress);

            // 몬스터 HP가 매우 높아 클리어 불가
            var stageData = StageData.Create(3, monsterHp: 10_000_000, monsterAtk: 999, xpPerSecond: 5, goldPerSecond: 10);
            gameDataCacheService.GetStageDataAsync(3).Returns(stageData);
            gameDataCacheService.GetJobBaseStatAsync(JobType.Warrior)
                .Returns(JobBaseStat.Create(JobType.Warrior, 1000, 100, 0, 1.5, 10, 10));
            gameDataCacheService.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);

            var sut = BuildSut(
                playerRepo: playerRepository, resourceRepo: resourceRepository,
                stageRepo: stageRepository, sessionRepo: sessionRepository,
                weaponRepo: weaponRepository, skillRepo: skillRepository,
                cache: gameDataCacheService, progressRepo: progressRepository,
                userProvider: currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.HighestClearedStage.Should().Be(3);
            await progressRepository.DidNotReceive().UpdateAsync(Arg.Any<PlayerDungeonProgress>());
        }
    }
```

- [ ] **Step 3: 실패 확인**

Run: `/test BossDungeonServiceTests`
Expected: FAIL — 생성자 시그니처 불일치 및 `BossDungeonResponse` 필드 불일치로 컴파일 에러.

- [ ] **Step 4: `BossDungeonService` 구현**

`Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/BossDungeonService.cs`를 아래로 교체:

```csharp
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.LevelUp.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class BossDungeonService : IBossDungeonService
{
    private const long BossMithrilReward = 1;
    private const long BossXpMultiplier = 10;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly ILevelUpService _levelUpService;
    private readonly IPlayerDungeonProgressRepository _playerDungeonProgressRepository;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ICombatStatCalculator _calculator;

    public BossDungeonService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IGameDataCacheService gameDataCacheService,
        ILevelUpService levelUpService,
        IPlayerDungeonProgressRepository playerDungeonProgressRepository,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider,
        ICombatStatCalculator calculator)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _gameDataCacheService = gameDataCacheService;
        _levelUpService = levelUpService;
        _playerDungeonProgressRepository = playerDungeonProgressRepository;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
        _calculator = calculator;
    }

    public async Task<BossDungeonResponse> ExecuteAsync()
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var jobType = player.JobType;

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var jobStat = await _gameDataCacheService.GetJobBaseStatAsync(player.JobType)
            ?? throw new NotFoundException("직업 기본 스탯 데이터를 찾을 수 없습니다.");

        WeaponData? weaponData = null;
        long weaponEnhancement = 0;
        if (session.LastWeaponId.HasValue)
        {
            weaponData = await _gameDataCacheService.GetWeaponDataAsync(session.LastWeaponId.Value);
            var equippedWeapon = weapons.FirstOrDefault(w => w.WeaponId == session.LastWeaponId.Value);
            weaponEnhancement = equippedWeapon?.EnhancementLevel ?? 0;
        }

        var jobSkillData = await _gameDataCacheService.GetSkillDataByJobAsync(player.JobType);
        var unlockedPassiveSkills = skills
            .Where(s => s.IsUnlocked)
            .Select(s => jobSkillData.FirstOrDefault(sd => sd.SkillId == s.SkillId))
            .Where(sd => sd is not null && !sd.IsActive)
            .Select(sd => (Skill: sd!, IsPassive: true));

        var combatStat = _calculator.Calculate(player.Level, jobStat, weaponData, weaponEnhancement, unlockedPassiveSkills);

        var progress = await _playerDungeonProgressRepository.FindByPlayerIdAndDungeonTypeAsync(player.Id, DungeonType.Boss);
        var currentStage = progress?.HighestClearedStage ?? 1;

        var stageData = await _gameDataCacheService.GetStageDataAsync(currentStage)
            ?? throw new NotFoundException("스테이지 데이터를 찾을 수 없습니다.");

        // 보스는 일반 몬스터의 5배 체력
        var bossHp = stageData.MonsterHp * 5;
        var dps = _calculator.CalculateDps(combatStat);
        var cleared = dps * 30 >= bossHp;

        if (!cleared)
            return new BossDungeonResponse(false, 0, null, 0, [], currentStage);

        var isNewProgress = progress is null;
        progress ??= PlayerDungeonProgress.Create(player.Id, DungeonType.Boss);
        progress.ClearStage(currentStage + 1);

        var earnedXp = stageData.XpPerSecond * BossXpMultiplier;
        var levelUps = await _levelUpService.ExecuteAsync(player, resource, earnedXp);
        resource.UpdateChangeData(null, resource.Mithril + BossMithrilReward, null);

        DroppedWeaponInfo? droppedWeapon = null;
        var aWeapons = await _gameDataCacheService.GetWeaponDataByGradeAsync(WeaponGrade.A);
        var aJobWeapons = aWeapons.Where(w => w.JobType == jobType).ToList();
        List<WeaponChangeItem> weaponChanges = [];

        if (aJobWeapons.Count > 0)
        {
            var dropped = aJobWeapons[Random.Shared.Next(aJobWeapons.Count)];
            droppedWeapon = new DroppedWeaponInfo(dropped.WeaponId, dropped.Name, dropped.Grade);

            var existing = weapons.FirstOrDefault(w => w.WeaponId == dropped.WeaponId);
            weaponChanges.Add(new WeaponChangeItem(dropped.WeaponId, (existing?.Count ?? 0) + 1,
                existing?.EnhancementLevel ?? 0, existing?.AwakeningCount ?? 0));
        }

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerRepository.UpdateAsync(player);
            await _playerResourceRepository.UpdateAsync(resource);
            if (weaponChanges.Count > 0)
                await _playerWeaponRepository.UpsertRangeAsync(player.Id, weaponChanges);

            if (isNewProgress)
                await _playerDungeonProgressRepository.SaveAsync(progress);
            else
                await _playerDungeonProgressRepository.UpdateAsync(progress);
        });

        await _playerRedisRepository.DeleteAsync(accountId);

        return new BossDungeonResponse(true, BossMithrilReward, droppedWeapon, earnedXp, levelUps, progress.HighestClearedStage);
    }
}
```

- [ ] **Step 5: 통과 확인**

Run: `/test BossDungeonServiceTests`
Expected: PASS (기존 7개 + 신규 4개 = 11개)

- [ ] **Step 6: 전체 빌드 확인**

Run: `/test`
Expected: 전체 통과.

- [ ] **Step 7: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/BossDungeonResponse.cs Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/BossDungeonService.cs Fantasy-server/Fantasy.Test/Dungeon/Service/BossDungeonServiceTests.cs
git commit -m "feat: 보스 던전에 독립 진행도(HighestClearedStage) 적용"
```

---

### Task 5: `GoldDungeonClaimService` — `HighScore` 갱신

**Files:**
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/GoldDungeonClaimResponse.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/GoldDungeonClaimService.cs`
- Modify: `Fantasy-server/Fantasy.Test/Dungeon/Service/GoldDungeonClaimServiceTest.cs`

**Interfaces:**
- Consumes: `IPlayerDungeonProgressRepository`(Task 2).
- Produces: `GoldDungeonClaimResponse(Guid RunId, long EarnedGold, int EarnedMithril, long HighScore, ChangesDto Changes, PlayerDataResponse Player)`.

**설계:** 정상 claim 시 `earnedGold`로 `PlayerDungeonProgress(Gold).UpdateHighScore`를 호출해 기존 트랜잭션 블록 안에서 저장한다(`HighScore`는 이미 더 크지 않으면 내부적으로 값이 바뀌지 않으므로 항상 호출해도 안전). 이미 claim된 run을 재호출하는 멱등 분기에서는 저장하지 않고 현재 `PlayerDungeonProgress(Gold)`를 읽기만 해서 응답에 포함한다.

- [ ] **Step 1: `GoldDungeonClaimResponse`에 필드 추가**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/GoldDungeonClaimResponse.cs
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record GoldDungeonClaimResponse(
    Guid RunId,
    long EarnedGold,
    int EarnedMithril,
    long HighScore,
    ChangesDto Changes,
    PlayerDataResponse Player
);
```

- [ ] **Step 2: 실패하는 서비스 테스트 작성**

`Fantasy-server/Fantasy.Test/Dungeon/Service/GoldDungeonClaimServiceTest.cs`의 `BuildSut` 헬퍼를 아래로 교체(새 파라미터 1개 추가):

```csharp
    private static GoldDungeonClaimService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IGoldDungeonRunRepository? runRepo = null,
        IPlayerDungeonProgressRepository? progressRepo = null,
        IRandomProvider? randomProvider = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null,
        TimeProvider? timeProvider = null) =>
        new(
            playerRepo ?? Substitute.For<IPlayerRepository>(),
            resourceRepo ?? Substitute.For<IPlayerResourceRepository>(),
            stageRepo ?? Substitute.For<IPlayerStageRepository>(),
            sessionRepo ?? Substitute.For<IPlayerSessionRepository>(),
            weaponRepo ?? Substitute.For<IPlayerWeaponRepository>(),
            skillRepo ?? Substitute.For<IPlayerSkillRepository>(),
            redisRepo ?? Substitute.For<IPlayerRedisRepository>(),
            runRepo ?? Substitute.For<IGoldDungeonRunRepository>(),
            progressRepo ?? Substitute.For<IPlayerDungeonProgressRepository>(),
            randomProvider ?? Substitute.For<IRandomProvider>(),
            txRunner ?? Substitute.For<IAppDbTransactionRunner>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>(),
            timeProvider ?? FixedTimeProvider(DateTimeOffset.UtcNow));
```

상단 using에 추가:

```csharp
using Fantasy.Server.Domain.Dungeon.Enum;
```

기존 `이미_클레임된_런일_때` 클래스는 **수정하지 않는다** — `BuildSut`의 새 `progressRepo` 파라미터가 옵션이라 전달하지 않으면 자동으로 빈 `Substitute.For<IPlayerDungeonProgressRepository>()`가 주입되고, `FindByPlayerIdAndDungeonTypeAsync`가 기본으로 `null`을 반환해 `existingProgress?.HighScore ?? 0`이 `0`이 될 뿐이다. 이 클래스는 `EarnedGold`/`EarnedMithril`만 검증하므로 그대로 컴파일·통과한다. 신규 테스트 클래스만 추가한다:

```csharp
    public class 정상_클레임시_HighScore가_갱신될_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IGoldDungeonRunRepository _runRepository = Substitute.For<IGoldDungeonRunRepository>();
        private readonly IPlayerDungeonProgressRepository _progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
        private readonly IRandomProvider _randomProvider = Substitute.For<IRandomProvider>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GoldDungeonRun _run;
        private readonly TimeProvider _timeProvider;

        public 정상_클레임시_HighScore가_갱신될_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            _runRepository.FindByIdAsync(_run.Id).Returns(_run);
            _randomProvider.Next(0, 100).Returns(50);
            _timeProvider = FixedTimeProvider(_run.StartedAt.AddSeconds(30));
            SetupPlayerData(_playerRepository, _resourceRepository, _stageRepository,
                _sessionRepository, _weaponRepository, _skillRepository);

            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Gold);
            existingProgress.UpdateHighScore(500L);
            _progressRepository.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Gold)
                .Returns(existingProgress);

            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 기존_최고점수보다_높으면_HighScore가_갱신된다()
        {
            // 클릭 100 * 10골드 = 1000 > 기존 500
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                runRepo: _runRepository, progressRepo: _progressRepository,
                randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider,
                timeProvider: _timeProvider);

            var result = await sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100));

            result.HighScore.Should().Be(1000L);
        }

        [Fact]
        public async Task 진행도_UpdateAsync가_호출된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                runRepo: _runRepository, progressRepo: _progressRepository,
                randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider,
                timeProvider: _timeProvider);

            await sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100));

            await _progressRepository.Received(1).UpdateAsync(Arg.Any<PlayerDungeonProgress>());
        }
    }

    public class 정상_클레임시_기존_최고점수보다_낮을_때
    {
        [Fact]
        public async Task HighScore가_기존값으로_유지된다()
        {
            var playerRepository = Substitute.For<IPlayerRepository>();
            var resourceRepository = Substitute.For<IPlayerResourceRepository>();
            var stageRepository = Substitute.For<IPlayerStageRepository>();
            var sessionRepository = Substitute.For<IPlayerSessionRepository>();
            var weaponRepository = Substitute.For<IPlayerWeaponRepository>();
            var skillRepository = Substitute.For<IPlayerSkillRepository>();
            var runRepository = Substitute.For<IGoldDungeonRunRepository>();
            var progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
            var randomProvider = Substitute.For<IRandomProvider>();
            var txRunner = Substitute.For<IAppDbTransactionRunner>();
            var currentUserProvider = Substitute.For<ICurrentUserProvider>();

            currentUserProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepository.FindByIdAsync(run.Id).Returns(run);
            randomProvider.Next(0, 100).Returns(50);
            SetupPlayerData(playerRepository, resourceRepository, stageRepository,
                sessionRepository, weaponRepository, skillRepository);

            // 기존 최고점수 5000 > 이번 클릭 10 * 10골드 = 100
            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Gold);
            existingProgress.UpdateHighScore(5000L);
            progressRepository.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Gold)
                .Returns(existingProgress);

            txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());

            var sut = BuildSut(playerRepo: playerRepository, resourceRepo: resourceRepository,
                stageRepo: stageRepository, sessionRepo: sessionRepository,
                weaponRepo: weaponRepository, skillRepo: skillRepository,
                runRepo: runRepository, progressRepo: progressRepository,
                randomProvider: randomProvider, txRunner: txRunner, userProvider: currentUserProvider,
                timeProvider: FixedTimeProvider(run.StartedAt.AddSeconds(30)));

            var result = await sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(10));

            result.HighScore.Should().Be(5000L);
        }
    }

    public class 이미_클레임된_런_재조회시_HighScore가_포함될_때
    {
        [Fact]
        public async Task 저장된_HighScore가_반환된다()
        {
            var playerRepository = Substitute.For<IPlayerRepository>();
            var resourceRepository = Substitute.For<IPlayerResourceRepository>();
            var stageRepository = Substitute.For<IPlayerStageRepository>();
            var sessionRepository = Substitute.For<IPlayerSessionRepository>();
            var weaponRepository = Substitute.For<IPlayerWeaponRepository>();
            var skillRepository = Substitute.For<IPlayerSkillRepository>();
            var runRepository = Substitute.For<IGoldDungeonRunRepository>();
            var progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
            var currentUserProvider = Substitute.For<ICurrentUserProvider>();

            currentUserProvider.GetAccountId().Returns(1L);
            var claimedRun = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            claimedRun.Claim(100, 1000L, 0);
            runRepository.FindByIdAsync(claimedRun.Id).Returns(claimedRun);
            SetupPlayerData(playerRepository, resourceRepository, stageRepository,
                sessionRepository, weaponRepository, skillRepository);

            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Gold);
            existingProgress.UpdateHighScore(2000L);
            progressRepository.FindByPlayerIdAndDungeonTypeAsync(1L, DungeonType.Gold)
                .Returns(existingProgress);

            var sut = BuildSut(playerRepo: playerRepository, resourceRepo: resourceRepository,
                stageRepo: stageRepository, sessionRepo: sessionRepository,
                weaponRepo: weaponRepository, skillRepo: skillRepository,
                runRepo: runRepository, progressRepo: progressRepository,
                userProvider: currentUserProvider);

            var result = await sut.ExecuteAsync(claimedRun.Id, new GoldDungeonClaimRequest(50));

            result.HighScore.Should().Be(2000L);
        }
    }
```

- [ ] **Step 3: 실패 확인**

Run: `/test GoldDungeonClaimServiceTest`
Expected: FAIL — 생성자 시그니처 불일치 및 `GoldDungeonClaimResponse` 필드 불일치로 컴파일 에러.

- [ ] **Step 4: `GoldDungeonClaimService` 구현**

`Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/GoldDungeonClaimService.cs`를 아래로 교체:

```csharp
using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class GoldDungeonClaimService : IGoldDungeonClaimService
{
    private const long GoldPerClick = 10L;
    private const int MithrilDropRatePercent = 2;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGoldDungeonRunRepository _goldDungeonRunRepository;
    private readonly IPlayerDungeonProgressRepository _playerDungeonProgressRepository;
    private readonly IRandomProvider _randomProvider;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly TimeProvider _timeProvider;

    public GoldDungeonClaimService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IGoldDungeonRunRepository goldDungeonRunRepository,
        IPlayerDungeonProgressRepository playerDungeonProgressRepository,
        IRandomProvider randomProvider,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider,
        TimeProvider timeProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _goldDungeonRunRepository = goldDungeonRunRepository;
        _playerDungeonProgressRepository = playerDungeonProgressRepository;
        _randomProvider = randomProvider;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
        _timeProvider = timeProvider;
    }

    public async Task<GoldDungeonClaimResponse> ExecuteAsync(Guid runId, GoldDungeonClaimRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var run = await _goldDungeonRunRepository.FindByIdAsync(runId)
            ?? throw new NotFoundException("골드 던전 런을 찾을 수 없습니다.");

        if (run.AccountId != accountId)
            throw new ForbiddenException("접근 권한이 없습니다.");

        if (run.IsClaimed)
        {
            var player = await _playerRepository.FindByAccountAsync(accountId)
                ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

            var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
                ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

            var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
                ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

            var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
                ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

            var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
            var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

            var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

            var existingProgress = await _playerDungeonProgressRepository.FindByPlayerIdAndDungeonTypeAsync(player.Id, DungeonType.Gold);

            var idempotentChanges = new ChangesDto(
                Gold: run.EarnedGold!.Value,
                Exp: 0,
                Sp: 0,
                Mithril: run.EarnedMithril!.Value,
                EnhancementScroll: 0,
                DungeonTickets: 0,
                LevelUps: [],
                UnlockedSkillIds: [],
                AcquiredWeaponIds: [],
                MaxStage: 0
            );

            return new GoldDungeonClaimResponse(run.Id, run.EarnedGold!.Value, run.EarnedMithril!.Value,
                existingProgress?.HighScore ?? 0, idempotentChanges, playerResponse);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now > run.ExpiresAt)
            throw new BadRequestException("골드 던전 제한 시간이 초과되었습니다.");

        if (request.Clicks > run.MaxClicks)
            throw new BadRequestException("비정상적인 클릭 횟수입니다.");

        var elapsedSeconds = Math.Clamp((now - run.StartedAt).TotalSeconds, 0, run.DurationSeconds);
        var maxAllowedClicks = (int)Math.Ceiling(elapsedSeconds * run.MaxClicks / run.DurationSeconds);
        if (request.Clicks > maxAllowedClicks)
            throw new BadRequestException("경과 시간 대비 비정상적인 클릭 횟수입니다.");

        var earnedGold = request.Clicks * GoldPerClick;
        var mithrilDropped = _randomProvider.Next(0, 100) < MithrilDropRatePercent;

        var claimPlayer = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var claimResource = await _playerResourceRepository.FindByPlayerIdAsync(claimPlayer.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var claimStage = await _playerStageRepository.FindByPlayerIdAsync(claimPlayer.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var claimSession = await _playerSessionRepository.FindByPlayerIdAsync(claimPlayer.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var claimWeapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(claimPlayer.Id);
        var claimSkills = await _playerSkillRepository.FindAllByPlayerIdAsync(claimPlayer.Id);

        claimResource.UpdateGold(claimResource.Gold + earnedGold);
        if (mithrilDropped)
            claimResource.UpdateChangeData(null, claimResource.Mithril + 1, null);

        run.Claim(request.Clicks, earnedGold, mithrilDropped ? 1 : 0);

        var progress = await _playerDungeonProgressRepository.FindByPlayerIdAndDungeonTypeAsync(claimPlayer.Id, DungeonType.Gold);
        var isNewProgress = progress is null;
        progress ??= PlayerDungeonProgress.Create(claimPlayer.Id, DungeonType.Gold);
        progress.UpdateHighScore(earnedGold);

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerResourceRepository.UpdateAsync(claimResource);
            await _goldDungeonRunRepository.UpdateAsync(run);

            if (isNewProgress)
                await _playerDungeonProgressRepository.SaveAsync(progress);
            else
                await _playerDungeonProgressRepository.UpdateAsync(progress);
        });

        var claimPlayerResponse = PlayerDataResponseBuilder.Build(claimPlayer, claimResource, claimStage, claimSession, claimWeapons, claimSkills);
        await _playerRedisRepository.SetPlayerDataAsync(accountId, claimPlayerResponse);

        var changes = new ChangesDto(
            Gold: earnedGold,
            Exp: 0,
            Sp: 0,
            Mithril: mithrilDropped ? 1 : 0,
            EnhancementScroll: 0,
            DungeonTickets: 0,
            LevelUps: [],
            UnlockedSkillIds: [],
            AcquiredWeaponIds: [],
            MaxStage: 0
        );

        return new GoldDungeonClaimResponse(run.Id, earnedGold, mithrilDropped ? 1 : 0, progress.HighScore, changes, claimPlayerResponse);
    }
}
```

- [ ] **Step 5: 통과 확인**

Run: `/test GoldDungeonClaimServiceTest`
Expected: PASS (기존 12개 + 신규 4개 = 16개)

- [ ] **Step 6: 전체 빌드 확인**

Run: `/test`
Expected: 전체 통과.

- [ ] **Step 7: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Dungeon/Dto/Response/GoldDungeonClaimResponse.cs Fantasy-server/Fantasy.Server/Domain/Dungeon/Service/GoldDungeonClaimService.cs Fantasy-server/Fantasy.Test/Dungeon/Service/GoldDungeonClaimServiceTest.cs
git commit -m "feat: 골드 던전 claim에 HighScore 갱신 적용"
```

---

## 완료 후 문서 업데이트 (커밋 범위 밖, 별도 확인)

- `docs/client-integration-guide.md`의 6.2/6.3절에 `WeaponDungeonResponse`/`BossDungeonResponse`/`GoldDungeonClaimResponse`의 신규 필드(`HighestClearedStage`, `HighScore`) 반영.
- `docs/superpowers/specs/2026-07-02-server-authority-feature-expansion-design.md`의 Phase 2 상태를 "구현 완료"로 갱신.
- 무기/보스 던전이 이제 기본 던전과 별개로 진행된다는 점을 클라이언트 팀에 공유(밸런싱 영향, 2026-07-02 확정 사항 참고).
