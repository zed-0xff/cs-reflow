using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

class UnusedLocalsRemover : CSharpSyntaxRewriter
{
    public int Verbosity = 0;
    Context? _mainCtx = null;
    HashSet<string> _keepVars;

    bool _needRetry;

    public UnusedLocalsRemover(SyntaxNode rootNode, int verbosity = 0, HashSet<string>? keepVars = null)
    {
        Verbosity = verbosity;
        _keepVars = keepVars ?? new HashSet<string>();
    }

    class Context : SemanticContext
    {
        HashSet<SyntaxAnnotation> _unusedLocals = new();

        public Context(SyntaxNode rootNode) : base(rootNode)
        {
        }

        public void SetUnusedLocals(HashSet<SyntaxAnnotation> unusedLocals)
        {
            _unusedLocals = unusedLocals;
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

        public bool IsUnusedLocal(SyntaxNode node)
        {
            var ann = node.GetAnnotations("VAR").FirstOrDefault();
            return ann != null && _unusedLocals.Contains(ann);
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
        public readonly HashSet<SyntaxAnnotation> keepLocals = new();

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
                                var ann = idLeft.GetAnnotations("VAR").FirstOrDefault();
                                if (ann == null)
                                    throw new InvalidOperationException($"Identifier {idLeft.Identifier.Text} has no 'VAR' annotation.");
                                keepLocals.Add(ann);
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
                if (!_ctx.IsUnusedLocal(variable))
                    continue;

                // Check if initializer is present and not a literal
                var init = variable.Initializer?.Value;
                if (init != null && !_ctx.IsSafeToRemove(init) && init is not AssignmentExpressionSyntax)
                {
                    var ann = variable.GetAnnotations("VAR").FirstOrDefault();
                    if (ann == null)
                        throw new InvalidOperationException($"Variable {variable.Identifier.Text} has no 'VAR' annotation.");
                    keepLocals.Add(ann);
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
    (HashSet<SyntaxAnnotation>, HashSet<SyntaxAnnotation>, HashSet<SyntaxAnnotation>) CollectVars(SyntaxNode rootNode)
    {
        var declared = rootNode.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(s => s.Declaration.Variables)
            .Select(v => v.GetAnnotations("VAR").FirstOrDefault())
            .Where(v => v != null)
            .ToHashSet();

        var written = new HashSet<SyntaxAnnotation>();
        var read = new HashSet<SyntaxAnnotation>();

        foreach (var id in rootNode.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var ann = id.GetAnnotations("VAR").FirstOrDefault();
            if (ann == null)
                continue;

            var expr = id.Parent;

            // ++x, --x, !x, ~x, ...
            if (expr is PrefixUnaryExpressionSyntax unaryPrefix)
            {
                if (expr.Parent is not ExpressionStatementSyntax) // y = ++x; / while(++x){ ... }
                    read.Add(ann);
                if (unaryPrefix.IsKind(SyntaxKind.PreIncrementExpression) || unaryPrefix.IsKind(SyntaxKind.PreDecrementExpression))
                    written.Add(ann);
                else
                    read.Add(ann);
                continue;
            }

            // x++, x--
            if (expr is PostfixUnaryExpressionSyntax unaryPostfix)
            {
                if (expr.Parent is not ExpressionStatementSyntax)
                    read.Add(ann);
                if (unaryPostfix.IsKind(SyntaxKind.PostIncrementExpression) || unaryPostfix.IsKind(SyntaxKind.PostDecrementExpression))
                    written.Add(ann);
                else
                    read.Add(ann);
                continue;
            }

            // =, +=, -=, *=, /=, %=, ...
            AssignmentExpressionSyntax? assExpr = expr.FirstAncestorOrSelfUntil<AssignmentExpressionSyntax, BlockSyntax>();
            if (assExpr != null)
            {
                switch (assExpr.Parent)
                {
                    case ExpressionStatementSyntax:
                    case EqualsValueClauseSyntax:
                    case AssignmentExpressionSyntax:
                    case ArgumentSyntax:
                        break;
                    default:
                        if (Verbosity > 1)
                            Console.Error.WriteLine($"[d] CollectVars: READ  {ann.Data} bc parent is {assExpr.Parent.Kind()}");
                        read.Add(ann);
                        break;
                }
                if (assExpr.Left is IdentifierNameSyntax idLeft && idLeft.IsSameVar(id))
                {
                    if (Verbosity > 1)
                        Console.Error.WriteLine($"[d] CollectVars: WRITE {ann.Data}");
                    written.Add(ann); // intentionally do not interpret self-read as 'read'
                }
                else
                {
                    if (Verbosity > 1)
                        Console.Error.WriteLine($"[d] CollectVars: READ  {ann.Data}");
                    read.Add(ann);
                }
                continue;
            }

            // foo(x)
            ArgumentSyntax? argExpr = expr.FirstAncestorOrSelfUntil<ArgumentSyntax, BlockSyntax>();
            if (argExpr != null)
            {
                if (Verbosity > 1)
                    Console.Error.WriteLine($"[d] CollectVars: READ  {ann.Data}");
                read.Add(ann);
                if (!argExpr.RefOrOutKeyword.IsKind(SyntaxKind.None))
                    written.Add(ann); // ref/out arguments are both read and written
                continue;
            }

            if (Verbosity > 1)
                Console.Error.WriteLine($"[d] CollectVars: unhandled expression: {expr.Kind()} at {expr.TitleWithLineNo()} => READ {ann.Data}");
            read.Add(ann); // fallback: treat as read
        }

        return (declared, read, written);
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
            if (dataFlow.VariablesDeclared.Count() > 0)
                Console.Error.WriteLine($"[d] dataFlow.VariablesDeclared: {string.Join(", ", dataFlow.VariablesDeclared.Select(s => s.Name))}");
            if (dataFlow.ReadInside.Count() > 0)
                Console.Error.WriteLine($"[d] dataFlow.ReadInside       : {string.Join(", ", dataFlow.ReadInside.Select(s => s.Name))}");
            if (dataFlow.ReadOutside.Count() > 0)
                Console.Error.WriteLine($"[d] dataFlow.ReadOutside      : {string.Join(", ", dataFlow.ReadOutside.Select(s => s.Name))}");
            if (dataFlow.WrittenInside.Count() > 0)
                Console.Error.WriteLine($"[d] dataFlow.WrittenInside    : {string.Join(", ", dataFlow.WrittenInside.Select(s => s.Name))}");
            if (dataFlow.WrittenOutside.Count() > 0)
                Console.Error.WriteLine($"[d] dataFlow.WrittenOutside   : {string.Join(", ", dataFlow.WrittenOutside.Select(s => s.Name))}");
        }

        var (declared, read, written) = CollectVars(block);
        if (Verbosity > 0)
        {
            if (declared.Count > 0)
                Console.Error.WriteLine($"[d] declared: {string.Join(", ", declared.Select(s => s.Data))}");
            if (read.Count > 0)
                Console.Error.WriteLine($"[d] read:     {string.Join(", ", read.Select(s => s.Data))}");
            if (written.Count > 0)
                Console.Error.WriteLine($"[d] written:  {string.Join(", ", written.Select(s => s.Data))}");
        }
        var unusedLocals = declared
            .Where(s => !read.Contains(s))
            .Where(s => !_keepVars.Contains(s.Data))
            .ToHashSet();

        if (Verbosity > 0 && unusedLocals.Count > 0)
            Console.Error.WriteLine($"[d] unused locals A: {string.Join(", ", unusedLocals.Select(s => s.Data))}");

        if (unusedLocals.Count == 0)
            return block;

        // First pass: collect assignments to unused locals
        ctx.SetUnusedLocals(unusedLocals);
        var collector = new Collector(ctx);
        collector.Visit(block);

        if (Verbosity > 0 && collector.keepLocals.Count > 0)
        {
            Console.Error.WriteLine($"[d] keepLocals: {string.Join(", ", collector.keepLocals.Select(s => s.Data))}");
        }

        unusedLocals.ExceptWith(collector.keepLocals);
        if (unusedLocals.Count == 0)
            return block;

        if (Verbosity > 0)
            Console.Error.WriteLine($"[d] unused locals B: {string.Join(", ", unusedLocals.Select(s => s.Data))}");

        ctx.SetUnusedLocals(unusedLocals);
        collector = new Collector(ctx);
        collector.Visit(block);

        if (Verbosity > 0)
        {
            if (collector.StatementsToRemove.Count > 0)
                Console.Error.WriteLine($"[d] Statements to remove: {string.Join(", ", collector.StatementsToRemove.Select(s => s.TitleWithLineNo()))}");
            if (collector.AssignmentsToReplace.Count > 0)
                Console.Error.WriteLine($"[d] Assignments to replace: {string.Join(", ", collector.AssignmentsToReplace.Select(s => s.TitleWithLineNo()))}");
        }

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
