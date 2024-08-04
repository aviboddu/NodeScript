namespace NodeScript;

using static TokenType;

public class Tokenizer(string source, Node callingNode)
{
    private readonly Node callingNode = callingNode;
    private readonly string source = source;
    private readonly List<Token> tokens = [];

    private int start = 0;
    private int current = 0;
    private int line = 1;

    public Token[] ScanTokens()
    {
        while (!IsAtEnd())
        {
            // We are at the beginning of the next lexeme.
            start = current;
            ScanToken();
        }

        tokens.Add(new Token(EOF, "", null, line));
        return [.. tokens];
    }

    private void ScanToken()
    {
        char c = Advance();
        if (char.IsDigit(c))
        {
            Number();
            return;
        }

        if (char.IsLetter(c))
        {
            Identifier();
            return;
        }
        switch (c)
        {
            case '(': AddToken(LEFT_PAREN); break;
            case ')': AddToken(RIGHT_PAREN); break;
            case '{': AddToken(LEFT_BRACE); break;
            case '}': AddToken(RIGHT_BRACE); break;
            case ',': AddToken(COMMA); break;
            case '.': AddToken(DOT); break;
            case '-': AddToken(MINUS); break;
            case '+': AddToken(PLUS); break;
            case ';': AddToken(SEMICOLON); break;
            case '*': AddToken(STAR); break;
            case '!':
                AddToken(Match('=') ? BANG_EQUAL : BANG);
                break;
            case '=':
                AddToken(Match('=') ? EQUAL_EQUAL : EQUAL);
                break;
            case '<':
                AddToken(Match('=') ? LESS_EQUAL : LESS);
                break;
            case '>':
                AddToken(Match('=') ? GREATER_EQUAL : GREATER);
                break;
            case '"': String(); break;
            default:
                Err(line, "Unexpected character.");
                break;
        }
    }

    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                line++;
            }
            Advance();
        }

        if (IsAtEnd())
        {
            Err(line, "Unterminated string");
            return;
        }

        // The closing quote.
        Advance();
        AddToken(STRING, int.Parse(source[start..current]));
    }

    private void Number()
    {
        while (char.IsDigit(Peek())) Advance();
        AddToken(NUMBER);
    }

    private void Identifier()
    {
        while (char.IsLetterOrDigit(Peek()) || Peek() == '_') Advance();
        AddToken(IdentifierType());
    }

    private TokenType IdentifierType()
    {
        return source[start] switch
        {
            'a' => CheckKeyword("and", AND),
            'e' => CheckKeyword("else", ELSE),
            'f' => CheckKeyword("false", FALSE),
            'i' => CheckKeyword("if", IF),
            'o' => CheckKeyword("or", OR),
            'p' => CheckKeyword("print", PRINT),
            'r' => CheckKeyword("return", RETURN),
            't' => CheckKeyword("true", TRUE),
            'v' => CheckKeyword("var", VAR),
            _ => IDENTIFIER,
        };
    }

    private TokenType CheckKeyword(string keyword, TokenType type)
    {
        if (current - start == keyword.Length && source[start..current] == keyword)
            return type;

        return IDENTIFIER;
    }

    private void Err(int line, string message)
    {
        Script.Error(callingNode, line, message);
    }

    private bool IsAtEnd() => current >= source.Length;

    private char Advance()
    {
        return source[current++];
    }

    private char Peek() => source[current];
    private char PeekNext()
    {
        if (current + 1 >= source.Length) return (char)0;
        return source[current + 1];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (source[current] != expected) return false;
        current++;
        return true;
    }

    private void SkipWhitespace()
    {
        for (; ; )
        {
            char c = Peek();
            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                    Advance();
                    break;
                case '\n':
                    line++;
                    Advance();
                    break;
                case '/':
                    if (PeekNext() == '/')
                        // A comment goes until the end of the line.
                        while (!IsAtEnd() && Peek() != '\n') Advance();
                    else
                        return;
                    break;
                default:
                    return;
            }
        }
    }

    private void AddToken(TokenType type, object? obj = null)
    {
        string text = source[start..current];
        tokens.Add(new(type, text, obj, line));
    }
}