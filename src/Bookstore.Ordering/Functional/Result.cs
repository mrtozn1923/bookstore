namespace Bookstore.Ordering.Functional;

public sealed record Error(string Code, string Message);

// A class, not a struct: default(Result<T>) would otherwise be a "success" with no value
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value) => (_value, Errors) = (value, []);

    private Result(IReadOnlyList<Error> errors) => Errors = errors;

    public IReadOnlyList<Error> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new([error]);

    public static Result<T> Failure(IReadOnlyList<Error> errors) =>
        errors.Count > 0 ? new(errors) : throw new ArgumentException("A failure needs at least one error.", nameof(errors));

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(Errors);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) =>
        IsSuccess ? bind(_value!) : Result<TOut>.Failure(Errors);

    public Result<T> Ensure(Func<T, bool> predicate, Func<T, Error> error) =>
        IsSuccess && !predicate(_value!) ? Failure(error(_value!)) : this;

    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess)
            action(_value!);
        return this;
    }

    public TOut Match<TOut>(Func<T, TOut> success, Func<IReadOnlyList<Error>, TOut> failure) =>
        IsSuccess ? success(_value!) : failure(Errors);

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({string.Join(", ", Errors.Select(error => error.Code))})";
}

public static class Result
{
    // Runs every validation and collects all errors instead of stopping at the first one
    public static Result<TOut> Combine<T1, T2, T3, TOut>(
        Result<T1> first, Result<T2> second, Result<T3> third, Func<T1, T2, T3, TOut> combine) =>
        first.IsSuccess && second.IsSuccess && third.IsSuccess
            ? first.Bind(a => second.Bind(b => third.Map(c => combine(a, b, c))))
            : Result<TOut>.Failure([.. first.Errors, .. second.Errors, .. third.Errors]);

    public static async Task<Result<TOut>> BindAsync<T, TOut>(this Result<T> result, Func<T, Task<Result<TOut>>> bind) =>
        await result.Match(bind, errors => Task.FromResult(Result<TOut>.Failure(errors)));

    public static async Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> task, Func<T, Task<Result<TOut>>> bind) =>
        await (await task).BindAsync(bind);

    public static async Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> task, Func<T, TOut> map) =>
        (await task).Map(map);

    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> task, Action<T> action) =>
        (await task).Tap(action);
}
