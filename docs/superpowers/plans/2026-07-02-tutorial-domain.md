# Tutorial 도메인 (Phase 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 튜토리얼 완료 상태를 서버에 영구 저장하고 조회/완료 처리하는 신규 `Tutorial` 도메인을 추가한다.

**Architecture:** `architecture.md`의 신규 도메인 추가 절차(Entity → Repository → Service → Controller → DI 등록)를 그대로 따른다. 튜토리얼은 성장 상태(재화/레벨/스킬/무기)에 영향을 주지 않는 순수 기록이므로 `PlayerDataResponse`는 건드리지 않고, 완료 여부 조회는 `GET /v1/dungeons/tickets`와 동일한 패턴의 전용 엔드포인트(`GET /v1/tutorials`)로 분리한다.

**Tech Stack:** ASP.NET Core Web API, EF Core(Npgsql), xUnit v3 + NSubstitute + FluentAssertions, Sqlite in-memory(repository 테스트).

## Global Constraints

- 엔티티: 모든 setter `private set`, public 생성자 대신 static `Create(...)` 팩토리, 타임스탬프는 `DateTime.UtcNow` 직접 호출(이 저장소의 기존 엔티티 전부가 이 방식이며 `TimeProvider` 주입은 서비스의 경과시간 계산에만 씀).
- EF 설정: `IEntityTypeConfiguration<T>` Fluent API만 사용, 엔티티에 데이터 애노테이션 금지. 테이블/스키마는 `snake_case`, 스키마명은 도메인 폴더명 그대로(`tutorial`).
- 서비스: 인터페이스 1개 + `ExecuteAsync` 메서드 1개. 생성자는 필드 대입만(로직 금지). 계정 식별은 항상 `ICurrentUserProvider.GetAccountId()`로만 하고 요청 바디/쿼리의 계정 식별자는 받지 않는다.
- 에러: `Gamism.SDK.Extensions.AspNetCore.Exceptions`의 `NotFoundException`/`BadRequestException`을 `throw`. `return NotFound()` 같은 컨트롤러 단축 응답 금지.
- DTO: `record` + 포지셔널 파라미터.
- 테스트: `NSubstitute`로 리포지토리 인터페이스만 모킹(`AppDbContext` 직접 모킹 금지). Repository 계층 테스트는 Sqlite in-memory + `AppDbContext` 서브클래스로 실제 EF 동작을 검증(`PlayerSkillRepositoryTests` 패턴). Arrange/Act/Assert 사이 빈 줄.
- 각 태스크가 끝나면 `/test <필터>`로 빌드+테스트를 확인한다(이 저장소는 `/test` 스킬로 빌드와 테스트를 함께 수행함).

---

### Task 1: `PlayerTutorial` 엔티티 + EF 설정 + 마이그레이션

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Entity/PlayerTutorial.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Entity/Config/PlayerTutorialConfig.cs`
- Modify: `Fantasy-server/Fantasy.Server/Global/Infrastructure/AppDbContext.cs`
- Create: `Fantasy-server/Fantasy.Server/Migrations/{timestamp}_CreateTutorialTable.cs` (자동 생성)

**Interfaces:**
- Produces: `PlayerTutorial.Create(long playerId, string tutorialId) : PlayerTutorial`, 프로퍼티 `Id(long)`, `PlayerId(long)`, `TutorialId(string)`, `CompletedAt(DateTime)`. `AppDbContext.PlayerTutorials : DbSet<PlayerTutorial>`.

- [ ] **Step 1: 엔티티 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Entity/PlayerTutorial.cs
namespace Fantasy.Server.Domain.Tutorial.Entity;

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

- [ ] **Step 2: EF Fluent 설정 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Entity/Config/PlayerTutorialConfig.cs
using Fantasy.Server.Domain.Tutorial.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Tutorial.Entity.Config;

public class PlayerTutorialConfig : IEntityTypeConfiguration<PlayerTutorial>
{
    public void Configure(EntityTypeBuilder<PlayerTutorial> builder)
    {
        builder.ToTable("player_tutorial", "tutorial");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.PlayerId)
            .IsRequired();

        builder.Property(t => t.TutorialId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.CompletedAt)
            .IsRequired();

        builder.HasIndex(t => new { t.PlayerId, t.TutorialId })
            .IsUnique();

        builder.HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(t => t.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 3: `AppDbContext`에 `DbSet` 등록**

`Fantasy-server/Fantasy.Server/Global/Infrastructure/AppDbContext.cs` 상단 using에 추가:

```csharp
using Fantasy.Server.Domain.Tutorial.Entity;
```

`Players` 관련 `DbSet` 선언들 바로 아래에 추가:

```csharp
    public DbSet<PlayerTutorial> PlayerTutorials => Set<PlayerTutorial>();
