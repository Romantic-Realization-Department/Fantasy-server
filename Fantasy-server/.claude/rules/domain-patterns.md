---
description: Domain layer patterns (Service, Repository, Controller). Applied when working on Domain/** files.
globs: ["Fantasy.Server/Domain/**"]
alwaysApply: false
---

## Service Pattern

Each use case is a single class implementing a single interface with one `ExecuteAsync` method.

```csharp
// Interface
public interface ICreateAccountService
{
    Task ExecuteAsync(CreateAccountRequest request);
}

// Implementation
public class CreateAccountService : ICreateAccountService
{
    private readonly IAccountRepository _accountRepository;

    public CreateAccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task ExecuteAsync(CreateAccountRequest request)
    {
        if (await _accountRepository.ExistsByEmailAsync(request.Email))
            throw new ConflictException("이미 사용중인 이메일입니다.");

        var hashed = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var account = AccountEntity.Create(request.Email, hashed);
        await _accountRepository.SaveAsync(account);
    }
}
```

## Repository Pattern

- Interface in `Repository/Interface/`, implementation in `Repository/`.
- Only repositories access `AppDbContext`.
- Use `AsNoTracking()` for read-only queries.

```csharp
public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;

    public AccountRepository(AppDbContext db) => _db = db;

    public async Task<Account?> FindByEmailAsync(string email)
        => await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Email == email);

    public async Task<Account> SaveAsync(Account account)
    {
        if (_db.Accounts.Entry(account).State == EntityState.Detached)
            await _db.Accounts.AddAsync(account);
        await _db.SaveChangesAsync();
        return account;
    }
}
```

## Controller Pattern

- Inject service interfaces only.
- Return `CommonApiResponse` from `Gamism.SDK`.
- Apply rate limiting via `[EnableRateLimiting("policy-name")]`.

```csharp
[ApiController]
[Route("v1/account")]
public class AccountController : ControllerBase
{
    private readonly ICreateAccountService _createAccountService;

    public AccountController(ICreateAccountService createAccountService)
    {
        _createAccountService = createAccountService;
    }

    [HttpPost("signup")]
    public async Task<CommonApiResponse> SignUp([FromBody] CreateAccountRequest request)
    {
        await _createAccountService.ExecuteAsync(request);
        return CommonApiResponse.Created("계정이 생성되었습니다.");
    }
}
```
