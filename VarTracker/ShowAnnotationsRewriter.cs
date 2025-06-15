using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    public class ShowAnnotationsRewriter : CSharpSyntaxRewriter
    {
        T add_comment<T>(T node) where T : SyntaxNode
        {
            if (node != null)
            {
                string? ann_str = node.NestedAnnotationsAsString();
                if (ann_str != null && ann_str.Length < 100)
                    node = node.WithComment(ann_str);
            }
            return node;
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return base.VisitExpressionStatement(add_comment(node));
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            return base.VisitLocalDeclarationStatement(add_comment(node));
        }
    }
}
