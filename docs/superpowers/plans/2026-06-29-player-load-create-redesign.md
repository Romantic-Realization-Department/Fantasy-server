# Player 로드/생성 흐름 재설계 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `POST /v1/player/init`의 생성·로드 겸용을 `GET /v1/player`(로드) + `POST /v1/player`(생성)로 분리해 모호함을 제거한다.

**Architecture:** 기존 `InitPlayerService`를 use-case 컨벤션에 맞춰 `GetPlayerService`(로드)와 `CreatePlayerService`(생성) 두 서비스로 분리한다. 컨트롤러는 GET/POST 두 액션으로 노출하고, DTO·DI·테스트·문서를 새 계약에 맞춰 갱신한다. `loadout`·`skill/unlock`은 변경하지 않는다.

**Tech Stack:** .NET 10 / ASP.NET Core, EF Core 10, StackExchange.Redis, xUnit v3, NSubstitute, FluentAssertions, Gamism.SDK.

## Global Constraints

- 에러는 항상 `throw` (SDK 예외: `NotFoundException`→404, `ConflictException`→409). `return NotFound()` 금지.
- DTO는 `record` + 위치 파라미터 + `DataAnnotations`.
- 서비스: 인터페이스 1개 + `ExecuteAsync` 1개. 의존성은 생성자 주입 → `private readonly` 필드.
- Repository만 `AppDbContext`/Redis 접근. 서비스는 repository 인터페이스만 의존.
- Redis 읽기: 캐시 우선 → 미스 시 DB → 캐시 SET.
- 테스트: 클래스명 `{Service}Test`, NSubstitute 모킹, FluentAssertions, AAA 구조. `AppDbContext` 직접 모킹 금지(repository 인터페이스 모킹).
- 모든 I/O는 `async Task`/`async Task<T>`, `.Result`/`.Wait()` 금지.
- `.cs` 변경 후 `/test`로 빌드+테스트 통과 확인. 빌드 실패 시 수정 후 재실행.
- 작업 디렉터리: 솔루션은 `Fantasy-server/` 아래. 전체 검증은 프로젝트 표준 `/test` 스킬 사용, 단일 테스트는 `dotnet test --filter "FullyQualifiedName~<name>"` (Fantasy-server 디렉터리에서 실행).

---

### Task 1: GetPlayerService (로드 / GET)

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Service/Interface/IGetPlayerService.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Service/GetPlayerService.cs`
- Test: `Fantasy-server/Fantasy.Test/Player/Service/GetPlayerServiceTest.cs`

**Interfaces:**
- Consumes: `IPlayerRepository.FindByAccountAsync(long): Task<Player?>`, `IPlayerResourceRepository.FindByPlayerIdAsync(long)`, `IPlayerStageRepository.FindByPlayerIdAsync(long)`, `IPlayerSessionRepository.FindByPlayerIdAsync(long)`, `IPlayerWeaponRepository.FindAllByPlayerIdAsync(long)`, `IPlayerSkillRepository.FindAllByPlayerIdAsync(long)`, `IPlayerRedisRepository.GetPlayerDataAsync(long): Task<PlayerDataResponse?>` / `SetPlayerDataAsync(long, PlayerDataResponse)`, `ICurrentUserProvider.GetAccountId(): long`, `PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills): PlayerDataResponse`.
- Produces: `IGetPlayerService.ExecuteAsync(): Task<PlayerDataResponse>` — 캐시/DB에서 현재 계정 플레이어 로드, 없으면 `NotFoundException`.

- [ ] **Step 1: 인터페이스 작성**

`Fantasy-server/Fantasy.Server/Domain/Player/Service/Interface/IGetPlayerService.cs`:
```csharp
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface IGetPlayerService
{
    Task<PlayerDataResponse> ExecuteAsync();
}
```

- [ ] **Step 2: 실패하는 테스트 작성**

`Fantasy-server/Fantasy.Test/Player/Service/GetPlayerServiceTest.cs`:
```csharp
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;
using PlayerResourceEntity = Fantasy.Server.Domain.Player.Entity.PlayerResource;

namespace Fantasy.Test.Player.Service;

