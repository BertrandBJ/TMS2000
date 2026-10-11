namespace TMS2000.Core.Exceptions;

public class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException() : base("Email ou mot de passe incorrect", 401)
    {
        
    }
}