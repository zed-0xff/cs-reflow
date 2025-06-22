using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SymbolAnnotationMap = System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxAnnotation>;

public partial class VarTracker
{
    class AnnotationRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;
        private readonly VarTracker _tracker;
        private readonly SymbolAnnotationMap _sym2ann;

        public AnnotationRewriter(VarTracker tracker, SemanticModel semanticModel, SymbolAnnotationMap sym2ann)
        {
            _tracker = tracker;
            _semanticModel = semanticModel;
            _sym2ann = sym2ann;
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            // capture annotation first bc base.VisitVariableDeclarator may return rewritten node, so semantic model would not match
            SyntaxAnnotation? annotation = null;
            if (node.SyntaxTree == _semanticModel.SyntaxTree)
                _sym2ann.TryGetValue(_semanticModel.GetDeclaredSymbol(node), out annotation);

            node = base.VisitVariableDeclarator(node) as VariableDeclaratorSyntax;
            if (node == null)
                return node;

            if (annotation != null)
                node = node.WithIdentifier(
                        node.Identifier.WithAdditionalAnnotations(annotation)
                        );
            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            node = base.VisitIdentifierName(node) as IdentifierNameSyntax;
            if (node == null || node.SyntaxTree != _semanticModel.SyntaxTree)
                return node;

            var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null && _sym2ann.TryGetValue(symbol, out var annotation))
                node = node.WithIdentifier(
                        node.Identifier.WithAdditionalAnnotations(annotation)
                        );
            return node;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            node = base.Visit(node);
            if (node is StatementSyntax)
            {
                node = node.WithAdditionalAnnotations(
                        new SyntaxAnnotation("StmtID", _tracker.NextStmtID())
                        );
            }
            return node;
        }
    }
}