public class GetPlayerServiceTest
{
    public class 캐시가_있을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GetPlayerService _sut;
        private readonly PlayerDataResponse _cached = new(
            JobType.Warrior, 5L, 3L, null, [], 1000L, 2000L, 0L, 0L, 0L, [], []);

        public 캐시가_있을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L).Returns(_cached);

            _sut = new GetPlayerService(
                _playerRepository,
                _playerResourceRepository,
                _playerStageRepository,
                _playerSessionRepository,
                _playerWeaponRepository,
                _playerSkillRepository,
                _playerRedisRepository,
                _currentUserProvider);
        }

        [Fact]
        public async Task 캐시된_데이터가_반환된다()
        {
            var data = await _sut.ExecuteAsync();

            data.Should().Be(_cached);
        }

        [Fact]
        public async Task DB_조회가_발생하지_않는다()
        {
            await _sut.ExecuteAsync();

            await _playerRepository.DidNotReceive().FindByAccountAsync(Arg.Any<long>());
        }
    }

    public class 플레이어가_있을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GetPlayerService _sut;

        public 플레이어가_있을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L).Returns((PlayerDataResponse?)null);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResourceEntity.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            _sut = new GetPlayerService(
                _playerRepository,
                _playerResourceRepository,
                _playerStageRepository,
                _playerSessionRepository,
                _playerWeaponRepository,
                _playerSkillRepository,
                _playerRedisRepository,
                _currentUserProvider);
        }

        [Fact]
        public async Task 기존_데이터가_반환된다()
        {
            var data = await _sut.ExecuteAsync();

            data.JobType.Should().Be(JobType.Warrior);
            data.Level.Should().Be(1L);
        }

        [Fact]
        public async Task Redis에_플레이어_데이터가_캐싱된다()
        {
            await _sut.ExecuteAsync();

            await _playerRedisRepository.Received(1).SetPlayerDataAsync(1L, Arg.Any<PlayerDataResponse>());
        }
    }

    public class 플레이어가_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GetPlayerService _sut;

        public 플레이어가_없을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L).Returns((PlayerDataResponse?)null);
            _playerRepository.FindByAccountAsync(1L).Returns((PlayerEntity?)null);

            _sut = new GetPlayerService(
                _playerRepository,
                _playerResourceRepository,
                _playerStageRepository,
                _playerSessionRepository,
                _playerWeaponRepository,
                _playerSkillRepository,
                _playerRedisRepository,
                _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            Func<Task> act = () => _sut.ExecuteAsync();

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
```

- [ ] **Step 3: 테스트가 실패(컴파일 실패)하는지 확인**

Run: `dotnet test --filter "FullyQualifiedName~GetPlayerServiceTest"` (Fantasy-server 디렉터리)
Expected: 빌드 실패 — `GetPlayerService` 타입이 존재하지 않음.

- [ ] **Step 4: GetPlayerService 구현**

`Fantasy-server/Fantasy.Server/Domain/Player/Service/GetPlayerService.cs`:
```csharp
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Service;

public class GetPlayerService : IGetPlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetPlayerService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<PlayerDataResponse> ExecuteAsync()
    {
        long accountId = _currentUserProvider.GetAccountId();

        PlayerDataResponse? cached = await _playerRedisRepository.GetPlayerDataAsync(accountId);
        if (cached != null)
            return cached;

        PlayerEntity player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어를 찾을 수 없습니다.");

        PlayerResource resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");
        PlayerStage stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");
        PlayerSession session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        List<Entity.PlayerWeapon> weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        List<Entity.PlayerSkill> skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        PlayerDataResponse response = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

        await _playerRedisRepository.SetPlayerDataAsync(accountId, response);
        return response;
    }
}
```

- [ ] **Step 5: 테스트 통과 확인**

Run: `dotnet test --filter "FullyQualifiedName~GetPlayerServiceTest"`
Expected: PASS (5 tests).

- [ ] **Step 6: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Player/Service/Interface/IGetPlayerService.cs \
        Fantasy-server/Fantasy.Server/Domain/Player/Service/GetPlayerService.cs \
        Fantasy-server/Fantasy.Test/Player/Service/GetPlayerServiceTest.cs
git commit -m "feat: GetPlayerService 추가 (플레이어 로드, 없으면 404)"
```

