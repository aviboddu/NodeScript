using System.Collections.Frozen;
using System.Reflection;

namespace NodeScript;

public static class CompilerUtils
{
    public delegate void CompileErrorHandler(int line, string message);

    public static bool IsType(Type testType, Type compareType) => testType == compareType || testType == typeof(object);

    public static FrozenDictionary<string, NativeDelegate> GetMethods(Type type)
    {
        MethodInfo[] nativeFuncs = type.GetMethods(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
        Dictionary<string, NativeDelegate> methods = [];
        foreach (MethodInfo nativeMeth in nativeFuncs)
            methods.Add(nativeMeth.Name, nativeMeth.CreateDelegate<NativeDelegate>());

        return methods.ToFrozenDictionary();
    }

    public static FrozenDictionary<string, Type> GetReturnTypes(Type type)
    {
        MethodInfo[] nativeFuncs = type.GetMethods(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
        Dictionary<string, Type> methods = [];
        foreach (MethodInfo nativeMeth in nativeFuncs)
            methods.Add(nativeMeth.Name, nativeMeth.ReturnType.GenericTypeArguments[0]);

        return methods.ToFrozenDictionary();
    }
}