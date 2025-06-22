using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public static class IdentifierNameSyntaxExtensions
{
    public static SyntaxAnnotation VarID(this IdentifierNameSyntax node) => node.Identifier.VarID();

    public static bool IsSameVar(this IdentifierNameSyntax node, IdentifierNameSyntax otherNode)
    {
        if (node.IsSameStmt(otherNode))
            return true;

        return node.Identifier.IsSameVar(otherNode.Identifier);
    }

    public static bool IsSameVar(this IdentifierNameSyntax node, SyntaxAnnotation otherAnn)
    {
        return node.Identifier.IsSameVar(otherAnn);
    }
}

