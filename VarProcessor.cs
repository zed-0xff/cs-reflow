using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

public partial class VarProcessor : ICloneable
{
    public VarDict VariableValues { get; private set; } = new();
    public static VarDict Constants { get; private set; } = new();
    public int Verbosity = 3;

    public VarProcessor(int verbosity = 0)
    {
        Verbosity = verbosity;
    }

    static VarProcessor()
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
        var clonedProcessor = new VarProcessor();
        clonedProcessor.VariableValues = (VarDict)this.VariableValues.Clone();
        clonedProcessor.Verbosity = this.Verbosity;
        return clonedProcessor;
    }

    public object EvaluateExpression(CSharpSyntaxNode node)
    {
        return new Expression(node, VariableValues)
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

    public bool HasVar(LocalDeclarationStatementSyntax decl)
    {
        foreach (var v in decl.Declaration.Variables)
        {
            // TODO: check type
            if (!VariableValues.ContainsKey(v.Identifier.ValueText))
                return false;
        }
        return true;
    }

    public void SetVarTypes(LocalDeclarationStatementSyntax decl)
    {
        foreach (var v in decl.Declaration.Variables)
        {
            if (VariableValues.ContainsKey(v.Identifier.ValueText))
            {
                // TODO: check type
            }
            else
            {
                VariableValues[v.Identifier.ValueText] = UnknownValue.Create(decl.Declaration.Type);
            }
        }
    }
}
