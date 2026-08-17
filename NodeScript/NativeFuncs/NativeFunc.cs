#pragma warning disable IDE1006 // Naming Styles
namespace NodeScript;

using System.Collections.Frozen;
using static CompilerUtils;
using static NativeFuncsKnownType;

internal delegate Result NativeDelegate(Span<Value> parameters);

internal static class NativeFuncs
{
    public static readonly FrozenDictionary<string, NativeDelegate> NativeFunctions = GetMethods(typeof(NativeFuncs));
    public static readonly FrozenDictionary<string, Type> NativeReturnTypes = new Dictionary<string, Type>()
    {
        [nameof(length)] = typeof(int),
        [nameof(split)] = typeof(string[]),
        [nameof(join)] = typeof(string),
        [nameof(index_of)] = typeof(int),
        [nameof(slice)] = typeof(object),
        [nameof(element_at)] = typeof(string),
        [nameof(to_string)] = typeof(string),
        [nameof(parse_int)] = typeof(int),
        [nameof(can_parse)] = typeof(bool),
        [nameof(remove_at)] = typeof(string[]),
        [nameof(trim)] = typeof(string),
    }.ToFrozenDictionary();

    public static Result length(Span<Value> objs)
    {

        if (objs.Length != 1) return Result.Fail("length takes exactly one parameter");

        return objs[0].Kind switch
        {
            ValueKind.Int => Result.Fail("Cannot find the length of an integer"),
            ValueKind.Bool => Result.Fail("Cannot find the length of a boolean"),
            ValueKind.String => Result.Ok(objs[0].AsString().Length),
            ValueKind.StringArray => Result.Ok(objs[0].AsStringArray().Length),
            _ => Result.Fail("Unknown parameter"),
        };
    }

    public static Result split(Span<Value> objs)
    {
        if (objs.Length != 2) return Result.Fail("split takes two parameters");
        if (objs[0].Kind != ValueKind.String || objs[1].Kind != ValueKind.String)
            return Result.Fail("Both parameters for split must be a string");
        return split_str_str(objs);
    }

    public static Result join(Span<Value> objs)
    {
        if (objs.Length != 2) return Result.Fail("join takes two parameters");
        if (objs[0].Kind != ValueKind.String || objs[1].Kind != ValueKind.StringArray)
            return Result.Fail("join takes one string and one string array");

        return join_str_stra(objs);
    }

    public static Result index_of(Span<Value> objs)
    {
        if (objs.Length != 2) return Result.Fail("index_of takes two parameters");
        if (objs[0].Kind != ValueKind.String || objs[1].Kind != ValueKind.String)
            return Result.Fail("index_of takes two strings");

        return index_of_str_str(objs);
    }

    public static Result slice(Span<Value> objs)
    {
        if (objs.Length != 3) return Result.Fail("slice takes three parameters");
        if (!(objs[0].Kind is ValueKind.String or ValueKind.StringArray) || objs[1].Kind != ValueKind.Int || objs[2].Kind != ValueKind.Int)
            return Result.Fail("slice takes one string or string array and two ints");

        return objs[0].Kind == ValueKind.String
            ? slice_str_int_int(objs)
            : slice_stra_int_int(objs);
    }

    public static Result element_at(Span<Value> objs)
    {
        if (objs.Length != 2) return Result.Fail("element_at takes two parameters");
        if (!(objs[0].Kind is ValueKind.String or ValueKind.StringArray) || objs[1].Kind != ValueKind.Int)
            return Result.Fail("element_at takes one string or string array and one int");

        if (objs[0].Kind == ValueKind.String)
            return element_at_str_int(objs);
        return element_at_stra_int(objs);
    }

    public static Result to_string(Span<Value> objs)
    {
        if (objs.Length != 1) return Result.Fail("to_string takes one parameter");
        return Result.Ok(objs[0].AsObject()?.ToString() ?? string.Empty);
    }

    public static Result parse_int(Span<Value> objs)
    {
        if (objs.Length != 1) return Result.Fail("parse_int takes one parameter");
        if (objs[0].Kind != ValueKind.String) return Result.Fail("parse_int takes in a single string");
        return parse_int_str(objs);
    }

    public static Result can_parse(Span<Value> objs)
    {
        if (objs.Length != 1) return Result.Fail("can_parse takes one parameter");
        if (objs[0].Kind != ValueKind.String) return Result.Ok(false);
        return can_parse_str(objs);
    }

    public static Result remove_at(Span<Value> objs)
    {
        if (objs.Length != 2) return Result.Fail("remove_at takes two parameters");
        if (objs[0].Kind != ValueKind.StringArray || objs[1].Kind != ValueKind.Int)
            return Result.Fail("remove_at takes in a string array and an int");
        return remove_at_stra_int(objs);
    }

    public static Result trim(Span<Value> objs)
    {
        if (objs.Length != 1) return Result.Fail("trim takes one parameter");
        if (objs[0].Kind != ValueKind.String)
            return Result.Fail("trim takes in a string");
        return trim_str(objs);
    }
}