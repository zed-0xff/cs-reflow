using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using HintsDictionary = System.Collections.Generic.Dictionary<int, bool>;

public class TraceLog
{
    public List<TraceEntry> entries = new();
    public HintsDictionary hints = new();

    // returns element key if only one difference, -1 otherwise
    public static int hints_diff1(HintsDictionary hints1, HintsDictionary hints2)
    {
        if (hints1.Count != hints2.Count)
            return -1;

        int diff_idx = -1;
        foreach (var kvp in hints1)
        {
            bool value1 = kvp.Value;
            if (!hints2.TryGetValue(kvp.Key, out bool value2))
                return -1;

            if (value1 != value2)
            {
                if (diff_idx == -1)
                {
                    diff_idx = kvp.Key;
                }
                else
                {
                    return -1; // more than one difference
                }
            }
        }
        return diff_idx;
    }

    public bool CanMergeWith(TraceLog other)
    {
        return hints_diff1(hints, other.hints) != -1;
    }

    public TraceLog Merge(TraceLog other)
    {
        int hint_idx = hints_diff1(hints, other.hints);
        if (hint_idx == -1)
            throw new System.Exception("Cannot merge TraceLogs with different hints.");

        int commonStart = 0;
        while (this.entries[commonStart].stmt == other.entries[commonStart].stmt)
        {
            commonStart++;
        }
        Console.WriteLine($"[=] common start: {commonStart}, uncommon: {this.entries.Count - commonStart} vs {other.entries.Count - commonStart}");

        int commonEnd = 0;
        while (this.entries[this.entries.Count - 1 - commonEnd].stmt == other.entries[other.entries.Count - 1 - commonEnd].stmt)
        {
            commonEnd++;
        }
        Console.WriteLine($"[=] common end: {commonEnd}, uncommon: {this.entries.Count - commonEnd - commonStart} vs {other.entries.Count - commonEnd - commonStart}");

        TraceLog result = new();

        // add common head
        result.entries.AddRange(this.entries.GetRange(0, commonStart));

        // make new if/then/else
        var ifEntry = this.entries[commonStart - 1];
        var ifStmt = ifEntry.stmt as IfStatementSyntax;
        if (ifStmt == null)
            throw new System.Exception($"Expected if statement at {commonStart - 1}, got {ifEntry.stmt}");

        Console.WriteLine($"[d] old if: {ifStmt}");

        BlockSyntax thenBlock = Block(
                this.entries.GetRange(commonStart, this.entries.Count - commonEnd - commonStart)
                .Select(e => e.stmt)
                .ToArray()
                );

        BlockSyntax elseBlock = Block(
                other.entries.GetRange(commonStart, other.entries.Count - commonEnd - commonStart)
                    .Select(e => e.stmt)
                    .ToArray()
                );

        BlockSyntax thenBlock1 = hints[hint_idx] ? thenBlock : elseBlock;
        BlockSyntax elseBlock1 = hints[hint_idx] ? elseBlock : thenBlock;

        var newIfStmt = ifStmt
            .WithStatement(thenBlock1)
            .WithElse(elseBlock1.Statements.Count > 0 ? ElseClause(elseBlock1) : null);

        Console.WriteLine($"[d] new if: {newIfStmt}");
        Console.WriteLine();

        result.entries[commonStart - 1] = new TraceEntry(newIfStmt, null, ifEntry.vars);

        // add common tail
        result.entries.AddRange(this.entries.GetRange(this.entries.Count - commonEnd, commonEnd));
        result.hints = new(hints);
        result.hints.Remove(hint_idx);

        return result;
    }
}
