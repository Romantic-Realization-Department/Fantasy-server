using Fantasy.Server.Domain.Account.Entity.Constant;

namespace Fantasy.Server.Domain.Account.Entity;

public class Account
{
    public long Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public AccountRole Role { get; private set; }
    public bool IsNewAccount { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Account Create(string email, string password) => new()
    {
        Email = email,
        Password = password,
        Role = AccountRole.User,
        IsNewAccount = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
