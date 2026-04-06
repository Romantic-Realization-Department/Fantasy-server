---
description: xUnit test conventions. Applied when working on Fantasy.Test/** files.
globs: ["Fantasy.Test/**"]
alwaysApply: false
---

## Test Project Structure

```
Fantasy.Test/
├── {DomainName}/
│   └── Service/     # Service unit tests
```

## Conventions

- Test class name: `{ServiceName}Test` — e.g., `CreateAccountServiceTest`
- Test method name: `{MethodName}_{Scenario}_{ExpectedResult}` — e.g., `ExecuteAsync_DuplicateEmail_ThrowsConflictException`
- Use `NSubstitute` for mocking dependencies (repository interfaces).
- Do not mock `AppDbContext` directly — mock repository interfaces instead.
- Arrange / Act / Assert structure with blank line separators.

```csharp
public class CreateAccountServiceTest
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();

    [Fact]
    public async Task ExecuteAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        _accountRepository.ExistsByEmailAsync("test@example.com").Returns(true);
        var service = new CreateAccountService(_accountRepository);
        var request = new CreateAccountRequest("test@example.com", "password123");

        // Act
        Func<Task> act = () => service.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }
}
```
