using System.Collections.Frozen;
using System.Reflection;

namespace NodeScript;

public static class NativeFuncs
{
    public static readonly FrozenDictionary<string, NativeDelegate> NativeFunctions = GetMethods();

    public delegate object NativeDelegate(Span<object> parameters);
    private static FrozenDictionary<string, NativeDelegate> GetMethods()
    {
        MethodInfo[] nativeFuncs = typeof(NativeFuncs).GetMethods(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
        Dictionary<string, NativeDelegate> methods = [];
        foreach (MethodInfo nativeMeth in nativeFuncs)
            methods.Add(nativeMeth.Name, nativeMeth.CreateDelegate<NativeDelegate>());

        return methods.ToFrozenDictionary();
    }

    public static object length(Span<object> objs)
    {

        if (objs.Length != 1) return new Err("length takes exactly one parameter");
        return objs[0] switch
        {
            int n => new Err("Cannot find the length of an integer"),
            bool b => new Err("Cannot find the length of a boolean"),
            string s => s.Length,
            string[] a => a.Length,
            _ => new Err("Unknown parameter"),
        };
    }

    public static object split(Span<object> objs)
    {
        if (objs.Length != 2) return new Err("split takes two parameters");
        if (objs[0] is not string || objs[1] is not string)
        {
            return new Err("Both parameters for split must be a string");
        }
        string separator = (string)objs[0];
        string s = (string)objs[1];
        return s.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static object join(Span<object> objs)
    {
        if (objs.Length != 2) return new Err("join takes two parameters");
        if (objs[0] is not string || objs[1] is not string[])
        {
            return new Err("join takes one string and one string array");
        }
        string separator = (string)objs[0];
        string[] s = (string[])objs[1];
        return string.Join(separator, s);
    }

    public static object index_of(Span<object> objs)
    {
        if (objs.Length != 2) return new Err("index_of takes two parameters");
        if (objs[0] is not string || objs[1] is not string)
        {
            return new Err("index_of takes two strings");
        }
        string search = (string)objs[0];
        string s = (string)objs[1];
        return s.IndexOf(search);
    }

    public static object slice(Span<object> objs)
    {
        if (objs.Length != 3) return new Err("slice takes three parameters");
        if (!(objs[0] is string || objs[0] is string[]) || objs[1] is not int || objs[2] is not int)
        {
            return new Err("slice takes one string or string array and two ints");
        }
        int start = (int)objs[1];
        int end = (int)objs[2];
        if (objs[0] is string s)
            return s.Substring(start, end);
        return ((string[])objs[0])[start..end];
    }

    public static object element_at(Span<object> objs)
    {
        if (objs.Length != 2) return new Err("element_at takes two parameters");
        if (!(objs[0] is string || objs[0] is string[]) || objs[1] is not int)
        {
            return new Err("element_at takes one string or string array and one int");
        }
        int idx = (int)objs[1];
        if (objs[0] is string s)
            return s[idx].ToString();
        return ((string[])objs[0])[idx];
    }
}

public class Err(string msg)
{
    public readonly string msg = msg;
}