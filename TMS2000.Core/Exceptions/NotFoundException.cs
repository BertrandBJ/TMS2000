namespace TMS2000.Core.Exceptions;

// 404 : ressource, règle ou joueur introuvable
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }
}