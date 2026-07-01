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
        private readonly PlayerEntity _player;
        private readonly PlayerTutorial _existing;

        public 이미_완료된_튜토리얼일_때()
        {
            _player = PlayerEntity.Create(1L, JobType.Warrior);
            _existing = PlayerTutorial.Create(_player.Id, "tutorial_first_game_start");

            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(_player);
            _tutorialRepository.FindByPlayerIdAndTutorialIdAsync(_player.Id, "tutorial_first_game_start")
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
        private readonly PlayerEntity _player;

        public 신규_완료일_때()
        {
            _player = PlayerEntity.Create(1L, JobType.Warrior);

            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(_player);
            _tutorialRepository.FindByPlayerIdAndTutorialIdAsync(_player.Id, "tutorial_first_game_start")
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
