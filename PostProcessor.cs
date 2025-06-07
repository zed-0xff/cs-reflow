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

    static readonly string[] assignmentOperators = { "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=" };
    static readonly string[] simpleTypes = { "int", "uint", "nint", "nuint", "long", "ulong", "bool", "float", "double", "char", "string", "object", "byte", "sbyte" };
    static readonly Regex defaultsRE = new Regex($@"default\(\s*({String.Join("|", simpleTypes.Select(type => type))})\s*\)", RegexOptions.Compiled);

    Dictionary<int, string[]> _blockStrCache = new();

    bool has_var(SyntaxNode node, string varName)
    {
        return node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(id => id.Identifier.Text == varName);
    }

    // TODO: better name
    bool is_var_used_(SyntaxNode child, string varName, BlockSyntax root)
    {
        var parent = child.Parent;

        // note: check semanticModel.GetSymbolInfo(id1).Symbol for more precise comparison
        while (parent != null && parent != root)
        {
            if (Verbosity > 1)
                // Console.Error.WriteLine($"[d] parent: [{parent.Kind()}] [{parent.GetType()}] {parent.Title()}");
                Console.Error.WriteLine($"[d] parent: [{parent.Kind()}] {parent.Title()}");

            switch (parent)
            {
                case AssignmentExpressionSyntax assignExpr:
                    // … = … func() … - keep them all
                    if (assignExpr.Right.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any())
                        return true;

                    // x = … needle … [where x is not needle]
                    // obj.y = … needle …
                    if (
                            assignExpr.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(id => id.Identifier.Text != varName) &&
                            has_var(assignExpr.Right, varName)
                       )
                    {
                        return true;
                    }
                    break;

                case ArgumentSyntax arg:
                    // func(… needle …) [where x is not needle]
                    if (has_var(arg, varName))
                        return true;
                    break;

                case ConditionalExpressionSyntax condExpr:
                    if (has_var(condExpr.Condition, varName)) // (… needle …) ? … : …
                        return true;
                    break;

                case DoStatementSyntax doStmt:
                    if (has_var(doStmt.Condition, varName)) // do { … } while (… needle …)
                        return true;
                    break;

                case ElementAccessExpressionSyntax elemAccessExpr:
                    if (has_var(elemAccessExpr.Expression, varName)) // arr[… needle …], but not needle[…]
                        return true;
                    break;

                case ForStatementSyntax forStmt:
                    if (has_var(forStmt.Condition, varName)) // for (… needle …) { … }
                        return true;
                    break;

                case IfStatementSyntax ifStmt:
                    if (has_var(ifStmt.Condition, varName)) // if (… needle …) { … }
                        return true;
                    break;

                case InvocationExpressionSyntax invocExpr:
                    if (has_var(invocExpr, varName)) // func(… needle …)
                        return true;
                    break;

                case LambdaExpressionSyntax lambdaExpr:
                    if (has_var(lambdaExpr, varName)) // () => { … needle … } XXX maybe too wide
                        return true;
                    break;

                case VariableDeclarationSyntax varDecl:
                    // int needle = … func() … - keep all bc function call might be important
                    if (varDecl.Variables.Any(v => v.Identifier.Text == varName) && varDecl.DescendantNodes().OfType<InvocationExpressionSyntax>().Any())
                        return true;

                    // note: Any(!=) is not an else case for Any(==)

                    // int x = … needle … [where x is not needle]
                    if (varDecl.Variables.Any(v => v.Identifier.Text != varName) && has_var(varDecl, varName))
                        return true;

                    break;

                case ReturnStatementSyntax returnStmt:
                    if (has_var(returnStmt.Expression, varName)) // return needle …
                        return true;
                    break;

                case WhileStatementSyntax whileStmt:
                    if (has_var(whileStmt.Condition, varName)) // while (… needle …) { … }
                        return true;
                    break;
            }
            parent = parent.Parent;
        }

        return false;
    }

    bool is_var_used(BlockSyntax block, string varName)
    {
        foreach (var id in block.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (id.Identifier.Text != varName)
                continue;

            if (is_var_used_(id, varName, block))
                return true;
        }

        return false;
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

            case TryStatementSyntax tryStmt:
                var newFinally = tryStmt.Finally?.WithBlock(PostProcess(tryStmt.Finally.Block));
                if (newFinally != null && newFinally.Block.Statements.Count == 0)
                    newFinally = null; // remove empty finally block

                return tryStmt
                    .WithBlock(PostProcess(tryStmt.Block))
                    .WithCatches(SyntaxFactory.List(tryStmt.Catches.Select(c => { return c.WithBlock(PostProcess(c.Block)); })))
                    .WithFinally(newFinally);
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
                    if (!isSwitchVar(varName))
                    {
                        if (!is_var_used(block, varName) && !is_var_used_(decl, varName, block))
                            setSwitchVar(varName); // XXX not actually a switch var, but an useless var
                    }
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
            BaseMethodDeclarationSyntax method => method.WithBody(PostProcess(method.Body)),
            LocalFunctionStatementSyntax func => func.WithBody(PostProcess(func.Body)),
            _ => throw new InvalidOperationException($"Unsupported function type: {root.Kind()}")
        };
    }

    public BlockSyntax PostProcessAll(BlockSyntax block)
    {
        for (int i = 0; i < 10; i++)
        {
            string s0 = RemoveAllComments(block).NormalizeWhitespace().ToString();
            block = PostProcess(block);
            string s1 = RemoveAllComments(block).NormalizeWhitespace().ToString();
            if (s0 == s1)
                break; // no changes
        }
        return block;
    }
}

