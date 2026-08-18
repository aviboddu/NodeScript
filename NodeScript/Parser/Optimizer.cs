namespace NodeScript;

using System.Collections.Frozen;
using static NodeScript.CompilerUtils;
using static TokenType;

internal static class Optimizer
{
    // These functions are explicitly opt-in because folding a native call changes when an error is reported.
    // length is total for strings, deterministic, and does not observe mutable state.
    private static readonly FrozenSet<string> CompileTimePureNativeFunctions =
        new[] { nameof(NativeFuncs.length) }.ToFrozenSet(StringComparer.Ordinal);

    private const int MaxCompileTimeStringLength = 4096;

    public static void PropogateConstants(Operation?[] operations, InternalErrorHandler errorHandler)
    {
        ProcessBlock(operations, 0, operations.Length, new(StringComparer.Ordinal), errorHandler);
        EliminateDeadLiteralStores(operations);
    }

    private static Dictionary<string, Literal> ProcessBlock(
        Operation?[] operations, int start, int end, Dictionary<string, Literal> constants, InternalErrorHandler errorHandler)
    {
        for (int line = start; line < end; line++)
        {
            Operation? op = operations[line];
            if (op is null || op.operation == NOP) continue;

            if (op.operation == RETURN)
            {
                MarkNops(operations, line + 1, end);
                break;
            }

            if (op.operation == IF)
            {
                OptimizeExpressions(op, constants, errorHandler, line);
                (int elseLine, int endifLine) = FindIfBounds(operations, line);
                if (endifLine == -1)
                    continue; // The validator will report malformed control flow.

                if (TryGetTruthiness(op.expressions[0], out bool condition))
                {
                    operations[line] = Nop();
                    if (condition)
                    {
                        Dictionary<string, Literal> selected = ProcessBlock(
                            operations, line + 1, elseLine == -1 ? endifLine : elseLine, constants, errorHandler);
                        if (elseLine != -1) MarkNops(operations, elseLine, endifLine + 1);
                        else operations[endifLine] = Nop();
                        constants = selected;
                    }
                    else if (elseLine != -1)
                    {
                        MarkNops(operations, line + 1, elseLine + 1);
                        constants = ProcessBlock(operations, elseLine + 1, endifLine, constants, errorHandler);
                        operations[endifLine] = Nop();
                    }
                    else
                    {
                        MarkNops(operations, line + 1, endifLine + 1);
                    }
                }
                else
                {
                    Dictionary<string, Literal> whenTrue = ProcessBlock(
                        operations, line + 1, elseLine == -1 ? endifLine : elseLine, new(constants, StringComparer.Ordinal), errorHandler);
                    Dictionary<string, Literal> whenFalse = elseLine == -1
                        ? new(constants, StringComparer.Ordinal)
                        : ProcessBlock(operations, elseLine + 1, endifLine, new(constants, StringComparer.Ordinal), errorHandler);
                    constants = Intersect(whenTrue, whenFalse);
                }

                line = endifLine;
                continue;
            }

            if (op.operation == ELSE || op.operation == ENDIF)
                continue;

            OptimizeExpressions(op, constants, errorHandler, line);
            if (op.operation == SET)
            {
                string name = ((Variable)op.expressions[0]).Name.Lexeme.ToString();
                if (name is not "input" and not "mem" && IsSafeConstant(op.expressions[1]))
                    constants[name] = Copy((Literal)op.expressions[1]);
                else
                    constants.Remove(name);
            }
        }
        return constants;
    }

    private static (int elseLine, int endifLine) FindIfBounds(Operation?[] operations, int ifLine)
    {
        int depth = 0;
        int elseLine = -1;
        for (int line = ifLine + 1; line < operations.Length; line++)
        {
            switch (operations[line]?.operation)
            {
                case IF: depth++; break;
                case ENDIF:
                    if (depth-- == 0) return (elseLine, line);
                    break;
                case ELSE when depth == 0:
                    if (elseLine != -1) return (-1, -1);
                    elseLine = line;
                    break;
            }
        }
        return (-1, -1);
    }

    private static void MarkNops(Operation?[] operations, int start, int end)
    {
        for (int line = start; line < end; line++)
            if (operations[line] is not null) operations[line] = Nop();
    }

    private static Operation Nop() => new(NOP, 0);

