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
        int _id = 0;

        public AnnotationRewriter(Dictionary<ISymbol, SyntaxAnnotation> varAnnotations, SemanticModel semanticModel)
        {
            _varAnnotations = varAnnotations;
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
            switch (node)
            {
                case DoStatementSyntax:
                case ForEachStatementSyntax:
                case ForStatementSyntax:
                case IfStatementSyntax:
                case LocalDeclarationStatementSyntax:
                case SwitchStatementSyntax:
                case TryStatementSyntax:
                case UsingStatementSyntax:
                case WhileStatementSyntax:
                    node = node.WithAdditionalAnnotations(
                            new SyntaxAnnotation("ID", $"{_id++}")
                            );
                    break;
            }
            return node;
        }
    }
}
