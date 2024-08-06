namespace NodeScript;

using static TokenType;
using static CompilerUtils;

public class Compiler(Operation[] operations, CompileErrorHandler errorHandler)
{
    private readonly Operation[] operations = operations;
    private CompileErrorHandler errorHandler = errorHandler;
    private int currentLine = 0;
    private byte[][] byteCode = new byte[operations.Length][];

    public (byte[] code, object[] constants, int[] lines) Compile()
    {
        List<object> constants = [];
        while (currentLine < operations.Length)
        {
            byteCode[currentLine] = CompileLine(constants);
        }
        (byte[] flattenedCode, int[] lines) = FlattenCode();
        PatchJumps(flattenedCode, lines);
        return (flattenedCode, constants.ToArray(), lines);
    }

    private byte[] CompileLine(List<object> constants)
    {
        Operation op = operations[currentLine];
        List<byte> bytes = [];
        if (op.expressions.Length != 0)
        {
            CompilerVisitor visitor = new(constants);
            foreach (Expr expr in op.expressions)
                expr.Accept(visitor);
            bytes = visitor.bytes;
        }
        switch (op.operation)
        {
            case SET:
                int idx = bytes.FindLastIndex((b) => b == (byte)OpCode.GET);
                if (idx == -1)
                {
                    errorHandler.Invoke(currentLine, "Not identifier for set command");
                    return [];
                }
                bytes[idx] = (byte)OpCode.SET;
                break;
            case PRINT: bytes.Add((byte)OpCode.PRINT); break;
            case RETURN: bytes.Add((byte)OpCode.RETURN); break;
            case IF: bytes.Add((byte)OpCode.JUMP_IF_FALSE); bytes.Add(0xff); break;
            case ELSE: bytes.Add((byte)OpCode.JUMP); bytes.Add(0xff); break;
            case ENDIF: bytes.Add((byte)OpCode.NOP); break;
        }
        return [.. bytes];
    }

    private (byte[] code, int[] lines) FlattenCode()
    {
        List<byte> code = [];
        List<int> lines = [];

        for (int lineNo = 0; lineNo < byteCode.Length; lineNo++)
        {
            if (byteCode.Length == 0) continue;
            code.AddRange(byteCode[lineNo]);
            code.Add((byte)OpCode.LINE_END);
            for (int i = 0; i < byteCode.Length + 1; i++)
                lines.Add(lineNo);
        }
        return (code.ToArray(), lines.ToArray());
    }

    private void PatchJumps(byte[] code, int[] lines)
    {
        Stack<(int idx, bool hasElse)> ifStmts = [];
        Stack<int> elseStmts = [];
        int ifIdx, elseIdx;
        bool hasElse;

        for (int opNo = 0; opNo < code.Length; opNo++)
        {
            OpCode opCode = (OpCode)code[opNo];
            switch (opCode)
            {
                case OpCode.CONSTANT:
                case OpCode.GET:
                case OpCode.SET: opNo++; break;
                case OpCode.JUMP:
                    elseStmts.Push(opNo + 1);
                    if (ifStmts.Count == 0)
                    {
                        errorHandler(lines[opNo], "ELSE without corresponding IF");
                        return;
                    }
                    (ifIdx, hasElse) = ifStmts.Pop();
                    if (hasElse)
                    {
                        errorHandler(lines[opNo], "Duplicate ELSE");
                        return;
                    }
                    code[ifIdx] = (byte)(opNo + 1 - ifIdx);
                    ifStmts.Push((ifIdx, true));
                    opNo++;
                    break;
                case OpCode.JUMP_IF_FALSE:
                    ifStmts.Push((opNo + 1, false));
                    opNo++;
                    break;
                case OpCode.NOP:
                    (ifIdx, hasElse) = ifStmts.Pop();
                    if (hasElse)
                    {
                        elseIdx = elseStmts.Pop();
                        code[elseIdx] = (byte)(opNo - elseIdx);
                    }
                    else
                    {
                        code[ifIdx] = (byte)(opNo - ifIdx);
                    }
                    break;
                case OpCode.CALL: opNo += 2; break;
            }
        }
    }

    private class CompilerVisitor(List<object> constants) : Expr.IVisitor<bool>
    {
        public List<byte> bytes = [];
        private readonly List<object> constants = constants;

        public bool VisitBinaryExpr(Binary expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            switch (expr.Op.type)
            {
                case LESS: Emit(OpCode.LESS); break;
                case LESS_EQUAL: Emit(OpCode.GREATER); Emit(OpCode.NEGATE); break;
                case MINUS: Emit(OpCode.SUBTRACT); break;
                case OR: Emit(OpCode.OR); break;
                case PLUS: Emit(OpCode.ADD); break;
                case SLASH: Emit(OpCode.DIVIDE); break;
                case STAR: Emit(OpCode.MULTIPLY); break;
                case AND: Emit(OpCode.AND); break;
                case BANG_EQUAL: Emit(OpCode.EQUAL); Emit(OpCode.NEGATE); break;
                case EQUAL_EQUAL: Emit(OpCode.EQUAL); break;
                case GREATER: Emit(OpCode.GREATER); break;
                case GREATER_EQUAL: Emit(OpCode.LESS); Emit(OpCode.NEGATE); break;
                default: return false;
            }
            return true;
        }

        public bool VisitCallExpr(Call expr)
        {
            Emit(OpCode.CALL, MakeConst(expr.Callee.Name.Lexeme.ToString()), (byte)expr.Arguments.Count);
            return true;
        }

        public bool VisitGroupingExpr(Grouping expr)
        {
            return expr.Accept(this);
        }

        public bool VisitLiteralExpr(Literal expr)
        {
            object val = expr.Value;
            if (val is bool b)
                Emit(b ? OpCode.TRUE : OpCode.FALSE);
            else
                Emit(OpCode.CONSTANT, MakeConst(expr.Value));
            return true;
        }

        public bool VisitUnaryExpr(Unary expr)
        {
            expr.Right.Accept(this);
            switch (expr.Op.type)
            {
                case MINUS: Emit(OpCode.NEGATE); break;
                case BANG: Emit(OpCode.NOT); break;
                default: return false;
            }
            return true;
        }

        public bool VisitVariableExpr(Variable expr)
        {
            string name = expr.Name.Lexeme.ToString();
            Emit(OpCode.GET, MakeConst(name));
            return true;
        }

        private byte MakeConst(object val)
        {
            int idx = constants.IndexOf(val);
            if (idx == -1)
            {
                idx = constants.Count;
                constants.Add(val);
            }
            return (byte)idx;
        }

        private void Emit(OpCode opCode) => bytes.Add((byte)opCode);

        private void Emit(OpCode opCode, params byte[] data)
        {
            Emit(opCode);
            foreach (byte b in data)
                bytes.Add(b);
        }
    }
}