    private static Dictionary<string, Literal> Intersect(Dictionary<string, Literal> left, Dictionary<string, Literal> right)
    {
        Dictionary<string, Literal> result = new(StringComparer.Ordinal);
        foreach ((string name, Literal value) in left)
            if (right.TryGetValue(name, out Literal? other) && Equals(value.Value, other.Value))
                result.Add(name, Copy(value));
        return result;
    }

    private static void OptimizeExpressions(Operation op, Dictionary<string, Literal> constants, InternalErrorHandler errorHandler, int line)
    {
        ExpressionOptimizer optimizer = new(constants, errorHandler, line);
        int firstExpression = op.operation == SET ? 1 : 0;
        for (int i = firstExpression; i < op.expressions.Length; i++)
            op.expressions[i] = op.expressions[i].Accept(optimizer);
    }

    private static void EliminateDeadLiteralStores(Operation?[] operations)
    {
        bool[] conditionalLines = new bool[operations.Length];
        HashSet<string> conditionalAssignments = new(StringComparer.Ordinal);
        int conditionalDepth = 0;
        for (int line = 0; line < operations.Length; line++)
        {
            Operation? op = operations[line];
            if (op?.operation == IF) conditionalDepth++;
            conditionalLines[line] = conditionalDepth > 0;
            if (conditionalLines[line] && op?.operation == SET)
                conditionalAssignments.Add(((Variable)op.expressions[0]).Name.Lexeme.ToString());
            if (op?.operation == ENDIF && conditionalDepth > 0) conditionalDepth--;
        }

        HashSet<string> live = new(StringComparer.Ordinal) { "input", "mem" };
        for (int line = operations.Length - 1; line >= 0; line--)
        {
            Operation? op = operations[line];
            if (op is null || op.operation == NOP) continue;

            if (op.operation == SET)
            {
                string name = ((Variable)op.expressions[0]).Name.Lexeme.ToString();
                if (!conditionalLines[line] && name is not "input" and not "mem" &&
                    op.expressions[1] is Literal && !live.Contains(name) && !conditionalAssignments.Contains(name))
                {
                    operations[line] = Nop();
                    continue;
                }
                live.Remove(name);
                AddVariables(op.expressions[1], live);
            }
            else
            {
                foreach (Expr expression in op.expressions)
                    AddVariables(expression, live);
            }
        }
    }

    private static void AddVariables(Expr expression, HashSet<string> variables)
    {
        switch (expression)
        {
            case Variable variable:
                variables.Add(variable.Name.Lexeme.ToString());
                break;
            case Binary binary:
                AddVariables(binary.Left, variables);
                AddVariables(binary.Right, variables);
                break;
            case Unary unary:
                AddVariables(unary.Right, variables);
                break;
            case Grouping grouping:
                AddVariables(grouping.Expression, variables);
                break;
            case Index index:
                AddVariables(index.Variable, variables);
                foreach (Expr argument in index.Arguments) AddVariables(argument, variables);
                break;
            case Call call:
                foreach (Expr argument in call.Arguments) AddVariables(argument, variables);
                break;
        }
    }

    private static bool TryGetTruthiness(Expr expression, out bool value)
    {
        if (expression is Literal literal)
        {
            value = literal.Value switch
            {
                int integer => integer != 0,
                bool boolean => boolean,
                string text => text.Length != 0,
                string[] array => array.Length != 0,
                _ => false,
            };
            return true;
        }
        value = false;
        return false;
    }

    private static bool IsSafeConstant(Expr expression) => expression is Literal literal &&
        literal.Value is int or bool or string { Length: <= MaxCompileTimeStringLength };

    private static Literal Copy(Literal literal) => new(literal.Value);

