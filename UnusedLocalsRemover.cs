using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

class UnusedLocalsRemover : CSharpSyntaxRewriter
{
    static readonly string TAG = "UnusedLocalsRemover";
    private static readonly TaggedLogger _logger = new(TAG);

    public int Verbosity = 0;
    Context? _mainCtx = null;
    readonly HashSet<int> _keepVars;
    readonly VarDB _varDB;

    bool _needRetry;

    public UnusedLocalsRemover(VarDB varDB, int verbosity = 0, HashSet<string>? keepVars = null)
    {
        _varDB = varDB;
        Verbosity = verbosity;
        _keepVars = new();
        if (keepVars != null)
        {
            foreach (string varName in keepVars)
            {
                var V = _varDB.FindByName(varName);
                if (V != null)
                    _keepVars.Add(V.id);
            }
        }
    }

    class Context : SemanticContext
    {
        HashSet<int> _unusedLocals = new();
        public readonly VarDB _varDB;

        public Context(SyntaxNode rootNode, VarDB varDB) : base(rootNode)
        {
            _varDB = varDB;
        }

        public void SetUnusedLocals(HashSet<int> unusedLocals)
        {
            _unusedLocals = unusedLocals;
        }

        int ann2id(SyntaxAnnotation ann) => _varDB[ann].id;

        public bool IsSafeToRemove(ExpressionSyntax node)
        {
            if (ContainsCalls(node))
                return false;

            var dataFlow = Model.AnalyzeDataFlow(node);
            if (dataFlow == null || !dataFlow.Succeeded)
                return false;

            if (dataFlow.WrittenInside.Any(w => !IsUnusedLocal(w)))
                return false;

            return true;
        }

        public bool IsUnusedLocal(VariableDeclaratorSyntax node)
        {
            var ann = node.VarID();
            return ann != null && _unusedLocals.Contains(ann2id(ann));
        }

        public bool IsUnusedLocal(SyntaxToken token)
        {
            var ann = token.VarID();
            return ann != null && _unusedLocals.Contains(ann2id(ann));
        }

        public bool IsUnusedLocal(IdentifierNameSyntax node)
        {
            var ann = node.VarID();
            return ann != null && _unusedLocals.Contains(ann2id(ann));
        }

        // TODO
        public bool IsUnusedLocal(ISymbol symbol)
        {
            return false;
        }
    }

    public static bool ContainsCalls(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any()
            || node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().Any()
            || node.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Any(m => !VarProcessor.Constants.ContainsKey(m.ToString()));
    }

    class Collector : CSharpSyntaxWalker
    {
        private readonly Context _ctx;

        public readonly List<ExpressionStatementSyntax> StatementsToRemove = new();
        public readonly List<AssignmentExpressionSyntax> AssignmentsToReplace = new();
        public readonly HashSet<int> keepLocals = new();
        public int Verbosity = 0;

        public Collector(Context ctx, int verbosity = 0)
        {
            _ctx = ctx;
            Verbosity = verbosity;
        }

        // assignment inside another statement:
        //   "foo(b = 333)" => "foo(333)"
        //   "a = b = 333"  => "b = 333" (if a is unused)
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (AllTokensUnused(node.Left))
            {
                if (node.Parent is not ExpressionStatementSyntax || node.Right is AssignmentExpressionSyntax)
                    AssignmentsToReplace.Add(node);
            }
            base.VisitAssignmentExpression(node);
        }