```

(`ApplyConfigurationsFromAssembly`가 이미 `OnModelCreating`에서 전체 어셈블리를 스캔하므로 `PlayerTutorialConfig`는 자동 적용된다. 추가 코드 불필요.)

- [ ] **Step 4: 빌드 확인**

Run: `/test PlayerTutorial`
Expected: 아직 테스트가 없으므로 "No test matches" 류 메시지와 함께 **빌드는 성공**해야 한다. 빌드 실패 시 using/네임스페이스 오탈자를 확인한다.

- [ ] **Step 5: 마이그레이션 생성**

Run: `/db-migrate add CreateTutorialTable`
Expected: `Fantasy-server/Fantasy.Server/Migrations/`에 `{timestamp}_CreateTutorialTable.cs`와 `.Designer.cs`가 생성되고, `Up()`에 `tutorial` 스키마 생성 + `player_tutorial` 테이블 생성 + `(player_id, tutorial_id)` unique index가 포함되어 있는지 확인한다.

- [ ] **Step 6: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Tutorial/Entity Fantasy-server/Fantasy.Server/Global/Infrastructure/AppDbContext.cs Fantasy-server/Fantasy.Server/Migrations
git commit -m "feat: PlayerTutorial 엔티티 및 마이그레이션 추가"
```

---

### Task 2: `IPlayerTutorialRepository` / `PlayerTutorialRepository`

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Repository/Interface/IPlayerTutorialRepository.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Repository/PlayerTutorialRepository.cs`
- Test: `Fantasy-server/Fantasy.Test/Tutorial/Repository/PlayerTutorialRepositoryTests.cs`

**Interfaces:**
- Consumes: `PlayerTutorial.Create(long, string)`, `AppDbContext.PlayerTutorials` (Task 1).
- Produces: `IPlayerTutorialRepository.FindAllByPlayerIdAsync(long playerId) : Task<List<PlayerTutorial>>`, `FindByPlayerIdAndTutorialIdAsync(long playerId, string tutorialId) : Task<PlayerTutorial?>`, `SaveAsync(PlayerTutorial tutorial) : Task<PlayerTutorial>`.

- [ ] **Step 1: 실패하는 리포지토리 테스트 작성**

```csharp
// Fantasy-server/Fantasy.Test/Tutorial/Repository/PlayerTutorialRepositoryTests.cs
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository;
using Fantasy.Server.Global.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fantasy.Test.Tutorial.Repository;