---

### Task 2: CreatePlayerService (생성 / POST) + CreatePlayerRequest

**Files:**
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Dto/Request/CreatePlayerRequest.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Service/Interface/ICreatePlayerService.cs`
- Create: `Fantasy-server/Fantasy.Server/Domain/Player/Service/CreatePlayerService.cs`
- Test: `Fantasy-server/Fantasy.Test/Player/Service/CreatePlayerServiceTest.cs`

**Interfaces:**
- Consumes: `IPlayerRepository.FindByAccountAsync(long): Task<Player?>` / `SaveAsync(Player): Task<Player>`, `IPlayerResourceRepository.SaveAsync(PlayerResource)`, `IPlayerStageRepository.SaveAsync(PlayerStage)`, `IPlayerSessionRepository.SaveAsync(PlayerSession)`, `IPlayerWeaponRepository.FindAllByPlayerIdAsync(long)`, `IPlayerSkillRepository.FindAllByPlayerIdAsync(long)`, `IPlayerRedisRepository.SetPlayerDataAsync(long, PlayerDataResponse)`, `ICurrentUserProvider.GetAccountId(): long`, `IAppDbTransactionRunner.ExecuteAsync<T>(Func<Task<T>>): Task<T>`, `Player.Create(long accountId, JobType)`, `PlayerResource.Create(long playerId)`, `PlayerStage.Create(long playerId)`, `PlayerSession.Create(long playerId)`, `PlayerDataResponseBuilder.Build(...)`.
- Produces: `CreatePlayerRequest(JobType JobType)` record. `ICreatePlayerService.ExecuteAsync(CreatePlayerRequest): Task<PlayerDataResponse>` — 신규 생성 후 데이터 반환, 이미 존재하면 `ConflictException`.

- [ ] **Step 1: DTO 작성**

`Fantasy-server/Fantasy.Server/Domain/Player/Dto/Request/CreatePlayerRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record CreatePlayerRequest(
    [Required] JobType JobType
);
```

- [ ] **Step 2: 인터페이스 작성**

`Fantasy-server/Fantasy.Server/Domain/Player/Service/Interface/ICreatePlayerService.cs`:
```csharp
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface ICreatePlayerService
{
    Task<PlayerDataResponse> ExecuteAsync(CreatePlayerRequest request);
}
```

- [ ] **Step 3: 실패하는 테스트 작성**

`Fantasy-server/Fantasy.Test/Player/Service/CreatePlayerServiceTest.cs`:
```csharp
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;
using PlayerResourceEntity = Fantasy.Server.Domain.Player.Entity.PlayerResource;

namespace Fantasy.Test.Player.Service;

public class CreatePlayerServiceTest
{
    public class 신규_플레이어일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IAppDbTransactionRunner _transactionRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly CreatePlayerService _sut;
        private readonly CreatePlayerRequest _request = new(JobType.Warrior);

        public 신규_플레이어일_때()
        {
            _transactionRunner.ExecuteAsync(Arg.Any<Func<Task<(PlayerEntity Player, PlayerResource Resource, PlayerStage Stage, PlayerSession Session)>>>())
                .Returns(callInfo => callInfo.Arg<Func<Task<(PlayerEntity, PlayerResource, PlayerStage, PlayerSession)>>>()());
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns((PlayerEntity?)null);
            _playerRepository.SaveAsync(Arg.Any<PlayerEntity>()).Returns(callInfo => callInfo.Arg<PlayerEntity>());
            _playerResourceRepository.SaveAsync(Arg.Any<PlayerResourceEntity>()).Returns(callInfo => callInfo.Arg<PlayerResourceEntity>());
            _playerStageRepository.SaveAsync(Arg.Any<PlayerStage>()).Returns(callInfo => callInfo.Arg<PlayerStage>());
            _playerSessionRepository.SaveAsync(Arg.Any<PlayerSession>()).Returns(callInfo => callInfo.Arg<PlayerSession>());
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            _sut = new CreatePlayerService(
                _playerRepository,
                _playerResourceRepository,
                _playerStageRepository,
                _playerSessionRepository,
                _playerWeaponRepository,
                _playerSkillRepository,
                _playerRedisRepository,
                _currentUserProvider,
                _transactionRunner);
        }

