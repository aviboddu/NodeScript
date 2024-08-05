namespace NodeScript;

public abstract record Expr
{
    public abstract R Accept<R>(IVisitor<R> visitor);
    public interface IVisitor<R>
    {
        public R VisitBinaryExpr(Binary expr);
        public R VisitCallExpr(Call expr);
        public R VisitGroupingExpr(Grouping expr);
        public R VisitLiteralExpr(Literal expr);
        public R VisitUnaryExpr(Unary expr);
        public R VisitVariableExpr(Variable expr);
    }

}

public record Binary(Expr Left, Token Op, Expr Right) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitBinaryExpr(this);
}

public record Call(Variable Callee, Token Paren, List<Expr> Arguments) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitCallExpr(this);
}

public record Grouping(Expr Expression) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitGroupingExpr(this);
}

public record Literal(object Value) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitLiteralExpr(this);
}

public record Unary(Token Op, Expr Right) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitUnaryExpr(this);
}

public record Variable(Token Name) : Expr
{
    public override R Accept<R>(IVisitor<R> visitor) => visitor.VisitVariableExpr(this);
}