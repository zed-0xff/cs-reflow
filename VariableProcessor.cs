using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class VariableProcessor : ICloneable
{
    public class VarNotFoundException : Exception
    {
        public VarNotFoundException(string varName) : base($"Variable '{varName}' not found.") { }
    }

    public VarDict VariableValues { get; private set; } = new();
    public static VarDict Constants { get; private set; } = new();

    static VariableProcessor()
    {
        Constants["string.Empty"] = string.Empty;
        Constants["Structs_e0d5.IDAT"] = 0x54414449;
        Constants["Structs_e0d5.IEND"] = 0x444e4549;
        Constants["Structs_e0d5.IHDR"] = 0x52444849;
        Constants["Structs_e0d5.PLTE"] = 0x45544c50;
        Constants["Structs_e0d5.QRR"] = 0x00525251;
        Constants["Structs_e0d5.tRNS"] = 0x534e5274;
    }

    public object Clone()
    {
        var clonedProcessor = new VariableProcessor();
        clonedProcessor.VariableValues = (VarDict)this.VariableValues.Clone();
        return clonedProcessor;
    }

    public class Expression
    {
        StatementSyntax? stmt = null;
        ExpressionSyntax? expr = null;
        VarDict variableValues;

        public List<string> VarsWritten { get; } = new();
        public List<string> VarsRead { get; } = new();
        public List<string> VarsReferenced => VarsRead.Union(VarsWritten).ToList();

        public object? Result { get; private set; } = null;

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

            if (initializerExpression != null)
            {
                // Evaluate the expression to determine its value
                var value = EvaluateExpression(initializerExpression);

                // Store the value of the variable
                variableValues[varName] = value; //, localDeclaration.Declaration.Type.ToString());
                return value;
            }
            return null;
        }

        object cast_var(object value, string toType)
        {
            switch (toType)
            {
                case "uint":
                    switch (value)
                    {
                        case int i:
                            return unchecked((uint)i);
                        case long l:
                            return unchecked((uint)l);
                        case uint u:
                            return u;
                        default:
                            throw new NotSupportedException($"Cast from'{value.GetType()}' to '{toType}' is not supported.");
                    }
                case "int":
                    switch (value)
                    {
                        case int i:
                            return i;
                        case long l:
                            return unchecked((int)l);
                        case uint u:
                            return unchecked((int)u);
                        default:
                            throw new NotSupportedException($"Cast from'{value.GetType()}' to '{toType}' is not supported.");
                    }
                default:
                    throw new NotSupportedException($"Cast from to '{toType}' is not supported.");
            }
        }

        object EvaluateExpression(ExpressionSyntax expression)
        {
            // You can add support for more complex expressions here, 
            // for now, we will handle simple assignments and basic operations.

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
                        var rightValue = EvaluateExpression(right);
                        switch (assignmentExpr.Kind())
                        {
                            case SyntaxKind.SimpleAssignmentExpression:
                                variableValues[varName] = rightValue;
                                break;

                            default:
                                // convert to BinaryExpressionSyntax
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
                        variableValues.Remove(varName);
                        throw;
                    }

                case BinaryExpressionSyntax binaryExpr:
                    return EvaluateBinaryExpression(binaryExpr);

                case CastExpressionSyntax castExpr:               // (uint)num2
                    var value = EvaluateExpression(castExpr.Expression);
                    return cast_var(value, castExpr.Type.ToString());

                case ConditionalExpressionSyntax conditionalExpr: // num3 == 0 ? num4 : num5
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
                        "default(double)" => default(double),
                        "default(int)" => default(int),
                        "default(object)" => default(object),
                        "default(string)" => default(string),
                        "default(uint)" => default(uint),
                        _ => throw new NotSupportedException($"Default expression '{defaultExpr.ToString()}' is not supported.")
                    };

                case IdentifierNameSyntax identifierName:
                    // If the expression is an identifier, fetch its value from the dictionary
                    string varName2 = identifierName.Identifier.Text;
                    VarsRead.Add(varName2);
                    if (variableValues.ContainsKey(varName2))
                        return variableValues[varName2];
                    else
                        throw new VarNotFoundException(varName2);

                case LiteralExpressionSyntax literal:
                    return ConvertLiteral(literal);

                case MemberAccessExpressionSyntax:
                    if (Constants.ContainsKey(expression.ToString()))
                        return Constants[expression.ToString()];
                    else
                        goto default;

                case ParenthesizedExpressionSyntax parenExpr:
                    return EvaluateExpression(parenExpr.Expression);

                case PrefixUnaryExpressionSyntax unaryExpr:
                    // Handle unary expressions (e.g., -num3)
                    var operandPrefix = unaryExpr.Operand;
                    var valuePrefix = EvaluateExpression(operandPrefix);
                    return unaryExpr.Kind() switch
                    {
                        SyntaxKind.UnaryPlusExpression => valuePrefix,
                        SyntaxKind.UnaryMinusExpression => -Convert.ToInt64(valuePrefix),
                        SyntaxKind.BitwiseNotExpression => ~Convert.ToInt64(valuePrefix),
                        _ => throw new NotSupportedException($"Unary operator '{unaryExpr.Kind()}' is not supported.")
                    };

                case PostfixUnaryExpressionSyntax postfixUnaryExpr:
                    // Handle postfix unary expressions (e.g., num3++)
                    var operandPostfix = postfixUnaryExpr.Operand;
                    var valuePostfix = EvaluateExpression(operandPostfix);
                    var newValue = postfixUnaryExpr.Kind() switch
                    {
                        SyntaxKind.PostIncrementExpression => Convert.ToInt64(valuePostfix) + 1,
                        SyntaxKind.PostDecrementExpression => Convert.ToInt64(valuePostfix) - 1,
                        _ => throw new NotSupportedException($"Postfix operator '{postfixUnaryExpr.Kind()}' is not supported.")
                    };
                    if (operandPostfix is IdentifierNameSyntax identifierPostfix)
                    {
                        string varNamePostfix = identifierPostfix.Identifier.Text;
                        VarsWritten.Add(varNamePostfix);
                        VarsRead.Add(varNamePostfix);
                        variableValues[varNamePostfix] = newValue;
                    }
                    else
                    {
                        throw new NotSupportedException($"Postfix operand '{operandPostfix.Kind()}' is not supported.");
                    }
                    return newValue;

                default:
                    throw new NotSupportedException($"{expression.GetType().ToString().Replace("Microsoft.CodeAnalysis.CSharp.Syntax.", "")} is not supported.");
            }
        }

        private object EvaluateBinaryExpression(BinaryExpressionSyntax binaryExpr)
        {
            // Recursively evaluate the left and right parts of the binary expression
            var leftValue = EvaluateExpression(binaryExpr.Left);
            var rightValue = EvaluateExpression(binaryExpr.Right);

            long ll = Convert.ToInt64(leftValue);
            long lr = Convert.ToInt64(rightValue);

            uint l = unchecked((uint)ll);
            uint r = unchecked((uint)lr);

            string op = binaryExpr.OperatorToken.Text;
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

                "||" => ((l != 0) || (r != 0)) ? 1 : 0,
                "&&" => ((l != 0) && (r != 0)) ? 1 : 0,

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
            switch (literal.Kind())
            {
                case SyntaxKind.NumericLiteralToken:
                    return Convert.ToUInt32(literal.Token.Value);
                case SyntaxKind.NumericLiteralExpression:
                    return Convert.ToUInt32(literal.Token.ValueText);
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.StringLiteralToken:
                    return literal.Token.ValueText;
                case SyntaxKind.TrueLiteralExpression:
                    return true;
                case SyntaxKind.FalseLiteralExpression:
                    return false;
                default:
                    throw new NotSupportedException($"Literal type '{literal.Kind()}' is not supported.");
            }
        }
    }

    public object EvaluateExpression(ExpressionSyntax expression)
    {
        return new Expression(expression, VariableValues).Evaluate();
    }

    public object ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration)
    {
        return new Expression(localDeclaration, VariableValues).Evaluate();
    }

    public Expression EvaluateExpressionEx(StatementSyntax expression)
    {
        var e = new Expression(expression, VariableValues);
        e.Evaluate();
        return e;
    }

    public Expression EvaluateExpressionEx(ExpressionSyntax expression)
    {
        var e = new Expression(expression, VariableValues);
        e.Evaluate();
        return e;
    }
}
