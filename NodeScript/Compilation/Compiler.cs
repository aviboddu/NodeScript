namespace NodeScript;

public class Compiler(Token[] tokens, Node callingNode)
{
    private static Dictionary<TokenType, ParseRule> rules = [];
    private bool hadError = false;
    private bool panicMode = false;
    private readonly Node callingNode = callingNode;
    private int current;
    private Chunk chunk = new();
    public Chunk? Compile()
    {
        Advance();
        while (!Match(TokenType.EOF))
            Declaration();

        if (hadError) return null;
        EndCompilation();
        return chunk;
    }

    public void EndCompilation()
    {
        EmitOperation(new(OpCode.RETURN, []));
    }

    private void ParsePrecedence(Precedence precedence)
    {
        Advance();
        Action<bool>? prefixRule = GetRule(tokens[current - 1].type).Prefix;
        if (prefixRule is null)
        {
            Error("Expect expression.");
            return;
        }

        bool canAssign = precedence <= Precedence.ASSIGNMENT;
        prefixRule(canAssign);

        while (precedence <= GetRule(tokens[current].type).Precedence)
        {
            Advance();
            Action<bool>? infixRule = GetRule(tokens[current - 1].type).Infix;
            if (infixRule is not null) infixRule(canAssign);
        }
        if (canAssign && Match(TokenType.EQUAL))
        {
            Error("Invalid assignment target.");
        }
    }

    private void Declaration()
    {
        if (Match(TokenType.VAR))
            VarDeclaration();
        else
            Statement();

        if (panicMode) Synchronize();
    }

    private void VarDeclaration()
    {
        byte global = ParseVariable("Expect variable name.");
        if (Match(TokenType.EQUAL))
        {
            Expression();
        }
        else
        {
            EmitByte(OpCode.FALSE);
        }
        Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
        DefineVariable(global);
    }

    void DefineVariable(byte global)
    {
        EmitOperation(new(OpCode.DEFINE_VARIABLE, [global]));
    }

    private byte ParseVariable(string errorMessage)
    {
        Consume(TokenType.IDENTIFIER, errorMessage);
        return IdentifierConstant(tokens[current - 1]);
    }

    private byte IdentifierConstant(Token name)
    {
        return MakeConstant(name.lexeme);
    }

    private void Statement()
    {
        if (Match(TokenType.PRINT))
            PrintStatement();
        else if (Match(TokenType.IF))
            IfStatement();
        else if (Match(TokenType.LEFT_BRACE))
        {
            EmitByte(OpCode.BEGIN_SCOPE);
            Block();
            EmitByte(OpCode.END_SCOPE);
        }
        else
            ExpressionStatement();
    }

    private void Block()
    {
        while (!Check(TokenType.RIGHT_BRACE) && !Check(TokenType.EOF))
        {
            Declaration();
        }

        Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
    }

    private void PrintStatement()
    {
        Expression();
        Consume(TokenType.COMMA, "Expect two arguments for print()");
        Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after print");
    }

    private void IfStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
        Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");

        int thenJump = EmitJump(OpCode.JUMP_IF_FALSE);
        EmitByte(OpCode.POP);
        Statement();

        int elseJump = EmitJump(OpCode.JUMP);

        PatchJump(thenJump);
        EmitByte(OpCode.POP);

