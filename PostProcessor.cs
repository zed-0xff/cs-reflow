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
        block = new IfRewriter().Visit(block) as BlockSyntax; // should be after EmptiesRemover
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
    public override SyntaxNode VisitIfStatement(IfStatementSyntax ifStmt)
    {
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

        return base.VisitIfStatement(ifStmt);
    }

    // remove unnecessary nested blocks
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        node = (BlockSyntax)base.VisitBlock(node);
        if (node.Statements.Any(stmt => stmt is BlockSyntax))
        {
            var statements = new List<StatementSyntax>();
            foreach (var stmt in node.Statements)
            {
                if (stmt is BlockSyntax innerBlock)
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
    public override SyntaxNode VisitLabeledStatement(LabeledStatementSyntax node)
    {
        if (node.Statement is not IfStatementSyntax ifStmt)
            return base.VisitLabeledStatement(node);

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

        return base.VisitLabeledStatement(node);
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

        if (token.Parent is EmptyStatementSyntax) // keep comments on empty statements (deleted nodes)
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

                int paddingSpaces = Math.Max(1, _targetColumn - offsetInLine);
                newTrivia.Add(SyntaxFactory.Whitespace(new string(' ', paddingSpaces)));
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
