using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;

namespace Fantasy.Test.Dungeon.Service;

public class AdRewardServiceTest
{
    private static AdRewardService BuildSut(
        IAccountDungeonTicketRepository? ticketRepo = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null) =>
        new(
            ticketRepo ?? Substitute.For<IAccountDungeonTicketRepository>(),
            txRunner ?? Substitute.For<IAppDbTransactionRunner>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>());

    public class 오늘_이미_광고_보상을_받았을_때
    {
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 오늘_이미_광고_보상을_받았을_때()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));
            var ticket = AccountDungeonTicket.Create(1L, today);
            ticket.GrantAdReward(today);
            _currentUserProvider.GetAccountId().Returns(1L);
            _ticketRepository.FindByAccountIdAsync(1L).Returns(ticket);
        }

        [Fact]
        public async Task ConflictException이_발생한다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, userProvider: _currentUserProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync())).Should().ThrowAsync<ConflictException>();
        }
    }

    public class 신규_계정_첫_요청시
    {
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 신규_계정_첫_요청시()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _ticketRepository.FindByAccountIdAsync(1L).Returns((AccountDungeonTicket?)null);
            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 광고_보상_티켓이_지급된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, txRunner: _txRunner, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            // Create gives 3, GrantAdReward gives +1 = 4
            result.TicketCount.Should().Be(4);
            result.DailyAdRewardClaimedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task SaveAsync가_호출된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, txRunner: _txRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _ticketRepository.Received(1).SaveAsync(Arg.Any<AccountDungeonTicket>());
        }
    }

    public class 기존_계정_정상_요청시
    {
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 기존_계정_정상_요청시()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));
            var ticket = AccountDungeonTicket.Create(1L, today);
            ticket.Consume(); // TicketCount = 2
            _currentUserProvider.GetAccountId().Returns(1L);
            _ticketRepository.FindByAccountIdAsync(1L).Returns(ticket);
            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 광고_보상_티켓이_지급된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, txRunner: _txRunner, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            // 2 + 1 (ad reward) = 3
            result.TicketCount.Should().Be(3);
            result.DailyAdRewardClaimedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync가_호출된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, txRunner: _txRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _ticketRepository.Received(1).UpdateAsync(Arg.Any<AccountDungeonTicket>());
        }
    }
}
