namespace NodeScript;

using System.Collections.Frozen;
using static CompilerUtils;

public static class NativeFuncs
{
    public static readonly FrozenDictionary<string, NativeDelegate> NativeFunctions = GetMethods(typeof(NativeFuncs));
    public static readonly FrozenDictionary<string, Type> NativeReturnTypes = GetReturnTypes(typeof(NativeFuncs));

    public static Result<int> length(Span<object> objs)
    {

        if (objs.Length != 1) return Result<int>.Fail("length takes exactly one parameter");
        return objs[0] switch
        {
            int n => Result<int>.Fail("Cannot find the length of an integer"),
            bool b => Result<int>.Fail("Cannot find the length of a boolean"),
            string s => Result<int>.Ok(s.Length),
            string[] a => Result<int>.Ok(a.Length),
            _ => Result<int>.Fail("Unknown parameter"),
        };
    }

    public static Result<string[]> split(Span<object> objs)
    {
        if (objs.Length != 2) return Result<string[]>.Fail("split takes two parameters");
        if (objs[0] is not string || objs[1] is not string)
        {
            return Result<string[]>.Fail("Both parameters for split must be a string");
        }
        string separator = (string)objs[0];
        string s = (string)objs[1];
        return Result<string[]>.Ok(s.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static Result<string> join(Span<object> objs)
    {
        if (objs.Length != 2) return Result<string>.Fail("join takes two parameters");
        if (objs[0] is not string || objs[1] is not string[])
        {
            return Result<string>.Fail("join takes one string and one string array");
        }
        string separator = (string)objs[0];
        string[] s = (string[])objs[1];
        return Result<string>.Ok(string.Join(separator, s));
    }

    public static Result<int> index_of(Span<object> objs)
    {
        if (objs.Length != 2) return Result<int>.Fail("index_of takes two parameters");
        if (objs[0] is not string || objs[1] is not string)
        {
            return Result<int>.Fail("index_of takes two strings");
        }
        string search = (string)objs[0];
        string s = (string)objs[1];
        return Result<int>.Ok(s.IndexOf(search));
    }

    public static Result<object> slice(Span<object> objs)
    {
        if (objs.Length != 3) return Result<object>.Fail("slice takes three parameters");
        if (!(objs[0] is string || objs[0] is string[]) || objs[1] is not int || objs[2] is not int)
            return Result<object>.Fail("slice takes one string or string array and two ints");

        int start = (int)objs[1];
        int end = (int)objs[2];
        if (objs[0] is string s)
            return Result<object>.Ok(s.Substring(start, end));
        return Result<object>.Ok(((string[])objs[0])[start..end]);
    }

    public static Result<string> element_at(Span<object> objs)
    {
        if (objs.Length != 2) return Result<string>.Fail("element_at takes two parameters");
        if (!(objs[0] is string || objs[0] is string[]) || objs[1] is not int)
        {
            return Result<string>.Fail("element_at takes one string or string array and one int");
        }
        int idx = (int)objs[1];
        if (objs[0] is string s)
            return Result<string>.Ok(s[idx].ToString());
        return Result<string>.Ok(((string[])objs[0])[idx]);
    }

    public static Result<string> to_string(Span<object> objs)
    {
        if (objs.Length != 1) return Result<string>.Fail("to_string takes one parameter");
        return Result<string>.Ok(objs[0].ToString()!);
    }

    public static Result<int> parse_int(Span<object> objs)
    {
        if (objs.Length != 1) return Result<int>.Fail("parse_int takes one parameter");
        if (objs[0] is not string s) return Result<int>.Fail("parse_int takes in a single string");
        if (!int.TryParse(s, out int i))
            return Result<int>.Fail("Failed to parse int");
        return Result<int>.Ok(i);
    }

    public static Result<bool> can_parse(Span<object> objs)
    {
        if (objs.Length != 1) return Result<bool>.Fail("can_parse takes one parameter");
        if (objs[0] is not string s) return Result<bool>.Ok(false);
        return Result<bool>.Ok(int.TryParse(s, out _));
    }

    public static Result<string[]> remove_at(Span<object> objs)
    {
        if (objs.Length != 2) return Result<string[]>.Fail("remove_at takes two parameters");
        if (objs[0] is not string[] a || objs[1] is not int i)
            return Result<string[]>.Fail("remove_at takes in a string array and an int");
        if (a.Length <= i) return Result<string[]>.Fail($"array does not have index {i}");
        string[] list = new string[a.Length - 1];
        for (int j = 0; j < i; j++)
            list[j] = a[j];
        for (int j = i + 1; j < a.Length; j++)
            list[j - 1] = a[j];
        return Result<string[]>.Ok([.. list]);
    }
}

public delegate Result NativeDelegate(Span<object> parameters);