namespace FoodHub.Orders.Domain.Exceptions;

public sealed class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }
}
