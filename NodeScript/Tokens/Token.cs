namespace NodeScript;

public class Token(TokenType type, string lexeme, object? literal, int line)
{
    public readonly TokenType type = type;
    public readonly string lexeme = lexeme;
    public readonly object? literal = literal;
    public int line = line;
}

public enum TokenType
{
    LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE, LEFT_SQUARE, RIGHT_SQUARE,
    COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR,

    // One or two character tokens.
    BANG, BANG_EQUAL,
    EQUAL, EQUAL_EQUAL,
    GREATER, GREATER_EQUAL,
    LESS, LESS_EQUAL,

    // Literals.
    IDENTIFIER, STRING, NUMBER,

    // Keywords.
    AND, ELSE, FALSE, IF, OR, ENDIF, SET,
    PRINT, RETURN, TRUE, VAR,

    EOF
}