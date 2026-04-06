namespace Fantasy.Server.Domain.Auth.Entity;

public class RefreshToken
{
    public string Id { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;

    public static RefreshToken Create(string id, string token) => new()
    {
        Id = id,
        Token = token
    };
}
