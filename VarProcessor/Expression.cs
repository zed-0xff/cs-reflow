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
        VarDict variableValues;
        public int Verbosity = 0;

        public List<string> VarsWritten { get; } = new();
        public List<string> VarsRead { get; } = new();
        public List<string> VarsReferenced => VarsRead.Union(VarsWritten).ToList();

        public object? Result { get; private set; } = UnknownValue.Create();

        public Expression(CSharpSyntaxNode node, VarDict variableValues)
        {
            this.node = node;
            this.variableValues = variableValues;
        }

        public Expression SetVerbosity(int verbosity)
        {
            Verbosity = verbosity;
            return this;
        }

        public object Evaluate()
        {
            object result;
            switch (node)
            {
                case ExpressionSyntax expression:
                    result = EvaluateExpression(expression);
                    break;
                case LocalDeclarationStatementSyntax localDeclaration:
                    result = ProcessLocalDeclaration(localDeclaration);
                    break;
                case ExpressionStatementSyntax expressionStatement:
                    result = EvaluateExpression(expressionStatement.Expression);
                    break;
                default:
                    throw new NotSupportedException($"Syntax node type '{node.GetType()}' is not supported.");
            }
            if (result is IntConstExpr ice)
            {
                // if the result is an IntConstExpr, convert it to its value
                result = ice.Value;
            }
            Result = result;
            return result;
        }

        object ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration)
        {
            // Extract the variable declaration
            var decl = localDeclaration.Declaration.Variables.First();
            //if (decl == null) return;

            // Get the variable name (e.g., "num3")
            string varName = decl.Identifier.Text;
            VarsWritten.Add(varName);

            // Extract the right-hand side expression (e.g., "(num4 = (uint)(num2 ^ 0x76ED016F))")
            var initializerExpression = decl.Initializer?.Value;

            var value = initializerExpression == null
                ? UnknownValue.Create(localDeclaration.Declaration.Type)
                : EvaluateExpression(initializerExpression);

            // if the variable type is known, use it
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

            // do not overwrite existing variable values if initializerExpression is null
            if (initializerExpression != null || !variableValues.ContainsKey(varName))
            {
                setVar(varName, value);
            }

            // don't return 'value' bc it might be an UnknownValue from empty declaration, but vars may already have its value
            return variableValues[varName];
        }

        void setVar(string varName, object value)
        {
            if (variableValues.TryGetValue(varName, out var existingValue))
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

            // both after previous if/switch or if the variable does not exist
            if (value is IntConstExpr ice2)
                value = ice2.Value; // int

            variableValues[varName] = value;
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

        object EvaluateExpression(ExpressionSyntax expression)
        {
            object result = EvaluateExpression_(expression);
            if (Verbosity > 0)
            {
                if (Verbosity > 1)
                    Console.WriteLine($"[d] {expression} => ({result?.GetType()}) {result}");
                else
                    Console.WriteLine($"[d] {expression} => {result}");
            }
            return result;
        }

        object EvaluateExpression_(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case AssignmentExpressionSyntax assignmentExpr:
                    // Handle assignment expressions (e.g., num3 = (num4 = (uint)(num2 ^ 0x76ED016F)))
                    var left = assignmentExpr.Left;
                    var right = assignmentExpr.Right;

                    if (!(left is IdentifierNameSyntax))
                        throw new NotSupportedException($"Assignment to '{left.Kind()}' is not supported");

                    string varName = left.ToString(); // XXX arrays?
                    VarsWritten.Add(varName);

                    // Evaluate the right-hand side expression
                    try
                    {
                        var rValue = EvaluateExpression(right);
                        switch (assignmentExpr.Kind())
                        {
                            case SyntaxKind.SimpleAssignmentExpression:
                                setVar(varName, rValue);
                                break;

                            default:
                                // +=, -=, etc
                                SyntaxKind binaryOperatorKind = assignmentExpr.Kind() switch
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
                                setVar(varName, EvaluateBinaryExpression(BinaryExpression(binaryOperatorKind, left, right)));
                                break;
                        }
                        return variableValues[varName];
                    }
                    catch (Exception)
                    {
                        Type? type = null;
                        if (variableValues.ContainsKey(varName))
                            type = variableValues[varName]?.GetType();
                        variableValues[varName] = UnknownValue.Create(type);
                        throw;
                    }

                case BinaryExpressionSyntax binaryExpr:
                    return EvaluateBinaryExpression(binaryExpr);

                case CastExpressionSyntax castExpr:               // (uint)num2
                    var value = EvaluateExpression(castExpr.Expression);
                    return cast_var(value, castExpr.Type.ToString());

                case ConditionalExpressionSyntax conditionalExpr: // ternary operator: num3 == 0 ? num4 : num5
                    var condition = EvaluateExpression(conditionalExpr.Condition);
                    if (Convert.ToBoolean(condition))
                        return EvaluateExpression(conditionalExpr.WhenTrue);
                    else
                        return EvaluateExpression(conditionalExpr.WhenFalse);

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
                        "default(nint)" => default(nint),   // TODO: 32/64 bit cmdline switch
                        "default(nuint)" => default(nuint), // TODO: 32/64 bit cmdline switch
                        "default(object)" => default(object),
                        "default(sbyte)" => default(sbyte),
                        "default(string)" => default(string),
                        "default(uint)" => default(uint),
                        "default(ulong)" => default(ulong),
                        "default(ushort)" => default(ushort),
                        _ => throw new NotSupportedException($"Default expression '{defaultExpr.ToString()}' is not supported.")
                    };

                case IdentifierNameSyntax identifierName:
                    // If the expression is an identifier, fetch its value from the dictionary
                    string varName2 = identifierName.Identifier.Text;
                    VarsRead.Add(varName2);
                    if (variableValues.ContainsKey(varName2))
                        return variableValues[varName2];
                    else
                        return UnknownValue.Create();

                case LiteralExpressionSyntax literal:
                    if (literal.Token.Value is int i)
                        return new IntConstExpr(i);
                    return literal.Token.Value;

                case MemberAccessExpressionSyntax:
                    if (Constants.ContainsKey(expression.ToString()))
                        return Constants[expression.ToString()];
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
                if (expr.Operand is IdentifierNameSyntax id)
                {
                    string varName = id.Identifier.Text;
                    VarsWritten.Add(varName);
                    VarsRead.Add(varName);
                    variableValues[varName] = retValue;
                }
                else
                {
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
                string varName = id.Identifier.Text;
                VarsWritten.Add(varName);
                VarsRead.Add(varName);
                variableValues[varName] = newValue;
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

        object promoted_eval(dynamic l, SyntaxKind kind, dynamic r)
        {
            var iceL = l as IntConstExpr;
            var iceR = r as IntConstExpr;
            var ltype = (iceL != null) ? iceL.IntType.Type : l.GetType();
            var rtype = (iceR != null) ? iceR.IntType.Type : r.GetType();
            // [floats skipped]
            // if either operand is of type ulong, the OTHER OPERAND is converted to type ulong,
            // or a binding-time error occurs if the other operand is of type sbyte, short, int, or long.
            if (ltype == typeof(ulong) || rtype == typeof(ulong))
            {
                if (ltype != typeof(ulong))
                    l = (iceL != null) ? iceL.Cast(TypeDB.ULong) : Convert.ToUInt64(l);
                if (rtype != typeof(ulong))
                    r = (iceR != null) ? iceR.Cast(TypeDB.ULong) : Convert.ToUInt64(r);
            }

            // Otherwise, if either operand is of type long, the OTHER OPERAND is converted to type long.
            else if (ltype == typeof(long) || rtype == typeof(long))
            {
                if (ltype != typeof(long))
                    l = (iceL != null) ? iceL.Cast(TypeDB.Long) : Convert.ToInt64(l);
                if (rtype != typeof(long))
                    r = (iceR != null) ? iceR.Cast(TypeDB.Long) : Convert.ToInt64(r);
            }

            // Otherwise, if either operand is of type uint and the other operand is of type sbyte, short, or int, BOTH OPERANDS are converted to type long.
            // XXX not always true XXX
            else if (
                    (ltype == typeof(uint) && (rtype == typeof(sbyte) || rtype == typeof(short) || rtype == typeof(int))) ||
                    (rtype == typeof(uint) && (ltype == typeof(sbyte) || ltype == typeof(short) || ltype == typeof(int)))
                    )
            {
                // if left is uint and right is int, but can fit in uint => right is converted to uint
                if (ltype == typeof(uint) && rtype == typeof(int) && iceR != null && iceR.Value >= 0)
                {
                    r = iceR.Cast(TypeDB.UInt);
                }
                else if (rtype == typeof(uint) && ltype == typeof(int) && iceL != null && iceL.Value >= 0)
                {
                    l = iceL.Cast(TypeDB.UInt);
                }
                else
                {
                    l = (iceL != null) ? iceL.Cast(TypeDB.Long) : Convert.ToInt64(l);
                    r = (iceR != null) ? iceR.Cast(TypeDB.Long) : Convert.ToInt64(r);
                }
            }

            // Otherwise, if either operand is of type uint, the OTHER OPERAND is converted to type uint.
            else if (ltype == typeof(uint) || rtype == typeof(uint))
            {
                if (ltype != typeof(uint))
                    l = (iceL != null) ? iceL.Cast(TypeDB.UInt) : Convert.ToUInt32(l);
                if (rtype != typeof(uint))
                    r = (iceR != null) ? iceR.Cast(TypeDB.UInt) : Convert.ToUInt32(r);
            }
            else
            {
                // Otherwise, BOTH OPERANDS are converted to type int.
                l = (iceL != null) ? iceL.Cast(TypeDB.Int) : Convert.ToInt32(l);
                r = (iceR != null) ? iceR.Cast(TypeDB.Int) : Convert.ToInt32(r);
            }

            if (l is IntConstExpr iceL2)
                l = iceL2.TypedValue();

            if (r is IntConstExpr iceR2)
                r = iceR2.TypedValue();

            return eval_binary(l, kind, r);
        }

        object eval_binary<TL, TR>(TL l, SyntaxKind kind, TR r)
            where TL : INumber<TL>, IBitwiseOperators<TL, TL, TL>
            where TR : INumber<TR>, IBitwiseOperators<TR, TR, TR>
        {
            if (Verbosity > 2)
                Console.WriteLine($"[d] eval_binary: ({l.GetType()}) #{l} {kind} ({r.GetType()}) {r}");

            // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12473-binary-numeric-promotions
            if ((l is IntConstExpr && r is not IntConstExpr) || (r is IntConstExpr))
            {
                return promoted_eval(l, kind, r);
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

            var lValue = EvaluateExpression(binaryExpr.Left); // always evaluated
                                                              // Console.WriteLine($"[d] {binaryExpr.Left} => {lValue}");

            // handle logic expressions bc rValue might not need to be evaluated
            if (op == "&&" || op == "||")
            {
                if (lValue is UnknownValueBase luv0)
                {
                    var lb = luv0.Cast(TypeDB.Bool);
                    if (lb is bool)
                    {
                        lValue = lb;
                    }
                    else
                    {
                        lValue = ("op" == "&&"); // tri-state logic
                    }
                }
            }

            switch (op)
            {
                case "&&":
                    return Convert.ToBoolean(lValue) ? EvaluateExpression(binaryExpr.Right) : false;

                case "||":
                    return Convert.ToBoolean(lValue) ? true : EvaluateExpression(binaryExpr.Right);
            }

            // evaluate rValue, handle everything else
            var rValue = EvaluateExpression(binaryExpr.Right); // NOT always evaluated
                                                               // Console.WriteLine($"[d] {binaryExpr.Right} => {rValue}");

            if (lValue is UnknownValueBase luv)
                return luv.Op(op, rValue);

            if (rValue is UnknownValueBase ruv)
                return ruv.InverseOp(op, lValue);

            return eval_binary((dynamic)lValue, binaryExpr.Kind(), (dynamic)rValue);
        }
    }
}
