using System;

namespace CabinetBilder.Core.Common;

/// <summary>
/// Represents the outcome of an operation â€” either success or failure with an error message.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; }

    protected Result(bool isSuccess, string? errorMessage)
    {
        if (isSuccess && errorMessage != null)
        {
            throw new InvalidOperationException("Cannot have an error message on a successful result.");
        }
        if (!isSuccess && string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException("Cannot have a failure result without an error message.");
        }

        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new Result(true, null);
    public static Result Failure(string errorMessage) => new Result(false, errorMessage);

    public static Result<T> Success<T>(T value) => new Result<T>(value, true, null);
    public static Result<T> Failure<T>(string errorMessage) => new Result<T>(default, false, errorMessage);
}

/// <summary>
/// Represents the outcome of an operation that returns a value on success.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"The result is a failure. Error: {ErrorMessage}");

    protected internal Result(T? value, bool isSuccess, string? errorMessage)
        : base(isSuccess, errorMessage)
    {
        _value = value;
    }
}

