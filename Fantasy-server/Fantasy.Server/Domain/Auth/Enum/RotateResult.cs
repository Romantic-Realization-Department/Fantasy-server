namespace Fantasy.Server.Domain.Auth.Enum;

public enum RotateResult
{
    NotFound = 0,
    Reused   = -1,
    Success  = 1
}
