using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

public class VariableProcessor : ICloneable
{
    public class VarNotFoundException : Exception
    {
        public VarNotFoundException(string varName) : base($"Variable '{varName}' not found.") { }
    }

    private VarDict variableValues = new();

    public object Clone()
    {
        // Create a new instance of VariableProcessor
        var clonedProcessor = new VariableProcessor();

        // Deep copy the dictionary
        foreach (var entry in this.variableValues)
        {
            if (entry.Value is ICloneable cloneableValue)
            {
                clonedProcessor.variableValues[entry.Key] = cloneableValue.Clone();
            }
            else
            {
                // If the object is not ICloneable, you might need to handle how to copy it
                clonedProcessor.variableValues[entry.Key] = entry.Value; // Just copy reference as is
            }
        }

        return clonedProcessor;
    }

    public void ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration)
    {
        // Extract the variable declaration
        var declarator = localDeclaration.Declaration?.Variables.FirstOrDefault();
        if (declarator == null) return;

        // Get the variable name (e.g., "num3")
        string variableName = declarator.Identifier.Text;

        // Extract the right-hand side expression (e.g., "(num4 = (uint)(num2 ^ 0x76ED016F))")
        var initializerExpression = declarator.Initializer?.Value;

        if (initializerExpression != null)
        {
            // Evaluate the expression to determine its value
            var value = EvaluateExpression(initializerExpression);

            // Store the value of the variable
            variableValues[variableName] = value;
        }
    }

    public object EvaluateExpression(ExpressionSyntax expression)
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

                // Evaluate the right-hand side expression
                try
                {
                    var rightValue = EvaluateExpression(right);
                    variableValues[varName] = rightValue;
                    return rightValue;
                }
                catch (Exception e)
                {
                    variableValues.Remove(varName);
                    throw;
                }

            case BinaryExpressionSyntax binaryExpr:
                return EvaluateBinaryExpression(binaryExpr);

            case CastExpressionSyntax castExpr:
                // Handle cast expressions (e.g., (uint)(num2 ^ 0x76ED016F))
                return EvaluateExpression(castExpr.Expression);

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
                if (variableValues.ContainsKey(varName2))
                    return variableValues[varName2];
                else
                    throw new VarNotFoundException(varName2);

            case LiteralExpressionSyntax literal:
                return ConvertLiteral(literal);

            case MemberAccessExpressionSyntax:
                if (expression.ToString() == "string.Empty")
                    return string.Empty;
                else
                    goto default;

            case ParenthesizedExpressionSyntax parenExpr:
                return EvaluateExpression(parenExpr.Expression);

            case PrefixUnaryExpressionSyntax unaryExpr:
                // Handle unary expressions (e.g., -num3)
                var operand = unaryExpr.Operand;
                var operandValue = EvaluateExpression(operand);
                return unaryExpr.Kind() switch
                {
                    SyntaxKind.UnaryPlusExpression => operandValue,
                    SyntaxKind.UnaryMinusExpression => -Convert.ToUInt32(operandValue),
                    _ => throw new NotSupportedException($"Unary operator '{unaryExpr.Kind()}' is not supported.")
                };

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
            "<<" => (int)l << (int)r,
            ">>" => (int)l >> (int)r,
            _ => throw new InvalidOperationException($"Unsupported operator '{op}'")
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
            case SyntaxKind.StringLiteralToken:
                return literal.Token.ValueText;
            case SyntaxKind.TrueLiteralExpression:
                return true;
            default:
                throw new NotSupportedException($"Literal type '{literal.Kind()}' is not supported.");
        }
    }

    public VarDict GetVariableValues()
    {
        return variableValues;
    }
}
