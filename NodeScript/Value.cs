namespace NodeScript;

internal enum ValueKind : byte
{
    Null,
    Int,
    Bool,
    String,
    StringArray,
    Object,
}

internal readonly struct Value : IEquatable<Value>
{
    private readonly ValueKind kind;
    private readonly int intValue;
    private readonly bool boolValue;
    private readonly object? refValue;

    private Value(ValueKind kind, int intValue = 0, bool boolValue = false, object? refValue = null)
    {
        this.kind = kind;
        this.intValue = intValue;
        this.boolValue = boolValue;
        this.refValue = refValue;
    }

    public ValueKind Kind => kind;
    public bool IsReference => kind is ValueKind.String or ValueKind.StringArray or ValueKind.Object;

    public static Value FromInt(int value) => new(ValueKind.Int, intValue: value);
    public static Value FromBool(bool value) => new(ValueKind.Bool, boolValue: value);
    public static Value FromString(string value) => new(ValueKind.String, refValue: value);
    public static Value FromStringArray(string[] value) => new(ValueKind.StringArray, refValue: value);
    public static Value FromObject(object? value)
    {
        return value switch
        {
            null => default,
            Value v => v,
            int i => FromInt(i),
            bool b => FromBool(b),
            string s => FromString(s),
            string[] array => FromStringArray(array),
            _ => new(ValueKind.Object, refValue: value),
        };
    }

    public int AsInt() => kind == ValueKind.Int ? intValue : throw new InvalidCastException("Value is not an int");
    public bool AsBool() => kind == ValueKind.Bool ? boolValue : throw new InvalidCastException("Value is not a bool");
    public string AsString() => kind == ValueKind.String ? (string)refValue! : throw new InvalidCastException("Value is not a string");
    public string[] AsStringArray() => kind == ValueKind.StringArray ? (string[])refValue! : throw new InvalidCastException("Value is not a string[]");
    public object? AsObject()
    {
        return kind switch
        {
            ValueKind.Null => null,
            ValueKind.Int => intValue,
            ValueKind.Bool => boolValue,
            _ => refValue,
        };
    }

    public bool Equals(Value other)
    {
        if (kind != other.kind)
            return false;
        return kind switch
        {
            ValueKind.Int => intValue == other.intValue,
            ValueKind.Bool => boolValue == other.boolValue,
            _ => Equals(refValue, other.refValue),
        };
    }

    public override bool Equals(object? obj) => obj is Value other && Equals(other);

    public override int GetHashCode()
    {
        return kind switch
        {
            ValueKind.Int => HashCode.Combine(kind, intValue),
            ValueKind.Bool => HashCode.Combine(kind, boolValue),
            _ => HashCode.Combine(kind, refValue),
        };
    }
}
