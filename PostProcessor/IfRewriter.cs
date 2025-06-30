using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
        {
            if (ifStmt.Else is not null && ifStmt.Else.Statement is BlockSyntax elseBlk && elseBlk.Statements.Count == 0)
                ifStmt = ifStmt.WithElse(null);
        }

        // remove empty then
        {
            if (ifStmt.Statement is BlockSyntax block && block.Statements.Count == 0 && ifStmt.Else != null)
                ifStmt = ifStmt
                    .WithCondition(invert_condition(ifStmt.Condition))
                    .WithStatement(ifStmt.Else.Statement)
                    .WithElse(null);
        }

        // if (…) {} else { if (…) {} } =>
        // if (…) {} else if (…) {}
        {
            if (ifStmt.Else != null
                    && ifStmt.Else.Statement is BlockSyntax block
                    && block.Statements.Count == 1
                    && block.Statements[0] is IfStatementSyntax)
            {
                ifStmt = ifStmt.WithElse(ifStmt.Else.WithStatement(block.Statements[0]));
            }
        }

        bool condIsNot = ifStmt.Condition.StripParentheses() is PrefixUnaryExpressionSyntax prefixUnary && prefixUnary.IsKind(SyntaxKind.LogicalNotExpression);

        // if (x) { return … } else {…} =>
        //
        // if (x) return …;
        // …
        {
            if (ifStmt.Else != null
                    && ifStmt.Statement is BlockSyntax thenBlk && thenBlk.Statements.Count == 1 && thenBlk.Statements[0] is ReturnStatementSyntax returnStmt
                    && ifStmt.Else.Statement is BlockSyntax elseBlk)
            {
                // invert a inverted condition if 'then' and 'else' blocks are single statements
                if (condIsNot && elseBlk.Statements.Count == 1)
                {
                    return Block(
                            (
                             ifStmt
                             .WithCondition(invert_condition(ifStmt.Condition))
                             .WithStatement(elseBlk)
                             .WithElse(null)
                            ),
                            thenBlk.Statements[0]
                    );
                }

                return Block(List([
                            ifStmt.WithElse(null),
                            ..elseBlk.Statements
                ]));
            }
        }

        // if (x) {          =>  if (x) {…}
        //   …                   return list2;
        //   return list2;
        // } else {
        //   return list2;
        // }
        {
            if (ifStmt.Else != null
                    && ifStmt.Statement is BlockSyntax thenBlk && thenBlk.Statements.Count > 0
                    && ifStmt.Else.Statement is BlockSyntax elseBlk && elseBlk.Statements.Count > 0
                    && thenBlk.Statements[^1] is ReturnStatementSyntax returnStmt && returnStmt.IsSameStmt(elseBlk.Statements[^1]))
            {
                return Block(List<StatementSyntax>([
                            (
                             ifStmt
                             .WithStatement(
                                 thenBlk.WithStatements(
                                     List(thenBlk.Statements.Take(thenBlk.Statements.Count - 1))))
                             .WithElse(
                                 ifStmt.Else
                                 .WithStatement(
                                     elseBlk.WithStatements(
                                         List(elseBlk.Statements.Take(elseBlk.Statements.Count - 1)))))
                            ),
                            returnStmt
                ]));
            }
        }

        // if (x) {…} else { return … }
        //
        // if (!x) return …;
        // …
        {
            if (ifStmt.Else != null
                    && ifStmt.Statement is BlockSyntax thenBlk && (thenBlk.Statements.Count > 1 || (thenBlk.Statements.Count == 1 && condIsNot))
                    && ifStmt.Else.Statement is BlockSyntax elseBlk
                    && elseBlk.Statements.Count == 1 && elseBlk.Statements[0] is ReturnStatementSyntax returnStmt)
            {
                return Block(List([
                            (
                             ifStmt
                             .WithCondition(invert_condition(ifStmt.Condition))
                             .WithStatement(elseBlk)
                             .WithElse(null)
                            ),
                            ..thenBlk.Statements
                ]));
            }
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
            node = node.WithStatements(List(statements));
        }
        return node;
    }

    // convert 'if' to 'while'
    public override SyntaxNode? VisitLabeledStatement(LabeledStatementSyntax node0)
    {
        var newNode = base.VisitLabeledStatement(node0);
        if (newNode is not LabeledStatementSyntax node)
            return newNode;

        // same as the next one, but label is on a Block (after the 'if' statements rewrite)
        {
            if (node.Statement is BlockSyntax block && block.Statements.Count > 1 && block.Statements[0] is IfStatementSyntax ifStmt0
                    && ifStmt0.Else == null
                    && !block.DescendantNodes().OfType<BreakStatementSyntax>().Any()
                    && !block.DescendantNodes().OfType<ContinueStatementSyntax>().Any()
                    && block.Statements[^1] is GotoStatementSyntax gotoStmt1 && gotoStmt1.Expression is IdentifierNameSyntax gotoId1
                    && gotoId1.Identifier.ValueText == node.Identifier.ValueText)
            {
                List<StatementSyntax> newStatements = new List<StatementSyntax>(
                        ifStmt0.Statement is BlockSyntax eb ? eb.Statements : SingletonList(ifStmt0.Statement)
                        );
                newStatements.Insert(0,
                        WhileStatement(
                            invert_condition(ifStmt0.Condition),
                            Block(block.Statements.Skip(1).Take(block.Statements.Count - 2))
                            )
                        );
                return VisitBlock(Block(newStatements)); // XXX in _most_ cases, block is not necessary here, but we can't return multiple statements directly
            }
        }

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
        {
            if (
                    // TODO: check if it's the only goto to this label
                    ifStmt.Else != null
                    && ifStmt.Statement is BlockSyntax thenBlk && thenBlk.Statements.Count > 1
                    && !thenBlk.DescendantNodes().OfType<BreakStatementSyntax>().Any()
                    && !thenBlk.DescendantNodes().OfType<ContinueStatementSyntax>().Any()
                    && thenBlk.Statements[^1] is GotoStatementSyntax gotoStmt1 && gotoStmt1.Expression is IdentifierNameSyntax gotoId1
                    && gotoId1.Identifier.ValueText == node.Identifier.ValueText)
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
        {
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

