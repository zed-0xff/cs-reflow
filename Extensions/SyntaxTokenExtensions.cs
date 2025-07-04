using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public static class SyntaxTokenExtensions
{
    public static SyntaxAnnotation? VarID(this SyntaxToken token) => token.GetAnnotations("VarID").FirstOrDefault();

    public static bool IsSameVar(this SyntaxToken token, SyntaxToken other)
    {
        var thisVarID = token.VarID();
        var otherVarID = other.VarID();
        return thisVarID is not null && otherVarID is not null && thisVarID.Data == otherVarID.Data;
    }

    public static bool IsSameVar(this SyntaxToken token, SyntaxAnnotation otherVarID)
    {
        var thisVarID = token.VarID();
        return thisVarID is not null && otherVarID is not null && thisVarID.Data == otherVarID.Data;
    }

    public static bool IsSameVar(this SyntaxToken token, Variable V)
    {
        var thisVarID = token.VarID();
        return thisVarID is not null && thisVarID.Data == V.VarID;
    }
}

