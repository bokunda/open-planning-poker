namespace OpenPlanningPoker.Shared.Results;

public class Result<TValue, TError>
{
    public readonly TValue? Value;
    public readonly TError? Error;

    public readonly bool IsSuccess;

    private Result(TValue value)
    {
        IsSuccess = true;
        Value = value;
        Error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    //happy path
    public static implicit operator Result<TValue, TError>(TValue value) => new(value);

    //error path
    public static implicit operator Result<TValue, TError>(TError error) => new(error);

    public Result<TValue, TError> Match(Func<TValue, Result<TValue, TError>> success, Func<TError, Result<TValue, TError>> failure)
    {
        return IsSuccess ? success(Value!) : failure(Error!);
    }
}