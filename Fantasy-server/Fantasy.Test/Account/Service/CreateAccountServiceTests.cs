using Fantasy.Server.Domain.Account.Dto.Request;
using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Account.Service;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;

namespace Fantasy.Test.Account.Service;

public class CreateAccountServiceTests
{
    public class 이메일이_존재하지_않을_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly CreateAccountService _sut;
        private readonly CreateAccountRequest _request = new("test@example.com", "password123");

        public 이메일이_존재하지_않을_때()
        {
            _accountRepository.ExistsByEmailAsync(_request.Email).Returns(false);
            _accountRepository.SaveAsync(Arg.Any<AccountEntity>())
                .Returns(callInfo => callInfo.Arg<AccountEntity>());
            _sut = new CreateAccountService(_accountRepository);
        }

        [Fact]
        public async Task 회원가입_요청_시_계정이_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _accountRepository.Received(1).SaveAsync(Arg.Any<AccountEntity>());
        }

        [Fact]
        public async Task 회원가입_요청_시_비밀번호가_해싱된다()
        {
            AccountEntity? saved = null;
            _accountRepository.SaveAsync(Arg.Do<AccountEntity>(a => saved = a))
                .Returns(callInfo => callInfo.Arg<AccountEntity>());

            await _sut.ExecuteAsync(_request);

            saved!.Password.Should().NotBe(_request.Password);
            BCrypt.Net.BCrypt.Verify(_request.Password, saved.Password).Should().BeTrue();
        }
    }

    public class 이미_사용중인_이메일일_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly CreateAccountService _sut;
        private readonly CreateAccountRequest _request = new("existing@example.com", "password123");

        public 이미_사용중인_이메일일_때()
        {
            _accountRepository.ExistsByEmailAsync(_request.Email).Returns(true);
            _sut = new CreateAccountService(_accountRepository);
        }

        [Fact]
        public async Task 회원가입_요청_시_DuplicateEmailException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<ConflictException>();
        }
    }
}
