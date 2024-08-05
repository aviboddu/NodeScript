namespace NodeScript;

using static TokenType;
using static CompilerUtils;

public class Parser(Token[][] tokens, CompileErrorHandler errorHandler)
{
    private readonly CompileErrorHandler errorHandler = errorHandler;
    private readonly Token[][] tokens = tokens;
    private int currentLine = 0;
    private int currentToken = 0;

    public Operation[] Parse()
    {
        Operation[] operations = new Operation[tokens.Length];
        while (currentLine < operations.Length)
        {
            Operation? op = ParseLine();
            if (op is null) return operations;
            operations[currentLine] = op;
            currentLine++;
        }
        return operations;
    }

    private Operation? ParseLine()
    {
        Operation? op = null;
        switch (tokens[currentLine][currentToken].type)
        {
            case SET: op = new(SET, 2); break;
            case PRINT: op = new(PRINT, 2); break;
            case IF: op = new(IF, 1); break;
            case ELSE: op = new(ELSE, 0); break;
            case ENDIF: op = new(ENDIF, 0); break;
            case RETURN: op = new(RETURN, 0); break;
            case EOF: return null;
            default:
                errorHandler.Invoke(currentLine, $"Unexpected token {tokens[currentLine][currentToken].type}");
                return null;
        }
        currentToken++;
        for (int i = 0; i < op.expressions.Length; i++)
        {
            Expr? expr = Expression();
            if (expr is null) return null;

            op.expressions[i] = expr;
        }

        if (Consume(SEMICOLON, "Expected semicolon") is null)
        {
            errorHandler.Invoke(currentLine, $"Unexpected token {tokens[currentLine][currentToken].type},"
                    + $"{op.operation} only requires {op.expressions.Length} expressions");
            return null;
        }

        return op;
    }

    private Expr? Expression()
    {
        return Or();
    }

    private Expr? Or()
    {
        Expr? expr = And();
        if (expr is null) return null;

        while (Match(OR))
        {
            Token op = Previous();
            Expr? right = And();
            if (right is null) return null;
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private Expr? And()
    {
        Expr? expr = Equality();
        if (expr is null) return null;

        while (Match(AND))
        {
            Token op = Previous();
            Expr? right = Equality();
            if (right is null) return null;

            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private Expr? Equality()
    {
        Expr? expr = Comparison();
        if (expr is null) return null;

        while (Match(BANG_EQUAL, EQUAL_EQUAL))
        {
            Token op = Previous();
            Expr? right = Comparison();
            if (right is null) return null;
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private Expr? Comparison()
    {
        Expr? expr = Term();
        if (expr is null) return null;

        while (Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL))
        {
            Token op = Previous();
            Expr? right = Term();
            if (right is null) return null;
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private Expr? Term()
    {
        Expr? expr = Factor();
        if (expr is null) return null;

        while (Match(MINUS, PLUS))
        {
            Token op = Previous();
            Expr? right = Factor();
            if (right is null) return null;
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private Expr? Factor()
    {
        Expr? expr = Unary();
        if (expr is null) return null;

        while (Match(SLASH, STAR))
        {
            Token op = Previous();
            Expr? right = Unary();
            if (right is null) return null;
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private Expr? Unary()
    {
        if (Match(BANG, MINUS))
        {
            Token op = Previous();
            Expr? right = Unary();
            if (right is null) return null;
            return new Unary(op, right);
        }

        return Call();
    }

    private Expr? Call()
    {
        Expr? expr = Primary();
        if (expr is null) return null;

        while (true)
        {
            if (Match(LEFT_PAREN))
            {
                if (expr is not Variable v)
                {
                    errorHandler.Invoke(currentLine, "Cannot call something that is not an identifier");
                    return null;
                }
                expr = FinishCall(v);
                if (expr is null) return null;
            }
            else
                break;
        }

        return expr;
    }

    private Call? FinishCall(Variable callee)
    {
        List<Expr> arguments = [];
        if (!Check(RIGHT_PAREN))
        {
            do
            {
                if (arguments.Count >= 255)
                    errorHandler.Invoke(currentLine, "Can't have more than 255 arguments.");
                Expr? expr = Expression();
                if (expr is null) return null;
                arguments.Add(expr);
            } while (Match(COMMA));
        }

        Token? paren = Consume(RIGHT_PAREN, "Expect ')' after arguments.");
        if (paren is null)
            return null;

        return new Call(callee, paren, arguments);
    }

    private Expr? Primary()
    {
        if (Match(FALSE)) return new Literal(false);
        if (Match(TRUE)) return new Literal(true);

        if (Match(NUMBER, STRING))
        {
            return new Literal(Previous().literal!);
        }

        if (Match(LEFT_PAREN))
        {
            Expr? expr = Expression();
            if (expr is null) return null;
            Consume(RIGHT_PAREN, "Expect ')' after expression.");
            return new Grouping(expr);
        }

        if (Match(IDENTIFIER))
        {
            return new Variable(Previous());
        }

        errorHandler.Invoke(currentLine, "Expect expression.");
        return null;
    }

    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().type == type;
    private Token? Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();

        errorHandler.Invoke(currentLine, message);
        return null;
    }

    private bool IsAtEnd() => currentToken >= tokens[currentLine].Length;
    private Token Advance() => tokens[currentLine][currentToken++];
    private Token Peek() => tokens[currentLine][currentToken];
    private Token Previous() => tokens[currentLine][currentToken - 1];

    public void Validate(Operation[] operations)
    {
        int ifEndif = 0;
        for (int i = 0; i < operations.Length; i++)
        {
            Operation op = operations[i];
            Validator v = new(errorHandler, i);
            switch (op.operation)
            {
                case IF: ifEndif++; break;
                case ENDIF:
                    ifEndif--;
                    if (ifEndif < 0) errorHandler.Invoke(i, "IF statements do not match ENDIF statements");
                    break;
            }

            foreach (Expr ex in op.expressions)
                ex.Accept(v);
        }
    }

    private class Validator(CompileErrorHandler errorHandler, int lineNo) : Expr.IVisitor<bool>
    {
        private readonly CompileErrorHandler errorHandler = errorHandler;

        public bool VisitBinaryExpr(Binary expr) => expr.Left.Accept(this) & expr.Right.Accept(this);

        public bool VisitCallExpr(Call expr)
        {
            string name = expr.Callee.Name.Lexeme.ToString();
            bool funcExists = NativeFuncs.NativeFunctions.ContainsKey(name);
            if (!funcExists)
                errorHandler.Invoke(lineNo, $"Function {name} does not exist");
            return funcExists;
        }

        public bool VisitGroupingExpr(Grouping expr) => expr.Expression.Accept(this);

        public bool VisitLiteralExpr(Literal expr) => true;

        public bool VisitUnaryExpr(Unary expr) => expr.Right.Accept(this);

        public bool VisitVariableExpr(Variable expr) => true;
    }
}