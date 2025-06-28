using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class PostProcessor
{
    readonly VarDB _varDB;

    public PostProcessor(VarDB varDB)
    {
        _varDB = varDB;
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
        block = new EmptiesRemover().Visit(block) as BlockSyntax;
        block = new DuplicateDeclarationRemover().Visit(block) as BlockSyntax;
        block = new DeclarationAssignmentMerger().Visit(block) as BlockSyntax;
        block = new IfRewriter(_varDB).Visit(block) as BlockSyntax; // should be after EmptiesRemover
        return block;
    }

    public static string ExpandTabs(string input, int tabSize = 4)
    {
        var result = new StringBuilder();
        int column = 0;

        foreach (char c in input)
        {
            if (c == '\t')
            {
                int spaces = tabSize - (column % tabSize);
                result.Append(' ', spaces);
                column += spaces;
            }
            else
            {
                result.Append(c);
                column = (c == '\n' || c == '\r') ? 0 : column + 1;
            }
        }

        return result.ToString();
    }
}

// removes:
//  - empty finally block
//  - all EmptyStatementSyntax
public class EmptiesRemover : CSharpSyntaxRewriter
{
    // remove empty finally block
    public override SyntaxNode VisitTryStatement(TryStatementSyntax node)
    {
        if (node.Finally != null && node.Finally.Block.Statements.Count == 0)
            node = node.WithFinally(null);

        return base.VisitTryStatement(node);
    }

    // remove all EmptyStatementSyntax
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        var newStatements = node.Statements
            .Where(stmt => !(stmt is EmptyStatementSyntax)) // remove empty statements
            .ToList();

        // Label + EmptyStatement => Label + next statement
        for (int i = 0; i < newStatements.Count - 1; i++)
        {
            if (newStatements[i] is LabeledStatementSyntax labelStmt && labelStmt.Statement is EmptyStatementSyntax)
            {
                newStatements[i] = labelStmt.WithStatement(newStatements[i + 1]);
                newStatements.RemoveAt(i + 1);
            }
        }

        if (newStatements.Count != node.Statements.Count)
            node = node.WithStatements(SyntaxFactory.List(newStatements));
        return base.VisitBlock(node);
    }
}

public class IfRewriter : CSharpSyntaxRewriter
{
    readonly VarDB _varDB;

    static readonly ExpressionSyntax TRUE_LITERAL = LiteralExpression(SyntaxKind.TrueLiteralExpression);
    static readonly ExpressionSyntax FALSE_LITERAL = LiteralExpression(SyntaxKind.FalseLiteralExpression);

    public IfRewriter(VarDB varDB)
    {
        _varDB = varDB;
    }

