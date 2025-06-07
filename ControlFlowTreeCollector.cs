using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

class ControlFlowTreeCollector : CSharpSyntaxWalker
{
    public ControlFlowNode Root { get; }

    const int IndentSpaces = 2;
    const int CommentPadding = 80;

    Stack<ControlFlowNode> _stack = new();
    Dictionary<string, ControlFlowNode> _labels = new();

    public ControlFlowTreeCollector()
    {
        // dummy root to hold top-level statements
        Root = new ControlFlowNode(null, null);
        _stack.Push(Root);
    }

    private ControlFlowNode AddNode(CSharpSyntaxNode stmt)
    {
        var node = new ControlFlowNode(stmt, _stack.Peek());
        _stack.Peek().Children.Add(node);
        _stack.Push(node);
        return node;
    }

    private void PopNode()
    {
        _stack.Pop();
    }

    private ControlFlowNode VisitControlFlow(CSharpSyntaxNode stmt, Action baseVisit)
    {
        var node = AddNode(stmt);
        baseVisit();
        PopNode();
        return node;
    }

    public override void VisitLabeledStatement(LabeledStatementSyntax node)
    {
        if (_labels.ContainsKey(node.Identifier.ToString()))
        {
            throw new InvalidOperationException($"Duplicate label: {node.Identifier}");
        }
        _labels[node.Identifier.ToString()] = VisitControlFlow(node, () => base.VisitLabeledStatement(node));
    }

    public override void VisitTryStatement(TryStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitTryStatement(node));
    }

    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        VisitControlFlow(node, () => base.VisitCatchClause(node));
    }

    public override void VisitFinallyClause(FinallyClauseSyntax node)
    {
        VisitControlFlow(node, () => base.VisitFinallyClause(node));
    }

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitIfStatement(node));
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitForStatement(node));
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitForEachStatement(node));
    }

    public override void VisitDoStatement(DoStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitDoStatement(node));
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitWhileStatement(node));
    }

    public override void VisitSwitchStatement(SwitchStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitSwitchStatement(node));
    }

    public override void VisitReturnStatement(ReturnStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitReturnStatement(node));
    }

    public override void VisitBreakStatement(BreakStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitBreakStatement(node));
    }

    public override void VisitContinueStatement(ContinueStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitContinueStatement(node));
    }

    public override void VisitGotoStatement(GotoStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitGotoStatement(node));
    }

    public override void VisitYieldStatement(YieldStatementSyntax node)
    {
        VisitControlFlow(node, () => base.VisitYieldStatement(node));
    }

    public void PrintTree(ControlFlowNode node, int depth = 0)
    {
        var stmt = node.Statement;
        if (stmt != null)
        {
            string line = $"{node.ShortFlags()} {stmt.LineNo().ToString().PadLeft(6)}: {new string(' ', depth * IndentSpaces)}{stmt.Title()}";
            if (stmt is GotoStatementSyntax gotoStmt && gotoStmt.Expression is IdentifierNameSyntax id && _labels.TryGetValue(id.Identifier.ToString(), out var label))
            {
                line = line.PadRight(CommentPadding);
                if (label.LineNo() > stmt.LineNo())
                {
                    line += $" // ▼ {label.LineNo()}";
                }
                else if (label.LineNo() < stmt.LineNo())
                {
                    line += $" //   {label.LineNo()} ▲";
                }
            }
            Console.WriteLine(line);
        }
        foreach (var child in node.Children)
        {
            PrintTree(child, depth + 1);
        }
    }

    public void PrintTree(int depth = 0)
    {
        PrintTree(Root, depth);
    }

    void gather_flags(ControlFlowNode node)
    {
        node.Children.ForEach(gather_flags);

        bool wasTry = false;
        switch (node.Statement)
        {
            case BreakStatementSyntax:
                wasTry = false;
                for (var pnode = node.Parent; pnode != null; pnode = pnode.Parent)
                {
                    if (pnode.Statement is TryStatementSyntax)
                        wasTry = true;

                    if (pnode.IsBreakable())
                    {
                        pnode.hasBreak = true;
                        if (wasTry)
                        {
                            node.keep = true;
                            pnode.keep = true;
                        }
                        break;
                    }
                }
                break;

            case ContinueStatementSyntax:
                wasTry = false;
                for (var pnode = node.Parent; pnode != null; pnode = pnode.Parent)
                {
                    if (pnode.Statement is TryStatementSyntax)
                        wasTry = true;

                    if (pnode.IsContinuable())
                    {
                        pnode.hasContinue = true;
                        if (wasTry)
                        {
                            node.keep = true;
                            pnode.keep = true;
                        }
                        break;
                    }
                }
                break;

            case GotoStatementSyntax gotoStmt:
                // if this goto from 'try' block outside the 'try' block => keep both 'goto' and 'label'
                var parentTry = node.FindParent(SyntaxKind.TryStatement);
                if (parentTry != null)
                {
                    switch (gotoStmt.CaseOrDefaultKeyword.Kind())
                    {
                        case SyntaxKind.DefaultKeyword:
                        case SyntaxKind.CaseKeyword:
                            // "goto case XXX"
                            // "goto default" handled below
                            var switchStmt = node.FindParent(SyntaxKind.SwitchStatement);
                            if (!object.Equals(parentTry, switchStmt.FindParent(SyntaxKind.TryStatement)))
                            {
                                node.keep = true;
                                switchStmt.keep = true;
                            }
                            break;

                        default:
                            // "goto labelXXX"
                            var labelNode = _labels[gotoStmt.Expression.ToString()];
                            if (!object.Equals(parentTry, labelNode.FindParent(SyntaxKind.TryStatement)))
                            {
                                node.keep = true;
                                labelNode.keep = true;
                            }
                            break;
                    }
                }
                break;
        }
    }

    public void Process(SyntaxNode node)
    {
        Visit(node);
        gather_flags(Root);
    }
}

