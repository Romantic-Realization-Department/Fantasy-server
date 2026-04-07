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

public class UpdatePlayerWeaponServiceTests
{
    public class 정상_요청일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerWeaponService _sut;
        private readonly List<WeaponChangeItem> _weapons = [new(1, 2L, 1L, 0L)];

        public 정상_요청일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));

            _sut = new UpdatePlayerWeaponService(
                _playerRepository, _playerWeaponRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 무기_Upsert가_호출된다()
        {
            var request = new UpdatePlayerWeaponRequest(JobType.Warrior, _weapons);

            await _sut.ExecuteAsync(request);

            await _playerWeaponRepository.Received(1).UpsertRangeAsync(Arg.Any<long>(), _weapons);
        }

        [Fact]
        public async Task Redis_캐시가_무효화된다()
        {
            var request = new UpdatePlayerWeaponRequest(JobType.Warrior, _weapons);

            await _sut.ExecuteAsync(request);

            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }
    }

    public class 플레이어가_존재하지_않을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerWeaponService _sut;

        public 플레이어가_존재하지_않을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(Arg.Any<long>(), Arg.Any<JobType>())
                .Returns((PlayerEntity?)null);

            _sut = new UpdatePlayerWeaponService(
                _playerRepository, _playerWeaponRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var request = new UpdatePlayerWeaponRequest(JobType.Warrior, [new(1, 1L, 0L, 0L)]);

            var act = async () => await _sut.ExecuteAsync(request);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
