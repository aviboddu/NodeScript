namespace NodeScript;

internal abstract class Result(string? message = null)
{
    public abstract object? GetValue();
    public readonly string? message = message;
    public abstract bool Success();
}

internal sealed class Result<T> : Result
{
    private readonly object? value;
    private readonly bool success;

    private Result(T? value, string? message, bool success) : base(message)
    {
        this.value = value;
        this.success = success;
    }
    public static Result<T> Ok(T val) => new(val, null, true);
    public static Result<T> Fail(string error) => new(default, error, false);
    public override bool Success() => success;
    public override object? GetValue() => value;
}