using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Dungeon.Service;

public class GoldDungeonRunServiceTest
{
    private static GoldDungeonRunService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IAccountDungeonTicketRepository? ticketRepo = null,
        IGoldDungeonRunRepository? runRepo = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null) =>
        new(
            playerRepo ?? Substitute.For<IPlayerRepository>(),
            resourceRepo ?? Substitute.For<IPlayerResourceRepository>(),
            stageRepo ?? Substitute.For<IPlayerStageRepository>(),
            sessionRepo ?? Substitute.For<IPlayerSessionRepository>(),
            weaponRepo ?? Substitute.For<IPlayerWeaponRepository>(),
            skillRepo ?? Substitute.For<IPlayerSkillRepository>(),
            ticketRepo ?? Substitute.For<IAccountDungeonTicketRepository>(),
            runRepo ?? Substitute.For<IGoldDungeonRunRepository>(),
            txRunner ?? Substitute.For<IAppDbTransactionRunner>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>());

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

            await ((Func<Task>)(() => sut.ExecuteAsync())).Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 티켓이_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 티켓이_없을_때()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            var ticket = AccountDungeonTicket.Create(1L, today);
            ticket.Consume();
            ticket.Consume();
            ticket.Consume();
            _ticketRepository.FindByAccountIdAsync(1L).Returns(ticket);
        }

        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                ticketRepo: _ticketRepository, userProvider: _currentUserProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync())).Should().ThrowAsync<BadRequestException>();
        }
    }

    public class 신규_계정_첫_요청시
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly IGoldDungeonRunRepository _runRepository = Substitute.For<IGoldDungeonRunRepository>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 신규_계정_첫_요청시()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _ticketRepository.FindByAccountIdAsync(1L).Returns((AccountDungeonTicket?)null);
            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task StartGoldDungeonResponse가_반환된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                ticketRepo: _ticketRepository, runRepo: _runRepository,
                txRunner: _txRunner, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.RunId.Should().NotBeEmpty();
            result.DurationSeconds.Should().Be(30);
        }

        [Fact]
        public async Task 새_티켓이_저장된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                ticketRepo: _ticketRepository, runRepo: _runRepository,
                txRunner: _txRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _ticketRepository.Received(1).SaveAsync(Arg.Any<AccountDungeonTicket>());
        }
    }

    public class 티켓이_있는_정상_요청시
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly IGoldDungeonRunRepository _runRepository = Substitute.For<IGoldDungeonRunRepository>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 티켓이_있는_정상_요청시()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _ticketRepository.FindByAccountIdAsync(1L).Returns(AccountDungeonTicket.Create(1L, today));
            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task StartGoldDungeonResponse가_반환된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                ticketRepo: _ticketRepository, runRepo: _runRepository,
                txRunner: _txRunner, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.RunId.Should().NotBeEmpty();
        }
    }
}
