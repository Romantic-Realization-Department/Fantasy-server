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
using PlayerResourceEntity = Fantasy.Server.Domain.Player.Entity.PlayerResource;

namespace Fantasy.Test.Player.Service;

public class UpdatePlayerResourceServiceTests
{
    public class 정상_요청일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerResourceService _sut;

        public 정상_요청일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResourceEntity.Create(1L));

            _sut = new UpdatePlayerResourceService(
                _playerRepository, _playerResourceRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 재화가_업데이트된다()
        {
            var request = new UpdatePlayerResourceRequest(JobType.Warrior, 10L, 5L, 20L);

            await _sut.ExecuteAsync(request);

            await _playerResourceRepository.Received(1).UpdateAsync(Arg.Any<PlayerResourceEntity>());
        }

        [Fact]
        public async Task 일부_필드만_있어도_업데이트된다()
        {
            var request = new UpdatePlayerResourceRequest(JobType.Warrior, 10L, null, null);

            await _sut.ExecuteAsync(request);

            await _playerResourceRepository.Received(1).UpdateAsync(Arg.Any<PlayerResourceEntity>());
        }

        [Fact]
        public async Task Redis_캐시가_무효화된다()
        {
            var request = new UpdatePlayerResourceRequest(JobType.Warrior, null, null, 5L);

            await _sut.ExecuteAsync(request);

            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }
    }

    public class 플레이어가_존재하지_않을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerResourceService _sut;

        public 플레이어가_존재하지_않을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(Arg.Any<long>(), Arg.Any<JobType>())
                .Returns((PlayerEntity?)null);

            _sut = new UpdatePlayerResourceService(
                _playerRepository, _playerResourceRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var request = new UpdatePlayerResourceRequest(JobType.Warrior, 10L, null, null);

            var act = async () => await _sut.ExecuteAsync(request);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
