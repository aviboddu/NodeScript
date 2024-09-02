namespace NodeScript;

internal class Token(TokenType type, int start, int end, object? literal, string code)
{
    public readonly TokenType type = type;
    private readonly int start = start;
    private readonly int end = end;
    public readonly object? literal = literal;
    public ReadOnlySpan<char> Lexeme => code.AsSpan(start, end - start);
}

internal enum TokenType : byte
{
    LEFT_PAREN, RIGHT_PAREN, LEFT_SQUARE, RIGHT_SQUARE,
    COMMA, DOT, MINUS, PLUS, SLASH, STAR, COLON,

    // One or two character tokens.
    BANG, BANG_EQUAL,
    /*EQUAL,*/
    EQUAL_EQUAL,
    GREATER, GREATER_EQUAL,
    LESS, LESS_EQUAL,

    // Literals.
    IDENTIFIER, STRING, NUMBER,

    // Keywords.
    AND, ELSE, FALSE, IF, OR, ENDIF, SET, NOP,
    PRINT, RETURN, TRUE,

    EOF
}