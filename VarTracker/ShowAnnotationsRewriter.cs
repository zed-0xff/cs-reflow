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
            bool append = node is EmptyStatementSyntax;
            string? ann_str;
            switch (node)
            {
                case BlockSyntax:
                    ann_str = node.AnnotationsAsString();
                    break;
                case DoStatementSyntax doStmt:
                    ann_str = doStmt.Condition.NestedAnnotationsAsString(doStmt);
                    break;
                case IfStatementSyntax ifStmt:
                    ann_str = ifStmt.Condition.NestedAnnotationsAsString(ifStmt);
                    break;
                case WhileStatementSyntax whileStmt:
                    ann_str = whileStmt.Condition.NestedAnnotationsAsString(whileStmt);
                    break;
                default:
                    ann_str = node.NestedAnnotationsAsString();
                    break;
            }

            if (ann_str != null)
            {
                if (ann_str.Length > 100)
                    ann_str = ann_str.Substring(0, 100) + "…";

                if (append)
                {
                    string cmt = node.GetTrailingTrivia().ToString();
                    if (cmt.EndsWith(ann_str))
                        return node;

                    cmt = string.IsNullOrEmpty(cmt) ? "" : cmt.Trim().Replace("// ", "");
                    return node.WithComment($"{cmt} // {ann_str}");
                }
                else
                {
                    return node.WithComment(ann_str);
                }
            }
            return node;
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            node = base.Visit(node);
            if (node is StatementSyntax)
            {
                return add_comment(base.Visit(node));
            }
            return node;
        }

        // public override SyntaxNode VisitEmptyStatement(EmptyStatementSyntax node)
        // {
        //   return add_comment(base.VisitEmptyStatement(node), true);
        // }
        // 
        // public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        // {
        //   return add_comment(base.VisitExpressionStatement(node));
        // }
        // 
        // public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        // {
        //   return add_comment(base.VisitLocalDeclarationStatement(node));
        // }
    }
}
