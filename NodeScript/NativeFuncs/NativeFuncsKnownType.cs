#pragma warning disable IDE1006 // Naming Styles
namespace NodeScript;

using System.Collections.Frozen;
using static CompilerUtils;
internal static class NativeFuncsKnownType
{
    public static readonly FrozenDictionary<string, NativeDelegate> NativeFunctions = GetMethods(typeof(NativeFuncsKnownType));
    public static readonly FrozenDictionary<string, Type> NativeReturnTypes = new Dictionary<string, Type>()
    {
        [nameof(length_str)] = typeof(int),
        [nameof(length_stra)] = typeof(int),
        [nameof(split_str_str)] = typeof(string[]),
        [nameof(join_str_stra)] = typeof(string),
        [nameof(index_of_str_str)] = typeof(int),
        [nameof(slice_stra_int_int)] = typeof(string[]),
        [nameof(slice_str_int_int)] = typeof(string),
        [nameof(element_at_stra_int)] = typeof(string),
        [nameof(element_at_str_int)] = typeof(string),
        [nameof(parse_int_str)] = typeof(int),
        [nameof(can_parse_str)] = typeof(bool),
        [nameof(remove_at_stra_int)] = typeof(string[]),
        [nameof(trim_str)] = typeof(string),
    }.ToFrozenDictionary();
    public static readonly FrozenDictionary<Type, string> typeToStr = new Dictionary<Type, string>()
    {
        [typeof(string)] = "_str",
        [typeof(int)] = "_int",
        [typeof(string[])] = "_stra",
        [typeof(bool)] = "_bool",
    }.ToFrozenDictionary();

    public static Result length_str(Span<Value> objs)
    {
        return Result.Ok(objs[0].AsString().Length);
    }

    public static Result length_stra(Span<Value> objs)
    {
        return Result.Ok(objs[0].AsStringArray().Length);
    }

    public static Result split_str_str(Span<Value> objs)
    {
        string separator = objs[0].AsString();
        string s = objs[1].AsString();
        return Result.Ok(s.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static Result join_str_stra(Span<Value> objs)
    {
        string separator = objs[0].AsString();
        string[] s = objs[1].AsStringArray();
        return Result.Ok(string.Join(separator, s));
    }

    public static Result index_of_str_str(Span<Value> objs)
    {
        string search = objs[0].AsString();
        string s = objs[1].AsString();
        return Result.Ok(s.IndexOf(search));
    }

    public static Result slice_stra_int_int(Span<Value> objs)
    {
        string[] a = objs[0].AsStringArray();
        int start = objs[1].AsInt();
        int end = objs[2].AsInt();
        if (end <= start) return Result.Fail("end index must be greater than start index");
        if (start < 0) return Result.Fail("start must be non-negative");
        if (end > a.Length) return Result.Fail($"array length is only {a.Length}");
        return Result.Ok(a[start..end]);
    }

    public static Result slice_str_int_int(Span<Value> objs)
    {
        string s = objs[0].AsString();
        int start = objs[1].AsInt();
        int end = objs[2].AsInt();
        if (end <= start) return Result.Fail("end index must be greater than start index");
        if (start < 0) return Result.Fail("start index must be non-negative");
        if (end > s.Length) return Result.Fail($"string is only length {s.Length}");
        return Result.Ok(s[start..end]);
    }

    public static Result element_at_stra_int(Span<Value> objs)
    {
        string[] a = objs[0].AsStringArray();
        int idx = objs[1].AsInt();
        if (idx < 0 || idx >= a.Length) return Result.Fail("index out of bounds");
        return Result.Ok(a[idx]);
    }


    public static Result element_at_str_int(Span<Value> objs)
    {
        string s = objs[0].AsString();
        int idx = objs[1].AsInt();
        if (idx < 0 || idx >= s.Length) return Result.Fail("index out of bounds");
        return Result.Ok(s[idx].ToString());
    }

    public static Result parse_int_str(Span<Value> objs)
    {
        string s = objs[0].AsString();
        if (!int.TryParse(s, out int i))
            return Result.Fail("Failed to parse int");
        return Result.Ok(i);
    }

    public static Result can_parse_str(Span<Value> objs)
    {
        string s = objs[0].AsString();
        return Result.Ok(int.TryParse(s, out _));
    }

    public static Result remove_at_stra_int(Span<Value> objs)
    {
        string[] a = objs[0].AsStringArray();
        int i = objs[1].AsInt();
        if (a.Length <= i || i < 0) return Result.Fail($"index {i} is out of bounds");
        string[] list = new string[a.Length - 1];
        for (int j = 0; j < i; j++)
            list[j] = a[j];
        for (int j = i + 1; j < a.Length; j++)
            list[j - 1] = a[j];
        return Result.Ok(list);
    }

    public static Result trim_str(Span<Value> objs)
    {
        string s = objs[0].AsString();
        return Result.Ok(s.Trim());
    }
}