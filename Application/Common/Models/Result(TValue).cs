using Microsoft.AspNetCore.Http;

namespace Application.Common.Models;

public class Result<TValue> : Result
    where TValue : notnull
{
    private readonly TValue? _value;

    public new int StatusCode { get; } = StatusCodes.Status200OK;

    protected internal Result(TValue? value, bool isSuccess, IEnumerable<Error> error, int statusCode)
        : base(isSuccess, error, statusCode)
    {
        _value = value;
        StatusCode = statusCode;
    }

    public TValue Value => Success
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static Result<TValue> CreateSuccess(TValue value, int statusCode = StatusCodes.Status200OK) => new(value, true, [], statusCode);

    public static new Result<TValue> CreateFailure(IEnumerable<string> errors, int statusCode = StatusCodes.Status400BadRequest) => new(default, false, errors.Select(x => { return new Error(x); }), statusCode);

    public static new Result<TValue> CreateFailure(IEnumerable<Error> errors, int statusCode = StatusCodes.Status400BadRequest) => new(default, false, errors, statusCode);
}
