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
            Expr left = expr.Left.Accept(this);
            Expr right = expr.Right.Accept(this);
            if (left is not Literal l || right is not Literal r)
                return expr;
            switch (expr.Op.type)
            {
                case GREATER: return new Literal((int)l.Value > (int)r.Value);
                case LESS: return new Literal((int)l.Value < (int)r.Value);
                case MINUS: return new Literal((int)l.Value - (int)r.Value);
                case STAR: return new Literal((int)l.Value * (int)r.Value);
                case SLASH: return new Literal((int)l.Value / (int)r.Value);
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

        public Expr VisitIndexExpr(Index expr) => expr;

        public Expr VisitCallExpr(Call expr)
        {
            for (int i = 0; i < expr.Arguments.Count; i++)
                expr.Arguments[i] = expr.Arguments[i].Accept(this);
            if (!expr.Arguments.All((a) => a is Literal))
                return expr;

            string name = expr.Callee.Name.Lexeme.ToString();
            NativeDelegate func = NativeFuncs.NativeFunctions[name];
            Result val = func.Invoke(expr.Arguments.Select((expr) => ((Literal)expr).Value).ToArray().AsSpan());
            if (!val.Success())
            {
                errorHandler.Invoke(lineNo, val.message!);
                return expr;
            }
            return new Literal(val.GetValue()!);
        }

        public Expr VisitGroupingExpr(Grouping expr)
        {
            Expr ex = expr.Expression.Accept(this);
            if (ex is not Literal)
                return expr;
            return ex;
        }

        public Expr VisitLiteralExpr(Literal expr) => expr;

        public Expr VisitUnaryExpr(Unary expr)
        {
            Expr ex = expr.Right.Accept(this);
            if (ex is not Literal l)
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