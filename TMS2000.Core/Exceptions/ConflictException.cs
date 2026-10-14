namespace TMS2000.Core.Exceptions;

// 409 : conflit avec l'état actuel (règle qui chevauche une autre, modification concurrente)
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409) { }
}