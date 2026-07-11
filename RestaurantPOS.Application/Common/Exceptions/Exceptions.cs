using FluentValidation.Results;

namespace RestaurantPOS.Application.Common.Exceptions;

/// <summary>Thrown when FluentValidation finds rule violations.</summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

/// <summary>Thrown when a requested entity is not found.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"{name} with key '{key}' was not found.") { }
}

/// <summary>Thrown when the current user is not authorized to perform an action.</summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("You do not have permission to perform this action.") { }
}

/// <summary>Thrown when a business rule is violated.</summary>
public class BusinessRuleException : Exception
{
    public string Code { get; }

    public BusinessRuleException(string message, string code = "BUSINESS_RULE_VIOLATION")
        : base(message)
    {
        Code = code;
    }
}