        [Fact]
        public async Task 트랜잭션_안에서_플레이어를_생성한다()
        {
            await _sut.ExecuteAsync(_request);

            await _transactionRunner.Received(1)
                .ExecuteAsync(Arg.Any<Func<Task<(PlayerEntity Player, PlayerResource Resource, PlayerStage Stage, PlayerSession Session)>>>());
        }

        [Fact]
        public async Task 플레이어_데이터가_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRepository.Received(1).SaveAsync(Arg.Any<PlayerEntity>());
        }

        [Fact]
        public async Task 생성된_데이터가_반환된다()
        {
            var data = await _sut.ExecuteAsync(_request);

            data.JobType.Should().Be(JobType.Warrior);
        }

        [Fact]
        public async Task Redis에_플레이어_데이터가_캐싱된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRedisRepository.Received(1).SetPlayerDataAsync(1L, Arg.Any<PlayerDataResponse>());
        }
    }

    public class 이미_플레이어가_있을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IAppDbTransactionRunner _transactionRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly CreatePlayerService _sut;
        private readonly CreatePlayerRequest _request = new(JobType.Mage);

        public 이미_플레이어가_있을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));

            _sut = new CreatePlayerService(
                _playerRepository,
                _playerResourceRepository,
                _playerStageRepository,
                _playerSessionRepository,
                _playerWeaponRepository,
                _playerSkillRepository,
                _playerRedisRepository,
                _currentUserProvider,
                _transactionRunner);
        }

        [Fact]
        public async Task ConflictException이_발생한다()
        {
            Func<Task> act = () => _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task 새_플레이어가_저장되지_않는다()
        {
            await Assert.ThrowsAsync<ConflictException>(() => _sut.ExecuteAsync(_request));

            await _playerRepository.DidNotReceive().SaveAsync(Arg.Any<PlayerEntity>());
        }
    }
}
```

- [ ] **Step 4: 테스트가 실패(컴파일 실패)하는지 확인**

Run: `dotnet test --filter "FullyQualifiedName~CreatePlayerServiceTest"`
Expected: 빌드 실패 — `CreatePlayerService` 타입이 존재하지 않음.

- [ ] **Step 5: CreatePlayerService 구현**

`Fantasy-server/Fantasy.Server/Domain/Player/Service/CreatePlayerService.cs`:
```csharp
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Service;

public class CreatePlayerService : ICreatePlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IAppDbTransactionRunner _transactionRunner;

    public CreatePlayerService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider,
        IAppDbTransactionRunner transactionRunner)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
        _transactionRunner = transactionRunner;
    }

    public async Task<PlayerDataResponse> ExecuteAsync(CreatePlayerRequest request)
    {
        long accountId = _currentUserProvider.GetAccountId();

        PlayerEntity? existing = await _playerRepository.FindByAccountAsync(accountId);
        if (existing != null)
            throw new ConflictException("이미 플레이어가 존재합니다.");

        var created = await _transactionRunner.ExecuteAsync(async () =>
        {
            PlayerEntity newPlayer = PlayerEntity.Create(accountId, request.JobType);
            await _playerRepository.SaveAsync(newPlayer);

            PlayerResource resource = PlayerResource.Create(newPlayer.Id);
            await _playerResourceRepository.SaveAsync(resource);

            PlayerStage stage = PlayerStage.Create(newPlayer.Id);
            await _playerStageRepository.SaveAsync(stage);

            PlayerSession session = PlayerSession.Create(newPlayer.Id);
            await _playerSessionRepository.SaveAsync(session);

            return (Player: newPlayer, Resource: resource, Stage: stage, Session: session);
        });

        List<Entity.PlayerWeapon> weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(created.Player.Id);
        List<Entity.PlayerSkill> skills = await _playerSkillRepository.FindAllByPlayerIdAsync(created.Player.Id);

        PlayerDataResponse response = PlayerDataResponseBuilder.Build(
            created.Player, created.Resource, created.Stage, created.Session, weapons, skills);

        await _playerRedisRepository.SetPlayerDataAsync(accountId, response);
        return response;
    }
}
```

- [ ] **Step 6: 테스트 통과 확인**

Run: `dotnet test --filter "FullyQualifiedName~CreatePlayerServiceTest"`
Expected: PASS (6 tests).

- [ ] **Step 7: 커밋**

```bash
git add Fantasy-server/Fantasy.Server/Domain/Player/Dto/Request/CreatePlayerRequest.cs \
        Fantasy-server/Fantasy.Server/Domain/Player/Service/Interface/ICreatePlayerService.cs \
        Fantasy-server/Fantasy.Server/Domain/Player/Service/CreatePlayerService.cs \
        Fantasy-server/Fantasy.Test/Player/Service/CreatePlayerServiceTest.cs
