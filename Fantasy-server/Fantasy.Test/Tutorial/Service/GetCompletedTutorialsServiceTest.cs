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
            tutorialRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns(
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
            tutorialRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            var sut = new GetCompletedTutorialsService(playerRepo, tutorialRepo, userProvider);

            var result = await sut.ExecuteAsync();

            result.CompletedTutorialIds.Should().BeEmpty();
        }
    }
}
