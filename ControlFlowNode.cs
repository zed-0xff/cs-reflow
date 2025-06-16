using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class ControlFlowNode
{
    public CSharpSyntaxNode? Statement { get; } = null;
    public List<ControlFlowNode> Children { get; } = new();
    public ControlFlowNode? Parent { get; } = null;
    public int flags = 0;

    public const int KEEP = 1;
    public const int HAS_BREAK = 2;
    public const int HAS_CONTINUE = 4;

    public const int KEPT = 0x1000;
    public const int FORCE_INLINE = 0x2000;

    public ControlFlowNode() { }

    public ControlFlowNode(CSharpSyntaxNode? statement, ControlFlowNode? parent)
    {
        Statement = statement;
        Parent = parent;
    }

    public ControlFlowNode Clone()
    {
        ControlFlowNode clone = new(Statement, Parent);
        clone.flags = flags;
        foreach (var child in Children)
        {
            clone.Children.Add(child);
        }
        return clone;
    }

    public int LineNo() => Statement?.LineNo() ?? 0;

    public bool keep
    {
        get => (flags & KEEP) != 0;
        set => _set_flag(KEEP, value);
    }

    public bool hasBreak
    {
        get => (flags & HAS_BREAK) != 0;
        set => _set_flag(HAS_BREAK, value);
    }

    public bool hasContinue
    {
        get => (flags & HAS_CONTINUE) != 0;
        set => _set_flag(HAS_CONTINUE, value);
    }

    public bool kept
    {
        get => (flags & KEPT) != 0;
        set => _set_flag(KEPT, value);
    }

    public bool forceInline
    {
        get => (flags & FORCE_INLINE) != 0;
        set => _set_flag(FORCE_INLINE, value);
    }

    bool _set_flag(int flag, bool value)
    {
        if (value)
            flags |= flag;
        else
            flags &= ~flag;
        return value;
    }

    // XXX supposed to be called ony from root node, otherwise all references from ControlFlowUnflattener._flowDict will be broken
    public ControlFlowNode RootClone(ControlFlowNode? parent = null)
    {
        ControlFlowNode clone = new(Statement, parent);
        clone.keep = keep;
        clone.kept = kept;
        clone.hasBreak = hasBreak;
        clone.hasContinue = hasContinue;
        clone.forceInline = forceInline;
        foreach (var child in Children)
        {
            clone.Children.Add(child.RootClone(clone));
        }
        return clone;
    }

    public FlowDictionary ToDictionary()
    {
        FlowDictionary dict = new();
        if (Statement != null)
        {
            dict[Statement] = this;
        }
        foreach (var child in Children)
        {
            var childDict = child.ToDictionary();
            foreach (var kvp in childDict)
                dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }

    public ControlFlowNode? FindParent(SyntaxKind kind)
    {
        ControlFlowNode? current = this;
        while (current != null)
        {
            if (current.Statement?.Kind() == kind)
                return current;
            current = current.Parent;
        }
        return null;
    }

    public ControlFlowNode? FindParent(Func<ControlFlowNode, bool> predicate)
    {
        ControlFlowNode? current = this.Parent;
        while (current != null)
        {
            if (predicate(current))
                return current;
            current = current.Parent;
        }
        return null;
    }

    public bool IsContinuable() =>
        Statement is ForStatementSyntax ||
        Statement is ForEachStatementSyntax ||
        Statement is WhileStatementSyntax ||
        Statement is DoStatementSyntax;

    public bool IsBreakable() => IsContinuable() || Statement is SwitchStatementSyntax;

    public string ShortFlags()
    {
        char[] a = new char[] { '_', '_', '_', '_', '_' };

        if (keep) a[0] = 'K';
        if (hasBreak) a[1] = 'B';
        if (hasContinue) a[2] = 'C';
        if (forceInline) a[3] = 'I';
        if (kept) a[4] = 'k';

        string s = new string(a);
        if (s.All(c => c == '_'))
            s = s.Replace("_", " ");
        return s.PadRight(8);
    }

    public override string ToString()
    {
        return $"[{ShortFlags()}]";
    }

    const int IndentSpaces = 2;

    public void PrintTree(int depth = 0)
    {
        var stmt = Statement;
        switch (stmt)
        {
            case null:
                break;
            case LabeledStatementSyntax labelStmt:
                if (ShortFlags().Trim() != "")
                    goto default;
                Console.WriteLine(stmt.Title());
                break;
            default:
                string line = $"{ShortFlags()} {stmt.LineNo().ToString().PadLeft(6)}: {new string(' ', depth * IndentSpaces)}{stmt.Title()}";
                Console.WriteLine(line);
                break;
        }

        foreach (var child in Children)
        {
            child.PrintTree(depth + 1);
        }
    }
}
