namespace TMS2000.Core.Exceptions;

// 401
public class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException() : base("Email ou mot de passe incorrect", 401)
    {

    }
}