namespace NodeScript;

using System.Text;
using static CompilerUtils;
using static TokenType;

internal static class Validator
{
    public static void Validate(Operation?[] operations, InternalErrorHandler errorHandler)
    {
        int ifEndif = 0;
        for (int i = 0; i < operations.Length; i++)
        {
            Operation? op = operations[i];
            if (op is null) continue;
            ExpressionValidator v = new(errorHandler, i);
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
        if (ifEndif != 0) errorHandler(operations.Length - 1, "IF statements do not match ENDIF statements");

        Stack<(Dictionary<string, Type>, Dictionary<string, Type>)> variableTypes = new();
        variableTypes.Push((new()
        {
            ["input"] = typeof(string),
            ["mem"] = typeof(string)
        }, []));

        Stack<bool> isSecondStack = new();
        isSecondStack.Push(false);
        Dictionary<string, Type> currentVariableTypeMap = variableTypes.Peek().Item1;

        for (int i = 0; i < operations.Length; i++)
        {
            Operation? op = operations[i];
            if (op is null) continue;

            TypeInferer typeInferer = new(currentVariableTypeMap, errorHandler, i);
            switch (op.operation)
            {
                case SET:
                    Type typeSet = op.expressions[1].Accept(typeInferer);
                    currentVariableTypeMap[((Variable)op.expressions[0]).Name.Lexeme.ToString()] = typeSet;
                    continue;
                case IF:
                    variableTypes.Push((new(currentVariableTypeMap), new(currentVariableTypeMap)));
                    isSecondStack.Push(false);
                    currentVariableTypeMap = variableTypes.Peek().Item1;
                    break;
                case ELSE:
                    isSecondStack.Pop();
                    isSecondStack.Push(true);
                    currentVariableTypeMap = variableTypes.Peek().Item2;
                    break;
                case ENDIF:
                    isSecondStack.Pop();
                    var pair = variableTypes.Pop();
                    Dictionary<string, Type> mergedDictionaries = MergeDictionaries(pair.Item1, pair.Item2);
                    currentVariableTypeMap = mergedDictionaries;
                    var pairToModify = variableTypes.Peek();
                    if (isSecondStack.Peek())
                        pairToModify.Item2 = mergedDictionaries;
                    else
                        pairToModify.Item1 = mergedDictionaries;
                    break;
            }
            foreach (Expr ex in op.expressions)
                ex.Accept(typeInferer);
        }

        for (int i = 0; i < operations.Length; i++)
        {
            if (operations[i] is null) continue;
            Operation op = operations[i]!;
            if (op.expressions.Length > 0)
            {
                TypeValidator typeValidator = new(errorHandler, i);
                foreach (Expr ex in op.expressions)
                    ex.Accept(typeValidator);
            }
            switch (op.operation)
            {
                case PRINT:
                    if (!IsType(op.expressions[0].Type, typeof(int)) || !IsType(op.expressions[1].Type, typeof(string)))
                        errorHandler(i, $"PRINT must be of the form: PRINT <int>, <string>");
                    break;
                case SET:
                    string variableName = ((Variable)op.expressions[0]).Name.Lexeme.ToString();
                    if (variableName == "input" || variableName == "mem")
                        if (!IsType(op.expressions[1].Type, typeof(string)))
                            errorHandler(i, $"{variableName} must be set to a string");
                    break;
            }
        }
    }

    private static Dictionary<string, Type> MergeDictionaries(Dictionary<string, Type> dictOne, Dictionary<string, Type> dictTwo)
    {
        Dictionary<string, Type> output = [];
        foreach (string key in dictOne.Keys)
        {
            Type typeOne = dictOne[key];
            if (dictTwo.TryGetValue(key, out Type? value))
            {
                Type typeTwo = value;
                if (typeOne == typeTwo)
                    output[key] = typeOne;
                else
                    output[key] = typeof(object);
            }
            else
            {
                output[key] = typeOne;
            }
        }

        foreach (string key in dictTwo.Keys)
        {
            Type typeTwo = dictTwo[key];
            if (!dictOne.ContainsKey(key))
                output[key] = typeTwo;
        }
        return output;
    }

    private class TypeInferer(Dictionary<string, Type> variableTypes, InternalErrorHandler errorHandler, int lineNo) : Expr.IVisitor<Type>
    {
        private readonly Dictionary<string, Type> variableTypes = variableTypes;

        public Type VisitBinaryExpr(Binary expr)
        {
            Type leftType = expr.Left.Accept(this);
            Type rightType = expr.Right.Accept(this);
            switch (expr.Op.type)
            {
                case EQUAL_EQUAL:
                case BANG_EQUAL:
                case GREATER:
                case GREATER_EQUAL:
                case LESS:
                case LESS_EQUAL:
                case AND:
                case OR:
                    expr.Type = typeof(bool);
                    break;
                case STAR:
                case MINUS:
                case SLASH:
                    expr.Type = typeof(int);
                    break;
                case PLUS:
                    if (leftType == typeof(int) || rightType == typeof(int))
                        expr.Type = typeof(int);
                    else if (leftType == typeof(string) || rightType == typeof(string))
                        expr.Type = typeof(string);
                    else if (leftType == typeof(string[]) || rightType == typeof(string[]))
                        expr.Type = typeof(string[]);
                    break;
            }
            return expr.Type;
        }

        public Type VisitCallExpr(Call expr)
        {
            Type[] argumentTypes = new Type[expr.Arguments.Count];
            for (int i = 0; i < argumentTypes.Length; i++)
                argumentTypes[i] = expr.Arguments[i].Accept(this);

            StringBuilder functionNameKnownType = new(expr.Callee.Name.Lexeme.ToString());
            foreach (Type t in argumentTypes)
                functionNameKnownType.Append(NativeFuncsKnownType.typeToStr[t]);

            if (NativeFuncsKnownType.NativeReturnTypes.TryGetValue(functionNameKnownType.ToString(), out Type? value))
            {
                expr.Type = value;
                return expr.Type;
            }

            if (NativeFuncs.NativeReturnTypes.TryGetValue(expr.Callee.Name.Lexeme.ToString(), out value))
            {
                expr.Type = value;
                return expr.Type;
            }
            return expr.Type;
        }

        public Type VisitGroupingExpr(Grouping expr)
        {
            expr.Type = expr.Expression.Accept(this);
            return expr.Type;
        }

        public Type VisitIndexExpr(Index expr)
        {
            for (int i = 0; i < expr.Arguments.Length; i++)
                expr.Arguments[i].Accept(this);
            Type variableType = expr.Variable.Accept(this);

            if (variableType == typeof(string))
                expr.Type = typeof(string);
            else if (variableType == typeof(string[]))
                expr.Type = expr.Arguments.Length == 1 ? expr.Type = typeof(string) : typeof(string[]);
            return expr.Type;
        }

        public Type VisitLiteralExpr(Literal expr)
        {
            return expr.Type;
        }

        public Type VisitUnaryExpr(Unary expr)
        {
            expr.Right.Accept(this);
            switch (expr.Op.type)
            {
                case MINUS:
                    expr.Type = typeof(int);
                    break;
                case BANG:
                    expr.Type = typeof(bool);
                    break;
            }
            return expr.Type;
        }

        public Type VisitVariableExpr(Variable expr)
        {
            if (!variableTypes.TryGetValue(expr.Name.Lexeme.ToString(), out Type? types))
            {
                errorHandler(lineNo, "Attempting to get a variable that doesn't exist.");
                return typeof(object);
            }
            expr.Type = types;
            return expr.Type;
        }
    }

    private class TypeValidator(InternalErrorHandler errorHandler, int lineNo) : Expr.IVisitor<bool>
    {
        private readonly InternalErrorHandler errorHandler = errorHandler;

        public bool VisitBinaryExpr(Binary expr)
        {
            bool valid = expr.Left.Accept(this) & expr.Right.Accept(this);
            switch (expr.Op.type)
            {
                case EQUAL_EQUAL:
                case BANG_EQUAL:
                    return valid;
                case GREATER:
                case GREATER_EQUAL:
                case LESS:
                case LESS_EQUAL:
                case STAR:
                case MINUS:
                case SLASH:
                    return valid && IsType(expr.Left.Type, typeof(int)) && IsType(expr.Right.Type, typeof(int));
                case AND:
                case OR:
                    return valid && IsType(expr.Left.Type, typeof(bool)) && IsType(expr.Right.Type, typeof(bool));
                case PLUS:
                    return valid && IsType(expr.Left.Type, expr.Type) && IsType(expr.Right.Type, expr.Type);
                default: return false;
            }
        }

        public bool VisitCallExpr(Call expr)
        {
            return true;
        }

        public bool VisitGroupingExpr(Grouping expr)
        {
            return expr.Accept(this);
        }

        public bool VisitIndexExpr(Index expr)
        {
            bool valid = expr.Arguments.All(e => e.Accept(this));
            if (!expr.Arguments.All(e => IsType(e.Type, typeof(int))))
            {
                errorHandler(lineNo, $"Cannot index with non-integer type");
                return false;
            }
            return valid;
        }

        public bool VisitLiteralExpr(Literal expr)
        {
            return true;
        }

        public bool VisitUnaryExpr(Unary expr)
        {
            bool valid = expr.Accept(this);
            switch (expr.Op.type)
            {
                case MINUS:
                    if (!IsType(expr.Right.Type, typeof(int)))
                    {
                        errorHandler(lineNo, $"Cannot negate type {expr.Right.Type}");
                        return false;
                    }
                    return valid;
                case BANG:
                    if (!IsType(expr.Right.Type, typeof(bool)))
                    {
                        errorHandler(lineNo, $"Cannot not type {expr.Right.Type}");
                        return false;
                    }
                    return valid;
                default: return false;
            }
        }

        public bool VisitVariableExpr(Variable expr) => true;
    }

    private class ExpressionValidator(InternalErrorHandler errorHandler, int lineNo) : Expr.IVisitor<bool>
    {
        private readonly InternalErrorHandler errorHandler = errorHandler;

        public bool VisitBinaryExpr(Binary expr) => expr.Left.Accept(this) & expr.Right.Accept(this);

        public bool VisitIndexExpr(Index expr) => expr.Arguments.All((a) => a.Accept(this));

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