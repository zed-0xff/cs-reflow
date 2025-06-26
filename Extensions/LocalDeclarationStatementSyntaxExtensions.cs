using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public static class LocalDeclarationStatementSyntaxExtensions
{
    public static bool IsSameVar(this LocalDeclarationStatementSyntax node, LocalDeclarationStatementSyntax otherNode)
    {
        if (node.IsSameStmt(otherNode))
            return true;

        if (node.Declaration.Variables.Count != 1 || otherNode.Declaration.Variables.Count != 1)
            return false;

        return node.Declaration.Variables[0].Identifier.IsSameVar(otherNode.Declaration.Variables[0].Identifier);
    }

    public static bool IsSameVar(this LocalDeclarationStatementSyntax node, SyntaxAnnotation otherAnn)
    {
        if (node.Declaration.Variables.Count != 1)
            return false;

        return node.Declaration.Variables[0].Identifier.IsSameVar(otherAnn);
    }

    public static bool IsSameVar(this LocalDeclarationStatementSyntax node, IdentifierNameSyntax otherNode)
    {
        if (node.Declaration.Variables.Count != 1)
            return false;

        return node.Declaration.Variables[0].Identifier.IsSameVar(otherNode.Identifier);
    }

    public static bool IsSameVar(this LocalDeclarationStatementSyntax node, Variable V)
    {
        if (node.Declaration.Variables.Count != 1)
            return false;

        return node.Declaration.Variables[0].Identifier.IsSameVar(V);
    }
}

