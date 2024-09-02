using System.Collections.Frozen;
using System.Reflection;

namespace NodeScript;

internal static class CompilerUtils
{
    public const int INPUT_VARIABLE_IDX = 0;
    public const int MEM_VARIABLE_IDX = 1;

    public delegate void InternalErrorHandler(int line, string message);

    public static bool IsTypeOrObj(Type testType, Type compareType) => testType == compareType || testType == typeof(object);

    // This method is used for native functions. It creates a dictionary mapping the function names to the delegate.
    public static FrozenDictionary<string, NativeDelegate> GetMethods(Type type)
    {
        MethodInfo[] nativeFuncs = type.GetMethods(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
        Dictionary<string, NativeDelegate> methods = [];
        foreach (MethodInfo nativeMeth in nativeFuncs)
            methods.Add(nativeMeth.Name, nativeMeth.CreateDelegate<NativeDelegate>());

        return methods.ToFrozenDictionary();
    }

    // This method is used for native functions. It creates a dictionary mapping the function names to the function's return type.
    public static FrozenDictionary<string, Type> GetReturnTypes(Type type)
    {
        MethodInfo[] nativeFuncs = type.GetMethods(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
        Dictionary<string, Type> methods = [];
        foreach (MethodInfo nativeMeth in nativeFuncs)
            methods.Add(nativeMeth.Name, nativeMeth.ReturnType.GenericTypeArguments[0]);

        return methods.ToFrozenDictionary();
    }
}