#pragma warning disable IDE1006 // Naming Styles
namespace NodeScript;

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using static CompilerUtils;
internal static class NativeFuncsKnownType
{
    public static readonly FrozenDictionary<string, NativeDelegate> NativeFunctions = GetMethods(typeof(NativeFuncsKnownType));
    public static readonly FrozenDictionary<string, Type> NativeReturnTypes = GetReturnTypes(typeof(NativeFuncsKnownType));
    public static readonly FrozenDictionary<Type, string> typeToStr = new Dictionary<Type, string>()
    {
        [typeof(string)] = "_str",
        [typeof(int)] = "_int",
        [typeof(string[])] = "_stra",
        [typeof(bool)] = "_bool",
    }.ToFrozenDictionary();

    public static Result<int> length_str(Span<object> objs)
    {
        return Result<int>.Ok(Unsafe.As<string>(objs[0]).Length);
    }

    public static Result<int> length_stra(Span<object> objs)
    {
        return Result<int>.Ok(Unsafe.As<string[]>(objs[0]).Length);
    }

    public static Result<string[]> split_str_str(Span<object> objs)
    {
        string separator = Unsafe.As<string>(objs[0]);
        string s = Unsafe.As<string>(objs[1]);
        return Result<string[]>.Ok(s.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static Result<string> join_str_stra(Span<object> objs)
    {
        string separator = Unsafe.As<string>(objs[0]);
        string[] s = Unsafe.As<string[]>(objs[1]);
        return Result<string>.Ok(string.Join(separator, s));
    }

    public static Result<int> index_of_str_str(Span<object> objs)
    {
        string search = Unsafe.As<string>(objs[0]);
        string s = Unsafe.As<string>(objs[1]);
        return Result<int>.Ok(s.IndexOf(search));
    }

    public static Result<string[]> slice_stra_int_int(Span<object> objs)
    {
        string[] a = Unsafe.As<string[]>(objs[0]);
        int start = (int)objs[1];
        int end = (int)objs[2];
        if (end <= start) return Result<string[]>.Fail("slice: end value must be larger than start value");
        if (start < 0) return Result<string[]>.Fail("slice: start must be non-negative");
        if (end > a.Length) return Result<string[]>.Fail($"slice: string is only length {a.Length}");
        return Result<string[]>.Ok(a[start..end]);
    }

    public static Result<string> slice_str_int_int(Span<object> objs)
    {
        string s = Unsafe.As<string>(objs[0]);
        int start = (int)objs[1];
        int end = (int)objs[2];
        if (end <= start) return Result<string>.Fail("slice: end value must be larger than start value");
        if (start < 0) return Result<string>.Fail("slice: start must be non-negative");
        if (end > s.Length) return Result<string>.Fail($"slice: string is only length {s.Length}");
        return Result<string>.Ok(s[start..end]);
    }

    public static Result<string> element_at_stra_int(Span<object> objs)
    {
        string[] a = Unsafe.As<string[]>(objs[0]);
        int idx = (int)objs[1];
        if (idx < 0 || idx >= a.Length) return Result<string>.Fail("index out of bounds");
        return Result<string>.Ok(a[idx]);
    }


    public static Result<string> element_at_str_int(Span<object> objs)
    {
        string s = Unsafe.As<string>(objs[0]);
        int idx = (int)objs[1];
        if (idx < 0 || idx >= s.Length) return Result<string>.Fail("index out of bounds");
        return Result<string>.Ok(s[idx].ToString());
    }

    public static Result<int> parse_int_str(Span<object> objs)
    {
        string s = Unsafe.As<string>(objs[0]);
        if (!int.TryParse(s, out int i))
            return Result<int>.Fail("Failed to parse int");
        return Result<int>.Ok(i);
    }

    public static Result<bool> can_parse_str(Span<object> objs)
    {
        string s = Unsafe.As<string>(objs[0]);
        return Result<bool>.Ok(int.TryParse(s, out _));
    }

    public static Result<string[]> remove_at_stra_int(Span<object> objs)
    {
        string[] a = Unsafe.As<string[]>(objs[0]);
        int i = (int)objs[1];
        if (a.Length <= i || i < 0) return Result<string[]>.Fail($"array does not have index {i}");
        string[] list = new string[a.Length - 1];
        for (int j = 0; j < i; j++)
            list[j] = a[j];
        for (int j = i + 1; j < a.Length; j++)
            list[j - 1] = a[j];
        return Result<string[]>.Ok(list);
    }

    public static Result<string> trim_str(Span<object> objs)
    {
        string s = Unsafe.As<string>(objs[0]);
        return Result<string>.Ok(s.Trim());
    }
}