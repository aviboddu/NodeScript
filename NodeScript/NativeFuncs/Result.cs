namespace NodeScript;

internal abstract class Result(string? message = null)
{
    public abstract object? GetValue();
    public readonly string? message = message;
    public abstract bool Success();
}

internal class Result<T> : Result
{
    private readonly object? value;

    protected Result(T? value = default, string? message = null) : base(message)
    {
        this.value = value;
    }

    public static Result<T> Ok(T val) => new(val);
    public static Result<T> Fail(string error) => new(message: error);
    public override bool Success() => value is not null;
    public override object? GetValue() => value;
}