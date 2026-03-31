---
description: C# code style rules. Always applied for all .cs files.
alwaysApply: true
---

## C# Code Style

### General

- Use `var` only when the type is obvious from the right-hand side.
- Prefer expression-body methods for single-line implementations.
- No XML doc comments unless explicitly requested.
- No `#region` blocks.

### Naming

- Private fields: `_camelCase` (underscore prefix)
- Properties, methods, classes: `PascalCase`
- Local variables and parameters: `camelCase`
- Interfaces: `IPascalCase`
- Database table/schema names: `snake_case`

### Classes & Constructors

- Constructor-inject all dependencies; assign to `private readonly` fields.
- Keep constructors minimal — no logic, only assignments.

```csharp
// Good
public class CreateAccountService : ICreateAccountService
{
    private readonly IAccountRepository _accountRepository;

    public CreateAccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }
}
```

### DTOs

- Always use `record` types with positional parameters.
- Apply `DataAnnotations` directly on parameters.

```csharp
public record CreateAccountRequest(
    [Required][EmailAddress][MaxLength(50)] string Email,
    [Required][MinLength(8)][MaxLength(20)] string Password
);
```

### Entities

- All setters `private set`.
- Use a static factory method `Create(...)` instead of a public constructor.
- Timestamps always use `DateTime.UtcNow`.

```csharp
public class Account
{
    public long Id { get; private set; }
    public string Email { get; private set; } = string.Empty;

    public static Account Create(string email, string password) => new()
    {
        Email = email,
        Password = password,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
```

### Async

- All I/O methods are `async Task` or `async Task<T>`.
- Never use `.Result` or `.Wait()`.
- Always `await` — no fire-and-forget unless intentional.

### Exception Handling

- Throw domain-specific exceptions from `Gamism.SDK` (e.g., `ConflictException`, `NotFoundException`).
- Do not catch and re-throw unless adding context.
- No empty catch blocks.
