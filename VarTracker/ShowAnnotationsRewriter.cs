using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    public class ShowAnnotationsRewriter : CSharpSyntaxRewriter
    {
        T add_comment<T>(T node, bool append = false) where T : SyntaxNode
        {
            if (node != null)
            {
                string? ann_str = node.NestedAnnotationsAsString();
                if (ann_str != null)
                {
                    if (ann_str.Length > 100)
                        ann_str = ann_str.Substring(0, 100) + "…";

                    if (append)
                    {
                        string cmt = node.GetTrailingTrivia().ToString();
                        cmt = string.IsNullOrEmpty(cmt) ? "" : cmt.Trim().Replace("// ", "");
                        return node.WithComment($"{cmt} // {ann_str}");
                    }
                    else
                    {
                        return node.WithComment(ann_str);
                    }
                }
            }
            return node;
        }

        public override SyntaxNode VisitEmptyStatement(EmptyStatementSyntax node)
        {
            return add_comment(base.VisitEmptyStatement(node), true);
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return add_comment(base.VisitExpressionStatement(node));
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            return add_comment(base.VisitLocalDeclarationStatement(node));
        }
    }
}
