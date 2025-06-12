using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class PostProcessor
{
    public bool RemoveSwitchVars = true;
    VariableProcessor _varProcessor;
    public int Verbosity = 0;

    public PostProcessor(VariableProcessor varProcessor, SyntaxNode? rootNode = null)
    {
        _varProcessor = varProcessor;
    }

    bool isSwitchVar(string varName)
    {
        return _varProcessor.VariableValues.IsSwitchVar(varName);
    }

    void setSwitchVar(string varName, bool isSwitch = true)
    {
        _varProcessor.VariableValues.SetSwitchVar(varName, isSwitch);
    }

    public static SyntaxNode RemoveAllComments(SyntaxNode root)
    {
        return root.ReplaceTrivia(
            root.DescendantTrivia(),
            (trivia, _) =>
                trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                    ? default
                    : trivia
        );
    }

    bool is_simple_expr(ExpressionSyntax expr)
    {
        if (expr is LiteralExpressionSyntax || expr is IdentifierNameSyntax)
            return true;

        if (expr is BinaryExpressionSyntax binaryExpr)
        {
            return is_simple_expr(binaryExpr.Left) && is_simple_expr(binaryExpr.Right);
        }

        return false;
    }

    bool is_empty_if(StatementSyntax stmt)
    {
        if (stmt is IfStatementSyntax ifStmt)
        {
            if ((ifStmt.Statement is EmptyStatementSyntax) || (ifStmt.Statement is BlockSyntax block && block.Statements.Count == 0))
            {
                if ((ifStmt.Else == null) ||
                    (ifStmt.Else.Statement is EmptyStatementSyntax) ||
                    (ifStmt.Else.Statement is BlockSyntax block2 && block2.Statements.Count == 0))
                {
                    // empty if/else
                    return is_simple_expr(ifStmt.Condition);
                }
            }
        }
        return false;
    }

    // XXX may be incorrect if operators are overridden
    ExpressionSyntax invert_condition(ExpressionSyntax condition)
    {
        switch (condition)
        {
            case BinaryExpressionSyntax binaryExpr:
                switch (binaryExpr.Kind())
                {
                    case SyntaxKind.EqualsExpression:
                        return binaryExpr.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken));

                    case SyntaxKind.NotEqualsExpression:
                        return binaryExpr.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken));

                    case SyntaxKind.LessThanExpression:
                        return binaryExpr.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken));

                    case SyntaxKind.LessThanOrEqualExpression:
                        return binaryExpr.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.GreaterThanToken));

                    case SyntaxKind.GreaterThanExpression:
                        return binaryExpr.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken));

                    case SyntaxKind.GreaterThanOrEqualExpression:
                        return binaryExpr.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.LessThanToken));
                }
                break;

            case PrefixUnaryExpressionSyntax unaryExpr:
                if (unaryExpr.IsKind(SyntaxKind.LogicalNotExpression))
                    return unaryExpr.Operand;
                break;
        }

        return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizedExpression(condition));
    }

    IfStatementSyntax postprocess_if(IfStatementSyntax ifStmt)
    {
        //      if (!bool_0aw) {} else …
        if (ifStmt.Statement is BlockSyntax block && block.Statements.Count == 0 && ifStmt.Else != null)
            ifStmt = ifStmt
                .WithCondition(invert_condition(ifStmt.Condition))
                .WithStatement(ifStmt.Else.Statement)
                .WithElse(null);

        //      if (…) {} else { if (…) {} }
        if (ifStmt.Else != null
                && ifStmt.Else.Statement is BlockSyntax block2
                && block2.Statements.Count == 1
                && block2.Statements[0] is IfStatementSyntax)
        {
            ifStmt = ifStmt.WithElse(ifStmt.Else.WithStatement(block2.Statements[0]));
        }

        // remove empty else
        if (ifStmt.Else is not null && ifStmt.Else.Statement is BlockSyntax block3 && block3.Statements.Count == 0)
            ifStmt = ifStmt.WithElse(null);

        return ifStmt;
    }

    StatementSyntax PostProcess(StatementSyntax stmt)
    {
        var result = PostProcess(stmt as SyntaxNode);
        if (result is StatementSyntax statement)
            return statement;
        else
            throw new InvalidOperationException($"PostProcess() returned {result.Kind()} instead of StatementSyntax");
    }

    ExpressionSyntax PostProcess(ExpressionSyntax expr)
    {
        var result = PostProcess(expr as SyntaxNode);
        if (result is ExpressionSyntax expression)
            return expression;
        else
            throw new InvalidOperationException($"PostProcess() returned {result.Kind()} instead of ExpressionSyntax");
    }

    SyntaxNode PostProcess(SyntaxNode stmt)
    {
        switch (stmt)
        {
            case LabeledStatementSyntax labelStmt:
                stmt = labelStmt.WithStatement(PostProcess(labelStmt.Statement));
                break;

            case ExpressionStatementSyntax exprStmt:
                var id_expr = exprStmt.Expression switch
                {
                    AssignmentExpressionSyntax assignExpr => assignExpr.Left,  // x = 1, x += 1, ...
                    PrefixUnaryExpressionSyntax preExpr => preExpr.Operand,    // ++x, --x
                    PostfixUnaryExpressionSyntax postExpr => postExpr.Operand, // x++, x--
                    _ => null
                };
                if (id_expr is IdentifierNameSyntax id && RemoveSwitchVars && isSwitchVar(id.Identifier.Text))
                    stmt = EmptyStatement();
                break;

            case IfStatementSyntax ifStmt:
                stmt = ifStmt.WithStatement(PostProcess(ifStmt.Statement as BlockSyntax));
                if (ifStmt.Else != null)
                {
                    stmt = (stmt as IfStatementSyntax).WithElse(ifStmt.Else.WithStatement(PostProcess(ifStmt.Else.Statement)));
                }
                break;

            case BlockSyntax blockStmt:
                stmt = PostProcess(blockStmt);
                break;

            case WhileStatementSyntax whileStmt:
                stmt = whileStmt
                    .WithCondition(PostProcess(whileStmt.Condition))
                    .WithStatement(PostProcess(whileStmt.Statement as BlockSyntax));
                break;

            case DoStatementSyntax doStmt:
                stmt = doStmt
                    .WithCondition(PostProcess(doStmt.Condition))
                    .WithStatement(PostProcess(doStmt.Statement as BlockSyntax));
                break;

            case ForStatementSyntax forStmt:
                stmt = forStmt
                    .WithCondition(PostProcess(forStmt.Condition))
                    .WithStatement(PostProcess(forStmt.Statement as BlockSyntax));
                break;

            case ForEachStatementSyntax forEachStmt:
                stmt = forEachStmt
                    .WithExpression(PostProcess(forEachStmt.Expression))
                    .WithStatement(PostProcess(forEachStmt.Statement as BlockSyntax));
                break;
        }
        return stmt;
    }

    BlockSyntax PostProcess(BlockSyntax block)
    {
        if (block == null)
            return null;

        List<StatementSyntax> statements = new();

        for (int i = 0; i < block.Statements.Count; i++)
        {
            var stmt = block.Statements[i];
            switch (stmt)
            {
                case LocalDeclarationStatementSyntax localDecl:
                    var decl = localDecl.Declaration.Variables.First();
                    string varName = decl.Identifier.Text;
                    if (RemoveSwitchVars && localDecl.Declaration.Variables.All(v => isSwitchVar(v.Identifier.Text)))
                        continue;
                    break;

                default:
                    stmt = PostProcess(stmt) as StatementSyntax;
                    if (stmt == null)
                        throw new InvalidOperationException($"PostProcess() returned null for {stmt.Kind()}");
                    break;
            }

            if (is_empty_if(stmt))
                continue;

            if (stmt is IfStatementSyntax ifStmt)
            {
                stmt = postprocess_if(ifStmt);
            }

            if (stmt is EmptyStatementSyntax)
                continue;

            statements.Add(stmt);
        }

        // label + empty statement => label + next statement
        for (int i = 0; i < statements.Count - 1; i++)
        {
            if (statements[i] is LabeledStatementSyntax labelStmt && labelStmt.Statement is EmptyStatementSyntax)
            {
                statements[i + 1] = labelStmt.WithStatement(statements[i + 1]);
                statements.RemoveAt(i);
            }
        }

        return block.WithStatements(List(statements));
    }

    public SyntaxNode ProcessFunction(SyntaxNode root)
    {
        return root switch
        {
            BaseMethodDeclarationSyntax method => method.WithBody(PostProcessAll(method.Body)),
            LocalFunctionStatementSyntax func => func.WithBody(PostProcessAll(func.Body)),
            _ => throw new InvalidOperationException($"Unsupported function type: {root.Kind()}")
        };
    }

    public BlockSyntax PostProcessAll(BlockSyntax block)
    {
        for (int i = 0; i < 10; i++)
        {
            var block2 = PostProcess(block);
            if (block2.IsEquivalentTo(block))
                break; // no changes
            block = block2;
        }
        block = new EmptyFinallyBlockRemover().Visit(block) as BlockSyntax;
        block = new DuplicateDeclarationRemover().Visit(block) as BlockSyntax;
        block = new DeclarationAssignmentMerger().Visit(block) as BlockSyntax;
        return block;
    }
}

