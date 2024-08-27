namespace NodeScript;

using System.Diagnostics;

using static TokenType;
using static CompilerUtils;
using System.Text;

[DebuggerDisplay("currentLine = {currentLine, nq}")]
internal class Compiler(Operation?[] operations, InternalErrorHandler errorHandler)
{
    private readonly Operation?[] operations = operations;
    private readonly InternalErrorHandler errorHandler = errorHandler;
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

        // Insert an additional return operation just in case the user didn't include one at the end.    
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

        // Each operation's sub-expressions are compiled first
        int startIdx = bytes.Count;
        if (op.expressions.Length != 0)
        {
            CompilerVisitor visitor = new(bytes, lines, constants, currentLine);
            foreach (Expr expr in op.expressions)
            {
                if (!expr.Accept(visitor))
                    return false;
            }
        }

        // The operation is compiled second
        switch (op.operation)
        {
            case SET:
                // In the SET case, our compiler will include a Get operation for the variable we wish to set. This should be the first GET which occurs.
                // We iterate through the operations to find that get OpCode.
                int idx = -1;
                for (; startIdx < bytes.Count; startIdx++)
                {
                    OpCode oper = (OpCode)bytes[startIdx];
                    switch (oper)
                    {
                        case OpCode.GET: idx = startIdx; startIdx = bytes.Count; break;
                        case OpCode.CONSTANT:
                        case OpCode.SET: startIdx++; break;
                        case OpCode.JUMP:
                        case OpCode.CALL: startIdx += 2; break;
                        case OpCode.JUMP_IF_FALSE:
                            startIdx++;
                            break;
                    }
                }
                if (idx == -1)
                {
                    errorHandler.Invoke(currentLine, "No identifier for set command");
                    return false;
                }
                // We remove the GET code and add a SET code at the end.
                bytes.RemoveAt(idx);
                byte id = bytes[idx];
                bytes.RemoveAt(idx);
                bytes.Add((byte)OpCode.SET);
                bytes.Add(id);
                break;
            case PRINT:
                if (op.expressions[0].Type == typeof(int) && op.expressions[1].Type == typeof(string))
                    bytes.Add((byte)OpCode.PRINTIS);
                else
                    bytes.Add((byte)OpCode.PRINT);
                lines.Add(currentLine);
                break;
            case RETURN: bytes.Add((byte)OpCode.RETURN); lines.Add(currentLine); break;
            // All jump codes temporarily use 0xffff as their offset.
            case IF:
                bytes.Add((byte)OpCode.JUMP_IF_FALSE); bytes.Add(0xff); bytes.Add(0xff);
                lines.Add(currentLine); lines.Add(currentLine); lines.Add(currentLine);
                break;
            case ELSE:
                bytes.Add((byte)OpCode.JUMP); bytes.Add(0xff); bytes.Add(0xff);
                lines.Add(currentLine); lines.Add(currentLine); lines.Add(currentLine);
                break;
            // A NOP code serves as the ENDIF marker. This is for jump patching.
            case ENDIF: bytes.Add((byte)OpCode.NOP); lines.Add(currentLine); break;
        }
        bytes.Add((byte)OpCode.LINE_END);
        lines.Add(currentLine);
        return true;
    }

    private bool PatchJumps(List<byte> code, List<int> lines)
    {
        // This is a stack of indices of IF statements. We need to use stacks to deal with nested IFs appropriately
        Stack<(int idx, bool hasElse)> ifStmts = [];
        Stack<int> elseStmts = [];
        int ifIdx, elseIdx;
        ushort diff;
        bool hasElse;

        // We iterate through all instructions and find JUMP or JUMP_IF_FALSE operations.
        for (int opNo = 0; opNo < code.Count; opNo++)
        {
            OpCode opCode = (OpCode)code[opNo];
            switch (opCode)
            {
                case OpCode.CONSTANT:
                case OpCode.GET:
                case OpCode.SET: opNo++; break;
                case OpCode.JUMP:
                    // A JUMP operation occurs during ELSE statements
                    elseStmts.Push(opNo + 1); // This points to the JUMP offset, allowing us to set it easily later on.
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

                    // This is the offset between the previous IF statement and our current ELSE statement.
                    // Technically this is the difference between the respective beginnings of the jump offsets, 
                    //      but that's equal to the difference between the respective ends of the jump offsets (ie the following instruction).
                    diff = (ushort)(opNo + 1 - ifIdx);
                    code[ifIdx] = (byte)(diff >> 8);
                    code[ifIdx + 1] = (byte)(diff & 0xFF);
                    ifStmts.Push((ifIdx, true));
                    opNo += 2;
                    break;
                case OpCode.JUMP_IF_FALSE:
                    ifStmts.Push((opNo + 1, false));
                    opNo += 2;
                    break;
                case OpCode.NOP:
                    (ifIdx, hasElse) = ifStmts.Pop();
                    // We don't need to do the same validation here because we've already done our validation step before.
                    if (hasElse)
                    {
                        // The JUMP at the else index is called at the end of the IF block.
                        // So we need this to jump to the end of the if statement, skipping the ELSE block
                        elseIdx = elseStmts.Pop();
                        diff = (ushort)(opNo - elseIdx);
                        code[elseIdx] = (byte)(diff >> 8);
                        code[elseIdx + 1] = (byte)(diff & 0xFF);
                    }
                    else
                    {
                        // The IF block will be skipped if the condition is false (ie the JUMP_IF_FALSE operation)
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

    // This class will visit expressions in post-order and emit the appropriate bytecode depending on the type of expression
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
                case LESS:
                    if (expr.Left.Type == typeof(int) && expr.Right.Type == typeof(int))
                        Emit(OpCode.LESSI);
                    else
                        Emit(OpCode.LESS);
                    break;
                case LESS_EQUAL:
                    if (expr.Left.Type == typeof(int) && expr.Right.Type == typeof(int))
                        Emit(OpCode.LESS_EQUALI);
                    else
                        Emit(OpCode.LESS_EQUAL);
                    break;
                case MINUS:
                    if (expr.Left.Type == typeof(int) && expr.Right.Type == typeof(int))
                        Emit(OpCode.SUBTRACTI);
                    else
                        Emit(OpCode.SUBTRACT);
                    break;
                case OR:
                    if (expr.Left.Type == typeof(bool) && expr.Right.Type == typeof(bool))
                        Emit(OpCode.ORB);
                    else
                        Emit(OpCode.OR);
                    break;
                case PLUS:
                    if (expr.Left.Type == typeof(int) && expr.Right.Type == typeof(int))
                        Emit(OpCode.ADDI);
                    else if (expr.Left.Type == typeof(string) && expr.Right.Type == typeof(string))
                        Emit(OpCode.ADDS);
                    else if (expr.Left.Type == typeof(string[]) && expr.Right.Type == typeof(string[]))
                        Emit(OpCode.ADDA);
                    else
                        Emit(OpCode.ADD);
                    break;
                case SLASH:
                    if (expr.Left.Type == typeof(int) && expr.Right.Type == typeof(int))
                        Emit(OpCode.DIVIDEI);
                    else
                        Emit(OpCode.DIVIDE);
                    break;
                case STAR:
                    if (expr.Left.Type == typeof(int) && expr.Right.Type == typeof(int))
                        Emit(OpCode.MULTIPLYI);
                    else
                        Emit(OpCode.MULTIPLY);
                    break;
                case AND:
                    if (expr.Left.Type == typeof(bool) && expr.Right.Type == typeof(bool))
                        Emit(OpCode.ANDB);
                    else
                        Emit(OpCode.AND);
                    break;
                case BANG_EQUAL: Emit(OpCode.NOT_EQUAL); break;
                case EQUAL_EQUAL: Emit(OpCode.EQUAL); break;
                case GREATER:
                    if (expr.Left.Type == typeof(int) && expr.Right.Type == typeof(int))
                        Emit(OpCode.GREATERI);
                    else
                        Emit(OpCode.GREATER);
                    break;
                case GREATER_EQUAL:
                    if (expr.Left.Type == typeof(int) && expr.Right.Type == typeof(int))
                        Emit(OpCode.GREATER_EQUALI);
                    else
                        Emit(OpCode.GREATER_EQUAL);
                    break;
                default: return false;
            }
            return true;
        }

        public bool VisitIndexExpr(Index expr)
        {
            if (!expr.Variable.Accept(this)) return false;
            if (!expr.Arguments.All((a) => a.Accept(this))) return false;

            string f = expr.Arguments.Length == 1 ? "element_at" : "slice";
            StringBuilder funcName = new(f);
            funcName.Append(NativeFuncsKnownType.typeToStr[expr.Variable.Type]);
            foreach (Expr ex in expr.Arguments)
                funcName.Append(NativeFuncsKnownType.typeToStr[ex.Type]);
            if (NativeFuncsKnownType.NativeFunctions.ContainsKey(funcName.ToString()))
                Emit(OpCode.CALL_TYPE_KNOWN, MakeConst(funcName.ToString()), (byte)(expr.Arguments.Length + 1));
            else
                Emit(OpCode.CALL, MakeConst(f), (byte)(expr.Arguments.Length + 1));
            return true;
        }

        public bool VisitCallExpr(Call expr)
        {
            if (!expr.Arguments.All((e) => e.Accept(this))) return false;

            StringBuilder funcName = new(expr.Callee.Name.Lexeme.ToString());
            foreach (Expr ex in expr.Arguments)
                funcName.Append(NativeFuncsKnownType.typeToStr[ex.Type]);
            if (NativeFuncsKnownType.NativeFunctions.TryGetValue(funcName.ToString(), out NativeDelegate? value))
                Emit(OpCode.CALL_TYPE_KNOWN, MakeConst(funcName.ToString()), (byte)expr.Arguments.Count);
            else
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
                case MINUS:
                    if (expr.Right.Type == typeof(int))
                        Emit(OpCode.NEGATEI);
                    else
                        Emit(OpCode.NEGATE);
                    break;
                case BANG:
                    if (expr.Right.Type == typeof(bool))
                        Emit(OpCode.NOTB);
                    else
                        Emit(OpCode.NOT);
                    break;
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