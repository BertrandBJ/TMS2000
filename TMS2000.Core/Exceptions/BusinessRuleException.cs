namespace TMS2000.Core.Exceptions;

// 422 : refus métier (solde insuffisant, action bloquée par une règle, max de règles atteint)

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message, 422) { }
}