using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

class ControlFlowNode
{
    public StatementSyntax Statement { get; }
    public List<ControlFlowNode> Children { get; } = new();

    public ControlFlowNode(StatementSyntax statement)
    {
        Statement = statement;
    }
}

class ControlFlowTreeCollector : CSharpSyntaxWalker
{
    public ControlFlowNode Root { get; }

    const int IndentSpaces = 2;
    const int CommentPadding = 80;

    Stack<ControlFlowNode> _stack = new();
    Dictionary<string, LabeledStatementSyntax> _labels = new();

    public ControlFlowTreeCollector()
    {
        // dummy root to hold top-level statements
        Root = new ControlFlowNode(null);
        _stack.Push(Root);
    }

    private void AddNode(StatementSyntax stmt)
    {
        var node = new ControlFlowNode(stmt);
        _stack.Peek().Children.Add(node);
        _stack.Push(node);
    }

    private void PopNode()
    {
        _stack.Pop();
    }

    private void VisitControlFlow(StatementSyntax stmt, Action baseVisit)
    {
        AddNode(stmt);
        baseVisit();
        PopNode();
    }

    public override void VisitLabeledStatement(LabeledStatementSyntax node)
    {
        if (_labels.ContainsKey(node.Identifier.ToString()))
        {
            throw new InvalidOperationException($"Duplicate label: {node.Identifier}");
        }
        _labels[node.Identifier.ToString()] = node;
        VisitControlFlow(node, () => base.VisitLabeledStatement(node));
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
            string line = $"{stmt.LineNo().ToString().PadLeft(6)}: {new string(' ', depth * IndentSpaces)}{stmt.Title()}";
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
}

