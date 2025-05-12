using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System;

using HintsDictionary = System.Collections.Generic.Dictionary<int, bool>;
using TraceLog = System.Collections.Generic.List<ControlFlowUnflattener.TraceEntry>;

class ControlFlowUnflattener : SyntaxTreeProcessor, ICloneable
{
    VariableProcessor _varProcessor = new();
    HintsDictionary _flowHints = new();
    TraceLog _traceLog = new();
    Dictionary<State, int> _states = new();

    public class State
    {
        readonly int lineno;
        readonly VarDict vars;

        public State(int lineno, VarDict vars)
        {
            this.lineno = lineno;
            this.vars = (VarDict)vars.Clone();
        }

        public override bool Equals(object obj)
        {
            if (obj is not State other) return false;
            return lineno == other.lineno && vars.Equals(other.vars);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(lineno, vars);
        }
    };

    public class TraceEntry
    {
        public readonly StatementSyntax stmt;
        public readonly object? value;
        public readonly VarDict vars;

        public TraceEntry(StatementSyntax stmt, object? value, VarDict vars)
        {
            this.stmt = stmt;
            this.value = value;
            this.vars = (VarDict)vars.Clone();
        }

        public override bool Equals(object obj)
        {
            if (obj is not TraceEntry other) return false;

            return stmt.Equals(other.stmt) && vars.Equals(other.vars);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    private SyntaxTree _tree;

    class ReturnException : Exception
    {
        public object? result;
        public ReturnException(object? result) : base($"return {result}")
        {
            this.result = result;
        }
    }

    class GotoException : Exception
    {
        public string label;
        public GotoException(string label) : base($"goto label '{label}' not found")
        {
            this.label = label;
        }
    }

    class GotoCaseException : Exception
    {
        public object value;
        public GotoCaseException(object value) : base($"goto case {value}")
        {
            this.value = value;
        }
    }

    class GotoDefaultCaseException : Exception
    {
        public GotoDefaultCaseException() : base("goto default case") { }
    }

    class ContinueException : Exception
    {
        public ContinueException() : base("continue") { }
    }

    class BreakException : Exception
    {
        public BreakException() : base("break") { }
    }

    class UndeterministicIfException : Exception
    {
        public int lineno;
        public UndeterministicIfException(string condition, int lineno) : base($"undeterministic if({condition}) at line {lineno}")
        {
            this.lineno = lineno;
        }
    }

    //    class LoopException : Exception
    //    {
    //        public LoopException(string message) : base(message) { }
    //    }

    public BlockSyntax GetMethodBody(string methodName)
    {
        var methods = _tree.GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == methodName);

        switch (methods.Count())
        {
            case 0:
                throw new ArgumentException($"Method '{methodName}' not found.");
            case 1:
                return methods.First().Body;
            default:
                throw new ArgumentException($"Multiple methods with the name '{methodName}' found.");
        }
    }

    public TraceLog TraceMethod(string methodName)
    {
        try
        {
            TraceBlock(GetMethodBody(methodName));
        }
        catch (ReturnException)
        {
        }
        //        catch (LoopException le)
        //        {
        //        }

        return _traceLog;
    }

    public ControlFlowUnflattener(string code, HintsDictionary flowHints = null)
    {
        if (flowHints != null)
            _flowHints = new(flowHints);
        _tree = CSharpSyntaxTree.ParseText(code);
    }

    // private empty constructor for cloning exclusively
    private ControlFlowUnflattener() { }

    public object Clone()
    {
        var clone = new ControlFlowUnflattener();
        clone._flowHints = new(_flowHints);
        clone._varProcessor = (VariableProcessor)_varProcessor.Clone();

        clone._tree = _tree;     // shared, r/o
        clone._states = _states; // shared, r/w

        return clone;
    }

    public ControlFlowUnflattener CloneWithHints(HintsDictionary flowHints)
    {
        var clone = (ControlFlowUnflattener)Clone();
        foreach (var hint in flowHints)
        {
            clone._flowHints[hint.Key] = hint.Value;
        }
        return clone;
    }

