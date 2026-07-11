using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RestaurantPOS.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior that automatically validates all commands/queries
/// with FluentValidation before execution. Returns validation errors as a
/// single aggregated exception to avoid partial processing.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehaviour<TRequest, TResponse>> _logger;

    public ValidationBehaviour(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehaviour<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            _logger.LogWarning("Validation errors for {RequestType}: {@Errors}",
                typeof(TRequest).Name, failures.Select(f => f.ErrorMessage));
            throw new Exceptions.ValidationException(failures);
        }

        return await next();
    }
}

/// <summary>Logs every MediatR request and its execution time for observability.</summary>
public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
            _logger.LogWarning("Slow request: {RequestName} took {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
        else
            _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
