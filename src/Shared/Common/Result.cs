namespace CleanArchitecture.Shared.Common;

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        if (isSuccess && errors.Count != 0)
        {
            throw new InvalidOperationException("A successful result cannot contain errors.");
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<string> Errors { get; }

    public static Result Success() => new(true, []);

    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => new(value, true, []);

    public static Result<T> Failure<T>(params string[] errors) => new(default, false, errors);
}

public class Result<T> : Result
{
    internal Result(T? value, bool isSuccess, IReadOnlyList<string> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public T? Value { get; }
}
