namespace TinyProc;

/// <summary>
/// An Either type instance contains either one instance of T1 or an instance of T2.
/// The either class wraps them up to be used in contexts where both types may appear.
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
/// <param name="a"></param>
/// <param name="b"></param>
public sealed class Either<T1, T2>(T1? a, T2? b)
{
    public T1? A { get; } = a;
    public T2? B { get; } = b;
    public Type Type { get; } = a != null ? typeof(T1) : typeof(T2);

    public bool Is<TCompare>() => typeof(TCompare) == Type;

    public static implicit operator Either<T1, T2>(T1 a) => new(a, default);
    public static implicit operator Either<T1, T2>(T2 b) => new(default, b);

    public static implicit operator T1(Either<T1, T2> either)
    {
        if (!either.Is<T1>())
            throw new InvalidCastException($"Either is not type T1:{typeof(T1)}, but of T2:{typeof(T2)}. Cannot convert to T1.");
        return either.A!;
    }
    public static implicit operator T2(Either<T1, T2> either)
    {
        if (!either.Is<T2>())
            throw new InvalidCastException($"Either is not type T2:{typeof(T2)}, but of T1:{typeof(T1)}. Cannot convert to T2.");
        return either.B!;
    }
}