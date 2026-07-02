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
