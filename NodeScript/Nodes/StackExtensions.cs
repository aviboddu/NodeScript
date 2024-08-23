namespace NodeScript;

using System.Reflection;
using System.Reflection.Emit;

internal static class StackExtensions
{
    static class ArrayAccessor<T>
    {
        public static Func<Stack<T>, T[]> Getter;

        static ArrayAccessor()
        {
            var dm = new DynamicMethod("get", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(T[]), [typeof(Stack<T>)], typeof(ArrayAccessor<T>), true);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // Load Stack<T> argument
            il.Emit(OpCodes.Ldfld, typeof(Stack<T>).GetField("_array", BindingFlags.NonPublic | BindingFlags.Instance)!); // Replace argument by field
            il.Emit(OpCodes.Ret); // Return field
            Getter = (Func<Stack<T>, T[]>)dm.CreateDelegate(typeof(Func<Stack<T>, T[]>));
        }
    }

    public static T[] GetInternalArray<T>(this Stack<T> stack)
    {
        return ArrayAccessor<T>.Getter(stack);
    }
}