git commit -m "feat: CreatePlayerService 추가 (플레이어 생성, 이미 있으면 409)"
```

---

### Task 3: 컨트롤러·DI 전환 및 InitPlayer 제거

**Files:**
- Modify: `Fantasy-server/Fantasy.Server/Domain/Player/Controller/PlayerController.cs`
- Modify: `Fantasy-server/Fantasy.Server/Domain/Player/Config/PlayerServiceConfig.cs:20`
- Delete: `Fantasy-server/Fantasy.Server/Domain/Player/Service/Interface/IInitPlayerService.cs`
- Delete: `Fantasy-server/Fantasy.Server/Domain/Player/Service/InitPlayerService.cs`
- Delete: `Fantasy-server/Fantasy.Server/Domain/Player/Dto/Request/InitPlayerRequest.cs`
- Delete: `Fantasy-server/Fantasy.Test/Player/Service/InitPlayerServiceTests.cs`
- Test: `Fantasy-server/Fantasy.Test/Player/Controller/PlayerControllerRouteTests.cs`

**Interfaces:**
- Consumes: `IGetPlayerService.ExecuteAsync(): Task<PlayerDataResponse>` (Task 1), `ICreatePlayerService.ExecuteAsync(CreatePlayerRequest): Task<PlayerDataResponse>` (Task 2), `ILoadoutService`, `ISkillUnlockService` (기존), `CommonApiResponse.Created(string, T)`, `CommonApiResponse.Success(string, T)`.
- Produces: `PlayerController`에 `GET /v1/player`(Get), `POST /v1/player`(Create) 액션. `IInitPlayerService`/`InitPlayerService`/`InitPlayerRequest` 더 이상 존재하지 않음.

- [ ] **Step 1: 라우트 테스트를 새 계약으로 갱신(실패 상태)**

`Fantasy-server/Fantasy.Test/Player/Controller/PlayerControllerRouteTests.cs` 전체를 다음으로 교체:
```csharp
using System.Reflection;
using Fantasy.Server.Domain.Player.Controller;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Fantasy.Test.Player.Controller;

public class PlayerControllerRouteTests
{
    private static readonly MethodInfo[] Actions = typeof(PlayerController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    [Fact]
    public void PlayerController에_PATCH_액션이_없다()
    {
        var hasPatch = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Any(method => method.Equals("PATCH", StringComparison.OrdinalIgnoreCase));

        hasPatch.Should().BeFalse();
    }

    [Fact]
    public void PlayerController는_GET과_POST만_노출한다()
    {
        var httpMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct();

        httpMethods.Should().BeEquivalentTo(["GET", "POST"]);
    }

    [Fact]
    public void 루트_경로에_GET_로드와_POST_생성이_있다()
    {
        var rootMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Where(a => a.Template == null)
            .SelectMany(a => a.HttpMethods);

        rootMethods.Should().BeEquivalentTo(["GET", "POST"]);
    }

    [Fact]
    public void 하위_경로_템플릿은_loadout과_skill_unlock이다()
    {
        var templates = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Select(a => a.Template)
            .Where(t => t != null);

        templates.Should().BeEquivalentTo(["loadout", "skill/unlock"]);
    }
}
```

- [ ] **Step 2: 컨트롤러 교체**

`Fantasy-server/Fantasy.Server/Domain/Player/Controller/PlayerController.cs` 전체를 다음으로 교체:
```csharp
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Service.Interface;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Player.Controller;

[ApiController]
[Route("v1/player")]
[Authorize]
[EnableRateLimiting("game")]
public class PlayerController : ControllerBase
{
    private readonly IGetPlayerService _getPlayerService;
    private readonly ICreatePlayerService _createPlayerService;
    private readonly ILoadoutService _loadoutService;
    private readonly ISkillUnlockService _skillUnlockService;

