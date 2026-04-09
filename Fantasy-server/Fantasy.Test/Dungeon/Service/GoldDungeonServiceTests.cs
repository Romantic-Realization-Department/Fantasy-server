using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Dungeon.Service;

public class GoldDungeonServiceTests
{
    public class 정상_요청일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GoldDungeonService _sut;

        public 정상_요청일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));

            _sut = new GoldDungeonService(
                _playerRepository, _playerResourceRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 클릭수에_비례한_골드가_반환된다()
        {
            var request = new GoldDungeonRequest(Clicks: 10);

            var result = await _sut.ExecuteAsync(JobType.Warrior, request);

            result.EarnedGold.Should().Be(10 * 10); // 10 clicks * GoldPerClick(10)
        }

        [Fact]
        public async Task 재화가_업데이트된다()
        {
            var request = new GoldDungeonRequest(Clicks: 10);

            await _sut.ExecuteAsync(JobType.Warrior, request);

            await _playerResourceRepository.Received(1).UpdateAsync(Arg.Any<PlayerResource>());
        }

        [Fact]
        public async Task Redis_캐시가_무효화된다()
        {
            var request = new GoldDungeonRequest(Clicks: 10);

            await _sut.ExecuteAsync(JobType.Warrior, request);

            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }
    }

    public class 플레이어가_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GoldDungeonService _sut;

        public 플레이어가_없을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(Arg.Any<long>(), Arg.Any<JobType>())
                .Returns((PlayerEntity?)null);

            _sut = new GoldDungeonService(
                _playerRepository, _playerResourceRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var request = new GoldDungeonRequest(Clicks: 10);

            var act = async () => await _sut.ExecuteAsync(JobType.Warrior, request);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
