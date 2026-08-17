namespace NodeScript;

internal readonly record struct NativeFunctionDescriptor(
    byte Id,
    string Name,
    Type ReturnType,
    Type[] ParameterTypes,
    NativeDelegate Target,
    bool IsDynamicBoundary);

internal static class NativeFunctionRegistry
{
    private static readonly NativeFunctionDescriptor Length = new(0, "length", typeof(int), [typeof(object)], NativeFuncs.length, true);
    private static readonly NativeFunctionDescriptor LengthStr = new(1, "length", typeof(int), [typeof(string)], NativeFuncsKnownType.length_str, false);
    private static readonly NativeFunctionDescriptor LengthStra = new(2, "length", typeof(int), [typeof(string[])], NativeFuncsKnownType.length_stra, false);
    private static readonly NativeFunctionDescriptor Split = new(3, "split", typeof(string[]), [typeof(object), typeof(object)], NativeFuncs.split, true);
    private static readonly NativeFunctionDescriptor SplitStrStr = new(4, "split", typeof(string[]), [typeof(string), typeof(string)], NativeFuncsKnownType.split_str_str, false);
    private static readonly NativeFunctionDescriptor Join = new(5, "join", typeof(string), [typeof(object), typeof(object)], NativeFuncs.join, true);
    private static readonly NativeFunctionDescriptor JoinStrStra = new(6, "join", typeof(string), [typeof(string), typeof(string[])], NativeFuncsKnownType.join_str_stra, false);
    private static readonly NativeFunctionDescriptor IndexOf = new(7, "index_of", typeof(int), [typeof(object), typeof(object)], NativeFuncs.index_of, true);
    private static readonly NativeFunctionDescriptor IndexOfStrStr = new(8, "index_of", typeof(int), [typeof(string), typeof(string)], NativeFuncsKnownType.index_of_str_str, false);
    private static readonly NativeFunctionDescriptor Slice = new(9, "slice", typeof(object), [typeof(object), typeof(object), typeof(object)], NativeFuncs.slice, true);
    private static readonly NativeFunctionDescriptor SliceStrIntInt = new(10, "slice", typeof(string), [typeof(string), typeof(int), typeof(int)], NativeFuncsKnownType.slice_str_int_int, false);
    private static readonly NativeFunctionDescriptor SliceStraIntInt = new(11, "slice", typeof(string[]), [typeof(string[]), typeof(int), typeof(int)], NativeFuncsKnownType.slice_stra_int_int, false);
    private static readonly NativeFunctionDescriptor ElementAt = new(12, "element_at", typeof(string), [typeof(object), typeof(object)], NativeFuncs.element_at, true);
    private static readonly NativeFunctionDescriptor ElementAtStrInt = new(13, "element_at", typeof(string), [typeof(string), typeof(int)], NativeFuncsKnownType.element_at_str_int, false);
    private static readonly NativeFunctionDescriptor ElementAtStraInt = new(14, "element_at", typeof(string), [typeof(string[]), typeof(int)], NativeFuncsKnownType.element_at_stra_int, false);
    private static readonly NativeFunctionDescriptor ToStringFunc = new(15, "to_string", typeof(string), [typeof(object)], NativeFuncs.to_string, true);
    private static readonly NativeFunctionDescriptor ParseInt = new(16, "parse_int", typeof(int), [typeof(object)], NativeFuncs.parse_int, true);
    private static readonly NativeFunctionDescriptor ParseIntStr = new(17, "parse_int", typeof(int), [typeof(string)], NativeFuncsKnownType.parse_int_str, false);
    private static readonly NativeFunctionDescriptor CanParse = new(18, "can_parse", typeof(bool), [typeof(object)], NativeFuncs.can_parse, true);
    private static readonly NativeFunctionDescriptor CanParseStr = new(19, "can_parse", typeof(bool), [typeof(string)], NativeFuncsKnownType.can_parse_str, false);
    private static readonly NativeFunctionDescriptor RemoveAt = new(20, "remove_at", typeof(string[]), [typeof(object), typeof(object)], NativeFuncs.remove_at, true);
    private static readonly NativeFunctionDescriptor RemoveAtStraInt = new(21, "remove_at", typeof(string[]), [typeof(string[]), typeof(int)], NativeFuncsKnownType.remove_at_stra_int, false);
    private static readonly NativeFunctionDescriptor Trim = new(22, "trim", typeof(string), [typeof(object)], NativeFuncs.trim, true);
    private static readonly NativeFunctionDescriptor TrimStr = new(23, "trim", typeof(string), [typeof(string)], NativeFuncsKnownType.trim_str, false);

