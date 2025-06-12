using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class VarTracker
{
    class VarCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel _model;
        private readonly Dictionary<ISymbol, SyntaxAnnotation> _dict;

        HashSet<string> _varNames = new HashSet<string>();

        public VarCollector(SemanticModel model, Dictionary<ISymbol, SyntaxAnnotation> dict)
        {
            _model = model;
            _dict = dict;
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            if (symbol != null && !_dict.ContainsKey(symbol))
            {
                string varName = symbol.Name;
                if (varName.Length > 20)
                {
                    varName = varName.Substring(0, 4); // Truncate long names
                }
                int suffix = 2;
                while (_varNames.Contains(varName))
                {
                    varName = $"{symbol.Name}_{suffix++}";
                }
                _dict[symbol] = new SyntaxAnnotation("VAR", varName);
            }
        }
    }
}
