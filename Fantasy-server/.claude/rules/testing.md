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
- Use `Moq` for mocking dependencies (repository interfaces).
- Do not mock `AppDbContext` directly — mock repository interfaces instead.
- Arrange / Act / Assert structure with blank line separators.

```csharp
public class CreateAccountServiceTest
{
    private readonly Mock<IAccountRepository> _mockRepo = new();

    [Fact]
    public async Task ExecuteAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        _mockRepo.Setup(r => r.ExistsByEmailAsync("test@example.com")).ReturnsAsync(true);
        var service = new CreateAccountService(_mockRepo.Object);
        var request = new CreateAccountRequest("test@example.com", "password123");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => service.ExecuteAsync(request));
    }
}
```
