using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

class UnusedLocalsRemover : CSharpSyntaxRewriter
{
    public int Verbosity = 0;
    Context? _mainCtx = null;

    static int aidx = 0;
    bool _needRetry;

    public UnusedLocalsRemover(SyntaxNode rootNode, int verbosity = 0)
    {
        Verbosity = verbosity;
    }

    class Context : SemanticContext
    {
        HashSet<ISymbol> _unusedLocalSyms = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        HashSet<String> _unusedLocalNames = new HashSet<string>(StringComparer.Ordinal);
        // false = only compare by symbol (stricter, but may miss some cases)
        // true  = compare by name (more lenient, but may remove some used locals)
        bool compareByName = false;

        public Context(SyntaxNode rootNode) : base(rootNode)
        {
        }

        public void SetUnusedLocals(IEnumerable<ISymbol> unusedLocals)
        {
            _unusedLocalSyms.Clear();
            _unusedLocalNames.Clear();

            foreach (var sym in unusedLocals)
            {
                if (sym != null)
                {
                    _unusedLocalSyms.Add(sym);
                    _unusedLocalNames.Add(sym.Name);
                }
            }
        }

        public bool IsSafeToRemove(ExpressionSyntax node)
        {
            if (ContainsCalls(node))
                return false;

            var dataFlow = Model.AnalyzeDataFlow(node);
            if (!dataFlow.Succeeded)
                return false;

            if (dataFlow.WrittenInside.Any(w => !IsUnusedLocal(w)))
                return false;

            return true;
        }

        public bool IsUnusedLocal(IdentifierNameSyntax id)
        {
            var symbol = Model.GetSymbolInfo(id).Symbol;
            if (IsUnusedLocal(symbol))
                return true;
            if (compareByName && _unusedLocalNames.Contains(id.Identifier.Text))
                return true;

            return false;
        }

        public bool IsUnusedLocal(ISymbol symbol)
        {
            if (symbol == null)
                return false;

            if (_unusedLocalSyms.Contains(symbol))
                return true;

            if (compareByName && _unusedLocalNames.Contains(symbol.Name))
                return true;

            return false;
        }
    }

    public static bool ContainsCalls(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any()
            || node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().Any()
            || node.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Any(m => !VariableProcessor.Constants.ContainsKey(m.ToString()));
    }

    class Collector : CSharpSyntaxWalker
    {
        private readonly Context _ctx;

        public readonly List<ExpressionStatementSyntax> StatementsToRemove = new();
        public readonly List<AssignmentExpressionSyntax> AssignmentsToReplace = new();
        public readonly HashSet<ISymbol> keepLocalSyms = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        public readonly HashSet<String> keepLocalNames = new HashSet<String>(StringComparer.Ordinal);

        public Collector(Context ctx)
        {
            _ctx = ctx;
        }

        // assignment inside another statement:
        //   "foo(b = 333)" => "foo(333)"
        //   "a = b = 333"  => "b = 333" (if a is unused)
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (node.Left is IdentifierNameSyntax idLeft && _ctx.IsUnusedLocal(idLeft))
            {
                if (node.Parent is not ExpressionStatementSyntax || node.Right is AssignmentExpressionSyntax)
                    AssignmentsToReplace.Add(node);
            }
            base.VisitAssignmentExpression(node);
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            switch (node.Expression)
            {
                // stand-alone assignment: "b = 333;" => delete
                case AssignmentExpressionSyntax assignment:
                    if (assignment.Left is IdentifierNameSyntax idLeft)
                    {
                        if (_ctx.IsUnusedLocal(idLeft))
                        {
                            if (_ctx.IsSafeToRemove(assignment.Right))
                            {
                                StatementsToRemove.Add(node);
                                return; // skip further processing of this node
                            }
                            if (assignment.Right is not AssignmentExpressionSyntax)
                            {
                                // If the right side is not a literal, we keep the local variable
                                keepLocalSyms.Add(_ctx.Model.GetSymbolInfo(idLeft).Symbol);
                                keepLocalNames.Add(idLeft.Identifier.Text);
                            }
                        }
                    }
                    break;

                // stand-alone "++x" / "--x"
                case PrefixUnaryExpressionSyntax prefixUnary:
                    if (prefixUnary.Operand is IdentifierNameSyntax idPrefix && _ctx.IsUnusedLocal(idPrefix))
                    {
                        StatementsToRemove.Add(node);
                    }
                    break;

                // stand-alone "x++" / "x--"
                case PostfixUnaryExpressionSyntax postfixUnary:
                    if (postfixUnary.Operand is IdentifierNameSyntax idPostfix && _ctx.IsUnusedLocal(idPostfix))
                    {
                        StatementsToRemove.Add(node);
                    }
                    break;
            }

            base.VisitExpressionStatement(node);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            foreach (var variable in node.Declaration.Variables)
            {
                var symbol = _ctx.Model.GetDeclaredSymbol(variable); // always present
                if (!_ctx.IsUnusedLocal(symbol))
                    continue;

                // Check if initializer is present and not a literal
                var init = variable.Initializer?.Value;
                if (init != null && !_ctx.IsSafeToRemove(init) && init is not AssignmentExpressionSyntax)
                {
                    keepLocalSyms.Add(symbol);
                }
            }

