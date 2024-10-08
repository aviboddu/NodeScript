namespace NodeScript;

internal abstract class Expr
{
    public Type Type = typeof(object);
    public abstract R Accept<R>(IVisitor<R> visitor);
    public interface IVisitor<R>
    {
        public R VisitBinaryExpr(Binary expr);
        public R VisitCallExpr(Call expr);
        public R VisitIndexExpr(Index expr);
        public R VisitGroupingExpr(Grouping expr);
        public R VisitLiteralExpr(Literal expr);
        public R VisitUnaryExpr(Unary expr);
        public R VisitVariableExpr(Variable expr);
    }

}

internal class Binary(Expr Left, Token Op, Expr Right) : Expr
{
    public Expr Left = Left;
    public Token Op = Op;
    public Expr Right = Right;

    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitBinaryExpr(this);
}

internal class Index(Expr Variable, Expr[] Arguments) : Expr
{
    public Expr Variable = Variable;
    public Expr[] Arguments = Arguments;
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitIndexExpr(this);
}

internal class Call(Variable Callee, List<Expr> Arguments) : Expr
{
    public Variable Callee = Callee;
    public List<Expr> Arguments = Arguments;
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitCallExpr(this);
}

internal class Grouping(Expr Expression) : Expr
{
    public Expr Expression = Expression;
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitGroupingExpr(this);
}

internal class Literal : Expr
{
    public object Value;

    public Literal(object Value)
    {
        this.Value = Value;
        Type = Value.GetType();
    }
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitLiteralExpr(this);
}

internal class Unary(Token Op, Expr Right) : Expr
{
    public Token Op = Op;
    public Expr Right = Right;
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitUnaryExpr(this);
}

internal class Variable(Token Name) : Expr
{
    public Token Name = Name;
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitVariableExpr(this);
}