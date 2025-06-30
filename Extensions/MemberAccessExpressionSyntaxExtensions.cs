using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

public static class MemberAccessExpressionSyntaxExtensions
{
    public static bool IsKnownConstant(this MemberAccessExpressionSyntax node)
    {
        while (node.Parent is MemberAccessExpressionSyntax parent)
            node = parent;

        return VarProcessor.Constants.ContainsKey(node.ToString());
    }
}
