namespace Fantasy.Common.Domain.Auth.Exception;

public class InvalidCredentialsException : System.Exception
{
    public InvalidCredentialsException() : base("Invalid email or password.") { }
}
