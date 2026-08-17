namespace NodeScript;

internal readonly struct Result
{
    private readonly Value value;
    public readonly string? message;
    private readonly bool success;

    private Result(Value value, bool success, string? message)
    {
        this.value = value;
        this.success = success;
        this.message = message;
    }

    public static Result Ok(Value val) => new(val, success: true, message: null);
    public static Result Ok(int val) => Ok(Value.FromInt(val));
    public static Result Ok(bool val) => Ok(Value.FromBool(val));
    public static Result Ok(string val) => Ok(Value.FromString(val));
    public static Result Ok(string[] val) => Ok(Value.FromStringArray(val));
    public static Result Fail(string error) => new(default, success: false, message: error);

    public bool Success() => success;
    public Value GetValue() => value;
}
