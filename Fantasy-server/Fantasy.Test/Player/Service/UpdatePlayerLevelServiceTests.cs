using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Player.Service;

public class UpdatePlayerLevelServiceTests
{
    public class 플레이어가_존재할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerLevelService _sut;
        private readonly UpdatePlayerLevelRequest _request = new(JobType.Warrior, 10L);

        public 플레이어가_존재할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));

            _sut = new UpdatePlayerLevelService(_playerRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 플레이어_레벨이_업데이트된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRepository.Received(1).UpdateAsync(Arg.Any<PlayerEntity>());
        }

        [Fact]
        public async Task Redis_캐시가_무효화된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }
    }

    public class 플레이어가_존재하지_않을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerLevelService _sut;

        public 플레이어가_존재하지_않을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(Arg.Any<long>(), Arg.Any<JobType>())
                .Returns((PlayerEntity?)null);

            _sut = new UpdatePlayerLevelService(_playerRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var request = new UpdatePlayerLevelRequest(JobType.Warrior, 5L);

            var act = async () => await _sut.ExecuteAsync(request);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
