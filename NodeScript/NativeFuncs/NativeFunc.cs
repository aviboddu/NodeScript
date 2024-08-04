using System.Collections.Frozen;
using System.Reflection;

namespace NodeScript;

public static class NativeFuncs
{
    public static FrozenDictionary<string, NativeDelegate> NativeFunctions = GetMethods();

    public delegate object NativeDelegate(object[] paramaters);
    public static FrozenDictionary<string, NativeDelegate> GetMethods()
    {
        MethodInfo[] nativeFuncs = typeof(NativeFuncs).GetMethods(BindingFlags.Static | BindingFlags.DeclaredOnly);
        Dictionary<string, NativeDelegate> methods = [];
        foreach (MethodInfo nativeMeth in nativeFuncs)
            methods.Add(nativeMeth.Name, nativeMeth.CreateDelegate<NativeDelegate>());

        return methods.ToFrozenDictionary();
    }

    public static object length(object[] objs)
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
}

public class Err(string msg)
{
    public readonly string msg = msg;
}