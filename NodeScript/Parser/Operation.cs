namespace NodeScript;

public class Operation(TokenType operation, int expressions)
{
    public readonly TokenType operation = operation;
    public Expr[] expressions = new Expr[expressions];
}