// remove empty finally block
public class EmptyFinallyBlockRemover : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitTryStatement(TryStatementSyntax node)
    {
        var newFinally = node.Finally?.WithBlock(Visit(node.Finally.Block) as BlockSyntax);
        if (newFinally != null && newFinally.Block.Statements.Count == 0)
            newFinally = null;

        return node
            .WithBlock(Visit(node.Block) as BlockSyntax)
            .WithCatches(SyntaxFactory.List(node.Catches.Select(c => { return c.WithBlock(Visit(c.Block) as BlockSyntax); })))
            .WithFinally(newFinally);
    }
}

// remove duplicate declarations left after flow tree unflattening
public class DuplicateDeclarationRemover : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        var groups = node.Statements
            .OfType<LocalDeclarationStatementSyntax>()
            .Where(decl => decl.HasAnnotations("ID"))
            .GroupBy(decl => decl.GetAnnotations("ID").First().Data)
            .Where(g => g.Count() > 1)
            .ToList();

        var newNode = node;

        foreach (var group in groups)
        {
            // Keep the first declaration, remove the rest
            var toRemove = group.Skip(1).ToList();
            newNode = newNode.RemoveNodes(toRemove, SyntaxRemoveOptions.KeepNoTrivia);
        }

        // "int a; int a=2;"   => "int a=2;"
        // "int a=2; int a;"   => "int a=2;"
        // "int a; int a;"     => "int a;"
        // "int a=1; int a=2;" => "int a=1; int a=2;"
        bool was = false;
        var newStatements = new List<StatementSyntax>();
        var stmts = newNode.Statements;
        for (int i = 0; i < stmts.Count; i++)
        {
            if (
                    i + 1 < stmts.Count &&
                    stmts[i] is LocalDeclarationStatementSyntax decl1 &&
                    stmts[i + 1] is LocalDeclarationStatementSyntax decl2 &&
                    (decl1.IsSameStmt(decl2) || decl1.IsSameVar(decl2))
                )
            {
                if (decl1.IsSameStmt(decl2) || (decl1.Declaration.Variables[0].Initializer == null && decl2.Declaration.Variables[0].Initializer == null))
                {
                    newStatements.Add(decl1);
                }
                else
                {
                    if (decl1.Declaration.Variables[0].Initializer != null)
                        newStatements.Add(decl1);
                    if (decl2.Declaration.Variables[0].Initializer != null)
                        newStatements.Add(decl2);
                }
                was = true;
                i++;
            }
            else
            {
                newStatements.Add(stmts[i]);
            }
        }

        if (was)
            newNode = newNode.WithStatements(SyntaxFactory.List(newStatements));

        // Visit the modified node further down the tree if needed
        return base.VisitBlock(newNode);
    }
}

