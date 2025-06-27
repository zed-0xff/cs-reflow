using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public partial class VarProcessor
{
    public class Expression
    {
        CSharpSyntaxNode node;
        VarDict _varDict;
        public int Verbosity = 0;

        public List<string> VarsWritten { get; } = new();
        public List<string> VarsRead { get; } = new();
        public List<string> VarsReferenced => VarsRead.Union(VarsWritten).ToList();

        public object? Result { get; private set; } = UnknownValue.Create();

        static readonly string TAG = "Expression";
        private static readonly TaggedLogger _logger = new(TAG);

        public Expression(CSharpSyntaxNode node, VarDict varDict)
        {
            this.node = node;
            this._varDict = varDict;
        }

        public Expression SetVerbosity(int verbosity)
        {
            Verbosity = verbosity;
            return this;
        }

        public object? Evaluate()
        {
            object? result = null;
            switch (node)
            {
                case ExpressionSyntax expression:
                    result = EvaluateExpression(expression);
                    break;
                case LocalDeclarationStatementSyntax localDeclaration:
                    ProcessLocalDeclaration(localDeclaration);
                    break;
                case ExpressionStatementSyntax expressionStatement:
                    result = EvaluateExpression(expressionStatement.Expression);
                    break;
                default:
                    throw new NotSupportedException($"Syntax node type '{node?.GetType()}' is not supported.");
            }
            if (result is IntConstExpr ice)
            {
                // if the result is an IntConstExpr, convert it to its value
                result = ice.Value;
            }
            Result = result;
            _logger.debug(() => $"{node.Title()} => {result}", "Expression.Evaluate");
            return result;
        }

        void ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration)
        {
            if (Verbosity > 2)
            {
                _logger.debug($"{localDeclaration}", "Expression.ProcessLocalDeclaration");
                if (Verbosity > 3)
                {
                    _logger.debug($"Expression.ProcessLocalDeclaration: .Declaration = {localDeclaration.Declaration}", "Expression.ProcessLocalDeclaration");
                    _logger.debug($"Expression.ProcessLocalDeclaration: .Declaration.Type = {localDeclaration.Declaration.Type}", "Expression.ProcessLocalDeclaration");
                    _logger.debug($"Expression.ProcessLocalDeclaration: .Declaration.Variables = {localDeclaration.Declaration.Variables}", "Expression.ProcessLocalDeclaration");
                }
            }

            foreach (var variable in localDeclaration.Declaration.Variables)
            {
                if (Verbosity > 2)
                    _logger.debug($"Expression.ProcessLocalDeclaration: A Variable = {variable}", "Expression.ProcessLocalDeclaration");

                // Get the variable name (e.g., "num3")
                var varID = variable.Identifier;
                if (!_varDict.IsVariableRegistered(varID))
                {
                    _varDict.RegisterVariable(variable);
                }

                // if (string.IsNullOrEmpty(varName))
                //     throw new NotSupportedException($"Empty variable name in: {localDeclaration}");
                // 
                // VarsWritten.Add(varName);

                // Extract the right-hand side expression (e.g., "(num4 = (uint)(num2 ^ 0x76ED016F))")
                var initializerExpression = variable.Initializer?.Value;

                object value = _varDict.DefaultValue(varID);

                if (initializerExpression != null)
                {
                    setVar(varID, value); // set to UnknownValue first, because EvaluateExpression() may throw an exception
                    value = EvaluateExpression(initializerExpression);
                }

                if (Verbosity > 2)
                    _logger.debug($"[d] Expression.ProcessLocalDeclaration: C varID={varID} value={value}", "Expression.ProcessLocalDeclaration");

                // narrow returned value type, when possible
                if (localDeclaration.Declaration.Type is not null && TypeDB.TryFind(localDeclaration.Declaration.Type.ToString()) is not null)
                {
                    switch (value)
                    {
                        case UnknownValue:
                            value = UnknownValue.Create(localDeclaration.Declaration.Type);
                            break;
                        case IntConstExpr ice:
                            value = ice.Cast(localDeclaration.Declaration.Type.ToString());
                            break;
                    }
                }

                if (Verbosity > 3)
                    _logger.debug($"[d] Expression.ProcessLocalDeclaration: D varID={varID} value={value}", "Expression.ProcessLocalDeclaration");

                // if (value is UnknownValueBase unk)
                //     value = unk.WithTag(varID);

                if (Verbosity > 3)
                    _logger.debug($"[d] Expression.ProcessLocalDeclaration: E varID={varID} value={value}", "Expression.ProcessLocalDeclaration");

                // do not overwrite existing variable values if initializerExpression is null
                if (initializerExpression != null || !_varDict.ContainsKey(varID))
                {
                    setVar(varID, value);
                }

                if (Verbosity > 3)
                    _logger.debug($"[d] Expression.ProcessLocalDeclaration: F varID={varID} value={value}", "Expression.ProcessLocalDeclaration");

                // don't return 'value' bc it might be an UnknownValue from empty declaration, but vars may already have its value
                //return _varDict[varID];
            }
        }

        void setVar(IdentifierNameSyntax id, object value) => setVar(id.Identifier, value);
        void setVar(SyntaxToken token, object value)
        {
            if (_varDict.TryGetValue(token, out var existingValue))
            {
                switch (value)
                {
                    case UnknownValue:
                        // if the variable already exists, use its type
                        value = existingValue switch
                        {
                            UnknownValue => value, // no luck
                            UnknownTypedValue utv => UnknownValue.Create(utv.type),
                            _ => UnknownValue.Create(existingValue?.GetType()) // handles null values as well
                        };
                        break;

                    case IntConstExpr ice:
                        // if the variable already exists, use its type
                        value = existingValue switch
                        {
                            null => ice.Value, // int
                            UnknownValue => ice.Value, // int
                            UnknownTypedValue utv => ice.Cast(utv.type),
                            _ => ice.TryCast(existingValue.GetType()) ?? ice.Value
                        };
                        break;
                }
            }

            // if (value is UnknownValueBase unk)
            //     value = unk.WithTag(varName);

            // both after previous if/switch or if the variable does not exist
            if (value is IntConstExpr ice2)
                value = ice2.Value; // int

            _varDict.Set(token, value);
        }

        object cast_var(dynamic value, string toType)
        {
            switch (value)
            {
                case UnknownValueBase uv:
                    return TypeDB.TryFind(toType) == null ? new UnknownValue() : uv.Cast(TypeDB.Find(toType));
                case IntConstExpr ice:
                    return ice.FakeCast(TypeDB.Find(toType));
            }

            return toType switch
            {
                "bool" => Convert.ToBoolean(value),
                "byte" => (byte)value,
                "sbyte" => (sbyte)value,
                "short" => (short)value,
                "ushort" => (ushort)value,
                "int" => (int)value,
                "uint" => (uint)value,
                "long" => (long)value,
                "ulong" => (ulong)value,
                "nint" => (int)value,   // TODO: 32/64 bit cmdline switch
                "nuint" => (uint)value, // TODO: 32/64 bit cmdline switch
                "string" => value.ToString() ?? string.Empty,
                _ => throw new NotSupportedException($"Cast from '{value?.GetType()}' to '{toType}' is not supported.")
            };
        }

        [ThreadStatic]
        static bool _excLogged;

        [ThreadStatic]
        static int _evalDepth;

        object EvaluateExpression(ExpressionSyntax expr)
        {
            _evalDepth++;
            try
            {
                var result = EvaluateExpression_(expr);
                if (Verbosity > 0 || Logger.HasTag("Expression.EvaluateExpression"))
                {
                    Console.Error.WriteColor($"[.] ", ConsoleColor.DarkGray);
                    Console.Error.Write($"{expr,-50}");
                    Console.Error.WriteColor(" => ", ConsoleColor.DarkGray);
                    switch (result)
                    {
                        case UnknownValue:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        case UnknownTypedValue utv:
                            Console.ForegroundColor = utv.Cardinality() == 1 ? ConsoleColor.Green : ConsoleColor.Yellow;
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            break;
                    }
                    if (Verbosity > 1) Console.Error.Write($"({result?.GetType()}) ");
                    switch (result)
                    {
                        case IntConstExpr ice:
                            Console.Error.WriteLine($"{ice.Value,-11} (0x{ice.Value:X})");
                            break;
                        case int i:
                            Console.Error.WriteLine($"{i,-11} (0x{i:X})");
                            break;
                        case uint u:
                            Console.Error.WriteLine($"{u,-11} (0x{u:X})");
                            break;
                        default:
                            Console.Error.WriteLine($"{result}");
                            break;
                    }
                    Console.ResetColor();
                }
                return result;
            }
            catch (Exception ex)
            {
                if (ex is not NotSupportedException && !_excLogged) // log only from the deepest level
                {
                    string msg = $"Error evaluating expr '{expr.TitleWithLineNo()}': {ex.GetType()}";
                    var vars = _varDict.VarsFromNode(expr);
                    if (vars.Count > 0)
                        msg += $" ({vars})";

                    _logger.error(msg);
                    _excLogged = true;
                }
                throw;
            }
            finally
            {
                _evalDepth--;
                if (_evalDepth == 0 && _excLogged)
                {
                    // Reset the flag to avoid logging multiple times in the same thread
                    _excLogged = false;
                }
            }
        }

        void reset_tuple_vars(TupleExpressionSyntax tupleExpr)
        {
            // Reset all variables in the tuple expression
            foreach (var item in tupleExpr.Arguments)
            {
                var expr = item.Expression.StripParentheses();
                switch (expr)
                {
                    case IdentifierNameSyntax idName:
                        _varDict.ResetVar(idName.Identifier);
                        break;
                    case TupleExpressionSyntax nestedTuple:
                        reset_tuple_vars(nestedTuple);
                        break;
                    default:
                        // Ignore other types of expressions
                        _logger.warn_once($"ignoring {expr.Kind()} in {tupleExpr}");
                        break;
                }
            }
        }

        // Handle assignment expressions (e.g., num3 = (num4 = (uint)(num2 ^ 0x76ED016F)))
        object? EvaluateAssignment(ExpressionSyntax left, SyntaxKind kind, ExpressionSyntax right)
        {
            left = left.StripParentheses();
            right = right.StripParentheses();

            if (left is TupleExpressionSyntax tupleL)
            {
                if (kind != SyntaxKind.SimpleAssignmentExpression)
                    throw new NotSupportedException($"Tuple assignment with '{kind}' is not supported");

                reset_tuple_vars(tupleL);
                if (right is TupleExpressionSyntax tupleR)
                {
                    if (tupleL.Arguments.Count != tupleR.Arguments.Count)
                        throw new NotSupportedException($"Tuple assignment mismatch: {tupleL.Arguments.Count} != {tupleR.Arguments.Count}");
                    for (int i = 0; i < tupleL.Arguments.Count; i++)
                    {
                        EvaluateAssignment(
                            tupleL.Arguments[i].Expression,
                            SyntaxKind.SimpleAssignmentExpression,
                            tupleR.Arguments[i].Expression
                        );
                    }
                }
                return UnknownValue.Create(); // TODO: return tuple?
            }

            if (!(left is IdentifierNameSyntax idNameLeft))
                throw new NotSupportedException($"Assignment to '{left.Kind()}' is not supported");
            SyntaxToken idLeft = idNameLeft.Identifier;

            // string varName = left.ToString(); // XXX arrays?
            // VarsWritten.Add(varName);

            // Evaluate the right-hand side expression
            try
            {
                var rValue = EvaluateExpression(right);
                switch (kind)
                {
                    case SyntaxKind.SimpleAssignmentExpression:
                        setVar(idLeft, rValue);
                        break;

                    default:
                        // +=, -=, etc
                        SyntaxKind binaryOperatorKind = kind switch
                        {
                            SyntaxKind.AddAssignmentExpression => SyntaxKind.AddExpression,
                            SyntaxKind.SubtractAssignmentExpression => SyntaxKind.SubtractExpression,
                            SyntaxKind.MultiplyAssignmentExpression => SyntaxKind.MultiplyExpression,
                            SyntaxKind.DivideAssignmentExpression => SyntaxKind.DivideExpression,
                            SyntaxKind.ModuloAssignmentExpression => SyntaxKind.ModuloExpression,
                            SyntaxKind.AndAssignmentExpression => SyntaxKind.BitwiseAndExpression,
                            SyntaxKind.OrAssignmentExpression => SyntaxKind.BitwiseOrExpression,
                            SyntaxKind.ExclusiveOrAssignmentExpression => SyntaxKind.ExclusiveOrExpression,
                            SyntaxKind.LeftShiftAssignmentExpression => SyntaxKind.LeftShiftExpression,
                            SyntaxKind.RightShiftAssignmentExpression => SyntaxKind.RightShiftExpression,
                            _ => throw new InvalidOperationException("Unsupported compound assignment operator")
                        };
                        setVar(idLeft, EvaluateBinaryExpression(BinaryExpression(binaryOperatorKind, left, right)));
                        break;
                }
                return _varDict[idLeft];
            }
            catch (Exception ex)
            {
                _logger.debug($"catched \"{ex.Message}\" in EvaluateExpression for {idLeft}");
                _varDict.ResetVar(idLeft);
                throw;
            }
        }

        object? EvaluateExpression_(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case AssignmentExpressionSyntax assignmentExpr:
                    return EvaluateAssignment(assignmentExpr.Left, assignmentExpr.Kind(), assignmentExpr.Right);

                case BinaryExpressionSyntax binaryExpr:
                    return EvaluateBinaryExpression(binaryExpr);

                case CastExpressionSyntax castExpr:               // (uint)num2
                    var value = EvaluateExpression(castExpr.Expression);
                    return cast_var(value, castExpr.Type.ToString());

                case ConditionalExpressionSyntax conditionalExpr: // ternary operator: num3 == 0 ? num4 : num5
                    var condition = EvaluateExpression(conditionalExpr.Condition);
                    try
                    {
                        if (Convert.ToBoolean(condition))
                            return EvaluateExpression(conditionalExpr.WhenTrue);
                        else
                            return EvaluateExpression(conditionalExpr.WhenFalse);
                    }
                    catch (InvalidCastException)
                    {
                        _logger.warn_once($"ConditionalExpression: cannot cast '{condition}' to bool in: {conditionalExpr}");
                        object whenTrue = EvaluateExpression(conditionalExpr.WhenTrue);
                        object whenFalse = EvaluateExpression(conditionalExpr.WhenFalse);
                        object result = (Equals(whenTrue, whenFalse)) ? whenTrue : VarProcessor.MergeVar("?", whenTrue, whenFalse);
                        _logger.warn_once($"ConditionalExpression: merged {whenTrue} and {whenFalse} => {result}");
                        return result;
                    }

                case DefaultExpressionSyntax defaultExpr:
                    // Handle default expressions (e.g., default(uint))
                    return defaultExpr.ToString() switch
                    {
                        // Handle default values for common types
                        "default(bool)" => default(bool),
                        "default(char)" => default(char),
                        "default(byte)" => default(byte),
                        "default(double)" => default(double),
                        "default(float)" => default(float),
                        "default(int)" => default(int),
                        "default(long)" => default(long),
                        "default(nint)" => default(nint),   // zero for both 32/64
                        "default(nuint)" => default(nuint), // zero for both 32/64
                        "default(object)" => default(object),
                        "default(sbyte)" => default(sbyte),
                        "default(string)" => default(string),
                        "default(uint)" => default(uint),
                        "default(ulong)" => default(ulong),
                        "default(ushort)" => default(ushort),
                        _ => throw new NotSupportedException($"Default expression '{defaultExpr.ToString()}' is not supported.")
                    };

                case IdentifierNameSyntax id:
                    // If the expression is an identifier, fetch its value from the dictionary
                    // string varName2 = id.Identifier.Text;
                    // VarsRead.Add(varName2);
                    return _varDict[id.Identifier];

                case LiteralExpressionSyntax literal:
                    if (literal.Token.Value is int i)
                        return new IntConstExpr(i);
                    return literal.Token.Value;

                case MemberAccessExpressionSyntax:
                    if (Constants.TryGetValue(expression.ToString(), out var constantValue))
                        return constantValue;
                    else
                        goto default;

                case ParenthesizedExpressionSyntax parenExpr:
                    return EvaluateExpression(parenExpr.Expression);

                case CheckedExpressionSyntax checkedExpr: // TODO: simulate exception on overflow?
                    return EvaluateExpression(checkedExpr.Expression);

                case PrefixUnaryExpressionSyntax prefixExpr:
                    return EvaluatePrefixExpression(prefixExpr);

                case PostfixUnaryExpressionSyntax postfixExpr:
                    return EvaluatePostfixExpression(postfixExpr);

                case SizeOfExpressionSyntax sizeOfExpr:
                    return sizeOfExpr.ToString() switch
                    {
                        "sizeof(bool)" => sizeof(bool),
                        "sizeof(byte)" => sizeof(byte),
                        "sizeof(char)" => sizeof(char),
                        "sizeof(double)" => sizeof(double),
                        "sizeof(float)" => sizeof(float),
                        "sizeof(int)" => sizeof(int),
                        "sizeof(long)" => sizeof(long),
                        // "sizeof(nint)" => sizeof(nint),   // TODO: 32/64 bit cmdline switch
                        // "sizeof(nuint)" => sizeof(nuint), // TODO: 32/64 bit cmdline switch
                        "sizeof(sbyte)" => sizeof(sbyte),
                        "sizeof(uint)" => sizeof(uint),
                        "sizeof(ulong)" => sizeof(ulong),
                        "sizeof(ushort)" => sizeof(ushort),
                        "sizeof(Guid)" => 0x10, // same for 32/64 bit hosts
                        _ => throw new NotSupportedException($"SizeOf expression '{sizeOfExpr.ToString()}' is not supported.")
                    };

                default:
                    throw new NotSupportedException($"{expression.Kind()} is not supported.");
            }
        }

        static INumber<T> eval_prefix<T>(T value, SyntaxKind kind) where T : INumber<T>, IBitwiseOperators<T, T, T>
        {
            return kind switch
            {
                SyntaxKind.BitwiseNotExpression => ~value,
                SyntaxKind.UnaryPlusExpression => value,
                SyntaxKind.UnaryMinusExpression => -value,
                SyntaxKind.PreDecrementExpression => value - T.One,
                SyntaxKind.PreIncrementExpression => value + T.One,
                _ => throw new NotSupportedException($"Unary operator '{kind}' is not supported for {typeof(T)}.")
            };
        }

        static bool eval_prefix(bool value, SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.LogicalNotExpression => !value,
                _ => throw new NotSupportedException($"Unary operator '{kind}' is not supported for bool.")
            };
        }

        static object eval_prefix(object value, SyntaxKind kind)
        {
            return value switch
            {
                bool b => eval_prefix(b, kind),
                byte b => eval_prefix(b, kind),
                int i => eval_prefix(i, kind),
                long l => eval_prefix(l, kind),
                nint ni => eval_prefix(ni, kind),
                nuint nu => eval_prefix(nu, kind),
                sbyte sb => eval_prefix(sb, kind),
                short s => eval_prefix(s, kind),
                uint u => eval_prefix(u, kind),
                ulong ul => eval_prefix(ul, kind),
                ushort us => eval_prefix(us, kind),
                IntConstExpr ice => eval_prefix(ice, kind),
                UnknownValueBase unk => unk.Op(kind),
                _ => throw new NotSupportedException($"Unary operator '{kind}' is not supported for {value.GetType()}.")
            };
        }

        // !x
        // -x
        // +x
        // ~x
        // --x
        // ++x
        object EvaluatePrefixExpression(PrefixUnaryExpressionSyntax expr)
        {
            var value = EvaluateExpression(expr.Operand);
            var retValue = eval_prefix(value, expr.Kind());

            if (expr.Kind() == SyntaxKind.PreDecrementExpression || expr.Kind() == SyntaxKind.PreIncrementExpression)
            {
                switch (expr.Operand)
                {
                    case IdentifierNameSyntax id:
                        // string varName = id.Identifier.Text;
                        // VarsWritten.Add(varName);
                        // VarsRead.Add(varName);
                        _varDict.Set(id, retValue);
                        break;
                    case LiteralExpressionSyntax num:
                        retValue = value;
                        _logger.warn_once($"Prefix operator '{expr.TitleWithLineNo()}' on literal '{num}'. Returning {retValue}");
                        break;
                    default:
                        throw new NotSupportedException($"Prefix operator '{expr.Kind()}' is not supported for {expr.Operand.Kind()}.");
                }
            }

            return retValue;
        }

        static INumber<T> eval_postfix<T>(T value, SyntaxKind kind) where T : INumber<T>, IBitwiseOperators<T, T, T>
        {
            return kind switch
            {
                SyntaxKind.PostDecrementExpression => value - T.One,
                SyntaxKind.PostIncrementExpression => value + T.One,
                _ => throw new NotSupportedException($"Postfix operator '{kind}' is not supported for {typeof(T)}.")
            };
        }

        // x--
        // x++
        object EvaluatePostfixExpression(PostfixUnaryExpressionSyntax expr)
        {
            var value = EvaluateExpression(expr.Operand);
            var retValue = value;
            var kind = expr.Kind();
            object newValue = value switch
            {
                byte b => eval_postfix(b, kind),
                int i => eval_postfix(i, kind),
                long l => eval_postfix(l, kind),
                nint ni => eval_postfix(ni, kind),
                nuint nu => eval_postfix(nu, kind),
                sbyte sb => eval_postfix(sb, kind),
                short s => eval_postfix(s, kind),
                uint u => eval_postfix(u, kind),
                ulong ul => eval_postfix(ul, kind),
                ushort us => eval_postfix(us, kind),
                UnknownValueBase u => u.Op(kind),
                _ => throw new NotSupportedException($"Unary postfix operator '{kind}' is not supported for {value.GetType()}.")
            };
            if (expr.Operand is IdentifierNameSyntax id)
            {
                // VarsWritten.Add(varName);
                // VarsRead.Add(varName);
                _varDict.Set(id, newValue);
            }
            else
            {
                throw new NotSupportedException($"Postfix operand '{expr.Operand.Kind()}' is not supported.");
            }
            return retValue;
        }

        BinaryExpressionSyntax? extract_common_factors(ExpressionSyntax left, ExpressionSyntax right)
        {
            {
                if (
                        left is BinaryExpressionSyntax lb && lb.IsKind(SyntaxKind.MultiplyExpression) &&
                        right is BinaryExpressionSyntax rb && rb.IsKind(SyntaxKind.MultiplyExpression)
                   )
                {
                    // Case A1: (lit1 * id) + (lit2 * id)
                    if (lb.Left is LiteralExpressionSyntax lc1 && lb.Right is IdentifierNameSyntax lv1 &&
                            rb.Left is LiteralExpressionSyntax rc1 && rb.Right is IdentifierNameSyntax rv1 &&
                            lv1.Identifier.Text == rv1.Identifier.Text)
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv1,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc1, rc1)
                                    )
                                );
                    }

                    // Case A2: (id * lit1) + (lit2 * id)
                    if (lb.Left is IdentifierNameSyntax lv2 && lb.Right is LiteralExpressionSyntax lc2 &&
                            rb.Left is LiteralExpressionSyntax rc2 && rb.Right is IdentifierNameSyntax rv2 &&
                            lv2.Identifier.Text == rv2.Identifier.Text)
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv2,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc2, rc2)
                                    )
                                );
                    }

                    // Case A3: (lit1 * id) + (id * lit2)
                    if (lb.Left is LiteralExpressionSyntax lc3 && lb.Right is IdentifierNameSyntax lv3 &&
                            rb.Left is IdentifierNameSyntax rv3 && rb.Right is LiteralExpressionSyntax rc3 &&
                            lv3.Identifier.Text == rv3.Identifier.Text)
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv3,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc3, rc3)
                                    )
                                );
                    }

                    // Case A4: (id * lit1) + (id * lit2)
                    if (lb.Left is IdentifierNameSyntax lv4 && lb.Right is LiteralExpressionSyntax lc4 &&
                            rb.Left is IdentifierNameSyntax rv4 && rb.Right is LiteralExpressionSyntax rc4 &&
                            lv4.Identifier.Text == rv4.Identifier.Text)
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv4,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc4, rc4)
                                    )
                                );
                    }
                }
            }

            {
                if (
                        left is BinaryExpressionSyntax lb && lb.IsKind(SyntaxKind.MultiplyExpression) &&
                        right is IdentifierNameSyntax rid
                   )
                {
                    // Case B1: (lit1 * id) + id
                    if (lb.Left is LiteralExpressionSyntax lc1 && lb.Right is IdentifierNameSyntax lv1 &&
                            lv1.Identifier.Text == rid.Identifier.Text)
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv1,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc1, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                    )
                                );
                    }

                    // Case A2: (id * lit1) + id
                    if (lb.Left is IdentifierNameSyntax lv2 && lb.Right is LiteralExpressionSyntax lc2 &&
                            lv2.Identifier.Text == rid.Identifier.Text)
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv2,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc2, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                    )
                                );
                    }
                }
            }

            {
                if (
                        left is IdentifierNameSyntax lid &&
                        right is BinaryExpressionSyntax rb && rb.IsKind(SyntaxKind.MultiplyExpression)
                   )
                {
                    // Case C1: id + (lit1 * id)
                    if (rb.Left is LiteralExpressionSyntax rc1 && rb.Right is IdentifierNameSyntax rv1 &&
                            rv1.Identifier.Text == lid.Identifier.Text)
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                rv1,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, rc1, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                    )
                                );
                    }

                    // Case C2: id + (id * lit1)
                    if (rb.Left is IdentifierNameSyntax rv2 && rb.Right is LiteralExpressionSyntax rc2 &&
                            rv2.Identifier.Text == lid.Identifier.Text)
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                rv2,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, rc2, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                    )
                                );
                    }
                }
            }

            return null;
        }

        bool is_always_false(BinaryExpressionSyntax binaryExpr)
        {
            {
                if (binaryExpr.Left is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.IsKind(SyntaxKind.BitwiseNotExpression) &&
                        unaryExpr.Operand is IdentifierNameSyntax lid && binaryExpr.Right is IdentifierNameSyntax rid &&
                        lid.Identifier.Text == rid.Identifier.Text && binaryExpr.OperatorToken.Text == "==")
                    return true; // ~x == x
            }

            {
                if (binaryExpr.Right is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.IsKind(SyntaxKind.BitwiseNotExpression) &&
                        unaryExpr.Operand is IdentifierNameSyntax rid && binaryExpr.Left is IdentifierNameSyntax lid &&
                        lid.Identifier.Text == rid.Identifier.Text && binaryExpr.OperatorToken.Text == "==")
                    return true; // x == ~x
            }

            return false;
        }

        bool is_always_true(BinaryExpressionSyntax binaryExpr)
        {
            {
                if (binaryExpr.Left is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.IsKind(SyntaxKind.BitwiseNotExpression) &&
                        unaryExpr.Operand is IdentifierNameSyntax lid && binaryExpr.Right is IdentifierNameSyntax rid &&
                        lid.Identifier.Text == rid.Identifier.Text && binaryExpr.OperatorToken.Text == "!=")
                    return true; // ~x != x
            }

            {
                if (binaryExpr.Right is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.IsKind(SyntaxKind.BitwiseNotExpression) &&
                        unaryExpr.Operand is IdentifierNameSyntax rid && binaryExpr.Left is IdentifierNameSyntax lid &&
                        lid.Identifier.Text == rid.Identifier.Text && binaryExpr.OperatorToken.Text == "!=")
                    return true; // x != ~x
            }

            return false;
        }

        object eval_binary<TL, TR>(TL l, SyntaxKind kind, TR r)
            where TL : INumber<TL>, IBitwiseOperators<TL, TL, TL>
            where TR : INumber<TR>, IBitwiseOperators<TR, TR, TR>
        {
            if (Verbosity > 2)
                Console.Error.WriteLine($"[d] eval_binary: ({l.GetType()}) #{l} {kind} ({r.GetType()}) {r}");

            // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12473-binary-numeric-promotions
            if ((l is IntConstExpr && r is not IntConstExpr) || (r is IntConstExpr))
            {
                var (lp, rp) = VarProcessor.PromoteInts(l, r);
                return eval_binary((dynamic)lp, kind, (dynamic)rp);
            }

            return kind switch
            {
                SyntaxKind.AddExpression => (dynamic)l + (dynamic)r,
                SyntaxKind.SubtractExpression => (dynamic)l - (dynamic)r,
                SyntaxKind.MultiplyExpression => (dynamic)l * (dynamic)r,
                SyntaxKind.DivideExpression => (dynamic)l / (dynamic)r,
                SyntaxKind.ModuloExpression => (dynamic)l % (dynamic)r,
                SyntaxKind.BitwiseAndExpression => (dynamic)l & (dynamic)r,
                SyntaxKind.BitwiseOrExpression => (dynamic)l | (dynamic)r,
                SyntaxKind.ExclusiveOrExpression => (dynamic)l ^ (dynamic)r,
                SyntaxKind.LeftShiftExpression => (dynamic)l << (int)(dynamic)r,
                SyntaxKind.RightShiftExpression => (dynamic)l >> (int)(dynamic)r,
                SyntaxKind.UnsignedRightShiftExpression => (l, r) switch
                {
                    // FIXME: add more types?
                    (int li, int ru) => li >>> ru,
                    (uint lu, int ru) => lu >>> ru,
                    (ulong lu, int ru) => lu >>> ru,
                    (nuint lu, int ru) => lu >>> ru,
                    _ => throw new NotSupportedException(">>> is only supported for uint, ulong, nuint with int shift count")
                },
                SyntaxKind.EqualsExpression => l.Equals(r),
                SyntaxKind.NotEqualsExpression => !l.Equals(r),
                SyntaxKind.LessThanExpression => (dynamic)l < (dynamic)r,
                SyntaxKind.LessThanOrEqualExpression => (dynamic)l <= (dynamic)r,
                SyntaxKind.GreaterThanExpression => (dynamic)l > (dynamic)r,
                SyntaxKind.GreaterThanOrEqualExpression => (dynamic)l >= (dynamic)r,
                SyntaxKind.LogicalAndExpression => Convert.ToBoolean(l) && Convert.ToBoolean(r),
                SyntaxKind.LogicalOrExpression => Convert.ToBoolean(l) || Convert.ToBoolean(r),
                _ => throw new NotSupportedException($"Binary operator '{kind}' is not supported for {typeof(TL)} and {typeof(TR)}.")
            };
        }

        object? eval_binary_and(BinaryExpressionSyntax binaryExpr, object? lValue)
        {
            if (lValue is UnknownValueBase luvb)
                lValue = luvb.Cast(TypeDB.Bool);

            switch (lValue)
            {
                case bool b:
                    return b ? EvaluateExpression(binaryExpr.Right) : false;
                case UnknownValueBase: // nor true nor false
                    var rValue = EvaluateExpression(binaryExpr.Right);
                    if (rValue is UnknownValueBase ruvb)
                        rValue = ruvb.Cast(TypeDB.Bool);
                    if (rValue is bool rb && rb == false)
                        return false; // a whole expression is always false because 2nd operand is always false
                    break;
                default:
                    throw new NotSupportedException($"Left operand '{lValue?.GetType()}' is not supported for '&&' operator.");
            }
            return UnknownValue.Create(TypeDB.Bool);
        }

        object? eval_binary_or(BinaryExpressionSyntax binaryExpr, object? lValue)
        {
            if (lValue is UnknownValueBase luvb)
                lValue = luvb.Cast(TypeDB.Bool);

            switch (lValue)
            {
                case bool b:
                    return b ? true : EvaluateExpression(binaryExpr.Right);
                case UnknownValueBase: // nor true nor false
                    var rValue = EvaluateExpression(binaryExpr.Right);
                    if (rValue is UnknownValueBase ruvb)
                        rValue = ruvb.Cast(TypeDB.Bool);
                    if (rValue is bool rb && rb == true)
                        return true; // a whole expression is always true because 2nd operand is always true
                    break;
                default:
                    throw new NotSupportedException($"Left operand '{lValue?.GetType()}' is not supported for '&&' operator.");
            }
            return UnknownValue.Create(TypeDB.Bool);
        }

        object EvaluateBinaryExpression(BinaryExpressionSyntax binaryExpr)
        {
            // handle falsely misinterpreted "(nint)-17648"
            if (binaryExpr.Left is ParenthesizedExpressionSyntax parenExpr
                    && binaryExpr.OperatorToken.Text == "-"
                    && binaryExpr.Right is LiteralExpressionSyntax)
            {
                string ls = binaryExpr.Left.ToString();
                switch (ls)
                {
                    case "(int)":
                    case "(nint)":
                    case "(uint)":
                    case "(nuint)":
                        return cast_var(
                                EvaluatePrefixExpression(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, binaryExpr.Right)),
                                parenExpr.Expression.ToString()
                                );
                }
            }

            if (is_always_false(binaryExpr))
                return false;

            if (is_always_true(binaryExpr))
                return true;

            string op = binaryExpr.OperatorToken.Text;

            // convert (4 * x + x * 4) => (4 + 4) * x
            // bc its easier then to evaluate when checking unknown values
            if (op == "+")
            {
                var factoredExpr = extract_common_factors(binaryExpr.Left, binaryExpr.Right);
                if (factoredExpr != null)
                {
                    return EvaluateBinaryExpression(factoredExpr);
                }
            }

            object? lValue = EvaluateExpression(binaryExpr.Left); // always evaluated

            switch (op)
            {
                case "&&":
                    return eval_binary_and(binaryExpr, lValue);
                case "||":
                    return eval_binary_or(binaryExpr, lValue);
            }

            // evaluate rValue, handle everything else
            var rValue = EvaluateExpression(binaryExpr.Right); // NOT always evaluated

            if (lValue is UnknownValueBase luv)
                return luv.Op(op, rValue);

            if (rValue is UnknownValueBase ruv)
                return ruv.InverseOp(op, lValue);

            return eval_binary((dynamic)lValue, binaryExpr.Kind(), (dynamic)rValue);
        }
    }
}
