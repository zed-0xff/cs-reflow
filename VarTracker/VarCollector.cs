using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SymbolAnnotationMap = System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.ISymbol, Microsoft.CodeAnalysis.SyntaxAnnotation>;

public partial class VarTracker
{
    class VarCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly VarTracker _tracker;
        private readonly SymbolAnnotationMap _sym2ann;
        private readonly VarDB _varDB;
        private readonly HashSet<IFieldSymbol> _fields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);

        public VarCollector(VarTracker tracker, SemanticModel model, SymbolAnnotationMap sym2ann, VarDB db)
        {
            _tracker = tracker;
            _semanticModel = model;
            _sym2ann = sym2ann;
            _varDB = db;
        }

        public void Process(SyntaxNode rootNode)
        {
            Visit(rootNode);

            // _fields may contain symbols declared in upper scopes
            foreach (var field in _fields)
            {
                if (!_sym2ann.ContainsKey(field))
                {
                    var decls = field.DeclaringSyntaxReferences;
                    if (decls.Length == 0)
                    {
                        if (VarProcessor.Constants.ContainsKey(field.ToString()!))
                        {
                            Logger.debug($"Field {field} is a known constant", "VarCollector.Process");
                            continue;
                        }
                        Logger.warn($"Field {field} has no declaring syntax references", "VarCollector.Process");
                        continue;
                    }
                    if (decls.Length > 1)
                    {
                        Logger.warn($"Field {field.Name} has multiple declaring syntax references, using the first one", "VarCollector.Process");
                    }
                    var declNode = decls[0].GetSyntax() as VariableDeclaratorSyntax;
                    if (declNode == null)
                    {
                        Logger.warn($"Field {field.Name} has a non-variable declarator syntax reference", "VarCollector.Process");
                        continue;
                    }
                    _sym2ann[field] = _varDB.Add(declNode, field.Type.ToString()).Annotation;
                }
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol is IFieldSymbol fieldSymbol)
                _fields.Add(fieldSymbol);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol == null || _sym2ann.ContainsKey(symbol))
                return;

            var typeSymbol = _semanticModel.GetTypeInfo(node).Type;

            switch (symbol)
            {
                case ILocalSymbol localSymbol:
                    typeSymbol = localSymbol.Type;
                    break;

                case IFieldSymbol fieldSymbol:
                    typeSymbol = fieldSymbol.Type;
                    break;

                default:
                    Logger.warn($"Unexpected symbol type {symbol?.GetType()}", "VarCollector.VisitVariableDeclarator");
                    return;
            }

            _sym2ann[symbol] = _varDB.Add(node, typeSymbol.ToString()!).Annotation;
        }
    }
}
