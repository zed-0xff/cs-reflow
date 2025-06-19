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
            ISymbol? symbol = null;
            if (node.SyntaxTree == _semanticModel.SyntaxTree)
                symbol = _semanticModel.GetDeclaredSymbol(node);

            node = base.VisitVariableDeclarator(node) as VariableDeclaratorSyntax;

            if (symbol == null && node.SyntaxTree == _semanticModel.SyntaxTree)
                symbol = _semanticModel.GetDeclaredSymbol(node);

            if (symbol != null && _sym2ann.TryGetValue(symbol, out var annotation))
                node = node.WithAdditionalAnnotations(annotation);
            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            node = base.VisitIdentifierName(node) as IdentifierNameSyntax;
            if (node.SyntaxTree == _semanticModel.SyntaxTree)
            {
                var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
                if (symbol != null && _sym2ann.TryGetValue(symbol, out var annotation))
                    node = node.WithAdditionalAnnotations(annotation);
            }
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