public class PlayerTutorialRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly PlayerTutorialRepository _sut;

    public PlayerTutorialRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestAppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new PlayerTutorialRepository(_dbContext);
    }

    [Fact]
    public async Task SaveAsync_저장한_레코드를_FindAllByPlayerIdAsync로_조회할_수_있다()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _sut.SaveAsync(PlayerTutorial.Create(1L, "tutorial_first_game_start"));

        List<PlayerTutorial> saved = await _dbContext.PlayerTutorials
            .Where(t => t.PlayerId == 1L)
            .ToListAsync(cancellationToken);

        saved.Should().HaveCount(1);
        saved[0].TutorialId.Should().Be("tutorial_first_game_start");
    }

    [Fact]
    public async Task FindByPlayerIdAndTutorialIdAsync_존재하지_않으면_null을_반환한다()
    {
        var result = await _sut.FindByPlayerIdAndTutorialIdAsync(1L, "tutorial_first_dungeon");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByPlayerIdAndTutorialIdAsync_존재하면_반환한다()
    {
        await _sut.SaveAsync(PlayerTutorial.Create(1L, "tutorial_first_dungeon"));

        var result = await _sut.FindByPlayerIdAndTutorialIdAsync(1L, "tutorial_first_dungeon");

        result.Should().NotBeNull();
        result!.PlayerId.Should().Be(1L);
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

            modelBuilder.Entity<PlayerTutorial>(entity =>
            {
                entity.ToTable("player_tutorial");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).ValueGeneratedOnAdd();
                entity.Property(t => t.PlayerId).IsRequired();
                entity.Property(t => t.TutorialId).IsRequired();
                entity.Property(t => t.CompletedAt).IsRequired();
                entity.HasIndex(t => new { t.PlayerId, t.TutorialId }).IsUnique();
            });
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: `/test PlayerTutorialRepositoryTests`
Expected: FAIL — `PlayerTutorialRepository`/`IPlayerTutorialRepository`가 없어 컴파일 에러.

- [ ] **Step 3: 인터페이스 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Repository/Interface/IPlayerTutorialRepository.cs
using Fantasy.Server.Domain.Tutorial.Entity;

namespace Fantasy.Server.Domain.Tutorial.Repository.Interface;

public interface IPlayerTutorialRepository
{
    Task<List<PlayerTutorial>> FindAllByPlayerIdAsync(long playerId);
    Task<PlayerTutorial?> FindByPlayerIdAndTutorialIdAsync(long playerId, string tutorialId);
    Task<PlayerTutorial> SaveAsync(PlayerTutorial tutorial);
}
```

- [ ] **Step 4: 구현 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Repository/PlayerTutorialRepository.cs
using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Tutorial.Repository;

public class PlayerTutorialRepository : IPlayerTutorialRepository
{
    private readonly AppDbContext _db;

    public PlayerTutorialRepository(AppDbContext db) => _db = db;

    public async Task<List<PlayerTutorial>> FindAllByPlayerIdAsync(long playerId)
        => await _db.PlayerTutorials
            .AsNoTracking()
            .Where(t => t.PlayerId == playerId)
            .ToListAsync();

    public async Task<PlayerTutorial?> FindByPlayerIdAndTutorialIdAsync(long playerId, string tutorialId)
        => await _db.PlayerTutorials
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PlayerId == playerId && t.TutorialId == tutorialId);

    public async Task<PlayerTutorial> SaveAsync(PlayerTutorial tutorial)
    {
        await _db.PlayerTutorials.AddAsync(tutorial);
        await _db.SaveChangesAsync();
        return tutorial;
    }
}
```

- [ ] **Step 5: 통과 확인**

Run: `/test PlayerTutorialRepositoryTests`
Expected: PASS (3 tests)

- [ ] **Step 6: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Tutorial/Repository Fantasy-server/Fantasy.Test/Tutorial/Repository
git commit -m "feat: PlayerTutorialRepository 추가"
```

---

### Task 3: 튜토리얼 완료 처리 (`ICompleteTutorialService`)

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Constant/TutorialIds.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Dto/Response/TutorialCompleteResponse.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Service/Interface/ICompleteTutorialService.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Service/CompleteTutorialService.cs`
- Test: `Fantasy-server/Fantasy.Test/Tutorial/Service/CompleteTutorialServiceTest.cs`

**Interfaces:**
- Consumes: `IPlayerRepository.FindByAccountAsync(long) : Task<Player?>` (기존), `IPlayerTutorialRepository`(Task 2), `ICurrentUserProvider.GetAccountId() : long`(기존).
- Produces: `ICompleteTutorialService.ExecuteAsync(string tutorialId) : Task<TutorialCompleteResponse>`. `TutorialCompleteResponse(string TutorialId, bool WasAlreadyCompleted, DateTime CompletedAt)`.

- [ ] **Step 1: 실패하는 서비스 테스트 작성**

```csharp
// Fantasy-server/Fantasy.Test/Tutorial/Service/CompleteTutorialServiceTest.cs
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Service;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Tutorial.Service;

public class CompleteTutorialServiceTest
{
    private static CompleteTutorialService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerTutorialRepository? tutorialRepo = null,
        ICurrentUserProvider? userProvider = null) =>
        new(
            playerRepo ?? Substitute.For<IPlayerRepository>(),
            tutorialRepo ?? Substitute.For<IPlayerTutorialRepository>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>());

    public class 화이트리스트에_없는_ID일_때
    {
        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var sut = BuildSut();

            await ((Func<Task>)(() => sut.ExecuteAsync("tutorial_unknown"))).Should()
                .ThrowAsync<BadRequestException>();
        }
    }

    public class 플레이어가_없을_때
    {
        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var playerRepo = Substitute.For<IPlayerRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            playerRepo.FindByAccountAsync(Arg.Any<long>()).Returns((PlayerEntity?)null);

            var sut = BuildSut(playerRepo: playerRepo, userProvider: userProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync("tutorial_first_game_start"))).Should()
                .ThrowAsync<NotFoundException>();
        }
    }

    public class 이미_완료된_튜토리얼일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerTutorialRepository _tutorialRepository = Substitute.For<IPlayerTutorialRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly PlayerTutorial _existing = PlayerTutorial.Create(1L, "tutorial_first_game_start");

        public 이미_완료된_튜토리얼일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _tutorialRepository.FindByPlayerIdAndTutorialIdAsync(1L, "tutorial_first_game_start")
                .Returns(_existing);
        }

        [Fact]
        public async Task WasAlreadyCompleted가_true로_반환된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, tutorialRepo: _tutorialRepository,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync("tutorial_first_game_start");

            result.WasAlreadyCompleted.Should().BeTrue();
            result.CompletedAt.Should().Be(_existing.CompletedAt);
        }

        [Fact]
        public async Task SaveAsync가_호출되지_않는다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, tutorialRepo: _tutorialRepository,
                userProvider: _currentUserProvider);

            await sut.ExecuteAsync("tutorial_first_game_start");

            await _tutorialRepository.DidNotReceive().SaveAsync(Arg.Any<PlayerTutorial>());
        }
    }

    public class 신규_완료일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerTutorialRepository _tutorialRepository = Substitute.For<IPlayerTutorialRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 신규_완료일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _tutorialRepository.FindByPlayerIdAndTutorialIdAsync(1L, "tutorial_first_game_start")
                .Returns((PlayerTutorial?)null);
            _tutorialRepository.SaveAsync(Arg.Any<PlayerTutorial>())
                .Returns(callInfo => callInfo.Arg<PlayerTutorial>());
        }

        [Fact]
        public async Task WasAlreadyCompleted가_false로_반환된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, tutorialRepo: _tutorialRepository,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync("tutorial_first_game_start");

            result.WasAlreadyCompleted.Should().BeFalse();
            result.TutorialId.Should().Be("tutorial_first_game_start");
        }

        [Fact]
        public async Task SaveAsync가_호출된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, tutorialRepo: _tutorialRepository,
                userProvider: _currentUserProvider);

            await sut.ExecuteAsync("tutorial_first_game_start");

            await _tutorialRepository.Received(1).SaveAsync(Arg.Any<PlayerTutorial>());
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: `/test CompleteTutorialServiceTest`
Expected: FAIL — `CompleteTutorialService`/`TutorialIds`/`TutorialCompleteResponse`가 없어 컴파일 에러.

- [ ] **Step 3: 화이트리스트 상수 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Constant/TutorialIds.cs
namespace Fantasy.Server.Domain.Tutorial.Constant;

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

- [ ] **Step 4: 응답 DTO 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Dto/Response/TutorialCompleteResponse.cs
namespace Fantasy.Server.Domain.Tutorial.Dto.Response;

public record TutorialCompleteResponse(string TutorialId, bool WasAlreadyCompleted, DateTime CompletedAt);
```

- [ ] **Step 5: 서비스 인터페이스 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Service/Interface/ICompleteTutorialService.cs
using Fantasy.Server.Domain.Tutorial.Dto.Response;

namespace Fantasy.Server.Domain.Tutorial.Service.Interface;

public interface ICompleteTutorialService
{
    Task<TutorialCompleteResponse> ExecuteAsync(string tutorialId);
}
```

- [ ] **Step 6: 서비스 구현 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Service/CompleteTutorialService.cs
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Constant;
using Fantasy.Server.Domain.Tutorial.Dto.Response;
using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Tutorial.Service;

public class CompleteTutorialService : ICompleteTutorialService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerTutorialRepository _playerTutorialRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public CompleteTutorialService(
        IPlayerRepository playerRepository,
        IPlayerTutorialRepository playerTutorialRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerTutorialRepository = playerTutorialRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<TutorialCompleteResponse> ExecuteAsync(string tutorialId)
    {
        if (!TutorialIds.All.Contains(tutorialId))
            throw new BadRequestException("존재하지 않는 튜토리얼입니다.");

        long accountId = _currentUserProvider.GetAccountId();

        PlayerEntity player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어를 찾을 수 없습니다.");

        PlayerTutorial? existing = await _playerTutorialRepository.FindByPlayerIdAndTutorialIdAsync(player.Id, tutorialId);
        if (existing != null)
            return new TutorialCompleteResponse(tutorialId, true, existing.CompletedAt);

        PlayerTutorial created = await _playerTutorialRepository.SaveAsync(PlayerTutorial.Create(player.Id, tutorialId));
        return new TutorialCompleteResponse(tutorialId, false, created.CompletedAt);
    }
}
```

- [ ] **Step 7: 통과 확인**

Run: `/test CompleteTutorialServiceTest`
Expected: PASS (6 tests)

- [ ] **Step 8: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Tutorial/Constant Fantasy-server/Fantasy.Server/Domain/Tutorial/Dto Fantasy-server/Fantasy.Server/Domain/Tutorial/Service Fantasy-server/Fantasy.Test/Tutorial/Service/CompleteTutorialServiceTest.cs
git commit -m "feat: CompleteTutorialService 추가"
```

---

### Task 4: 완료 목록 조회 (`IGetCompletedTutorialsService`)

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Dto/Response/CompletedTutorialsResponse.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Service/Interface/IGetCompletedTutorialsService.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Service/GetCompletedTutorialsService.cs`
- Test: `Fantasy-server/Fantasy.Test/Tutorial/Service/GetCompletedTutorialsServiceTest.cs`

**Interfaces:**
- Consumes: `IPlayerRepository.FindByAccountAsync`, `IPlayerTutorialRepository.FindAllByPlayerIdAsync`(Task 2), `ICurrentUserProvider.GetAccountId()`.
- Produces: `IGetCompletedTutorialsService.ExecuteAsync() : Task<CompletedTutorialsResponse>`. `CompletedTutorialsResponse(List<string> CompletedTutorialIds)`.

- [ ] **Step 1: 실패하는 서비스 테스트 작성**

```csharp
// Fantasy-server/Fantasy.Test/Tutorial/Service/GetCompletedTutorialsServiceTest.cs
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Service;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Tutorial.Service;

public class GetCompletedTutorialsServiceTest
{
    public class 플레이어가_없을_때
    {
        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var playerRepo = Substitute.For<IPlayerRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            playerRepo.FindByAccountAsync(Arg.Any<long>()).Returns((PlayerEntity?)null);

            var sut = new GetCompletedTutorialsService(
                playerRepo, Substitute.For<IPlayerTutorialRepository>(), userProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync())).Should()
                .ThrowAsync<NotFoundException>();
        }
    }

    public class 완료한_튜토리얼이_있을_때
    {
        [Fact]
        public async Task 완료_목록이_반환된다()
        {
            var playerRepo = Substitute.For<IPlayerRepository>();
            var tutorialRepo = Substitute.For<IPlayerTutorialRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            playerRepo.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            tutorialRepo.FindAllByPlayerIdAsync(1L).Returns(
            [
                PlayerTutorial.Create(1L, "tutorial_first_game_start"),
                PlayerTutorial.Create(1L, "tutorial_first_dungeon")
            ]);

            var sut = new GetCompletedTutorialsService(playerRepo, tutorialRepo, userProvider);

            var result = await sut.ExecuteAsync();

            result.CompletedTutorialIds.Should().BeEquivalentTo(
                ["tutorial_first_game_start", "tutorial_first_dungeon"]);
        }
    }

    public class 완료한_튜토리얼이_없을_때
    {
        [Fact]
        public async Task 빈_목록이_반환된다()
        {
            var playerRepo = Substitute.For<IPlayerRepository>();
            var tutorialRepo = Substitute.For<IPlayerTutorialRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            playerRepo.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            tutorialRepo.FindAllByPlayerIdAsync(1L).Returns([]);

            var sut = new GetCompletedTutorialsService(playerRepo, tutorialRepo, userProvider);

            var result = await sut.ExecuteAsync();

            result.CompletedTutorialIds.Should().BeEmpty();
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: `/test GetCompletedTutorialsServiceTest`
Expected: FAIL — `GetCompletedTutorialsService`/`CompletedTutorialsResponse`가 없어 컴파일 에러.

- [ ] **Step 3: 응답 DTO 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Dto/Response/CompletedTutorialsResponse.cs
namespace Fantasy.Server.Domain.Tutorial.Dto.Response;

public record CompletedTutorialsResponse(List<string> CompletedTutorialIds);
```

- [ ] **Step 4: 서비스 인터페이스 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Service/Interface/IGetCompletedTutorialsService.cs
using Fantasy.Server.Domain.Tutorial.Dto.Response;

namespace Fantasy.Server.Domain.Tutorial.Service.Interface;

public interface IGetCompletedTutorialsService
{
    Task<CompletedTutorialsResponse> ExecuteAsync();
}
```

- [ ] **Step 5: 서비스 구현 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Service/GetCompletedTutorialsService.cs
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Dto.Response;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Tutorial.Service;

public class GetCompletedTutorialsService : IGetCompletedTutorialsService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerTutorialRepository _playerTutorialRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetCompletedTutorialsService(
        IPlayerRepository playerRepository,
        IPlayerTutorialRepository playerTutorialRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerTutorialRepository = playerTutorialRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<CompletedTutorialsResponse> ExecuteAsync()
    {
        long accountId = _currentUserProvider.GetAccountId();

        PlayerEntity player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어를 찾을 수 없습니다.");

        List<Entity.PlayerTutorial> tutorials = await _playerTutorialRepository.FindAllByPlayerIdAsync(player.Id);
        return new CompletedTutorialsResponse(tutorials.Select(t => t.TutorialId).ToList());
    }
}
```

- [ ] **Step 6: 통과 확인**

Run: `/test GetCompletedTutorialsServiceTest`
Expected: PASS (3 tests)

- [ ] **Step 7: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Tutorial/Dto Fantasy-server/Fantasy.Server/Domain/Tutorial/Service Fantasy-server/Fantasy.Test/Tutorial/Service/GetCompletedTutorialsServiceTest.cs
git commit -m "feat: GetCompletedTutorialsService 추가"
```

---

### Task 5: `TutorialController` + DI 등록 + 라우트 테스트

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Controller/TutorialController.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Tutorial/Config/TutorialServiceConfig.cs`
- Modify: `Fantasy-server/Fantasy.Server/Program.cs`
- Test: `Fantasy-server/Fantasy.Test/Tutorial/Controller/TutorialControllerRouteTests.cs`

**Interfaces:**
- Consumes: `ICompleteTutorialService`(Task 3), `IGetCompletedTutorialsService`(Task 4), `IPlayerTutorialRepository`/`PlayerTutorialRepository`(Task 2).
- Produces: `GET /v1/tutorials`, `POST /v1/tutorials/{tutorialId}/complete` 라우트. `AddTutorialServices()` DI 확장 메서드.

- [ ] **Step 1: 실패하는 라우트 테스트 작성**

```csharp
// Fantasy-server/Fantasy.Test/Tutorial/Controller/TutorialControllerRouteTests.cs
using System.Reflection;
using Fantasy.Server.Domain.Tutorial.Controller;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Fantasy.Test.Tutorial.Controller;

public class TutorialControllerRouteTests
{
    private static readonly MethodInfo[] Actions = typeof(TutorialController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    [Fact]
    public void TutorialController는_GET과_POST만_노출한다()
    {
        var httpMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct();

        httpMethods.Should().BeEquivalentTo(["GET", "POST"]);
    }

    [Fact]
    public void POST_경로_템플릿은_tutorialId_complete이다()
    {
        var templates = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Where(a => a.HttpMethods.Contains("POST"))
            .Select(a => a.Template);

        templates.Should().BeEquivalentTo(["{tutorialId}/complete"]);
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: `/test TutorialControllerRouteTests`
Expected: FAIL — `TutorialController`가 없어 컴파일 에러.

- [ ] **Step 3: 컨트롤러 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Controller/TutorialController.cs
using Fantasy.Server.Domain.Tutorial.Dto.Response;
using Fantasy.Server.Domain.Tutorial.Service.Interface;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Tutorial.Controller;

[ApiController]
[Route("v1/tutorials")]
[Authorize]
[EnableRateLimiting("game")]
public class TutorialController : ControllerBase
{
    private readonly ICompleteTutorialService _completeTutorialService;
    private readonly IGetCompletedTutorialsService _getCompletedTutorialsService;

    public TutorialController(
        ICompleteTutorialService completeTutorialService,
        IGetCompletedTutorialsService getCompletedTutorialsService)
    {
        _completeTutorialService = completeTutorialService;
        _getCompletedTutorialsService = getCompletedTutorialsService;
    }

    [HttpGet]
    public async Task<CommonApiResponse<CompletedTutorialsResponse>> Get()
    {
        var result = await _getCompletedTutorialsService.ExecuteAsync();
        return CommonApiResponse.Success("완료한 튜토리얼 목록을 조회했습니다.", result);
    }

    [HttpPost("{tutorialId}/complete")]
    public async Task<CommonApiResponse<TutorialCompleteResponse>> Complete([FromRoute] string tutorialId)
    {
        var result = await _completeTutorialService.ExecuteAsync(tutorialId);
        return CommonApiResponse.Success("튜토리얼 완료가 처리되었습니다.", result);
    }
}
```

- [ ] **Step 4: DI 등록 확장 메서드 작성**

```csharp
// Fantasy-server/Fantasy.Server/Domain/Tutorial/Config/TutorialServiceConfig.cs
using Fantasy.Server.Domain.Tutorial.Repository;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Service;
using Fantasy.Server.Domain.Tutorial.Service.Interface;

namespace Fantasy.Server.Domain.Tutorial.Config;

public static class TutorialServiceConfig
{
    public static IServiceCollection AddTutorialServices(this IServiceCollection services)
    {
        services.AddScoped<IPlayerTutorialRepository, PlayerTutorialRepository>();
        services.AddScoped<ICompleteTutorialService, CompleteTutorialService>();
        services.AddScoped<IGetCompletedTutorialsService, GetCompletedTutorialsService>();

        return services;
    }
}
```

- [ ] **Step 5: `Program.cs`에 등록**

`Fantasy-server/Fantasy.Server/Program.cs`의 using 목록에 추가:

```csharp
using Fantasy.Server.Domain.Tutorial.Config;
```

`builder.Services.AddPlayerServices();` 바로 아래 줄에 추가:

```csharp
builder.Services.AddTutorialServices();
```

- [ ] **Step 6: 통과 확인**

Run: `/test`
Expected: 전체 빌드 성공 + 전체 테스트 PASS (기존 테스트 포함, 회귀 없음).

- [ ] **Step 7: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Tutorial/Controller Fantasy-server/Fantasy.Server/Domain/Tutorial/Config Fantasy-server/Fantasy.Server/Program.cs Fantasy-server/Fantasy.Test/Tutorial/Controller
git commit -m "feat: TutorialController 및 DI 등록 추가"
```

---

## 완료 후 문서 업데이트 (커밋 범위 밖, 별도 확인)

- `docs/client-integration-guide.md`에 `GET /v1/tutorials`, `POST /v1/tutorials/{tutorialId}/complete` 표 추가.
- `docs/superpowers/specs/2026-07-02-server-authority-feature-expansion-design.md`의 Phase 1 상태를 "구현 완료"로 갱신.