// convert declaration followed by assignment into a single declaration with initializer
// i.e.:
//   byte[] array;
//   array = fun(obj2);
// =>
//   byte[] array = fun(obj2);
//
// XXX FIXME may hide default initializer call for objects
public class DeclarationAssignmentMerger : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        var newStatements = new List<StatementSyntax>();
        var stmts = node.Statements;

        bool was = false;
        int i = 0;
        while (i < stmts.Count)
        {
            // Try to match pattern: declaration + assignment
            if (i + 1 < stmts.Count &&
                stmts[i] is LocalDeclarationStatementSyntax declStmt && declStmt.Declaration.Variables.Count == 1 &&
                declStmt.Declaration.Variables[0].Initializer == null &&
                stmts[i + 1] is ExpressionStatementSyntax assignStmt &&
                assignStmt.Expression is AssignmentExpressionSyntax assignExpr &&
                assignExpr.Left is IdentifierNameSyntax leftId &&
                leftId.IsSameVar(declStmt))
            {
                // Build combined declaration
                var variable = declStmt.Declaration.Variables[0]
                    .WithInitializer(SyntaxFactory.EqualsValueClause(assignExpr.Right))
                    .WithTrailingTrivia(assignStmt.GetTrailingTrivia());

                var newDecl = declStmt.WithDeclaration(
                    declStmt.Declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(variable)));

                newStatements.Add(newDecl);
                i += 2; // Skip both
                was = true;
            }
            else
            {
                newStatements.Add(stmts[i]);
                i++;
            }
        }

        return base.VisitBlock(was ? node.WithStatements(SyntaxFactory.List(newStatements)) : node);
    }
}
