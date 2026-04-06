namespace Fantasy.Server.Domain.Auth.Dto.Response;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    long AccessTokenExpiresAt
);
