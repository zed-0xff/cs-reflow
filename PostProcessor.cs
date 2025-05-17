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
    static readonly string[] assignmentOperators = { "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=" };

    public bool RemoveSwitchVars = true;
    VariableProcessor _varProcessor;

    public PostProcessor(VariableProcessor varProcessor)
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


    bool is_var_used(BlockSyntax block, string varName)
    {
        string blockStr = RemoveAllComments(block).NormalizeWhitespace().ToFullString();
        foreach (string line in blockStr.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.Trim();
            if (trimmed == $"int {varName};")
                continue;

            if (trimmed.StartsWith($"int {varName} = "))
                continue;

            if (assignmentOperators.Any(op => trimmed.StartsWith($"{varName} {op}"))) // TODO: spaces
                continue;

            if (Regex.IsMatch(trimmed, $@"\b{Regex.Escape(varName)}\b"))
                return true;
        }

        return false;
    }

    StatementSyntax PostProcess(StatementSyntax stmt)
    {
        switch (stmt)
        {
            case ExpressionStatementSyntax exprStmt:
                var expr = exprStmt.Expression;
                if (expr is AssignmentExpressionSyntax assignExpr)
                {
                    var left = assignExpr.Left;
                    var right = assignExpr.Right;
                    if (left is IdentifierNameSyntax id && RemoveSwitchVars && isSwitchVar(id.Identifier.Text))
                        stmt = EmptyStatement();
                }
                break;

            case IfStatementSyntax ifStmt:
                stmt = ifStmt.WithStatement(PostProcess(ifStmt.Statement as BlockSyntax));
                if (ifStmt.Else != null)
                {
                    stmt = (stmt as IfStatementSyntax).WithElse(ifStmt.Else.WithStatement(PostProcess(ifStmt.Else.Statement)));
                }
                break;

            case TryStatementSyntax tryStmt:
                return tryStmt
                    .WithBlock(PostProcess(tryStmt.Block))
                    .WithCatches(SyntaxFactory.List(tryStmt.Catches.Select(c => { return c.WithBlock(PostProcess(c.Block)); })));
                break;

            case BlockSyntax blockStmt:
                stmt = PostProcess(blockStmt);
                break;
        }
        return stmt;
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

    ExpressionSyntax invert_condition(ExpressionSyntax condition)
    {
        switch (condition)
        {
            case BinaryExpressionSyntax binaryExpr:
                if (binaryExpr.IsKind(SyntaxKind.EqualsExpression))
                    return binaryExpr.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken));
                else if (binaryExpr.IsKind(SyntaxKind.NotEqualsExpression))
                    return binaryExpr.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken));
                break;

            case PrefixUnaryExpressionSyntax unaryExpr:
                if (unaryExpr.IsKind(SyntaxKind.LogicalNotExpression))
                    return unaryExpr.Operand;
                break;
        }

        return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condition);
    }

    IfStatementSyntax postprocess_if(IfStatementSyntax ifStmt)
    {
        //      if (!bool_0aw) {} else ...
        if (ifStmt.Statement is BlockSyntax block && block.Statements.Count == 0)
            ifStmt = ifStmt
                .WithCondition(invert_condition(ifStmt.Condition))
                .WithStatement(ifStmt.Else.Statement)
                .WithElse(null);

        //      if (...) {} else { if (...) {} }
        if (ifStmt.Else != null
                && ifStmt.Else.Statement is BlockSyntax block2
                && block2.Statements.Count == 1
                && block2.Statements[0] is IfStatementSyntax)
        {
            ifStmt = ifStmt.WithElse(ifStmt.Else.WithStatement(block2.Statements[0]));
        }
        return ifStmt;
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
                    if (!isSwitchVar(varName) && !is_var_used(block, varName))
                        setSwitchVar(varName); // XXX not actually a switch var, but an useless var
                    if (RemoveSwitchVars && isSwitchVar(varName))
                        continue;
                    break;

                default:
                    stmt = PostProcess(stmt);
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

        return block.WithStatements(List(statements));
    }

    public BlockSyntax PostProcessAll(BlockSyntax block)
    {
        // TODO
        return PostProcess(PostProcess(block));
    }
}