    private static readonly NativeFunctionDescriptor[] AllDescriptors =
    [
        Length, LengthStr, LengthStra,
        Split, SplitStrStr,
        Join, JoinStrStra,
        IndexOf, IndexOfStrStr,
        Slice, SliceStrIntInt, SliceStraIntInt,
        ElementAt, ElementAtStrInt, ElementAtStraInt,
        ToStringFunc,
        ParseInt, ParseIntStr,
        CanParse, CanParseStr,
        RemoveAt, RemoveAtStraInt,
        Trim, TrimStr
    ];

    private static readonly NativeFunctionDescriptor[] LengthOverloads = [LengthStr, LengthStra, Length];
    private static readonly NativeFunctionDescriptor[] SplitOverloads = [SplitStrStr, Split];
    private static readonly NativeFunctionDescriptor[] JoinOverloads = [JoinStrStra, Join];
    private static readonly NativeFunctionDescriptor[] IndexOfOverloads = [IndexOfStrStr, IndexOf];
    private static readonly NativeFunctionDescriptor[] SliceOverloads = [SliceStrIntInt, SliceStraIntInt, Slice];
    private static readonly NativeFunctionDescriptor[] ElementAtOverloads = [ElementAtStrInt, ElementAtStraInt, ElementAt];
    private static readonly NativeFunctionDescriptor[] ToStringOverloads = [ToStringFunc];
    private static readonly NativeFunctionDescriptor[] ParseIntOverloads = [ParseIntStr, ParseInt];
    private static readonly NativeFunctionDescriptor[] CanParseOverloads = [CanParseStr, CanParse];
    private static readonly NativeFunctionDescriptor[] RemoveAtOverloads = [RemoveAtStraInt, RemoveAt];
    private static readonly NativeFunctionDescriptor[] TrimOverloads = [TrimStr, Trim];

    public static int Count => AllDescriptors.Length;

    public static bool FunctionExists(string name) => TryGetOverloads(name, out _);

    public static bool TryGetById(byte id, out NativeFunctionDescriptor descriptor)
    {
        if (id < AllDescriptors.Length)
        {
            descriptor = AllDescriptors[id];
            return true;
        }
        descriptor = default;
        return false;
    }

    public static bool TryResolveForCompile(
        string name,
        ReadOnlySpan<Type> argumentTypes,
        out NativeFunctionDescriptor descriptor,
        out string? error)
    {
        if (!TryGetOverloads(name, out NativeFunctionDescriptor[] overloads))
        {
            descriptor = default;
            error = $"Function {name} does not exist";
            return false;
        }

        for (int i = 0; i < overloads.Length; i++)
        {
            NativeFunctionDescriptor candidate = overloads[i];
            if (MatchesExact(candidate, argumentTypes))
            {
                descriptor = candidate;
                error = null;
                return true;
            }
        }

        bool hasUnknownType = false;
        for (int i = 0; i < argumentTypes.Length; i++)
        {
            if (argumentTypes[i] == typeof(object))
            {
                hasUnknownType = true;
                break;
            }
        }

        if (hasUnknownType)
        {
            if (TryGetDynamicBoundary(overloads, argumentTypes.Length, out NativeFunctionDescriptor dynamicBoundary))
            {
                descriptor = dynamicBoundary;
                error = null;
                return true;
            }
        }

        if (TryGetDynamicBoundary(overloads, argumentTypes.Length, out NativeFunctionDescriptor dynamicFallback)
            && AllowsKnownTypeDynamicFallback(name))
        {
            descriptor = dynamicFallback;
            error = null;
            return true;
        }

        descriptor = default;
        if (HasOverloadWithArity(overloads, argumentTypes.Length))
            error = $"No overload for function {name} takes parameters ({FormatArgumentTypes(argumentTypes)})";
        else
            error = $"Function {name} does not take {argumentTypes.Length} parameter{(argumentTypes.Length == 1 ? string.Empty : "s")}";
        return false;
    }