    public List<string> Methods => _tree.GetRoot().DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(m => m.Identifier.Text)
        .ToList();

    int get_lineno(CSharpSyntaxNode stmt)
    {
        var lineno = stmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return lineno;
    }

    void trace_while(WhileStatementSyntax whileStmt)
    {
        while (true)
        {
            var condition = whileStmt.Condition;
            var body = whileStmt.Statement;

            var value = EvaluateExpression(condition);

            switch (value)
            {
                case bool b:
                    if (b)
                    {
                        try
                        {
                            TraceBlock(body as BlockSyntax); // TODO: single-statement body
                        }
                        catch (BreakException)
                        {
                            return;
                        }
                        catch (ContinueException)
                        {
                            break;
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;

                default:
                    throw new NotImplementedException($"While condition type '{condition.GetType()}' is not supported.");
            }
        }
    }

    void trace_switch(SwitchStatementSyntax switchStmt, object value)
    {
        if (value == null)
        {
            throw new NotImplementedException("Switch statement with null value.");
        }
        SwitchLabelSyntax swLabel = null, defaultLabel = null;

        foreach (var s in switchStmt.Sections)
        {
            foreach (var l in s.Labels)
            {
                if (l is CaseSwitchLabelSyntax caseLabel)
                {
                    var caseValue = EvaluateExpression(caseLabel.Value);
                    if (caseValue.Equals(value))
                    {
                        swLabel = caseLabel;
                        break;
                    }
                }
                else if (l is DefaultSwitchLabelSyntax)
                {
                    defaultLabel = l;
                }
            }
        }

        if (swLabel == null)
        {
            swLabel = defaultLabel;
        }

        if (swLabel == null)
            return;

        //        Console.WriteLine($"{get_lineno(swLabel).ToString().PadLeft(6)}: {swLabel}");
        SwitchSectionSyntax section = swLabel.Parent as SwitchSectionSyntax;
        int start_idx = 0;

        while (true)
        {
            try
            {
                TraceBlock(section.Statements, start_idx); // TODO: fallthrough
                break;

            }
            catch (BreakException)
            {
                break;

            }
            catch (GotoCaseException e)
            {
                trace_switch(switchStmt, e.value);
                return;

            }
            catch (GotoException e)
            {
                // real-life example - jump from one case inside a middle of another
                //
                //    case 1:
                //        foo();
                //        goto IL_02ce;
                //    case 3:
                //        goto IL_0352;
                //
                //        IL_02ce:
                //        bar();
                string label = e.label;
                bool found = false;
                foreach (var section2 in switchStmt.Sections)
                {
                    for (int i = 0; i < section2.Statements.Count; i++)
                    {
                        var stmt = section2.Statements[i];
                        if (stmt is LabeledStatementSyntax l && l.Identifier.Text == label)
                        {
                            section = section2;
                            start_idx = i;
                            found = true;
                            break;
                        }
                    }
                }
                if (!found) throw;
            }
        }
    }

    void trace_if(IfStatementSyntax ifStmt, object value)
    {
        var condition = ifStmt.Condition;
        int lineno = get_lineno(condition);

        switch (value)
        {
            case bool b:
                if (b)
                {
                    TraceBlock(ifStmt.Statement as BlockSyntax); // TODO: single-statement body
                }
                else
                {
                    if (ifStmt.Else != null)
                    {
                        TraceBlock(ifStmt.Else.Statement as BlockSyntax);
                    }
                }
                break;
            case null:
                throw new UndeterministicIfException(condition.ToString(), get_lineno(condition));

                // trace both
                Console.WriteLine($"[d] {get_lineno(condition).ToString().PadLeft(6)}: {condition} // => true");
                TraceBlock(ifStmt.Statement as BlockSyntax);

                Console.WriteLine();
                Console.WriteLine($"[d] {get_lineno(condition).ToString().PadLeft(6)}: {condition} // => false");
                if (ifStmt.Else != null)
                {
                    TraceBlock(ifStmt.Else.Statement as BlockSyntax);
                    Console.WriteLine();
                }
                break;
            default:
                throw new NotImplementedException($"If condition type '{condition.GetType()}' is not supported.");
        }
    }

    public object EvaluateExpression(ExpressionSyntax expression)
    {
        return _varProcessor.EvaluateExpression(expression);
    }

    public object EvaluateHintedExpression(ExpressionSyntax expression)
    {
        if (_flowHints.TryGetValue(get_lineno(expression), out bool hint))
        {
            return hint;
        }
        return _varProcessor.EvaluateExpression(expression);
    }

    public void TraceBlock(BlockSyntax block, int start_idx = 0)
    {
        TraceBlock(block.Statements, start_idx);
    }

    public void TraceBlock(SyntaxList<StatementSyntax> statements, int start_idx = 0)
    {
        Dictionary<string, int> labels = new();

        foreach (var stmt in statements.OfType<LabeledStatementSyntax>())
        {
            var labelName = stmt.Identifier.Text;
            labels[stmt.Identifier.Text] = statements.IndexOf(stmt) - 1;
        }

        // Console.WriteLine($"[d] {get_lineno(statements.First())}: labels: {String.Join(", ", labels.Keys)}");

        // main loop
        for (int i = start_idx; i < statements.Count; i++)
        {
            StatementSyntax stmt = statements[i];
            string comment = "";
            string label = "";
            object? value = null;

            if (stmt is LabeledStatementSyntax l0)
            {
                label = l0.Identifier.Text;
                stmt = l0.Statement;
            }

            try
            {
                switch (stmt)
                {
                    case IfStatementSyntax ifStmt:
                        value = EvaluateHintedExpression(ifStmt.Condition);
                        if (value is bool b)
                        {
                            comment = b.ToString();
                        }
                        break;

                    case ExpressionStatementSyntax e:
                        value = EvaluateExpression(e.Expression);
                        comment = value.ToString();
                        break;

                    case LocalDeclarationStatementSyntax l:
                        _varProcessor.ProcessLocalDeclaration(l);
                        break;

                    case SwitchStatementSyntax sw:
                        value = EvaluateExpression(sw.Expression);
                        comment = value.ToString();
                        break;
                }
            }
            catch (NotSupportedException e)
            {
                comment = e.Message;
            }
            catch (VariableProcessor.VarNotFoundException /* e */)
            {
                //comment = e.Message;
            }

            string line = NodeTitle(stmt);
            //            if (label != "")
            //            {
            //                line = label + ": " + line;
            //            }
            if (comment != "")
            {
                line = line.PadRight(120) + " // " + comment;
            }
            // Console.WriteLine($"{get_lineno(stmt).ToString().PadLeft(6)}: {line}");
            //            State state = new(get_lineno(stmt), _varProcessor.VariableValues);
            _traceLog.Add(new TraceEntry(stmt, value, _varProcessor.VariableValues));

            //            if (_states.TryGetValue(state, out int x))
            //            {
            //                throw new LoopException($"Loop: {x} -> {get_lineno(stmt)}");
            //            }
            //_states[state] = get_lineno(stmt);

            try
            {
                switch (stmt)
                {
                    case LocalDeclarationStatementSyntax: // already handled above
                    case ExpressionStatementSyntax:       // already handled above
                    case TryStatementSyntax:              // copy as-is
                        break;

                    case GotoStatementSyntax gotoStmt:
                        switch (gotoStmt.CaseOrDefaultKeyword.Kind())
                        {
                            case SyntaxKind.CaseKeyword:
                                throw new GotoCaseException(EvaluateExpression(gotoStmt.Expression));

                            case SyntaxKind.DefaultKeyword:
                                throw new GotoDefaultCaseException();

                            default:
                                // goto label_123
                                if (gotoStmt.Expression is IdentifierNameSyntax labelId)
                                {
                                    if (labels.TryGetValue(labelId.Identifier.Text, out i))
                                    {
                                        // local block goto
                                        continue;
                                    }
                                    else
                                    {
                                        // jump to outer block
                                        throw new GotoException(labelId.Identifier.Text);
                                    }
                                }
                                break;
                        }
                        throw new ArgumentException($"Label '{gotoStmt.Expression}' not found. ({gotoStmt.CaseOrDefaultKeyword})");

                    case SwitchStatementSyntax switchStmt:
                        trace_switch(switchStmt, value);
                        break;

                    case WhileStatementSyntax whileStmt:
                        trace_while(whileStmt);
                        break;

                    case IfStatementSyntax ifStmt:
                        trace_if(ifStmt, value);
                        break;

                    case BlockSyntax block:
                        TraceBlock(block); // TODO: local vars
                        break;

                    case BreakStatementSyntax: throw new BreakException();
                    case ContinueStatementSyntax: throw new ContinueException();
                    case ReturnStatementSyntax: throw new ReturnException(null); // TODO: return value

                    default:
                        throw new NotImplementedException($"Unhandled statement type: {stmt.GetType().ToString().Replace("Microsoft.CodeAnalysis.CSharp.Syntax.", "")}");
                        //                    stmt = GetNextStatement(stmt, block);
                        //                    continue;
                } // switch stmt

            }
            catch (GotoException e)
            {
                //                Console.WriteLine($"[d] Goto '{e.label}' at line {get_lineno(stmt)}, labels: {String.Join(", ", labels.Keys)}");
                if (labels.TryGetValue(e.label, out i))
                {
                    // local block goto
                    continue;
                }
                else
                {
                    // jump to outer block
                    throw;
                }
            }
        }
    }

    public void ReflowBlock(BlockSyntax block)
    {
        Queue<HintsDictionary> queue = new();
        queue.Enqueue(_flowHints);
        List<TraceLog> logs = new();

        while (queue.Count > 0)
        {
            HintsDictionary hints = queue.Dequeue();
            Console.WriteLine($"[d] hints: {String.Join(", ", hints.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");

            ControlFlowUnflattener clone = CloneWithHints(hints);
            try
            {
                clone.TraceBlock(block);
                logs.Add(clone._traceLog);
            }
            catch (ReturnException)
            {
                logs.Add(clone._traceLog);
            }
            catch (UndeterministicIfException e)
            {
                HintsDictionary hints0 = new(hints);
                hints0[e.lineno] = false;
                queue.Enqueue(hints0);

                HintsDictionary hints1 = new(hints);
                hints1[e.lineno] = true;
                queue.Enqueue(hints1);
                continue;
            }

            //            // have successful trace
            //            foreach (var entry in log)
            //            {
            //                var stmt = entry.stmt;
            //                var value = entry.value;
            //                Console.WriteLine($"{get_lineno(stmt).ToString().PadLeft(6)}: {stmt} => {value}");
            //            }
        }
        Console.WriteLine($"[=] got {logs.Count} traces: {String.Join(", ", logs.Select(l => l.Count.ToString()))}");
        cmp_logs(logs[0], logs[1]);
        cmp_logs(logs[1], logs[2]);
    }

    public void ReflowMethod(string methodName)
    {
        ReflowBlock(GetMethodBody(methodName));
    }

    void cmp_logs(TraceLog log1, TraceLog log2)
    {
        int commonStart = 0;
        while (log1[commonStart].stmt == log2[commonStart].stmt)
        {
            commonStart++;
        }
        Console.WriteLine($"[=] common start: {commonStart}, uncommon: {log1.Count - commonStart} vs {log2.Count - commonStart}");

        int commonEnd = 0;
        while (log1[log1.Count - 1 - commonEnd].stmt == log2[log2.Count - 1 - commonEnd].stmt)
        {
            commonEnd++;
        }
        Console.WriteLine($"[=] common end: {commonEnd}, uncommon: {log1.Count - commonEnd - commonStart} vs {log2.Count - commonEnd - commonStart}");
    }
}
