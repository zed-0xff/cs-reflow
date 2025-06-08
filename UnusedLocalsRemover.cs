using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class UnusedLocalsRemover : CSharpSyntaxRewriter
{
    SemanticModel _semanticModel;

    public UnusedLocalsRemover(SyntaxNode rootNode)
    {
        var compilation = CSharpCompilation.Create("MyAnalysis")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(rootNode.SyntaxTree);
        _semanticModel = compilation.GetSemanticModel(rootNode.SyntaxTree);
    }

    public static bool IsSafeToRemove(ExpressionSyntax expr)
    {
        return expr switch
        {
            LiteralExpressionSyntax => true,
            DefaultExpressionSyntax d => d.Type is PredefinedTypeSyntax,
            MemberAccessExpressionSyntax m => VariableProcessor.Constants.ContainsKey(m.ToString()),
            _ => false
        };
    }

    class AssignmentCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<ISymbol> _unusedLocals;

        public readonly List<ExpressionStatementSyntax> AssignmentsToRemove = new();
        public readonly List<AssignmentExpressionSyntax> AssignmentsToReplace = new();
        public readonly HashSet<ISymbol> keepLocals = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        public AssignmentCollector(SemanticModel semanticModel, HashSet<ISymbol> unusedLocals)
        {
            _semanticModel = semanticModel;
            _unusedLocals = unusedLocals;
        }

        // assignment inside another statement: "foo(b = 333)" => replace with rvalue
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var symbol = _semanticModel.GetSymbolInfo(node.Left).Symbol;
            if (symbol != null && _unusedLocals.Contains(symbol))
            {
                AssignmentsToReplace.Add(node);
            }
            base.VisitAssignmentExpression(node);
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            // stand-alone assignment: "b = 333;" => delete
            if (node.Expression is AssignmentExpressionSyntax assignment)
            {
                var symbol = _semanticModel.GetSymbolInfo(assignment.Left).Symbol;
                if (symbol != null && _unusedLocals.Contains(symbol))
                {
                    if (IsSafeToRemove(assignment.Right))
                    {
                        AssignmentsToRemove.Add(node);
                        return; // skip further processing of this node
                    }
                    if (assignment.Right is not AssignmentExpressionSyntax)
                    {
                        // If the right side is not a literal, we keep the local variable
                        keepLocals.Add(symbol);
                    }
                }
            }

            base.VisitExpressionStatement(node);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            foreach (var variable in node.Declaration.Variables)
            {
                var symbol = _semanticModel.GetDeclaredSymbol(variable);
                if (symbol == null || !_unusedLocals.Contains(symbol))
                    continue;

                // Check if initializer is present and not a literal
                var init = variable.Initializer?.Value;
                if (init != null && !IsSafeToRemove(init) && init is not AssignmentExpressionSyntax)
                {
                    keepLocals.Add(symbol); // keep it
                }
            }

            base.VisitLocalDeclarationStatement(node);
        }
    }

    class DeclarationRemover : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<ISymbol> _unusedLocals;
        private readonly HashSet<SyntaxNode> _assignmentsToRemove;
        private readonly HashSet<AssignmentExpressionSyntax> _assignmentsToReplace;

        public DeclarationRemover(SemanticModel semanticModel, HashSet<ISymbol> unusedLocals, IEnumerable<SyntaxNode> assignmentsToRemove,
                                   IEnumerable<AssignmentExpressionSyntax> assignmentsToReplace)
        {
            _semanticModel = semanticModel;
            _unusedLocals = unusedLocals;
            _assignmentsToRemove = new HashSet<SyntaxNode>(assignmentsToRemove);
            _assignmentsToReplace = new HashSet<AssignmentExpressionSyntax>(assignmentsToReplace);
        }

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            var toKeep = new List<VariableDeclaratorSyntax>();
            var toConvert = new List<AssignmentExpressionSyntax>();

            foreach (var variable in node.Declaration.Variables)
            {
                var symbol = _semanticModel.GetDeclaredSymbol(variable);
                if (symbol == null || !_unusedLocals.Contains(symbol))
                {
                    toKeep.Add(variable);
                }

                // Check if initializer is present and not a literal
                var init = variable.Initializer?.Value;
                if (init is AssignmentExpressionSyntax)
                {
                    toConvert.Add(init as AssignmentExpressionSyntax);
                }
                else if (init != null && !IsSafeToRemove(init))
                {
                    toKeep.Add(variable); // keep it
                }
                // Else: no initializer, or initializer is literal → safe to remove
            }

            if (toKeep.Count == 0 && toConvert.Count == 1)
            {
                // "int a = b = 111;" => "b = 111;"
                var assignment = toConvert[0];
                return SyntaxFactory.ExpressionStatement(assignment);
            }

            if (toConvert.Count == 0)
            {
                // If all variables are unused: remove the whole statement
                if (toKeep.Count == 0)
                {
                    return null;
                }

                // If only some variables are unused: rewrite declaration
                if (toKeep.Count < node.Declaration.Variables.Count)
                {
                    var newDecl = node.Declaration.WithVariables(SyntaxFactory.SeparatedList(toKeep));
                    return node.WithDeclaration(newDecl);
                }
            }

            return base.VisitLocalDeclarationStatement(node);
        }

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            // Remove collected assignments
            if (_assignmentsToRemove.Contains(node))
                return null;

            return base.VisitExpressionStatement(node);
        }

        public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            // Replace collected assignments with their right-hand side
            if (_assignmentsToReplace.Contains(node))
            {
                return node.Right;
            }

            return base.VisitAssignmentExpression(node);
        }
    }

    public SyntaxNode ProcessTree(SyntaxNode rootNode)
    {
        var dataFlow = _semanticModel.AnalyzeDataFlow(rootNode);
        if (!dataFlow.Succeeded)
            return rootNode;

        // collect variables that are only declared [and written to] but never read
        var unusedLocals = dataFlow.VariablesDeclared
            .Where(v => !dataFlow.ReadInside.Contains(v) && !dataFlow.ReadOutside.Contains(v))
            .ToHashSet<ISymbol>(SymbolEqualityComparer.Default);

        if (unusedLocals.Count == 0)
            return rootNode;

        // First pass: collect assignments to unused locals
        var collector = new AssignmentCollector(_semanticModel, unusedLocals);
        collector.Visit(rootNode);

        // keep only those unused locals that are assigned a literal value
        unusedLocals.ExceptWith(collector.keepLocals);

        if (unusedLocals.Count == 0)
            return rootNode;

        return new DeclarationRemover(_semanticModel, unusedLocals, collector.AssignmentsToRemove, collector.AssignmentsToReplace)
            .Visit(rootNode);
    }
}