        bool AllTokensUnused(ExpressionSyntax expr) => AllTokensUnused(expr.CollectTokens());
        bool AllTokensUnused(List<SyntaxToken> tokens) => tokens.Any() && tokens.All(t => _ctx.IsUnusedLocal(t));

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            switch (node.Expression)
            {
                // stand-alone assignment: "b = 333;" => delete
                case AssignmentExpressionSyntax assignment:
                    var tokens = assignment.Left.CollectTokens();
                    if (AllTokensUnused(tokens))
                    {
                        if (_ctx.IsSafeToRemove(assignment.Right))
                        {
                            StatementsToRemove.Add(node);
                            return; // skip further processing of this node
                        }
                        if (assignment.Right is not AssignmentExpressionSyntax)
                        {
                            // If the right side is not a literal, we keep the local variable
                            keepLocals.UnionWith(
                                    tokens
                                    .Select(t => _ctx._varDB.TryGetValue(t, out var v) ? v.id : -1)
                                    .Where(id => id != -1)
                                    );
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
                if (!_ctx.IsUnusedLocal(variable))
                    continue;

                // Check if initializer is present and not a literal
                var init = variable.Initializer?.Value;
                if (init != null && !_ctx.IsSafeToRemove(init) && init is not AssignmentExpressionSyntax)
                {
                    var ann = variable.VarID();
                    if (ann == null)
                        throw new InvalidOperationException($"Variable {variable.Identifier.Text} has no 'VarID' annotation.");
                    _logger.debug(() => $"[d] Collector: keeping local variable {ann.Data} because of initializer {init.TitleWithLineNo()}");
                    keepLocals.Add(_ctx._varDB[ann].id);
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

        SyntaxNode gen_empty_stmt(SyntaxNode prev)
        {
            return prev.ToEmptyStmt();
        }

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            var toKeep = new List<VariableDeclaratorSyntax>();
            var toConvert = new List<AssignmentExpressionSyntax>();

            foreach (var variable in node.Declaration.Variables)
            {
                if (!_ctx.IsUnusedLocal(variable))
                    toKeep.Add(variable);

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
                return assignment == null ? gen_empty_stmt(node) : SyntaxFactory.ExpressionStatement(assignment);
            }

            if (toConvert.Count == 0)
            {
                // If all variables are unused: remove the whole statement
                if (toKeep.Count == 0)
                {
                    return gen_empty_stmt(node);
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
            //_logger.debug(() => "[d] {node.Title()} => {_statementsToRemove.Contains(node)}");
            if (_statementsToRemove.Contains(node))
                return gen_empty_stmt(node);

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
    (HashSet<int>, HashSet<int>, HashSet<int>) CollectVars(SyntaxNode rootNode)
    {
        var declared = rootNode.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(s => s.Declaration.Variables)
            .Select(v => v.VarID())
            .Where(v => v != null)
            .Select(v => _varDB[v].id)
            .ToHashSet();

        var written = new HashSet<int>();
        var read = new HashSet<int>();

        foreach (var idNode in rootNode.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var ann = idNode.VarID();
            if (ann == null)
                continue;
            int id = _varDB[ann].id;

            var expr = idNode.Parent;

            // ++x, --x, !x, ~x, ...
            if (expr is PrefixUnaryExpressionSyntax unaryPrefix)
            {
                if (expr.Parent is not ExpressionStatementSyntax) // y = ++x; / while(++x){ ... }
                    read.Add(id);
                if (unaryPrefix.IsKind(SyntaxKind.PreIncrementExpression) || unaryPrefix.IsKind(SyntaxKind.PreDecrementExpression))
                    written.Add(id);
                else
                    read.Add(id);
                continue;
            }

            // x++, x--
            if (expr is PostfixUnaryExpressionSyntax unaryPostfix)
            {
                if (expr.Parent is not ExpressionStatementSyntax)
                    read.Add(id);
                if (unaryPostfix.IsKind(SyntaxKind.PostIncrementExpression) || unaryPostfix.IsKind(SyntaxKind.PostDecrementExpression))
                    written.Add(id);
                else
                    read.Add(id);
                continue;
            }

            // =, +=, -=, *=, /=, %=, ...
            AssignmentExpressionSyntax? assExpr = expr.FirstAncestorOrSelfUntil<AssignmentExpressionSyntax, BlockSyntax>();
            if (assExpr != null)
            {
                var tokens = assExpr.Left.CollectTokens();
                if (tokens.Any(t => t.IsSameVar(ann)))
                {
                    _logger.debug(() => $"WRITE {ann.Data}");
                    written.Add(id); // intentionally do not interpret self-read as 'read'
                }
                else
                {
                    _logger.debug(() => $"READ  {ann.Data}");
                    read.Add(id);
                }
                continue;
            }

            LocalDeclarationStatementSyntax? declStmt = expr.FirstAncestorOrSelfUntil<LocalDeclarationStatementSyntax, BlockSyntax>();
            if (declStmt != null)
            {
                // local variable declaration: int x = 123 + [...]
                if (declStmt.IsSameVar(ann))
                {
                    _logger.debug(() => $"DECLARE {ann.Data}");
                    declared.Add(id);
                }
                else
                {
                    _logger.debug(() => $"READ  {ann.Data}");
                    read.Add(id); // variable is read in the other variable's initializer
                }
                continue;
            }

            // foo(x)
            ArgumentSyntax? argExpr = expr.FirstAncestorOrSelfUntil<ArgumentSyntax, BlockSyntax>();
            if (argExpr != null)
            {
                _logger.debug(() => $"READ  {ann.Data}");
                read.Add(id);
                if (!argExpr.RefOrOutKeyword.IsKind(SyntaxKind.None))
                    written.Add(id); // ref/out arguments are both read and written
                continue;
            }

            _logger.debug(() => $"unhandled expression: {expr.Kind()} at {expr.TitleWithLineNo()} => READ {ann.Data}");
            read.Add(id); // fallback: treat as read
        }

        return (declared, read, written);
    }

    BlockSyntax? RewriteBlock(BlockSyntax block)
    {
        if (_mainCtx == null)
            throw new TaggedException(TAG, "Main context is not set. Call Process().");

        if (Verbosity > 2)
            _logger.debug(() => $"{block}");
        else if (Verbosity > 0)
            _logger.debug(() => $"{block.TitleWithLineSpan()}");

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

        if (dataFlow.VariablesDeclared.Count() > 0)
            _logger.debug(() => $"[d] dataFlow.VariablesDeclared: {string.Join(", ", dataFlow.VariablesDeclared.Select(s => s.Name))}");
        if (dataFlow.ReadInside.Count() > 0)
            _logger.debug(() => $"[d] dataFlow.ReadInside       : {string.Join(", ", dataFlow.ReadInside.Select(s => s.Name))}");
        if (dataFlow.ReadOutside.Count() > 0)
            _logger.debug(() => $"[d] dataFlow.ReadOutside      : {string.Join(", ", dataFlow.ReadOutside.Select(s => s.Name))}");
        if (dataFlow.WrittenInside.Count() > 0)
            _logger.debug(() => $"[d] dataFlow.WrittenInside    : {string.Join(", ", dataFlow.WrittenInside.Select(s => s.Name))}");
        if (dataFlow.WrittenOutside.Count() > 0)
            _logger.debug(() => $"[d] dataFlow.WrittenOutside   : {string.Join(", ", dataFlow.WrittenOutside.Select(s => s.Name))}");

        var (declared, read, written) = CollectVars(block);

        if (declared.Count > 0)
            _logger.debug(() => $"[d] declared: {string.Join(", ", declared.Select(s => _varDB[s]))}");
        if (read.Count > 0)
            _logger.debug(() => $"[d] read:     {string.Join(", ", read.Select(s => _varDB[s]))}");
        if (written.Count > 0)
            _logger.debug(() => $"[d] written:  {string.Join(", ", written.Select(s => _varDB[s]))}");

        var rwOutside =
            dataFlow.ReadOutside.Select(s => s.Name)
            .Concat(
                    dataFlow.WrittenOutside.Select(s => s.Name)
                   )
            .ToHashSet();

        var unusedLocals = declared
            .Where(s => !read.Contains(s))
            .Where(s => !_keepVars.Contains(s))
            .Where(s => !rwOutside.Contains(_varDB[s].Name))
            .ToHashSet();

        if (unusedLocals.Count > 0)
            _logger.debug(() => $"[d] unused locals A: {string.Join(", ", unusedLocals.Select(id => _varDB[id]))}");

        if (unusedLocals.Count == 0)
            return block;

        // First pass: collect assignments to unused locals
        ctx.SetUnusedLocals(unusedLocals);
        var collector = new Collector(ctx, Verbosity);
        collector.Visit(block);

        if (collector.keepLocals.Count > 0)
        {
            _logger.debug(() => $"[d] keepLocals: {string.Join(", ", collector.keepLocals.Select(id => _varDB[id]))}");
        }

        unusedLocals.ExceptWith(collector.keepLocals);
        if (unusedLocals.Count == 0)
            return block;

        _logger.debug(() => $"[d] unused locals B: {string.Join(", ", unusedLocals.Select(id => _varDB[id]))}");

        ctx.SetUnusedLocals(unusedLocals);
        collector = new Collector(ctx, Verbosity);
        collector.Visit(block);

        if (collector.StatementsToRemove.Count > 0)
            _logger.debug(() => $"[d] Statements to remove: {string.Join(", ", collector.StatementsToRemove.Select(s => s.TitleWithLineNo()))}");
        if (collector.AssignmentsToReplace.Count > 0)
            _logger.debug(() => $"[d] Assignments to replace: {string.Join(", ", collector.AssignmentsToReplace.Select(s => s.TitleWithLineNo()))}");

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
        var updated = (BlockSyntax)base.VisitBlock(node); // visit children first (x2.2 faster than visiting them afterwards)
        if (updated == null)
            return null;

        var result = RewriteBlock(updated);
        return result;
    }

    // node is typically 'method body' block, but can be any syntax node
    public BlockSyntax Process(BlockSyntax node)
    {
        int i;
        _mainCtx = new Context(node, _varDB);
        for (i = 0; i < 1000; i++)
        {
            _logger.debug($"\niteration #{i}");

            _needRetry = false;
            var newNode = Visit(node);

            if (newNode.IsEquivalentTo(node) && _needRetry)
                throw new TaggedException(TAG, $"Process: no changes after iteration #{i}");

            node = node.ReplaceWith(newNode);
            _mainCtx.Update(node);

            if (!_needRetry)
                return node;
        }
        throw new TaggedException(TAG, $"Process: too many iterations: {i}");
    }
}