    // "if (condition || always_true)" => "if (condition)"
    public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node0)
    {
        var newNode = base.VisitBinaryExpression(node0);
        if (newNode is not BinaryExpressionSyntax binaryExpr)
            return newNode;

        return binaryExpr.Kind() switch
        {
            SyntaxKind.LogicalOrExpression => maybe_reduce_or(binaryExpr),
            SyntaxKind.LogicalAndExpression => maybe_reduce_and(binaryExpr),
            _ => binaryExpr
        };
    }

    // use SyntaxFactory.AreEquivalent() to also match literals that were here before the reduction
    ExpressionSyntax maybe_reduce_or(BinaryExpressionSyntax binaryExpr)
    {
        var leftExpr = maybe_reduce(binaryExpr.Left);
        if (SyntaxFactory.AreEquivalent(leftExpr, TRUE_LITERAL) && binaryExpr.Right.IsIdempotent()) // (true || Ri) => true
            return TRUE_LITERAL;

        var rightExpr = maybe_reduce(binaryExpr.Right);
        if (SyntaxFactory.AreEquivalent(leftExpr, FALSE_LITERAL))
            return rightExpr;  // (false || R) => R

        if (SyntaxFactory.AreEquivalent(rightExpr, FALSE_LITERAL))
            return leftExpr; // (L || false) => L

        if (SyntaxFactory.AreEquivalent(rightExpr, TRUE_LITERAL) && binaryExpr.Left.IsIdempotent()) // (Li || true) => true
            return TRUE_LITERAL;

        if (leftExpr != binaryExpr.Left)
            binaryExpr = binaryExpr.WithLeft(leftExpr);

        if (rightExpr != binaryExpr.Right)
            binaryExpr = binaryExpr.WithRight(rightExpr);

        return binaryExpr;
    }

    ExpressionSyntax maybe_reduce_and(BinaryExpressionSyntax binaryExpr)
    {
        var leftExpr = maybe_reduce(binaryExpr.Left);
        if (SyntaxFactory.AreEquivalent(leftExpr, FALSE_LITERAL) && binaryExpr.Right.IsIdempotent()) // (false && Ri) => false
            return FALSE_LITERAL;

        var rightExpr = maybe_reduce(binaryExpr.Right);
        if (SyntaxFactory.AreEquivalent(leftExpr, TRUE_LITERAL))
            return rightExpr;  // (true && R) => R

        if (SyntaxFactory.AreEquivalent(rightExpr, TRUE_LITERAL))
            return leftExpr; // (L && true) => L

        if (SyntaxFactory.AreEquivalent(rightExpr, FALSE_LITERAL) && binaryExpr.Left.IsIdempotent()) // (Li && false) => false
            return FALSE_LITERAL;

        if (leftExpr != binaryExpr.Left)
            binaryExpr = binaryExpr.WithLeft(leftExpr);

        if (rightExpr != binaryExpr.Right)
            binaryExpr = binaryExpr.WithRight(rightExpr);

        return binaryExpr;
    }

    ExpressionSyntax maybe_reduce(ExpressionSyntax expr)
    {
        if (expr.IsIdempotent())
        {
            var result = eval_constexpr(expr);
            if (result is not null)
            {
                Logger.debug(() => $"Reducing {expr} to {result}", "IfRewriter.maybe_reduce");
                return result.Value ? TRUE_LITERAL : FALSE_LITERAL;
            }
        }
        return expr;
    }

    bool? eval_constexpr(ExpressionSyntax expr)
    {
        VarProcessor processor = new(_varDB);
        var result = processor.EvaluateExpression(expr);
        return result switch
        {
            bool b => b,
            UnknownValueBase unk => unk.Cast(TypeDB.Bool) switch
            {
                bool b => b,
                _ => null // can't evaluate
            },
            _ => null // can't evaluate
        };
    }

    public override SyntaxNode VisitIfStatement(IfStatementSyntax node0)
    {
        var newNode = base.VisitIfStatement(node0);
        if (newNode is not IfStatementSyntax ifStmt)
            return newNode;

        // remove empty else
        if (ifStmt.Else is not null && ifStmt.Else.Statement is BlockSyntax elseBlk && elseBlk.Statements.Count == 0)
            ifStmt = ifStmt.WithElse(null);

        // if (!bool_0aw) {} else …
        if (ifStmt.Statement is BlockSyntax block && block.Statements.Count == 0 && ifStmt.Else != null)
            ifStmt = ifStmt
                .WithCondition(invert_condition(ifStmt.Condition))
                .WithStatement(ifStmt.Else.Statement)
                .WithElse(null);

        // if (…) {} else { if (…) {} }
        if (ifStmt.Else != null
                && ifStmt.Else.Statement is BlockSyntax block2
                && block2.Statements.Count == 1
                && block2.Statements[0] is IfStatementSyntax)
        {
            ifStmt = ifStmt.WithElse(ifStmt.Else.WithStatement(block2.Statements[0]));
        }

        return ifStmt;
    }

    bool CanBeFlattened(BlockSyntax parentBlock, BlockSyntax childBlock)
    {
        // collect declarations from parent block
        var parentDeclarations = parentBlock.Statements
            .OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(decl => decl.Declaration.Variables.Select(var => var.Identifier.ValueText))
            .ToHashSet();

        // + all siblings but current
        foreach (var sibling in parentBlock.Statements)
        {
            if (sibling is BlockSyntax siblingBlock && siblingBlock != childBlock)
            {
                parentDeclarations.UnionWith(
                    siblingBlock.Statements
                        .OfType<LocalDeclarationStatementSyntax>()
                        .SelectMany(decl => decl.Declaration.Variables.Select(var => var.Identifier.ValueText))
                );
            }
        }

        // collect declarations from this block
        var childDeclarations = childBlock.Statements
            .OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(decl => decl.Declaration.Variables.Select(var => var.Identifier.ValueText))
            .ToHashSet();
        // return true if there are no conflicts
        return !childDeclarations.Any(var => parentDeclarations.Contains(var));
    }

    // remove unnecessary nested blocks
    public override SyntaxNode? VisitBlock(BlockSyntax node0)
    {
        var newNode = base.VisitBlock(node0);
        if (newNode is not BlockSyntax node)
            return newNode;

        if (node.Statements.Any(stmt => stmt is BlockSyntax blk2 && CanBeFlattened(node, blk2)))
        {
            var statements = new List<StatementSyntax>();
            foreach (var stmt in node.Statements)
            {
                if (stmt is BlockSyntax innerBlock && CanBeFlattened(node, innerBlock))
                {
                    statements.AddRange(innerBlock.Statements);
                }
                else
                {
                    statements.Add(stmt);
                }
            }
            node = node.WithStatements(SyntaxFactory.List(statements));
        }
        return node;
    }

    // convert 'if' to 'while'
    public override SyntaxNode VisitLabeledStatement(LabeledStatementSyntax node0)
    {
        var newNode = base.VisitLabeledStatement(node0);
        if (newNode is not LabeledStatementSyntax node)
            return newNode;

        if (node.Statement is not IfStatementSyntax ifStmt)
            return newNode;

        // lbl_372:
        //   if (num6 < int_0nk)
        //   {
        //     ((short[])obj)[num6] = (short)(ushort)((Random)obj4).Next(65, 90);
        //     num6++;
        //     goto lbl_372;
        //   }
        // else ...
        if (
                ifStmt.Else != null
                && ifStmt.Statement is BlockSyntax thenBlk && thenBlk.Statements.Count > 1
                && !thenBlk.DescendantNodes().OfType<BreakStatementSyntax>().Any()
                && !thenBlk.DescendantNodes().OfType<ContinueStatementSyntax>().Any()
                && thenBlk.Statements[^1] is GotoStatementSyntax gotoStmt1 && gotoStmt1.Expression is IdentifierNameSyntax gotoId1
                && gotoId1.Identifier.ValueText == node.Identifier.ValueText
           )
        {
            List<StatementSyntax> newStatements = new List<StatementSyntax>(
                    ifStmt.Else.Statement is BlockSyntax eb ? eb.Statements : SingletonList(ifStmt.Else.Statement)
                    );
            newStatements.Insert(0,
                    WhileStatement(
                        ifStmt.Condition,
                        Block(thenBlk.Statements.Take(thenBlk.Statements.Count - 1))
                        )
                    );
            return VisitBlock(Block(newStatements)); // XXX in _most_ cases, block is not necessary here, but we can't return multiple statements directly
        }

        // lbl_336:
        //    if (num10 >= int_1nl)
        //       ...
        //    else
        //    {
        //        ((short[])obj2)[num10] = (short)(ushort)((Random)obj4).Next(97, 122);
        //        num10++;
        //        goto lbl_336;
        //    }
        if (
                ifStmt.Else != null
                && ifStmt.Else.Statement is BlockSyntax elseBlk && elseBlk.Statements.Count > 1
                && !elseBlk.DescendantNodes().OfType<BreakStatementSyntax>().Any()
                && !elseBlk.DescendantNodes().OfType<ContinueStatementSyntax>().Any()
                && elseBlk.Statements[^1] is GotoStatementSyntax gotoStmt2 && gotoStmt2.Expression is IdentifierNameSyntax gotoId2
                && gotoId2.Identifier.ValueText == node.Identifier.ValueText
           )
        {
            List<StatementSyntax> newStatements = new List<StatementSyntax>(
                    ifStmt.Statement is BlockSyntax tb ? tb.Statements : SingletonList(ifStmt.Statement)
                    );
            newStatements.Insert(0,
                    WhileStatement(
                        invert_condition(ifStmt.Condition),
                        Block(elseBlk.Statements.Take(elseBlk.Statements.Count - 1))
                        )
                    );
            return VisitBlock(Block(newStatements)); // XXX in _most_ cases, block is not necessary here, but we can't return multiple statements directly
        }

        return node;
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
}

