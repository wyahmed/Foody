namespace RestaurantPOS.Shared.Common;

/// <summary>
/// Represents the outcome of an operation, encapsulating success/failure and optional data.
/// Follows the Result pattern to avoid exception-driven flow control.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T>
{
    private Result(bool isSuccess, T? value, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string error, string? errorCode = null) => new(false, default, error, errorCode);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

/// <summary>Non-generic result for void operations.</summary>
public class Result
{
    private Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error, string? errorCode = null) => Result<T>.Failure(error, errorCode);
}
