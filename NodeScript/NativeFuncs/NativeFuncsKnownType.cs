using System.Collections.Frozen;

namespace NodeScript;

public static class NativeFuncsKnownType
{
    public static readonly FrozenDictionary<string, NativeDelegate> NativeFunctions = NativeFuncs.GetMethods(typeof(NativeFuncsKnownType));
    public static readonly FrozenDictionary<string, Type> NativeReturnTypes = NativeFuncs.GetReturnTypes(typeof(NativeFuncsKnownType));
    public static readonly FrozenDictionary<Type, string> typeToStr = new Dictionary<Type, string>()
    {
        [typeof(string)] = "_str",
        [typeof(int)] = "_int",
        [typeof(string[])] = "_stra",
        [typeof(bool)] = "_bool",
    }.ToFrozenDictionary();

    public static Result<int> length_str(Span<object> objs)
    {
        return Result<int>.Ok(((string)objs[0]).Length);
    }

    public static Result<int> length_stra(Span<object> objs)
    {
        return Result<int>.Ok(((string[])objs[0]).Length);
    }

    public static Result<string[]> split_str_str(Span<object> objs)
    {
        string separator = (string)objs[0];
        string s = (string)objs[1];
        return Result<string[]>.Ok(s.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static Result<string> join_str_stra(Span<object> objs)
    {
        string separator = (string)objs[0];
        string[] s = (string[])objs[1];
        return Result<string>.Ok(string.Join(separator, s));
    }

    public static Result<int> index_of_str_str(Span<object> objs)
    {
        string search = (string)objs[0];
        string s = (string)objs[1];
        return Result<int>.Ok(s.IndexOf(search));
    }

    public static object slice_stra_int_int(Span<object> objs)
    {
        string[] a = (string[])objs[0];
        int start = (int)objs[1];
        int end = (int)objs[2];
        return Result<string[]>.Ok(a[start..end]);
    }

    public static object slice_str_int_int(Span<object> objs)
    {
        string s = (string)objs[0];
        int start = (int)objs[1];
        int end = (int)objs[2];
        return Result<string>.Ok(s.Substring(start, end));
    }

    public static Result<string> element_at_stra(Span<object> objs)
    {
        string[] a = (string[])objs[0];
        int idx = (int)objs[1];
        return Result<string>.Ok(a[idx]);
    }


    public static Result<string> element_at_str(Span<object> objs)
    {
        string s = (string)objs[0];
        int idx = (int)objs[1];
        return Result<string>.Ok(s[idx].ToString());
    }

    public static Result<int> parse_int_str(Span<object> objs)
    {
        string s = (string)objs[0];
        if (!int.TryParse(s, out int i))
            return Result<int>.Fail("Failed to parse int");
        return Result<int>.Ok(i);
    }

    public static Result<bool> can_parse_str(Span<object> objs)
    {
        string s = (string)objs[0];
        return Result<bool>.Ok(int.TryParse(s, out _));
    }

    public static Result<string[]> remove_at_stra_int(Span<object> objs)
    {
        string[] a = (string[])objs[0];
        int i = (int)objs[1];
        if (a.Length <= i) return Result<string[]>.Fail($"array does not have index {i}");
        string[] list = new string[a.Length - 1];
        for (int j = 0; j < i; j++)
            list[j] = a[j];
        for (int j = i + 1; j < a.Length; j++)
            list[j - 1] = a[j];
        return Result<string[]>.Ok(list);
    }
}