// remove duplicate declarations left after flow tree unflattening
public class DuplicateDeclarationRemover : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        var groups = node.Statements
            .OfType<LocalDeclarationStatementSyntax>()
            .Where(decl => decl.HasAnnotations("StmtID"))
            .GroupBy(decl => decl.GetAnnotations("StmtID").First().Data)
            .Where(g => g.Count() > 1)
            .ToList();

        var newNode = node;

        foreach (var group in groups)
        {
            // Keep the first declaration, remove the rest
            var toRemove = group.Skip(1).ToList();
            newNode = newNode!.RemoveNodes(toRemove, SyntaxRemoveOptions.KeepNoTrivia);
        }

        // "int a; int a=2;"   => "int a=2;"
        // "int a=2; int a;"   => "int a=2;"
        // "int a; int a;"     => "int a;"
        // "int a=1; int a=2;" => "int a=1; int a=2;"
        bool was = false;
        var newStatements = new List<StatementSyntax>();
        var stmts = newNode!.Statements;
        for (int i = 0; i < stmts.Count; i++)
        {
            if (
                    i + 1 < stmts.Count &&
                    stmts[i] is LocalDeclarationStatementSyntax decl1 &&
                    stmts[i + 1] is LocalDeclarationStatementSyntax decl2 &&
                    decl1.IsSameVar(decl2)
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
                leftId.Identifier.IsSameVar(declStmt.Declaration.Variables[0].Identifier))
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

public class CommentAligner : CSharpSyntaxRewriter
{
    private readonly SourceText _text;
    private readonly int _targetColumn;

    public CommentAligner(SourceText text, int targetColumn = 80)
    {
        _text = text;
        _targetColumn = targetColumn;
    }

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (!token.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)))
            return token;

        var newTrivia = new List<SyntaxTrivia>();
        bool aligned = false;

        foreach (var trivia in token.TrailingTrivia)
        {
            if (!aligned && trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                var tokenEnd = token.Span.End;
                var line = _text.Lines.GetLineFromPosition(tokenEnd);
                int offsetInLine = tokenEnd - line.Start;

                if (token.Parent is EmptyStatementSyntax)
                {
                    var trimmedText = trivia.ToString().Trim();

                    if (trimmedText.Contains(" // "))
                    {
                        // Split at the first occurrence of " // "
                        var splitIndex = trimmedText.IndexOf(" // ");

                        var leftCommentText = trimmedText.Substring(0, splitIndex);
                        var rightCommentText = trimmedText.Substring(splitIndex);

                        // Add left comment (non-aligned)
                        newTrivia.Add(SyntaxFactory.Comment(leftCommentText));

                        // Add padding spaces for alignment
                        int paddingSpaces = Math.Max(1, _targetColumn - offsetInLine - leftCommentText.Length - 1);
                        newTrivia.Add(SyntaxFactory.Whitespace(new string(' ', paddingSpaces)));

                        // Add right comment (aligned)
                        newTrivia.Add(SyntaxFactory.Comment(rightCommentText));

                        aligned = true;
                        continue; // skip rest of loop to avoid duplicate addition
                    }
                    else
                    {
                        // Case A: just add the comment as-is (non-aligned)
                        newTrivia.Add(trivia);
                        aligned = true;
                        continue;
                    }
                }

                // For tokens NOT under EmptyStatementSyntax, do default alignment
                int padSpaces = Math.Max(1, _targetColumn - offsetInLine);
                newTrivia.Add(SyntaxFactory.Whitespace(new string(' ', padSpaces)));
                newTrivia.Add(trivia);
                aligned = true;
            }
            else
            {
                newTrivia.Add(trivia);
            }
        }

        return token.WithTrailingTrivia(newTrivia);
    }


}
