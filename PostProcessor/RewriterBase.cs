using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public abstract class RewriterBase : CSharpSyntaxRewriter
{
    protected readonly VarDB _varDB;

    public RewriterBase(VarDB varDB)
    {
        _varDB = varDB;
    }

    protected bool? eval_constexpr(ExpressionSyntax expr)
    {
        VarProcessor processor = new(_varDB);
        var result = processor.EvaluateExpression(expr);
        return result switch
        {
            bool b => b,
            UnknownValueBase unk => unk.Cast(TypeDB.Bool) switch
            {
                bool b => b,
                _ => null // can't evaluate
            },
            _ => null // can't evaluate
        };
    }
}
