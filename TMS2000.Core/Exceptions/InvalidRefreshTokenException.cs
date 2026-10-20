namespace TMS2000.Core.Exceptions;

public class InvalidRefreshTokenException : AppException
{
    public InvalidRefreshTokenException() : base("Session expirée ou invalide, reconnectez-vous", 401)
    {

    }
}