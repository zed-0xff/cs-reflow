using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

class UnusedLocalsRemover : CSharpSyntaxRewriter
{
    class Context
    {
        public SemanticModel Model;
        public HashSet<ISymbol> UnusedLocals;

        public Context(SemanticModel model, HashSet<ISymbol> unusedLocals)
        {
            Model = model;
            UnusedLocals = unusedLocals;
        }

        public bool IsSafeToRemove(ExpressionSyntax node)
        {
            if (ContainsCalls(node))
                return false;

            var dataFlow = Model.AnalyzeDataFlow(node);
            if (!dataFlow.Succeeded)
                return false;

            if (dataFlow.WrittenInside.Any(w => !UnusedLocals.Contains(w)))
                return false;

            return true;
        }
    }
    Context _ctx;

    public UnusedLocalsRemover(SyntaxNode rootNode)
    {
        var compilation = CSharpCompilation.Create("MyAnalysis")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(rootNode.SyntaxTree);
        _ctx = new(compilation.GetSemanticModel(rootNode.SyntaxTree), new HashSet<ISymbol>(SymbolEqualityComparer.Default));
    }

    public static bool ContainsCalls(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any()
            || node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().Any()
            || node.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Any(m => !VariableProcessor.Constants.ContainsKey(m.ToString()));
    }

    class AssignmentCollector : CSharpSyntaxWalker
    {
        private readonly Context _ctx;

        public readonly List<ExpressionStatementSyntax> AssignmentsToRemove = new();
        public readonly List<AssignmentExpressionSyntax> AssignmentsToReplace = new();
        public readonly HashSet<ISymbol> keepLocals = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        public AssignmentCollector(Context ctx)
        {
            _ctx = ctx;
        }

        // assignment inside another statement:
        //   "foo(b = 333)" => "foo(333)"
        //   "a = b = 333"  => "b = 333" (if a is unused)
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var symLeft = _ctx.Model.GetSymbolInfo(node.Left).Symbol;
            if (symLeft != null && _ctx.UnusedLocals.Contains(symLeft))
            {
                if (node.Parent is not ExpressionStatementSyntax || node.Right is AssignmentExpressionSyntax)
                    AssignmentsToReplace.Add(node);
            }
            base.VisitAssignmentExpression(node);
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            // stand-alone assignment: "b = 333;" => delete
            if (node.Expression is AssignmentExpressionSyntax assignment)
            {
                var symbol = _ctx.Model.GetSymbolInfo(assignment.Left).Symbol;
                if (symbol != null && _ctx.UnusedLocals.Contains(symbol))
                {
                    if (_ctx.IsSafeToRemove(assignment.Right))
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
                var symbol = _ctx.Model.GetDeclaredSymbol(variable);
                if (symbol == null || !_ctx.UnusedLocals.Contains(symbol))
                    continue;

                // Check if initializer is present and not a literal
                var init = variable.Initializer?.Value;
                if (init != null && !_ctx.IsSafeToRemove(init) && init is not AssignmentExpressionSyntax)
                    keepLocals.Add(symbol); // keep it
            }

            base.VisitLocalDeclarationStatement(node);
        }
    }

    class DeclarationRemover : CSharpSyntaxRewriter
    {
        private readonly Context _ctx;
        private readonly HashSet<SyntaxNode> _assignmentsToRemove;
        private readonly HashSet<AssignmentExpressionSyntax> _assignmentsToReplace;

        public DeclarationRemover(Context ctx, IEnumerable<SyntaxNode> assignmentsToRemove,
                                   IEnumerable<AssignmentExpressionSyntax> assignmentsToReplace)
        {
            _ctx = ctx;
            _assignmentsToRemove = new HashSet<SyntaxNode>(assignmentsToRemove);
            _assignmentsToReplace = new HashSet<AssignmentExpressionSyntax>(assignmentsToReplace);
        }

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            var toKeep = new List<VariableDeclaratorSyntax>();
            var toConvert = new List<AssignmentExpressionSyntax>();

            foreach (var variable in node.Declaration.Variables)
            {
                var symbol = _ctx.Model.GetDeclaredSymbol(variable);
                if (symbol == null || !_ctx.UnusedLocals.Contains(symbol))
                {
                    toKeep.Add(variable);
                }

                // Check if initializer is present and not a literal
                var init = variable.Initializer?.Value;
                if (init is AssignmentExpressionSyntax assEx)
                {
                    toConvert.Add(assEx);
                }
                else if (init != null && !_ctx.IsSafeToRemove(init))
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
        var dataFlow = _ctx.Model.AnalyzeDataFlow(rootNode);
        if (!dataFlow.Succeeded)
            return rootNode;

        // collect variables that are only declared [and written to] but never read
        _ctx.UnusedLocals = dataFlow.VariablesDeclared
            .Where(v => !dataFlow.ReadInside.Contains(v) && !dataFlow.ReadOutside.Contains(v))
            .ToHashSet<ISymbol>(SymbolEqualityComparer.Default);

        if (_ctx.UnusedLocals.Count == 0)
            return rootNode;

        // First pass: collect assignments to unused locals
        var collector = new AssignmentCollector(_ctx);
        collector.Visit(rootNode);

        // keep only those unused locals that are assigned a literal value
        _ctx.UnusedLocals.ExceptWith(collector.keepLocals);

        if (_ctx.UnusedLocals.Count == 0)
            return rootNode;

        return new DeclarationRemover(_ctx, collector.AssignmentsToRemove, collector.AssignmentsToReplace)
            .Visit(rootNode);
    }
}