    private class ExpressionOptimizer(
        Dictionary<string, Literal> constants, InternalErrorHandler errorHandler, int lineNo) : Expr.IVisitor<Expr>
    {
        private readonly Dictionary<string, Literal> constants = constants;
        private readonly InternalErrorHandler errorHandler = errorHandler;

        public Expr VisitBinaryExpr(Binary expr)
        {
            Expr originalRight = expr.Right;
            expr.Left = expr.Left.Accept(this);
            expr.Right = expr.Right.Accept(this);
            if (expr.Op.type == SLASH && originalRight is not Literal && expr.Right is Literal { Value: 0 })
            {
                expr.Right = originalRight;
                return expr;
            }
            if (expr.Left is not Literal l || expr.Right is not Literal r)
                return expr;

            switch (expr.Op.type)
            {
                case GREATER: return new Literal((int)l.Value > (int)r.Value);
                case LESS: return new Literal((int)l.Value < (int)r.Value);
                case MINUS: return new Literal((int)l.Value - (int)r.Value);
                case STAR: return new Literal((int)l.Value * (int)r.Value);
                case SLASH:
                    if ((int)r.Value == 0 || ((int)l.Value == int.MinValue && (int)r.Value == -1))
                        return expr;
                    return new Literal((int)l.Value / (int)r.Value);
                case GREATER_EQUAL: return new Literal((int)l.Value >= (int)r.Value);
                case LESS_EQUAL: return new Literal((int)l.Value <= (int)r.Value);
                case AND: return new Literal((bool)l.Value & (bool)r.Value);
                case OR: return new Literal((bool)l.Value | (bool)r.Value);
                case EQUAL_EQUAL: return new Literal(Equals(l.Value, r.Value));
                case BANG_EQUAL: return new Literal(!Equals(l.Value, r.Value));
                case PLUS:
                    if (l.Value is string sl && r.Value is string sr && sl.Length + sr.Length <= MaxCompileTimeStringLength)
                        return new Literal(sl + sr);
                    if (l.Value is int il && r.Value is int ir)
                        return new Literal(il + ir);
                    return expr;
                default:
                    errorHandler(lineNo, "Unexpected binary operator");
                    return expr;
            }
        }

        public Expr VisitIndexExpr(Index expr)
        {
            expr.Variable = expr.Variable.Accept(this);
            for (int i = 0; i < expr.Arguments.Length; i++)
                expr.Arguments[i] = expr.Arguments[i].Accept(this);
            if (expr.Variable is not Literal { Value: string } || expr.Arguments.Any(a => a is not Literal))
                return expr;

            Expr[] arguments = expr.Arguments.Prepend(expr.Variable).ToArray();
            Result result = NativeFuncs.index_of(arguments.Select(arg => Value.FromObject(((Literal)arg).Value)).ToArray().AsSpan());
            if (!result.Success())
            {
                errorHandler(lineNo, result.message!);
                return expr;
            }
            return result.Success() && IsSafeConstant(new Literal(result.GetValue().AsObject()!))
                ? new Literal(result.GetValue().AsObject()!)
                : expr;
        }

        public Expr VisitCallExpr(Call expr)
        {
            for (int i = 0; i < expr.Arguments.Count; i++)
                expr.Arguments[i] = expr.Arguments[i].Accept(this);
            string name = expr.Callee.Name.Lexeme.ToString();
            if (!NativeFuncs.NativeFunctions.TryGetValue(name, out NativeDelegate? function))
                return expr;

            if (expr.Arguments.All(argument => argument is Literal))
            {
                Result validationResult = function.Invoke(
                    expr.Arguments.Select(argument => Value.FromObject(((Literal)argument).Value)).ToArray().AsSpan());
                if (!validationResult.Success())
                    errorHandler(lineNo, validationResult.message!);
            }

            if (!CompileTimePureNativeFunctions.Contains(name) || !expr.Arguments.All(IsSafeConstant))
                return expr;

            Result result = function.Invoke(
                expr.Arguments.Select(argument => Value.FromObject(((Literal)argument).Value)).ToArray().AsSpan());
            return result.Success() && IsSafeConstant(new Literal(result.GetValue().AsObject()!))
                ? new Literal(result.GetValue().AsObject()!)
                : expr;
        }

        public Expr VisitGroupingExpr(Grouping expr)
        {
            expr.Expression = expr.Expression.Accept(this);
            return expr.Expression is Literal ? expr.Expression : expr;
        }

        public Expr VisitLiteralExpr(Literal expr) => expr;

        public Expr VisitUnaryExpr(Unary expr)
        {
            expr.Right = expr.Right.Accept(this);
            if (expr.Right is not Literal literal) return expr;
            return expr.Op.type switch
            {
                MINUS => new Literal(-(int)literal.Value),
                BANG => new Literal(!(bool)literal.Value),
                _ => expr,
            };
        }

        public Expr VisitVariableExpr(Variable expr) =>
            constants.TryGetValue(expr.Name.Lexeme.ToString(), out Literal? literal) ? Copy(literal) : expr;
    }
}
