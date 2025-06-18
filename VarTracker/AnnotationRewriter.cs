using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    class AnnotationRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<ISymbol, SyntaxAnnotation> _varAnnotations;
        private readonly SemanticModel _semanticModel;
        private readonly VarTracker _tracker;

        public AnnotationRewriter(VarTracker tracker, SemanticModel semanticModel)
        {
            _tracker = tracker;
            _varAnnotations = tracker._varAnnotations;
            _semanticModel = semanticModel;
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            ISymbol? symbol = null;
            if (node.SyntaxTree == _semanticModel.SyntaxTree)
                symbol = _semanticModel.GetDeclaredSymbol(node);

            node = base.VisitVariableDeclarator(node) as VariableDeclaratorSyntax;

            if (symbol == null && node.SyntaxTree == _semanticModel.SyntaxTree)
                symbol = _semanticModel.GetDeclaredSymbol(node);

            if (symbol != null && _varAnnotations.TryGetValue(symbol, out var annotation))
                node = node.WithAdditionalAnnotations(annotation);
            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            node = base.VisitIdentifierName(node) as IdentifierNameSyntax;
            if (node.SyntaxTree == _semanticModel.SyntaxTree)
            {
                var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
                if (symbol != null && _varAnnotations.TryGetValue(symbol, out var annotation))
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
                        new SyntaxAnnotation("ID", _tracker.GetNextId())
                        );
            }
            return node;
        }
    }
}
