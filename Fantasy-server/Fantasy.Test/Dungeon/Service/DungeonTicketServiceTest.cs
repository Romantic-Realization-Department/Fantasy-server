using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Fantasy.Test.Dungeon.Service;

public class DungeonTicketServiceTest
{
    private static DungeonTicketService BuildSut(
        IAccountDungeonTicketRepository? ticketRepo = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null)
    {
        if (txRunner is null)
        {
            txRunner = Substitute.For<IAppDbTransactionRunner>();
            txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        return new(
            ticketRepo ?? Substitute.For<IAccountDungeonTicketRepository>(),
            txRunner,
            userProvider ?? Substitute.For<ICurrentUserProvider>());
    }

    public class 티켓_레코드가_없을_때
    {
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 티켓_레코드가_없을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _ticketRepository.FindByAccountIdAsync(1L).Returns((AccountDungeonTicket?)null);
        }

        [Fact]
        public async Task 티켓_3장으로_생성된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.TicketCount.Should().Be(3);
        }

        [Fact]
        public async Task SaveAsync가_호출된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _ticketRepository.Received(1).SaveAsync(Arg.Any<AccountDungeonTicket>());
        }
    }

    public class 오늘_이미_지급된_티켓이_있을_때
    {
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly AccountDungeonTicket _ticket;

        public 오늘_이미_지급된_티켓이_있을_때()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));
            _ticket = AccountDungeonTicket.Create(1L, today);
            _ticket.Consume(); // TicketCount = 2
            _currentUserProvider.GetAccountId().Returns(1L);
            _ticketRepository.FindByAccountIdAsync(1L).Returns(_ticket);
        }

        [Fact]
        public async Task 기존_티켓_수량이_반환된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.TicketCount.Should().Be(2);
        }

        [Fact]
        public async Task UpdateAsync가_호출되지_않는다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _ticketRepository.DidNotReceive().UpdateAsync(Arg.Any<AccountDungeonTicket>());
        }
    }

    public class 어제_지급된_티켓이_있을_때
    {
        private readonly IAccountDungeonTicketRepository _ticketRepository = Substitute.For<IAccountDungeonTicketRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 어제_지급된_티켓이_있을_때()
        {
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9).AddDays(-1));
            var ticket = AccountDungeonTicket.Create(1L, yesterday);
            ticket.Consume(); ticket.Consume(); // TicketCount = 1
            _currentUserProvider.GetAccountId().Returns(1L);
            _ticketRepository.FindByAccountIdAsync(1L).Returns(ticket);
        }

        [Fact]
        public async Task 일일_지급으로_티켓이_3으로_초기화된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.TicketCount.Should().Be(3);
        }

        [Fact]
        public async Task UpdateAsync가_호출된다()
        {
            var sut = BuildSut(ticketRepo: _ticketRepository, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _ticketRepository.Received(1).UpdateAsync(Arg.Any<AccountDungeonTicket>());
        }
    }
}
