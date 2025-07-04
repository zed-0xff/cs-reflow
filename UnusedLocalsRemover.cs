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
    readonly IEnumerable<SyntaxTree> _trees;

    bool _needRetry;

    public UnusedLocalsRemover(VarDB varDB, IEnumerable<SyntaxTree> trees, int verbosity = 0, HashSet<string>? keepVars = null)
    {
        _varDB = varDB;
        _trees = trees;
        Verbosity = verbosity;
        _keepVars = new();
        if (keepVars is not null)
        {
            foreach (string varName in keepVars)
            {
                var V = _varDB.FindByName(varName);
                if (V is not null)
                    _keepVars.Add(V.id);
            }
        }
    }

    class Context : SemanticContext
    {
        HashSet<int> _unusedLocals = new();
        public readonly VarDB _varDB;

        public Context(SyntaxNode rootNode, IEnumerable<SyntaxTree> trees, VarDB varDB) : base(rootNode, trees)
        {
            _varDB = varDB;
        }

        public void SetUnusedLocals(HashSet<int> unusedLocals)
        {
            _unusedLocals = unusedLocals;
        }

        int ann2id(SyntaxAnnotation ann) => _varDB[ann].id;
        int ann2id(string var_id) => int.Parse(var_id, System.Globalization.NumberStyles.HexNumber);

        public bool IsSafeToRemove(ExpressionSyntax node)
        {
            if (node.IsIdempotent())
                return true;

            if (ContainsCalls(node))
                return false;

            var dataFlow = Model.AnalyzeDataFlow(node);
            if (dataFlow is null || !dataFlow.Succeeded)
                return false;

            if (dataFlow.WrittenInside.Any(w => !IsUnusedLocal(w)))
                return false;

            return true;
        }

        public bool IsUnusedLocal(VariableDeclaratorSyntax node)
        {
            var ann = node.VarID();
            return ann is not null && _unusedLocals.Contains(ann2id(ann));
        }

        public bool IsUnusedLocal(SyntaxToken token)
        {
            var ann = token.VarID();
            return ann is not null && _unusedLocals.Contains(ann2id(ann));
        }

        public bool IsUnusedLocal(IdentifierNameSyntax node)
        {
            var ann = node.VarID();
            return ann is not null && _unusedLocals.Contains(ann2id(ann));
        }

        public bool IsUnusedLocal(string var_id) => _unusedLocals.Contains(ann2id(var_id));

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
            || node.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Any(m => !m.IsKnownConstant());
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

        bool AllTokensUnused(ExpressionSyntax expr) => AllTokensUnused(expr.CollectVarIDs());
        bool AllTokensUnused(IEnumerable<string> tokens) => tokens.Any() && tokens.All(t => _ctx.IsUnusedLocal(t));

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            switch (node.Expression)
            {
                // stand-alone assignment: "b = 333;" => delete
                case AssignmentExpressionSyntax assignment:
                    var left_ids = assignment.Left.CollectVarIDs();
                    if (AllTokensUnused(left_ids))
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
                                    left_ids
                                    .Select(t => _ctx._varDB.TryGetValue(t, out var v) ? v!.id : -1)
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
                if (init is not null && !_ctx.IsSafeToRemove(init) && init is not AssignmentExpressionSyntax)
                {
                    var ann = variable.VarID();
                    if (ann is null)
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
                else if (init is not null && !_ctx.IsSafeToRemove(init))
                {
                    toKeep.Add(variable); // keep it
                }
                // Else: no initializer, or initializer is literal → safe to remove
            }

            if (toKeep.Count == 0 && toConvert.Count == 1)
            {
                // "int a = b = 111;" => "b = 111;"
                AssignmentExpressionSyntax? assignment = VisitAssignmentExpression(toConvert[0]) as AssignmentExpressionSyntax;
                return assignment is null ? gen_empty_stmt(node) : SyntaxFactory.ExpressionStatement(assignment);
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

            if (newStatement is null)
            {
                // You CANNOT return a LabeledStatement without a statement
                return node.WithStatement(SyntaxFactory.EmptyStatement());
            }

            return node.WithStatement(newStatement);
        }
    }

    SyntaxNode? RewriteBlock(SyntaxNode block)
    {
        if (_mainCtx is null)
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

        if (dataFlow is null || !dataFlow.Succeeded)
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

        var (declared, read, written) = _varDB.CollectVars(block);

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
        var updated = base.VisitBlock(node); // visit children first (x2.2 faster than visiting them afterwards)
        if (updated is null)
            return null;

        var result = RewriteBlock(updated);
        return result;
    }

    // node is typically 'method body' block, but can be any syntax node
    public BlockSyntax Process(BlockSyntax node)
    {
        int i;
        _mainCtx = new Context(node, _trees, _varDB);
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
