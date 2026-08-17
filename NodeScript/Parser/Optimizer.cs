namespace NodeScript;

using static NodeScript.CompilerUtils;
using static TokenType;

internal static class Optimizer
{
    public static void PropogateConstants(Operation?[] operations, InternalErrorHandler errorHandler)
    {
        for (int currentLine = 0; currentLine < operations.Length; currentLine++)
        {
            Operation? op = operations[currentLine];
            if (op is null) continue;
            ExpressionOptimizer optimizer = new(errorHandler, currentLine);
            for (int i = 0; i < op.expressions.Length; i++)
                op.expressions[i] = op.expressions[i].Accept(optimizer);
        }
    }

    private class ExpressionOptimizer(InternalErrorHandler errorHandler, int lineNo) : Expr.IVisitor<Expr>
    {
        private readonly InternalErrorHandler errorHandler = errorHandler;

        public Expr VisitBinaryExpr(Binary expr)
        {
            expr.Left = expr.Left.Accept(this);
            expr.Right = expr.Right.Accept(this);
            if (expr.Left is not Literal && expr.Right is not Literal)
                return expr;
            if (expr.Left is Literal l && expr.Right is Literal r)
            {
                switch (expr.Op.type)
                {
                    case GREATER: return new Literal((int)l.Value > (int)r.Value);
                    case LESS: return new Literal((int)l.Value < (int)r.Value);
                    case MINUS: return new Literal((int)l.Value - (int)r.Value);
                    case STAR: return new Literal((int)l.Value * (int)r.Value);
                    case SLASH:
                        if ((int)r.Value == 0)
                            return expr; // Division by zero is reported by the validator
                        return new Literal((int)l.Value / (int)r.Value);
                    case GREATER_EQUAL: return new Literal((int)l.Value >= (int)r.Value);
                    case LESS_EQUAL: return new Literal((int)l.Value <= (int)r.Value);
                    case AND: return new Literal((bool)l.Value & (bool)r.Value);
                    case OR: return new Literal((bool)l.Value | (bool)r.Value);
                    case PLUS:
                        if (l.Value is string sl && r.Value is string sr)
                            return new Literal(sl + sr);
                        if (l.Value is int il && r.Value is int ir)
                            return new Literal(ir + il);
                        errorHandler(lineNo, "Illegal binary expression");
                        return expr;
                    default:
                        errorHandler(lineNo, "Unexpected binary operator");
                        return expr;
                }
            }
            else if (expr.Left is Literal l1)
            {
                switch (expr.Op.type)
                {
                    case STAR:
                        if (l1.Value is int il)
                        {
                            switch (il)
                            {
                                case -1:
                                    return new Unary(new Token(MINUS, 0, 0, string.Empty), expr.Right);
                                case 0:
                                    return new Literal(0);
                                case 1:
                                    return expr.Right;
                            }
                        }
                        return expr;
                    case AND:
                        if (l1.Value is bool bl && bl == false)
                            return new Literal(false);
                        return expr;
                    case OR:
                        if (l1.Value is bool bl2 && bl2 == true)
                            return new Literal(true);
                        return expr;
                    case PLUS:
                        if (l1.Value is int il2 && il2 == 0)
                            return expr.Right;
                        return expr;
                    case SLASH:
                        if (l1.Value is int il3 && il3 == 0)
                            return new Literal(0);
                        return expr;
                    case MINUS:
                        if (l1.Value is int il4 && il4 == 0)
                            return new Unary(new Token(MINUS, 0, 0, string.Empty), expr.Right);
                        return expr;
                    case GREATER:
                    case GREATER_EQUAL:
                    case LESS:
                    case LESS_EQUAL:
                        return expr;
                    default:
                        errorHandler(lineNo, "Unexpected binary operator");
                        return expr;
                }
            }
            else
            {
                Literal r1 = (Literal)expr.Right;
                switch (expr.Op.type)
                {
                    case STAR:
                        if (r1.Value is int ir)
                        {
                            switch (ir)
                            {
                                case -1:
                                    return new Unary(new Token(MINUS, 0, 0, string.Empty), expr.Left);
                                case 0:
                                    return new Literal(0);
                                case 1:
                                    return expr.Left;
                            }
                        }
                        return expr;
                    case AND:
                        if (r1.Value is bool br && br == false)
                            return new Literal(false);
                        return expr;
                    case OR:
                        if (r1.Value is bool br2 && br2 == true)
                            return new Literal(true);
                        return expr;
                    case PLUS:
                        if (r1.Value is int ir2 && ir2 == 0)
                            return expr.Right;
                        return expr;
                    case SLASH:
                        if (r1.Value is int ir3 && ir3 == 1)
                            return expr.Left;
                        return expr;
                    case MINUS:
                        if (r1.Value is int ir4 && ir4 == 0)
                            return expr.Left;
                        return expr;
                    case GREATER:
                    case GREATER_EQUAL:
                    case LESS:
                    case LESS_EQUAL:
                        return expr;
                    default:
                        errorHandler(lineNo, "Unexpected binary operator");
                        return expr;
                }
            }
        }

        public Expr VisitIndexExpr(Index expr)
        {
            expr.Variable = expr.Variable.Accept(this);
            for (int i = 0; i < expr.Arguments.Length; i++)
                expr.Arguments[i] = expr.Arguments[i].Accept(this);
            if (expr.Variable is not Literal || expr.Arguments.Any(a => a is not Literal))
                return expr;
            Expr[] func_args = expr.Arguments.Prepend(expr.Variable).ToArray();
            Result result = NativeFuncs.index_of(func_args.Select(arg => Value.FromObject(((Literal)arg).Value)).ToArray().AsSpan());
            if (!result.Success())
            {
                errorHandler(lineNo, result.message!);
                return expr;
            }
            else
                return new Literal(result.GetValue().AsObject()!);

        }

        public Expr VisitCallExpr(Call expr)
        {
            for (int i = 0; i < expr.Arguments.Count; i++)
                expr.Arguments[i] = expr.Arguments[i].Accept(this);
            if (!expr.Arguments.All((a) => a is Literal))
                return expr;

            string name = expr.Callee.Name.Lexeme.ToString();
            NativeDelegate func = NativeFuncs.NativeFunctions[name];
            Result val = func.Invoke(expr.Arguments.Select((expr) => Value.FromObject(((Literal)expr).Value)).ToArray().AsSpan());
            if (!val.Success())
            {
                errorHandler.Invoke(lineNo, val.message!);
                return expr;
            }
            return new Literal(val.GetValue().AsObject()!);
        }

        public Expr VisitGroupingExpr(Grouping expr)
        {
            expr.Expression = expr.Expression.Accept(this);
            if (expr.Expression is not Literal)
                return expr;
            return expr.Expression;
        }

        public Expr VisitLiteralExpr(Literal expr) => expr;

        public Expr VisitUnaryExpr(Unary expr)
        {
            expr.Right = expr.Right.Accept(this);
            if (expr.Right is not Literal l)
                return expr;
            switch (expr.Op.type)
            {
                case MINUS:
                    return new Literal(-(int)l.Value);
                case BANG:
                    return new Literal(!(bool)l.Value);
                default:
                    errorHandler(lineNo, "Unexpected operator");
                    return expr;
            }
        }

        public Expr VisitVariableExpr(Variable expr) => expr;
    }
}