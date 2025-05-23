using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class VariableProcessor : ICloneable
{
    public VarDict VariableValues { get; private set; } = new();
    public static VarDict Constants { get; private set; } = new();
    public int Verbosity = 0;

    static VariableProcessor()
    {
        Constants["string.Empty"] = string.Empty;
        Constants["int.MinValue"] = int.MinValue;
        Constants["int.MaxValue"] = int.MaxValue;
        Constants["uint.MinValue"] = uint.MinValue;
        Constants["uint.MaxValue"] = uint.MaxValue;

        Constants["Png_a7cb.BZh"] = 0x00685a42;
        Constants["Png_e0d5.IDAT"] = 0x54414449;
        Constants["Png_e0d5.IEND"] = 0x444e4549;
        Constants["Png_e0d5.IHDR"] = 0x52444849;
        Constants["Png_e0d5.PLTE"] = 0x45544c50;
        Constants["Png_e0d5.QRR"] = 0x00525251;
        Constants["Png_e0d5.tRNS"] = 0x534e5274;

        Constants["Structs_a7cb.BZh"] = 0x00685a42;
        Constants["Structs_e0d5.IDAT"] = 0x54414449;
        Constants["Structs_e0d5.IEND"] = 0x444e4549;
        Constants["Structs_e0d5.IHDR"] = 0x52444849;
        Constants["Structs_e0d5.PLTE"] = 0x45544c50;
        Constants["Structs_e0d5.QRR"] = 0x00525251;
        Constants["Structs_e0d5.tRNS"] = 0x534e5274;

        Constants["Type.EmptyTypes.LongLength"] = Type.EmptyTypes.LongLength;
    }

    public object Clone()
    {
        var clonedProcessor = new VariableProcessor();
        clonedProcessor.VariableValues = (VarDict)this.VariableValues.Clone();
        clonedProcessor.Verbosity = this.Verbosity;
        return clonedProcessor;
    }

    public class Expression
    {
        StatementSyntax? stmt = null;
        ExpressionSyntax? expr = null;
        VarDict variableValues;
        public int Verbosity = 0;

        public List<string> VarsWritten { get; } = new();
        public List<string> VarsRead { get; } = new();
        public List<string> VarsReferenced => VarsRead.Union(VarsWritten).ToList();

        public object? Result { get; private set; } = UnknownValue.Create();

        public Expression(StatementSyntax stmt, VarDict variableValues)
        {
            this.stmt = stmt;
            this.variableValues = variableValues;
        }

        public Expression(ExpressionSyntax expr, VarDict variableValues)
        {
            this.expr = expr;
            this.variableValues = variableValues;
        }

        public Expression SetVerbosity(int verbosity)
        {
            Verbosity = verbosity;
            return this;
        }

        public object Evaluate()
        {
            if (expr != null)
            {
                Result = EvaluateExpression(expr);
                return Result;
            }

            switch (stmt)
            {
                case LocalDeclarationStatementSyntax localDeclaration:
                    Result = ProcessLocalDeclaration(localDeclaration);
                    break;
                case ExpressionStatementSyntax expressionStatement:
                    Result = EvaluateExpression(expressionStatement.Expression);
                    break;
                default:
                    throw new NotSupportedException($"Syntax node type '{stmt?.GetType()}' is not supported.");
            }
            return Result;
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

            if (value is UnknownValue && localDeclaration.Declaration.Type is not null)
            {
                // if the variable type is known, use it
                value = UnknownValue.Create(localDeclaration.Declaration.Type);
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
            if (value is UnknownValue && variableValues.TryGetValue(varName, out var existingValue))
            {
                // if the variable already exists, use its type
                value = existingValue switch
                {
                    UnknownValue => value, // no luck
                    UnknownTypedValue utv => UnknownValue.Create(utv.type),
                    _ => UnknownValue.Create(existingValue?.GetType()) // handles null values as well
                };
            }

            variableValues[varName] = value;
        }

        object cast_var(object value, string toType)
        {
            if (value is UnknownValueBase uv)
                return uv.Cast(toType);

            switch (toType)
            {
                case "uint":
                case "nuint": // TODO: 32/64 bit cmdline switch
                    switch (value)
                    {
                        case int i:
                            return unchecked((uint)i);
                        case long l:
                            return unchecked((uint)l);
                        case nint ni:
                            return unchecked((uint)ni);
                        case nuint nu:
                            return unchecked((uint)nu);
                        case uint u:
                            return u;
                        default:
                            throw new NotSupportedException($"Cast from '{value?.GetType()}' to '{toType}' is not supported.");
                    }
                case "int":
                case "nint": // TODO: 32/64 bit cmdline switch
                    switch (value)
                    {
                        case int i:
                            return i;
                        case long l:
                            return unchecked((int)l);
                        case nint ni:
                            return unchecked((int)ni);
                        case nuint nu:
                            return unchecked((int)nu);
                        case uint u:
                            return unchecked((int)u);
                        default:
                            throw new NotSupportedException($"Cast from '{value?.GetType()}' to '{toType}' is not supported.");
                    }

                case "string":
                    if (value is string)
                        return value;
                    else
                        throw new NotSupportedException($"Cast from '{value?.GetType()}' to '{toType}' is not supported.");
                    break;

                default:
                    throw new NotSupportedException($"Cast from '{value?.GetType()}' to '{toType}' is not supported.");
            }
        }

        object EvaluateExpression(ExpressionSyntax expression)
        {
            object result = EvaluateExpression_(expression);
            if (Verbosity > 0)
                Console.WriteLine($"[d] {expression} => {result}");
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
                                variableValues[varName] = EvaluateBinaryExpression(BinaryExpression(binaryOperatorKind, left, right));
                                break;
                        }
                        return variableValues[varName];
                    }
                    catch (Exception e)
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
                    return ConvertLiteral(literal);

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
                    throw new NotSupportedException($"{expression.GetType().ToString().Replace("Microsoft.CodeAnalysis.CSharp.Syntax.", "")} is not supported.");
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
            if (
                    expr.Operand is not IdentifierNameSyntax &&
                    (expr.Kind() == SyntaxKind.PreDecrementExpression || expr.Kind() == SyntaxKind.PreIncrementExpression)
               )
            {
                throw new NotSupportedException($"Prefix operator '{expr.Kind()}' is not supported for {expr.Operand.Kind()}.");
            }

            var value = EvaluateExpression(expr.Operand);
            var retValue = eval_prefix(value, expr.Kind());

            if (expr.Operand is IdentifierNameSyntax id)
            {
                string varName = id.Identifier.Text;
                VarsWritten.Add(varName);
                VarsRead.Add(varName);
                variableValues[varName] = retValue;
            }
            return retValue;
        }

        // x--
        // x++
        object EvaluatePostfixExpression(PostfixUnaryExpressionSyntax expr)
        {
            var retValue = EvaluateExpression(expr.Operand);
            object newValue = retValue switch
            {
                UnknownValueBase u => expr.Kind() switch
                {
                    SyntaxKind.PostIncrementExpression => u.Add(1),
                    SyntaxKind.PostDecrementExpression => u.Sub(1),
                    _ => throw new NotSupportedException($"Postfix operator '{expr.Kind()}' is not supported.")
                },
                _ => expr.Kind() switch
                {
                    SyntaxKind.PostIncrementExpression => Convert.ToInt64(retValue) + 1,
                    SyntaxKind.PostDecrementExpression => Convert.ToInt64(retValue) - 1,
                    _ => throw new NotSupportedException($"Postfix operator '{expr.Kind()}' is not supported.")
                }
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
                    var lb = luv0.Cast("bool");
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

            long ll = Convert.ToInt64(lValue);
            long lr = Convert.ToInt64(rValue);

            uint l = unchecked((uint)ll);
            uint r = unchecked((uint)lr);

            long result = op switch
            {
                "+" => l + r,
                "-" => l - r,
                "*" => l * r,
                "/" => r != 0 ? l / r : throw new DivideByZeroException(),
                "%" => r != 0 ? l % r : throw new DivideByZeroException(),
                "&" => l & r,
                "|" => l | r,
                "^" => l ^ r,

                "<<" => l << (int)r,
                ">>" => l >> (int)r,
                ">>>" => l >> (int)r,

                "==" => l == r ? 1 : 0,
                "!=" => l != r ? 1 : 0,
                ">=" => l >= r ? 1 : 0,
                "<=" => l <= r ? 1 : 0,
                ">" => l > r ? 1 : 0,
                "<" => l < r ? 1 : 0,

                _ => throw new InvalidOperationException($"Unsupported operator '{op}' in '{binaryExpr}'")
            };
            return unchecked((uint)result);
        }

        private object ConvertLiteral(LiteralExpressionSyntax literal)
        {
            return literal.Token.Value;
        }
    }

    public object EvaluateExpression(ExpressionSyntax expression)
    {
        return new Expression(expression, VariableValues)
            .SetVerbosity(Verbosity)
            .Evaluate();
    }

    public object ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration)
    {
        return new Expression(localDeclaration, VariableValues)
            .SetVerbosity(Verbosity)
            .Evaluate();
    }

    public Expression EvaluateExpressionEx(StatementSyntax expression)
    {
        var e = new Expression(expression, VariableValues);
        e.SetVerbosity(Verbosity);
        e.Evaluate();
        return e;
    }

    public Expression EvaluateExpressionEx(ExpressionSyntax expression)
    {
        var e = new Expression(expression, VariableValues);
        e.SetVerbosity(Verbosity);
        e.Evaluate();
        return e;
    }
}