        if (Match(TokenType.ELSE)) Statement();
        PatchJump(elseJump);
    }

    private void ExpressionStatement()
    {
        Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
        EmitByte(OpCode.POP);
    }

    private void Expression()
    {
        ParsePrecedence(Precedence.ASSIGNMENT);
    }

    private void String(bool _)
    {
        EmitConstant(tokens[current - 1].lexeme[1..(tokens[current - 1].lexeme.Length - 1)]);
    }

    private void And_(bool canAssign)
    {
        int endJump = EmitJump(OpCode.JUMP_IF_FALSE);

        EmitByte(OpCode.POP);
        ParsePrecedence(Precedence.AND);

        PatchJump(endJump);
    }

    private void Or_(bool canAssign)
    {
        int elseJump = EmitJump(OpCode.JUMP_IF_FALSE);
        int endJump = EmitJump(OpCode.JUMP);

        PatchJump(elseJump);
        EmitByte(OpCode.POP);

        ParsePrecedence(Precedence.OR);
        PatchJump(endJump);
    }

    private void Variable(bool canAssign)
    {
        NamedVariable(tokens[current - 1], canAssign);
    }

    private void NamedVariable(Token name, bool canAssign)
    {
        byte arg = IdentifierConstant(name);
        if (canAssign && Match(TokenType.EQUAL))
        {
            Expression();
            EmitOperation(new(OpCode.SET_VARIABLE, [arg]));
        }
        else
        {
            EmitOperation(new(OpCode.GET_VARIABLE, [arg]));
        }
    }

    private void Number(bool _)
    {
        EmitConstant(tokens[current].literal!);
    }

    private void Literal(bool _)
    {
        switch (tokens[current - 1].type)
        {
            case TokenType.FALSE: EmitByte(OpCode.FALSE); break;
            case TokenType.TRUE: EmitByte(OpCode.TRUE); break;
            default: return; // Unreachable.
        }
    }

    private void Grouping(bool _)
    {
        Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
    }

    private void Unary(bool _)
    {
        TokenType operatorType = tokens[current - 1].type;

        // Compile the operand.
        Expression();

        // Emit the operator instruction.
        switch (operatorType)
        {
            case TokenType.BANG: EmitByte(OpCode.NOT); break;
            case TokenType.MINUS: EmitByte(OpCode.NEGATE); break;
            default: return; // Unreachable.
        }
    }

    private void Binary(bool _)
    {
        TokenType operatorType = tokens[current - 1].type;
        ParseRule rule = GetRule(operatorType);
        ParsePrecedence(rule.Precedence + 1);

        switch (operatorType)
        {
            case TokenType.BANG_EQUAL: EmitByte(OpCode.EQUAL); EmitByte(OpCode.NOT); break;
            case TokenType.EQUAL_EQUAL: EmitByte(OpCode.EQUAL); break;
            case TokenType.GREATER: EmitByte(OpCode.GREATER); break;
            case TokenType.GREATER_EQUAL: EmitByte(OpCode.LESS); EmitByte(OpCode.NOT); break;
            case TokenType.LESS: EmitByte(OpCode.LESS); break;
            case TokenType.LESS_EQUAL: EmitByte(OpCode.GREATER); EmitByte(OpCode.NOT); break;
            case TokenType.PLUS: EmitByte(OpCode.ADD); break;
            case TokenType.MINUS: EmitByte(OpCode.SUBTRACT); break;
            case TokenType.STAR: EmitByte(OpCode.MULTIPLY); break;
            case TokenType.SLASH: EmitByte(OpCode.DIVIDE); break;
            default: return; // Unreachable.
        }
    }

    private void Call(bool canAssign)
    {
        byte argCount = ArgumentList();
        EmitOperation(new(OpCode.CALL, [argCount]));
    }

    private byte ArgumentList()
    {
        byte argCount = 0;
        if (!Check(TokenType.RIGHT_PAREN))
        {
            do
            {
                Expression();
                if (argCount == 255)
                {
                    Error("Can't have more than 255 arguments.");
                }
                argCount++;
            } while (Match(TokenType.COMMA));
        }
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
        return argCount;
    }

    private void Advance()
    {
        current++;
    }

    private void Consume(TokenType type, string message)
    {
        if (tokens[current].type == type)
        {
            Advance();
            return;
        }

        Error(message);
    }

    public bool Match(TokenType type)
    {
        if (!Check(type)) return false;
        Advance();
        return true;
    }

    private bool Check(TokenType type) => tokens[current].type == type;

    private void EmitByte(OpCode code) => EmitOperation(new(code, []));

    private void EmitOperation(Operation operation)
    {
        chunk.operations.Add(operation);
    }

    private void EmitConstant(object value)
    {
        EmitOperation(new(OpCode.CONSTANT, [MakeConstant(value)]));
    }

    private int EmitJump(OpCode instruction)
    {
        EmitOperation(new(instruction, [0xff, 0xff]));
        return chunk.operations.Count - 1;
    }

    private void PatchJump(int offset)
    {
        // -2 to adjust for the bytecode for the jump offset itself.
        int jump = chunk.operations.Count - 1 - offset;

        if (jump > ushort.MaxValue)
        {
            Error("Too much code to jump over.");
        }

        chunk.operations[offset].data[0] = (byte)((jump >> 8) & 0xff);
        chunk.operations[offset].data[1] = (byte)(jump & 0xff);
    }

    private byte MakeConstant(object value)
    {
        int idx = chunk.constants.Count;
        if (idx >= byte.MaxValue)
        {
            Error("Too many constants");
            return 0;
        }
        chunk.constants.Add(value);
        return (byte)idx;
    }

    private void Error(string message)
    {
        if (panicMode) return;
        Script.script!.Error(callingNode, tokens[current].line, tokens[current].column, message);
        hadError = true;
        panicMode = true;
    }

    private void Synchronize()
    {
        panicMode = false;

        while (tokens[current].type != TokenType.EOF)
        {
            if (tokens[current - 1].type == TokenType.SEMICOLON) return;
            switch (tokens[current].type)
            {
                case TokenType.VAR:
                case TokenType.IF:
                case TokenType.PRINT:
                case TokenType.RETURN:
                    return;
                default: break;
            }

            Advance();
        }
    }

    private ParseRule GetRule(TokenType type)
    {
        if (rules.Count == 0)
        {
            rules = new()
            {
                { TokenType.LEFT_PAREN,  new ParseRule(Grouping, Call, Precedence.CALL)},
                { TokenType.RIGHT_PAREN,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.LEFT_BRACE,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.RIGHT_BRACE,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.COMMA,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.DOT,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.MINUS,  new ParseRule(Unary, Binary, Precedence.TERM)},
                { TokenType.PLUS,  new ParseRule(null, Binary, Precedence.TERM)},
                { TokenType.SEMICOLON,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.SLASH,  new ParseRule(null, Binary, Precedence.FACTOR)},
                { TokenType.STAR,  new ParseRule(null, Binary, Precedence.FACTOR)},
                { TokenType.BANG,  new ParseRule(Unary, null, Precedence.NONE)},
                { TokenType.BANG_EQUAL,  new ParseRule(null, Binary, Precedence.NONE)},
                { TokenType.EQUAL,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.EQUAL_EQUAL,  new ParseRule(null, Binary, Precedence.EQUALITY)},
                { TokenType.GREATER,  new ParseRule(null, Binary, Precedence.COMPARISON)},
                { TokenType.GREATER_EQUAL,  new ParseRule(null, Binary, Precedence.COMPARISON)},
                { TokenType.LESS,  new ParseRule(null, Binary, Precedence.COMPARISON)},
                { TokenType.LESS_EQUAL,  new ParseRule(null, Binary, Precedence.COMPARISON)},
                { TokenType.IDENTIFIER,  new ParseRule(Variable, null, Precedence.NONE)},
                { TokenType.STRING,  new ParseRule(String, null, Precedence.NONE)},
                { TokenType.NUMBER,  new ParseRule(Number, null, Precedence.NONE)},
                { TokenType.AND,  new ParseRule(null, And_, Precedence.AND)},
                { TokenType.ELSE,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.FALSE,  new ParseRule(Literal, null, Precedence.NONE)},
                { TokenType.IF,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.OR,  new ParseRule(null, Or_, Precedence.OR)},
                { TokenType.PRINT,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.RETURN,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.TRUE,  new ParseRule(Literal, null, Precedence.NONE)},
                { TokenType.VAR,  new ParseRule(null, null, Precedence.NONE)},
                { TokenType.EOF,  new ParseRule(null, null, Precedence.NONE)}
                };
        }
        return rules[type];
    }

    private readonly struct ParseRule(Action<bool>? Prefix, Action<bool>? Infix, Precedence Precedence)
    {
        public readonly Action<bool>? Prefix = Prefix;
        public readonly Action<bool>? Infix = Infix;
        public readonly Precedence Precedence = Precedence;
    }

    private enum Precedence
    {
        NONE,
        ASSIGNMENT,  // =
        OR,          // or
        AND,         // and
        EQUALITY,    // == !=
        COMPARISON,  // < > <= >=
        TERM,        // + -
        FACTOR,      // * /
        UNARY,       // ! -
        CALL,        // . ()
        PRIMARY
    }
}