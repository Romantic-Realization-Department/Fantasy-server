namespace Fantasy.Common.Domain.Account.Exception;

public class DuplicateEmailException : System.Exception
{
    public DuplicateEmailException() 
        : base("이미 사용중인 이메일입니다.") { }
    
    public DuplicateEmailException(string email) 
        : base($"{email}은 이미 사용중인 이메일입니다.") { }
}