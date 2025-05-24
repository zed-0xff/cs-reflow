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

    public override string ToString()
    {
        return $"<TraceLog: {entries.Count} entries, {hints.Count} hints>";
    }

    public TraceLog CutFrom(int start)
    {
        if (start < 0 || start >= entries.Count)
            throw new ArgumentOutOfRangeException(nameof(start), "Start index is out of range");

        TraceLog result = new();
        result.hints = new(hints);
        result.entries.AddRange(entries.GetRange(start, entries.Count - start));
        entries.RemoveRange(start, entries.Count - start);
        return result;
    }

    // returns element key if only one difference, -1 otherwise
    public static int hints_diff1(HintsDictionary hints1, HintsDictionary hints2)
    {
        if (hints1.Count != hints2.Count)
            return -1;

        int diff_key = -1;
        foreach (var kvp in hints1)
        {
            bool value1 = kvp.Value;
            if (!hints2.TryGetValue(kvp.Key, out bool value2))
                return -1;

            if (value1 != value2)
            {
                if (diff_key == -1)
                {
                    diff_key = kvp.Key;
                }
                else
                {
                    return -1; // more than one difference
                }
            }
        }
        return diff_key;
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

    bool eq_stmt(StatementSyntax stmt1, StatementSyntax stmt2)
    {
        if (stmt1.Equals(stmt2))
            return true;

        // converted blocks like try{}
        string s1 = PostProcessor.RemoveAllComments(stmt1).NormalizeWhitespace().ToString().Trim();
        string s2 = PostProcessor.RemoveAllComments(stmt2).NormalizeWhitespace().ToString().Trim();
        if (s1 == s2)
            return true;

        // case: label was added when a loop was detected

        if (stmt1 is LabeledStatementSyntax l1)
            return eq_stmt(l1.Statement, stmt2);

        if (stmt2 is LabeledStatementSyntax l2)
            return eq_stmt(stmt1, l2.Statement);

        return false;
    }

    // copy labels from both sources
    void add_with_labels(TraceLog log1, TraceLog log2, int start1, int start2, int n)
    {
        for (int i = 0; i < n; i++)
        {
            int pos1 = start1 + i;
            int pos2 = start2 + i;
            if (log2.entries[pos2].stmt is LabeledStatementSyntax)
                entries.Add(log2.entries[pos2]);
            else
                entries.Add(log1.entries[pos1]);
        }
    }

    public TraceLog Merge(TraceLog other, int verbosity = 0)
    {
        int hint_key = hints_diff1(hints, other.hints);
        if (hint_key == -1)
            throw new Exception($"Cannot merge TraceLogs with different hints: {hints2str(hints)} vs {hints2str(other.hints)}");

        int commonStart = 0;
        while (commonStart < this.entries.Count &&
               commonStart < other.entries.Count &&
               eq_stmt(this.entries[commonStart].stmt, other.entries[commonStart].stmt))
        {
            if (this.entries[commonStart].stmt.LineNo() == hint_key)
                break;

            commonStart++;
        }

        int commonEnd = 0;
        if (commonStart < this.entries.Count && commonStart < other.entries.Count)
        {
            while (eq_stmt(this.entries[this.entries.Count - commonEnd - 1].stmt, other.entries[other.entries.Count - commonEnd - 1].stmt))
                commonEnd++;
        }

        if (verbosity > 0)
            Console.WriteLine($"[d] TraceLog.Merge: commonStart = {commonStart}, commonEnd = {commonEnd}");

        TraceLog result = new();

        // add common head
        if (commonStart > 0)
            result.add_with_labels(this, other, 0, 0, commonStart);

        // make new if/then/else
        List<TraceEntry> ifEntries = new();
        ifEntries.Add(this.entries[commonStart]);
        ifEntries.Add(other.entries[commonStart]);

        TraceEntry? ifEntry = null;
        IfStatementSyntax? ifStmt = null;
        LabeledStatementSyntax? labelStmt = null;
        foreach (var entry in ifEntries)
        {
            if (entry.stmt is LabeledStatementSyntax labeledStmt && labeledStmt.Statement is IfStatementSyntax if2)
            {
                ifStmt = if2;
                ifEntry = entry;
                labelStmt = labeledStmt;
                break;
            }
        }

        if (ifStmt == null)
        {
            foreach (var entry in ifEntries)
            {
                if (entry.stmt is IfStatementSyntax if1)
                {
                    ifStmt = if1;
                    ifEntry = entry;
                    break;
                }
            }
        }

        if (ifStmt == null)
        {
            Print("A", Math.Max(commonStart - 10, 0), commonStart);
            Print("A", commonStart, commonStart + 1, title: false, full: (verbosity > 0));
            Console.WriteLine();

            other.Print("B", Math.Max(commonStart - 10, 0), commonStart);
            other.Print("B", commonStart, commonStart + 1, title: false, full: (verbosity > 0));
            Console.WriteLine();

            throw new Exception($"Expected if statement at {commonStart - 1}, got {ifEntry?.stmt}");
        }

        if (ifStmt.LineNo() != hint_key)
        {
            Console.WriteLine($"[d] if statement: {ifStmt.LineNo()}: {ifStmt}");
            throw new Exception($"Wrong if statement: expected {hint_key}, got {ifEntry.TitleWithLineNo()}");
        }

        BlockSyntax thenBlock = Block(
                this.entries.GetRange(commonStart + 1, this.entries.Count - commonEnd - commonStart - 1)
                .Select(e => e.stmt)
                .ToArray()
                );

        BlockSyntax elseBlock = Block(
                other.entries.GetRange(commonStart + 1, other.entries.Count - commonEnd - commonStart - 1)
                    .Select(e => e.stmt)
                    .ToArray()
                );

        BlockSyntax thenBlock1 = hints[hint_key] ? thenBlock : elseBlock;
        BlockSyntax elseBlock1 = hints[hint_key] ? elseBlock : thenBlock;

        StatementSyntax newIfStmt = ifStmt
            .WithStatement(thenBlock1)
            .WithElse(elseBlock1.Statements.Count > 0 ? ElseClause(elseBlock1) : null)
            .WithAdditionalAnnotations(
                    new SyntaxAnnotation("OriginalLineNo", ifStmt.LineNo().ToString())
                    );

        if (labelStmt != null)
        {
            newIfStmt = labelStmt
                .WithStatement(newIfStmt);
        }

        result.entries.Add(new TraceEntry(newIfStmt, null, ifEntry.vars));

        // add common tail
        if (commonEnd > 0)
            //result.entries.AddRange(this.entries.GetRange(this.entries.Count - commonEnd, commonEnd));
            result.add_with_labels(this, other, this.entries.Count - commonEnd, other.entries.Count - commonEnd, commonEnd);

        result.hints = new(hints);
        result.hints.Remove(hint_key);

        return result;
    }

    public void Print(string prefix = "", int start = 0, int end = -1, bool title = true, bool full = false)
    {
        if (title)
            Console.WriteLine($"{(prefix == "" ? "" : $"{prefix}: ")}TraceLog: {entries.Count} entries, {hints.Count} hints");

        if (end == -1)
            end = entries.Count;

        for (int i = start; i < end && i < entries.Count; i++)
        {
            string line = prefix;
            line += full ? entries[i].FormatStmtWithLineNo() : entries[i].TitleWithLineNo();
            Console.WriteLine(line);
        }
    }
}
