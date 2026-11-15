namespace Bookstore.Ordering.Functional;

// default(Option<T>) is None: a struct with no value inside
public readonly record struct Option<T>
{
    private readonly T _value;

    private Option(T value) => (_value, IsSome) = (value, true);

    public bool IsSome { get; }

    public static Option<T> None => default;

    public static Option<T> Some(T value) =>
        value is null ? throw new ArgumentNullException(nameof(value)) : new Option<T>(value);

    public TOut Match<TOut>(Func<T, TOut> some, Func<TOut> none) => IsSome ? some(_value) : none();

    public Option<TOut> Map<TOut>(Func<T, TOut> map) => IsSome ? Option<TOut>.Some(map(_value)) : Option<TOut>.None;

    public Option<TOut> Bind<TOut>(Func<T, Option<TOut>> bind) => IsSome ? bind(_value) : Option<TOut>.None;

    public Option<T> Where(Func<T, bool> predicate) => IsSome && predicate(_value) ? this : None;

    public T GetValueOrDefault(T fallback) => IsSome ? _value : fallback;

    public override string ToString() => IsSome ? $"Some({_value})" : "None";
}

public static class Option
{
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);

    public static Option<T> ToOption<T>(this T? value) where T : class =>
        value is null ? Option<T>.None : Option<T>.Some(value);

    public static Option<T> ToOption<T>(this T? value) where T : struct =>
        value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;

    public static Option<TValue> Find<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key) =>
        source.TryGetValue(key, out TValue? value) ? Option<TValue>.Some(value) : Option<TValue>.None;

    public static Option<T> TryFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (T item in source)
        {
            if (predicate(item))
                return Option<T>.Some(item);
        }
        return Option<T>.None;
    }

    // LINQ query syntax looks for methods with these names and shapes
    public static Option<TOut> Select<T, TOut>(this Option<T> option, Func<T, TOut> map) => option.Map(map);

    public static Option<TOut> SelectMany<T, TMid, TOut>(
        this Option<T> option, Func<T, Option<TMid>> bind, Func<T, TMid, TOut> project) =>
        option.Bind(first => bind(first).Map(second => project(first, second)));
}