    public static bool TryGetReturnType(string name, ReadOnlySpan<Type> argumentTypes, out Type? returnType)
    {
        if (TryResolveForCompile(name, argumentTypes, out NativeFunctionDescriptor descriptor, out _))
        {
            returnType = descriptor.ReturnType;
            return true;
        }

        if (!TryGetOverloads(name, out NativeFunctionDescriptor[] overloads))
        {
            returnType = null;
            return false;
        }

        for (int i = 0; i < overloads.Length; i++)
        {
            if (overloads[i].IsDynamicBoundary)
            {
                returnType = overloads[i].ReturnType;
                return true;
            }
        }
        returnType = null;
        return false;
    }

    private static bool TryGetOverloads(string name, out NativeFunctionDescriptor[] overloads)
    {
        switch (name)
        {
            case "length": overloads = LengthOverloads; return true;
            case "split": overloads = SplitOverloads; return true;
            case "join": overloads = JoinOverloads; return true;
            case "index_of": overloads = IndexOfOverloads; return true;
            case "slice": overloads = SliceOverloads; return true;
            case "element_at": overloads = ElementAtOverloads; return true;
            case "to_string": overloads = ToStringOverloads; return true;
            case "parse_int": overloads = ParseIntOverloads; return true;
            case "can_parse": overloads = CanParseOverloads; return true;
            case "remove_at": overloads = RemoveAtOverloads; return true;
            case "trim": overloads = TrimOverloads; return true;
            default:
                overloads = [];
                return false;
        }
    }

    private static bool MatchesExact(NativeFunctionDescriptor descriptor, ReadOnlySpan<Type> argumentTypes)
    {
        if (descriptor.ParameterTypes.Length != argumentTypes.Length)
            return false;
        for (int i = 0; i < argumentTypes.Length; i++)
            if (descriptor.ParameterTypes[i] != argumentTypes[i])
                return false;
        return true;
    }

    private static bool HasOverloadWithArity(ReadOnlySpan<NativeFunctionDescriptor> overloads, int arity)
    {
        for (int i = 0; i < overloads.Length; i++)
            if (overloads[i].ParameterTypes.Length == arity)
                return true;
        return false;
    }

    private static bool TryGetDynamicBoundary(ReadOnlySpan<NativeFunctionDescriptor> overloads, int arity, out NativeFunctionDescriptor descriptor)
    {
        for (int i = 0; i < overloads.Length; i++)
        {
            if (overloads[i].IsDynamicBoundary && overloads[i].ParameterTypes.Length == arity)
            {
                descriptor = overloads[i];
                return true;
            }
        }
        descriptor = default;
        return false;
    }

    private static bool AllowsKnownTypeDynamicFallback(string name) =>
        name is "to_string" or "can_parse";

    private static string FormatArgumentTypes(ReadOnlySpan<Type> argumentTypes)
    {
        if (argumentTypes.Length == 0)
            return string.Empty;
        if (argumentTypes.Length == 1)
            return argumentTypes[0].Name;

        string[] names = new string[argumentTypes.Length];
        for (int i = 0; i < argumentTypes.Length; i++)
            names[i] = argumentTypes[i].Name;
        return string.Join(", ", names);
    }
}