            base.VisitLocalDeclarationStatement(node);
        }
    }

    class Remover : CSharpSyntaxRewriter
    {
        private readonly Context _ctx;
        private readonly HashSet<SyntaxNode> _statementsToRemove;
        private readonly HashSet<AssignmentExpressionSyntax> _assignmentsToReplace;

        public Remover(Context ctx, IEnumerable<SyntaxNode> assignmentsToRemove,
                                   IEnumerable<AssignmentExpressionSyntax> assignmentsToReplace)
        {
            _ctx = ctx;
            _statementsToRemove = new HashSet<SyntaxNode>(assignmentsToRemove);
            _assignmentsToReplace = new HashSet<AssignmentExpressionSyntax>(assignmentsToReplace);
        }

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            var toKeep = new List<VariableDeclaratorSyntax>();
            var toConvert = new List<AssignmentExpressionSyntax>();

            foreach (var variable in node.Declaration.Variables)
            {
                var symbol = _ctx.Model.GetDeclaredSymbol(variable);
                if (symbol == null || !_ctx.IsUnusedLocal(symbol))
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
                AssignmentExpressionSyntax? assignment = VisitAssignmentExpression(toConvert[0]) as AssignmentExpressionSyntax;
                return assignment == null ? null : SyntaxFactory.ExpressionStatement(assignment);
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
            //Console.Error.WriteLine($"[d] {node.Title()} => {_statementsToRemove.Contains(node)}");
            if (_statementsToRemove.Contains(node))
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

        public override SyntaxNode VisitLabeledStatement(LabeledStatementSyntax node)
        {
            var newStatement = Visit(node.Statement) as StatementSyntax;

            if (newStatement == null)
            {
                // You CANNOT return a LabeledStatement without a statement
                return node.WithStatement(SyntaxFactory.EmptyStatement());
            }

            return node.WithStatement(newStatement);
        }
    }

    //    public HashSet<ISymbol> FindCallArgs(SyntaxNode rootNode)
    //    {
    //        var ids = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
    //
    //        foreach (var id in rootNode.DescendantNodes().OfType<IdentifierNameSyntax>())
    //        {
    //            var symbol = _ctx.Model.GetSymbolInfo(id).Symbol;
    //            if (symbol == null || symbol.Kind != SymbolKind.Local)
    //                continue;
    //
    //            var call = id.FirstAncestorOrSelf<InvocationExpressionSyntax>();
    //            if (call == null)
    //                continue;
    //
    //            ids.Add(symbol);
    //        }
    //
    //        return ids;
    //    }

    // same as ReadInside(), but also include ++/--
    HashSet<ISymbol> FindWriteOnlyVars(Context ctx, SyntaxNode rootNode)
    {
        var declared = rootNode.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(s => s.Declaration.Variables)
            .Select(v => ctx.Model.GetDeclaredSymbol(v))
            .Where(s => s != null && s.Kind == SymbolKind.Local)
            .ToHashSet(SymbolEqualityComparer.Default);

        var written = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var read = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        foreach (var id in rootNode.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var symbol = ctx.Model.GetSymbolInfo(id).Symbol;
            if (symbol == null || symbol.Kind != SymbolKind.Local)
                continue;

            var expr = id.Parent;

            // ++x, --x, !x, ~x, ...
            if (expr is PrefixUnaryExpressionSyntax unaryPrefix)
            {
                if (expr.Parent is not ExpressionStatementSyntax) // y = ++x; / while(++x){ ... }
                    read.Add(symbol);
                if (unaryPrefix.IsKind(SyntaxKind.PreIncrementExpression) || unaryPrefix.IsKind(SyntaxKind.PreDecrementExpression))
                    written.Add(symbol);
                else
                    read.Add(symbol);
                continue;
            }

            // x++, x--
            if (expr is PostfixUnaryExpressionSyntax unaryPostfix)
            {
                if (expr.Parent is not ExpressionStatementSyntax)
                    read.Add(symbol);
                if (unaryPostfix.IsKind(SyntaxKind.PostIncrementExpression) || unaryPostfix.IsKind(SyntaxKind.PostDecrementExpression))
                    written.Add(symbol);
                else
                    read.Add(symbol);
                continue;
            }

            // =, +=, -=, *=, /=, %=, ...
            AssignmentExpressionSyntax? assExpr = expr.FirstAncestorOrSelfUntil<AssignmentExpressionSyntax, BlockSyntax>();
            if (assExpr != null)
            {
                if (assExpr.Parent is not ExpressionStatementSyntax)
                    read.Add(symbol);
                if (assExpr.Left is IdentifierNameSyntax idLeft && idLeft.Identifier.Text == id.Identifier.Text)
                    written.Add(symbol); // intentionally do not interpret self-read as 'read'
                else
                    read.Add(symbol);
                continue;
            }

            // foo(x)
            ArgumentSyntax? argExpr = expr.FirstAncestorOrSelfUntil<ArgumentSyntax, BlockSyntax>();
            if (argExpr != null)
            {
                read.Add(symbol);
                if (!argExpr.RefOrOutKeyword.IsKind(SyntaxKind.None))
                    written.Add(symbol); // ref/out arguments are both read and written
                continue;
            }

            if (Verbosity > 2)
                Console.Error.WriteLine($"[d] FindWriteOnlyVars: unhandled expression: {expr.Kind()} at {expr.TitleWithLineNo()}");
            read.Add(symbol); // fallback: treat as read
        }

        // Only keep variables that are written but never read
        written.ExceptWith(read);
        return declared.Intersect(written, SymbolEqualityComparer.Default).ToHashSet(SymbolEqualityComparer.Default);
    }

    BlockSyntax? RewriteBlock(BlockSyntax block)
    {
        if (_mainCtx == null)
            throw new InvalidOperationException("Main context is not set. Call Process().");

        if (Verbosity > 2)
            Console.Error.WriteLine($"[d] UnusedLocalsRemover.RewriteBlock: {block}");
        else if (Verbosity > 0)
            Console.Error.WriteLine($"[d] UnusedLocalsRemover.RewriteBlock: {block.TitleWithLineSpan()}");

        var ctx = _mainCtx;
        DataFlowAnalysis? dataFlow = null;

        try
        {
            dataFlow = ctx.Model.AnalyzeDataFlow(block);
        }
        catch (ArgumentException ex)
        {
            if (ex.Message != "statements not within tree")
                throw;
            _needRetry = true;
            return block; // return original block
        }

        if (!dataFlow.Succeeded)
            return block;

        if (Verbosity > 1)
        {
            Console.Error.WriteLine($"[d] dataFlow.VariablesDeclared: {string.Join(", ", dataFlow.VariablesDeclared.Select(s => s.Name))}");
            Console.Error.WriteLine($"[d] dataFlow.ReadInside       : {string.Join(", ", dataFlow.ReadInside.Select(s => s.Name))}");
        }

        // collect variables that are only declared [and written to] but never read
        var readInsideNames = dataFlow.ReadInside.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        var unusedLocals =
            dataFlow.VariablesDeclared
            .Where(v => !readInsideNames.Contains(v.Name))
            .ToHashSet<ISymbol>(SymbolEqualityComparer.Default);

        if (Verbosity > 0 && unusedLocals.Count > 0)
            Console.Error.WriteLine($"[d] unused locals A: {string.Join(", ", unusedLocals.Select(s => s.Name))}");

        var wrOnly = FindWriteOnlyVars(ctx, block);
        if (Verbosity > 0 && wrOnly.Count > 0)
            Console.Error.WriteLine($"[d] write-only vars: {string.Join(", ", wrOnly.Select(s => s.Name))}");
        unusedLocals.UnionWith(wrOnly);

        if (unusedLocals.Count == 0)
            return block;

        // First pass: collect assignments to unused locals
        ctx.SetUnusedLocals(unusedLocals);
        var collector = new Collector(ctx);
        collector.Visit(block);

        // keep only those unused locals that are assigned a literal value
        unusedLocals.ExceptWith(collector.keepLocalSyms);
        if (unusedLocals.Count == 0)
            return block;

        if (Verbosity > 0)
            Console.Error.WriteLine($"[d] unused locals B: {string.Join(", ", unusedLocals.Select(s => s.Name))}");

        ctx.SetUnusedLocals(unusedLocals);
        collector = new Collector(ctx);
        collector.Visit(block);

        var newNode = new Remover(ctx, collector.StatementsToRemove, collector.AssignmentsToReplace)
            .Visit(block);

        return newNode switch
        {
            BlockSyntax newBlock => newBlock.IsEquivalentTo(block) ? block : newBlock,
            null => null,
            _ => throw new InvalidOperationException($"Unexpected node type: {newNode.GetType().Name}"),
        };
    }

    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        var updated = (BlockSyntax)base.VisitBlock(node); // visit children first
        var result = RewriteBlock(updated);
        return result;
    }

    // node is typically 'method body' block, but can be any syntax node
    public SyntaxNode Process(SyntaxNode node)
    {
        int i;
        _mainCtx = new Context(node);
        for (i = 0; i < 1000; i++)
        {
            if (Verbosity > 0)
                Console.Error.WriteLine($"\n[d] UnusedLocalsRemover.Process: iteration #{i}");

            _needRetry = false;
            var newNode = Visit(node);

            if (newNode.IsEquivalentTo(node) && _needRetry)
                throw new InvalidOperationException($"UnusedLocalsRemover.Process: no changes after iteration #{i}");

            node = node.ReplaceAndGetNewNode(newNode);
            _mainCtx.Update(node);

            if (!_needRetry)
                return node;
        }
        throw new InvalidOperationException($"UnusedLocalsRemover.Process: too many iterations: {i}");
    }
}
