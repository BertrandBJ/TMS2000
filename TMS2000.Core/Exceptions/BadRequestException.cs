namespace TMS2000.Core.Exceptions;

// 400 : la requête est bien formée mais incohérente (ex : début de règle dans le passé)

public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message, 400) { }
}