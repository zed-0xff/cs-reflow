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

    public int diff1(TraceLog other)
    {
        return hints_diff1(hints, other.hints);
    }

    public bool CanMergeWith(TraceLog other)
    {
        return diff1(other) != -1;
    }

    string hints2str(HintsDictionary hints)
    {
        return "{" + string.Join(", ", hints.Select(kv => $"{kv.Key}:{(kv.Value ? 1 : 0)}")) + "}";
    }

    public TraceLog Merge(TraceLog other)
    {
        int hint_idx = hints_diff1(hints, other.hints);
        if (hint_idx == -1)
            throw new System.Exception($"Cannot merge TraceLogs with different hints: {hints2str(hints)} vs {hints2str(other.hints)}");

        int commonStart = 0;
        while (this.entries[commonStart].stmt == other.entries[commonStart].stmt)
        {
            commonStart++;
        }
        //        Console.WriteLine($"[=] common start: {commonStart}, uncommon: {this.entries.Count - commonStart} vs {other.entries.Count - commonStart}");

        int commonEnd = 0;
        while (this.entries[this.entries.Count - 1 - commonEnd].stmt == other.entries[other.entries.Count - 1 - commonEnd].stmt)
        {
            commonEnd++;
        }
        //        Console.WriteLine($"[=] common end: {commonEnd}, uncommon: {this.entries.Count - commonEnd - commonStart} vs {other.entries.Count - commonEnd - commonStart}");

        TraceLog result = new();

        // add common head
        if (commonStart > 0)
            result.entries.AddRange(this.entries.GetRange(0, commonStart - 1));

        // make new if/then/else
        var ifEntry = this.entries[commonStart - 1];
        var ifStmt = ifEntry.stmt as IfStatementSyntax;
        if (ifStmt == null)
        {
            for (int i = 0; i <= commonStart; i++)
            {
                if (i >= this.entries.Count)
                    break;
                Console.WriteLine($"A{i}: {this.entries[i].TitleWithLineNo()}");
            }
            Console.WriteLine();

            for (int j = 0; j <= commonStart; j++)
            {
                if (j >= other.entries.Count)
                    break;
                Console.WriteLine($"B{j}: {other.entries[j].TitleWithLineNo()}");
            }
            Console.WriteLine();

            //            Console.WriteLine($"[d] {this.entries[commonStart - 1].stmt} vs {other.entries[commonStart - 1].stmt}");
            //            Console.WriteLine($"[d] {this.entries[commonStart - 1].stmt == other.entries[commonStart - 1].stmt}");
            //            Console.WriteLine();
            //
            //            Console.WriteLine($"[d] {this.entries[commonStart].stmt == other.entries[commonStart].stmt}");
            //            Console.WriteLine($"[d] {this.entries[commonStart].stmt.ToString() == other.entries[commonStart].stmt.ToString()}");
            //            Console.WriteLine();
            //
            //            System.IO.File.WriteAllText("a.txt", this.entries[commonStart].stmt.ToString());
            //            System.IO.File.WriteAllText("b.txt", other.entries[commonStart].stmt.ToString());

            throw new System.Exception($"Expected if statement at {commonStart - 1}, got {ifEntry.stmt}");
        }

        //        Console.WriteLine($"[d] old if: {ifStmt}");

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

        //        Console.WriteLine($"[d] new if: {newIfStmt}");
        //        Console.WriteLine();

        result.entries.Add(new TraceEntry(newIfStmt, null, ifEntry.vars));

        // add common tail
        result.entries.AddRange(this.entries.GetRange(this.entries.Count - commonEnd, commonEnd));
        result.hints = new(hints);
        result.hints.Remove(hint_idx);

        return result;
    }
}