    public PlayerController(
        IGetPlayerService getPlayerService,
        ICreatePlayerService createPlayerService,
        ILoadoutService loadoutService,
        ISkillUnlockService skillUnlockService)
    {
        _getPlayerService = getPlayerService;
        _createPlayerService = createPlayerService;
        _loadoutService = loadoutService;
        _skillUnlockService = skillUnlockService;
    }

    [HttpGet]
    public async Task<PlayerDataResponse> Get() => await _getPlayerService.ExecuteAsync();

    [HttpPost]
    public async Task<CommonApiResponse<PlayerDataResponse>> Create([FromBody] CreatePlayerRequest request)
    {
        var data = await _createPlayerService.ExecuteAsync(request);
        return CommonApiResponse.Created("플레이어가 생성되었습니다.", data);
    }

    [HttpPost("loadout")]
    public async Task<CommonApiResponse<LoadoutResponse>> Loadout([FromBody] LoadoutRequest request)
    {
        var result = await _loadoutService.ExecuteAsync(request);
        return CommonApiResponse.Success("로드아웃이 저장되었습니다.", result);
    }

    [HttpPost("skill/unlock")]
    public async Task<CommonApiResponse<SkillUnlockResponse>> UnlockSkill([FromBody] SkillUnlockRequest request)
    {
        var result = await _skillUnlockService.ExecuteAsync(request);
        return CommonApiResponse.Success("스킬 해금이 처리되었습니다.", result);
    }
}
```

- [ ] **Step 3: DI 등록 교체**

`Fantasy-server/Fantasy.Server/Domain/Player/Config/PlayerServiceConfig.cs`에서 다음 줄을 교체.

Old (line 20):
```csharp
        services.AddScoped<IInitPlayerService, InitPlayerService>();
```
New:
```csharp
        services.AddScoped<IGetPlayerService, GetPlayerService>();
        services.AddScoped<ICreatePlayerService, CreatePlayerService>();
```

- [ ] **Step 4: InitPlayer 잔재 삭제**

다음 4개 파일을 삭제:
```bash
git rm Fantasy-server/Fantasy.Server/Domain/Player/Service/Interface/IInitPlayerService.cs \
       Fantasy-server/Fantasy.Server/Domain/Player/Service/InitPlayerService.cs \
       Fantasy-server/Fantasy.Server/Domain/Player/Dto/Request/InitPlayerRequest.cs \
       Fantasy-server/Fantasy.Test/Player/Service/InitPlayerServiceTests.cs
```

- [ ] **Step 5: 빌드 + 전체 테스트 통과 확인**

Run: `/test` (프로젝트 표준 — Fantasy.Server 빌드 후 Fantasy.Test 전체 실행)
Expected: 빌드 성공, 전체 테스트 PASS. `InitPlayer` 참조 잔존 시 컴파일 에러 → 해당 참조 제거 후 재실행.

- [ ] **Step 6: 커밋**

```bash
git add -A
git commit -m "refactor: player init을 GET 로드 + POST 생성으로 분리"
```

---

### Task 4: 클라이언트 연동 가이드 문서 갱신

**Files:**
- Modify: `docs/client-integration-guide.md`

**Interfaces:**
- Consumes: Task 3에서 확정된 라우트(`GET /v1/player` 200/404, `POST /v1/player` 201/409).
- Produces: 없음(문서).

- [ ] **Step 1: 전체 흐름도 갱신**

`docs/client-integration-guide.md`에서 다음 블록을 교체.

Old:
```
[최초]   signup → login → player/init(직업 선택)
[재접속] login(또는 refresh) → player/init(기존 데이터 로드)
```
New:
```
[최초]   signup → login → GET player(404) → POST player(직업 선택·생성)
[재접속] login(또는 refresh) → GET player(기존 데이터 로드)
```

- [ ] **Step 2: 핵심 원칙 문구 갱신**

Old:
```
- **로그인 직후 반드시 `player/init`을 호출**해 플레이어 상태를 확보합니다. (없으면 생성, 있으면 로드 — 멱등)
```
New:
```
- **로그인 직후 `GET /v1/player`로 플레이어를 로드**합니다. **404면 직업 선택 후 `POST /v1/player`로 생성**합니다.
```

- [ ] **Step 3: 에러 표 갱신**

Old (404 행):
```
| 404 | 없음 | 플레이어/스킬/스테이지 데이터 없음 |
```
New:
```
| 404 | 없음 | 플레이어 미생성(`GET /v1/player`), 스킬/스테이지 데이터 없음 |
```

Old (409 행):
```
| 409 | 충돌 | 이메일 중복, 다른 직업의 플레이어 존재, 광고 보상 중복 수령 |
```
New:
```
| 409 | 충돌 | 이메일 중복, 플레이어 이미 존재(`POST /v1/player`), 광고 보상 중복 수령 |
```

- [ ] **Step 4: 플레이어 엔드포인트 표 갱신**

Old:
```
| POST | `/v1/player/init` | `{ jobType }` | 플레이어 생성/로드 |
```
New:
```
| GET | `/v1/player` | (없음) | 플레이어 로드 (없으면 404) |
| POST | `/v1/player` | `{ jobType }` | 플레이어 생성 (이미 있으면 409) |
```

- [ ] **Step 5: init 설명 불릿 갱신**

Old:
```
- **init**: 플레이어가 없으면 `jobType`으로 신규 생성(201), 있으면 기존 데이터 로드(200). `jobType`은 **최초 생성 시에만 의미**가 있고, 이미 다른 직업으로 존재하면 **409**. 따라서 재접속 시에도 안전하게 호출 가능합니다.
```
New:
```
- **GET /v1/player**: 현재 플레이어를 로드(200). 아직 생성 전이면 **404** → 클라이언트는 직업 선택 화면으로 유도합니다.
- **POST /v1/player**: `jobType`으로 신규 생성(201). 이미 플레이어가 있으면 **409**. 재접속 로드에는 GET을 사용하므로 직업을 보낼 필요가 없습니다.
```

- [ ] **Step 6: 커밋**

```bash
git add docs/client-integration-guide.md
git commit -m "docs: 클라이언트 가이드를 GET 로드 + POST 생성 흐름으로 갱신"
```

---

## Self-Review

**1. Spec coverage:**
- 스펙 §3 API 계약(GET 200/404, POST 201/409) → Task 1·2(서비스), Task 3(컨트롤러), Task 4(문서). ✓
- 스펙 §4.1 서비스 분리 → Task 1·2. ✓
- 스펙 §4.2 DTO 개명 → Task 2 Step 1. ✓
- 스펙 §4.3 컨트롤러 → Task 3 Step 2. ✓
- 스펙 §4.4 DI → Task 3 Step 3. ✓
- 스펙 §5 제거 대상(라우트/서비스/DTO/튜플) → Task 3 Step 4(파일 삭제) + Step 2(튜플 제거된 컨트롤러). ✓
- 스펙 §6 테스트 → Task 1 Step 2, Task 2 Step 3, Task 3 Step 1. ✓
- 스펙 §7 문서 → Task 4. ✓
- 스펙 §8 PR #28 처리 → 본 브랜치(refactor/player-load-create)가 PR #28 브랜치에서 분기되어 문서·코드 통합. (계획 범위 밖 운영 결정, 별도 처리)

**2. Placeholder scan:** TBD/TODO/"적절히 처리" 없음. 모든 코드 스텝에 완전한 코드 포함. ✓

**3. Type consistency:** `GetPlayerService` 생성자 8개 인자(transactionRunner 없음), `CreatePlayerService` 9개 인자(transactionRunner 포함) — Task 1·2 테스트의 `new(...)` 호출과 일치. `CreatePlayerRequest(JobType)`는 Task 2에서 정의되고 Task 3 컨트롤러에서 사용. `IGetPlayerService.ExecuteAsync()`(무인자)·`ICreatePlayerService.ExecuteAsync(CreatePlayerRequest)` 시그니처가 컨트롤러 호출과 일치. ✓
