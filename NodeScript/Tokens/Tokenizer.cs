namespace NodeScript;

using static TokenType;
using static CompilerUtils;
using System.Diagnostics;


[DebuggerDisplay("start = {start, nq}, current = {current, nq}, line = {line, nq}")]
internal class Tokenizer(string source, InternalErrorHandler compileError)
{
    private readonly InternalErrorHandler compileError = compileError;
    private readonly string source = source;
    private readonly List<List<Token>> tokens = [];

    private int start, current, line = 0;

    public Token[][] ScanTokens()
    {
        tokens.Add([]);
        while (!IsAtEnd())
        {
            // We are at the beginning of the next lexeme.
            ScanToken();
        }
        return tokens.Select(Enumerable.ToArray).ToArray();
    }

    private void ScanToken()
    {
        SkipWhitespace();
        start = current;
        if (IsAtEnd()) return;
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
            case '[': AddToken(LEFT_SQUARE); break;
            case ']': AddToken(RIGHT_SQUARE); break;
            case ',': AddToken(COMMA); break;
            case '.': AddToken(DOT); break;
            case '-': AddToken(MINUS); break;
            case '+': AddToken(PLUS); break;
            case '*': AddToken(STAR); break;
            case '/': AddToken(SLASH); break;
            case ':': AddToken(COLON); break;
            case '!':
                AddToken(Match('=') ? BANG_EQUAL : BANG);
                break;
            case '=':
                if (Match('=')) AddToken(EQUAL_EQUAL);
                else goto default;
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
        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\n')
            {
                Err(line, "Unterminated string");
                return;
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
        AddToken(STRING);
    }

    private void Number()
    {
        while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        AddToken(NUMBER);
    }

    private void Identifier()
    {
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_')) Advance();
        AddToken(IdentifierType());
    }

    private TokenType IdentifierType()
    {
        return source[start] switch
        {
            'a' => CheckKeyword("and", AND),
            'E' => start + 1 < source.Length && source[start + 1] == 'L' ? CheckKeyword("ELSE", ELSE) : CheckKeyword("ENDIF", ENDIF),
            'f' => CheckKeyword("false", FALSE),
            'I' => CheckKeyword("IF", IF),
            'N' => CheckKeyword("NOP", NOP),
            'o' => CheckKeyword("or", OR),
            'P' => CheckKeyword("PRINT", PRINT),
            'R' => CheckKeyword("RETURN", RETURN),
            'S' => CheckKeyword("SET", SET),
            't' => CheckKeyword("true", TRUE),
            _ => IDENTIFIER,
        };
    }

    private TokenType CheckKeyword(string keyword, TokenType type)
    {
        if (current - start == keyword.Length && source[start..current] == keyword)
            return type;

        return IDENTIFIER;
    }

    private void Err(int line, string message) => compileError.Invoke(line, message);

    private bool IsAtEnd() => current >= source.Length;

    private char Advance() => source[current++];

    private char Peek() => source[current];
    private char PeekNext()
    {
        if (current + 1 >= source.Length) return '\0';
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
        while (!IsAtEnd())
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
                    tokens.Add([]);
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

    private void AddToken(TokenType type)
    {
        tokens[line].Add(new(type, start, current, source));
    }
}