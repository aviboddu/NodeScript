namespace NodeScript;

using static TokenType;
using static CompilerUtils;

public class Compiler(Operation[] operations, CompileErrorHandler errorHandler)
{
    private readonly Operation[] operations = operations;
    private CompileErrorHandler errorHandler = errorHandler;
    private int currentLine = 0;

    public (byte[] code, object[] constants, int[] lines) Compile()
    {
        List<object> constants = [];
        List<byte> bytes = [];
        List<int> lines = [];
        while (currentLine < operations.Length)
        {
            if (!CompileLine(bytes, lines, constants))
                return ([], [], []);
            currentLine++;
        }
        if (!PatchJumps(bytes, lines))
            return ([], [], []);
        bytes.Insert(bytes.Count - 1, (byte)OpCode.RETURN);
        lines.Add(lines[^1]);
        return (bytes.ToArray(), constants.ToArray(), lines.ToArray());
    }

    private bool CompileLine(List<byte> bytes, List<int> lines, List<object> constants)
    {
        Operation? op = operations[currentLine];
        if (op is null)
        {
            currentLine++;
            return true;
        }
        if (op.expressions.Length != 0)
        {
            CompilerVisitor visitor = new(bytes, lines, constants, currentLine);
            foreach (Expr expr in op.expressions)
            {
                if (!expr.Accept(visitor))
                    return false;
            }
        }
        switch (op.operation)
        {
            case SET:
                int idx = bytes.FindLastIndex((b) => b == (byte)OpCode.GET);
                if (idx == -1)
                {
                    errorHandler.Invoke(currentLine, "Not identifier for set command");
                    return false;
                }
                bytes[idx] = (byte)OpCode.SET;
                break;
            case PRINT: bytes.Add((byte)OpCode.PRINT); lines.Add(currentLine); break;
            case RETURN: bytes.Add((byte)OpCode.RETURN); lines.Add(currentLine); break;
            case IF:
                bytes.Add((byte)OpCode.JUMP_IF_FALSE); bytes.Add(0xff); bytes.Add(0xff);
                lines.Add(currentLine); lines.Add(currentLine); lines.Add(currentLine);
                break;
            case ELSE:
                bytes.Add((byte)OpCode.JUMP); bytes.Add(0xff); bytes.Add(0xff);
                lines.Add(currentLine); lines.Add(currentLine); lines.Add(currentLine);
                break;
            case ENDIF: bytes.Add((byte)OpCode.NOP); lines.Add(currentLine); break;
        }
        bytes.Add((byte)OpCode.LINE_END);
        lines.Add(currentLine);
        return true;
    }

    private bool PatchJumps(List<byte> code, List<int> lines)
    {
        Stack<(int idx, bool hasElse)> ifStmts = [];
        Stack<int> elseStmts = [];
        int ifIdx, elseIdx;
        ushort diff;
        bool hasElse;

        for (int opNo = 0; opNo < code.Count; opNo++)
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
                        return false;
                    }
                    (ifIdx, hasElse) = ifStmts.Pop();
                    if (hasElse)
                    {
                        errorHandler(lines[opNo], "Duplicate ELSE");
                        return false;
                    }
                    diff = (ushort)(opNo + 1 - ifIdx);
                    code[ifIdx] = (byte)(diff >> 8);
                    code[ifIdx + 1] = (byte)(diff & 0xFF);
                    ifStmts.Push((ifIdx, true));
                    opNo += 2;
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
                        diff = (ushort)(opNo - elseIdx);
                        code[elseIdx] = (byte)(diff >> 8);
                        code[elseIdx + 1] = (byte)(diff & 0xFF);
                    }
                    else
                    {
                        diff = (ushort)(opNo - ifIdx);
                        code[ifIdx] = (byte)(diff >> 8);
                        code[ifIdx + 1] = (byte)(diff & 0xFF);
                    }
                    break;
                case OpCode.CALL: opNo += 2; break;
            }
        }
        return true;
    }

    private class CompilerVisitor(List<byte> bytes, List<int> lines, List<object> constants, int currentLine) : Expr.IVisitor<bool>
    {
        private List<byte> bytes = bytes;
        private List<int> lines = lines;
        private readonly List<object> constants = constants;
        private readonly int currentLine = currentLine;

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

        private void Emit(OpCode opCode)
        {
            bytes.Add((byte)opCode);
            lines.Add(currentLine);
        }

        private void Emit(OpCode opCode, params byte[] data)
        {
            Emit(opCode);
            foreach (byte b in data)
            {
                bytes.Add(b);
                lines.Add(currentLine);
            }
        }
    }
}