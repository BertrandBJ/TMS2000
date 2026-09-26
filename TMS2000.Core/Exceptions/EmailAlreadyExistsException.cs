namespace TMS2000.Core.Exceptions;

public class EmailAlreadyExistsException : Exception // hérite de exception from C# lib
{
    public EmailAlreadyExistsException(string email) 
        : base($"L'email '{email}' est déjà utilisé.") // base => appel le constructeur parent, celui d'exception
    {
    }
}