using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using HintsDictionary = System.Collections.Generic.Dictionary<int, bool>;

public class AutoDefaultIntDict : Dictionary<int, int>
{
    public new int this[int key]
    {
        get
        {
            if (!TryGetValue(key, out var value))
            {
                value = 0;
                base[key] = value;
            }
            return value;
        }
        set => base[key] = value;
    }
}

public class ControlFlowUnflattener : SyntaxTreeProcessor, ICloneable
{
    VariableProcessor _varProcessor = new();
    HintsDictionary _flowHints = new();
    TraceLog _traceLog = new();
    Dictionary<State, int> _states = new();
    AutoDefaultIntDict _visitedLines = new();

    public int Verbosity = 0;
    public bool RemoveSwitchVars = true;
    public bool AddComments = true;

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

    class LoopException : Exception
    {
        public int lineno;
        public LoopException(string message, int lineno) : base(message)
        {
            this.lineno = lineno;
        }
    }

    public CSharpSyntaxNode GetMethod(string methodName)
    {
        var methods = _tree.GetRoot().DescendantNodes()
            .Where(n =>
                    (n is MethodDeclarationSyntax m && m.Identifier.Text == methodName) ||
                    (n is LocalFunctionStatementSyntax l && l.Identifier.Text == methodName) ||
                    (n is ConstructorDeclarationSyntax c && c.Identifier.Text == methodName)
                  )
            .ToList();

        switch (methods.Count())
        {
            case 0:
                throw new ArgumentException($"Method '{methodName}' not found.");
            case 1:
                return methods.First() as CSharpSyntaxNode;
            default:
                throw new ArgumentException($"Multiple methods with the name '{methodName}' found.");
        }
    }

    public CSharpSyntaxNode GetMethod(int lineno)
    {
        return _tree.GetRoot().DescendantNodes()
            .Where(n =>
                    (n is BaseMethodDeclarationSyntax b && b.SpanStart <= lineno && b.Span.End > lineno) ||
                    (n is LocalFunctionStatementSyntax l && l.SpanStart <= lineno && l.Span.End > lineno)
                  )
            .First() as CSharpSyntaxNode;
    }

    public BlockSyntax GetMethodBody(string methodName)
    {
        return GetMethod(methodName) switch
        {
            BaseMethodDeclarationSyntax baseMethod => baseMethod.Body,
            LocalFunctionStatementSyntax localFunc => localFunc.Body,
            null => throw new ArgumentNullException(nameof(methodName), "Method name cannot be null."),
            _ => throw new ArgumentException($"Unsupported method node type: {methodName.GetType()}", nameof(methodName))
        };
    }

    public TraceLog TraceMethod(string methodName)
    {
        try
        {
            trace_block(GetMethodBody(methodName));
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

        clone.Verbosity = Verbosity;
        clone.RemoveSwitchVars = RemoveSwitchVars;
        clone.AddComments = AddComments;

        clone._tree = _tree;     // shared, r/o

        return clone;
    }

    public ControlFlowUnflattener CloneWithHints(HintsDictionary flowHints)
    {
        var clone = (ControlFlowUnflattener)Clone();
        foreach (var hint in flowHints)
        {
            clone._flowHints[hint.Key] = hint.Value;
        }
        clone._traceLog.hints = new(clone._flowHints);
        return clone;
    }

    public Dictionary<int, string> Methods => _tree.GetRoot().DescendantNodes()
        .Where(n => n is MethodDeclarationSyntax || n is ConstructorDeclarationSyntax || n is LocalFunctionStatementSyntax)
        .ToDictionary(
                n => n.SpanStart,
                n => n is MethodDeclarationSyntax m ? m.Identifier.Text :
                n is ConstructorDeclarationSyntax c ? c.Identifier.Text :
                n is LocalFunctionStatementSyntax l ? l.Identifier.Text :
                "<unknown>"
                );

    int get_lineno(CSharpSyntaxNode stmt)
    {
        var lineno = stmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return lineno;
    }

    void trace_while(WhileStatementSyntax whileStmt)
    {
        var condition = whileStmt.Condition;
        var body = whileStmt.Statement;

        while (true)
        {
            var value = EvaluateBoolExpression(condition);

            switch (value)
            {
                case bool b:
                    if (b)
                    {
                        try
                        {
                            trace_block(body as BlockSyntax); // TODO: single-statement body
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

    void trace_do(DoStatementSyntax doStmt)
    {
        var body = doStmt.Statement;
        var condition = doStmt.Condition;

        while (true)
        {
            try
            {
                trace_block(body as BlockSyntax); // TODO: single-statement body
            }
            catch (BreakException)
            {
                return;
            }
            catch (ContinueException)
            {
                // continue jumps to condition eval
            }

            var value = EvaluateBoolExpression(condition);
            switch (value)
            {
                case bool b:
                    if (b)
                        continue;
                    else
                        return;

                default:
                    throw new NotImplementedException($"Do condition type '{condition.GetType()}' is not supported.");
            }
        }
    }

    void trace_switch(SwitchStatementSyntax switchStmt, object value)
    {
        if (value == null)
        {
            throw new NotImplementedException($"{get_lineno(switchStmt)}: Switch statement with null value.");
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
                    if (value is GotoDefaultCaseException)
                    {
                        swLabel = l;
                        break;
                    }
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
                trace_block(section.Statements, start_idx); // TODO: fallthrough
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
            catch (GotoDefaultCaseException e)
            {
                trace_switch(switchStmt, e);
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
                    trace_block(ifStmt.Statement as BlockSyntax); // TODO: single-statement body
                }
                else
                {
                    if (ifStmt.Else != null)
                    {
                        switch (ifStmt.Else.Statement)
                        {
                            case BlockSyntax block:
                                trace_block(block);
                                break;

                            case IfStatementSyntax ifStmt2:
                                trace_if(ifStmt2, EvaluateHintedExpression(ifStmt2.Condition));
                                break;

                            default:
                                throw new NotImplementedException($"Else statement type '{ifStmt.Else.Statement.GetType()}' is not supported.");
                        }
                    }
                }
                break;

            case null:
                throw new UndeterministicIfException(condition.ToString(), get_lineno(condition));

            default:
                throw new NotImplementedException($"If condition type '{value?.GetType()}' is not supported.");
        }
    }

    TryStatementSyntax convert_try(TryStatementSyntax tryStmt)
    {
        var clone = (ControlFlowUnflattener)Clone();
        var newBlock = clone.ReflowBlock(tryStmt.Block);

        var newCatches = SyntaxFactory.List(
                tryStmt.Catches.Select(c =>
                    {
                        clone = (ControlFlowUnflattener)Clone();
                        return c.WithBlock(clone.ReflowBlock(c.Block));
                    })
                );

        return tryStmt
            .WithBlock(newBlock)
            .WithCatches(newCatches);
    }

    public object EvaluateExpression(ExpressionSyntax expression)
    {
        return _varProcessor.EvaluateExpression(expression);
    }

    public VariableProcessor.Expression EvaluateExpressionEx(StatementSyntax expression)
    {
        return _varProcessor.EvaluateExpressionEx(expression);
    }

    public VariableProcessor.Expression EvaluateExpressionEx(ExpressionSyntax expression)
    {
        return _varProcessor.EvaluateExpressionEx(expression);
    }

    public object EvaluateBoolExpression(ExpressionSyntax expression)
    {
        object value = _varProcessor.EvaluateExpression(expression);
        switch (value)
        {
            case UInt32 u32:
                value = u32 != 0;
                break;
            case Int32 i32:
                value = i32 != 0;
                break;
            case UInt64 u64:
                value = u64 != 0;
                break;
            case Int64 i64:
                value = i64 != 0;
                break;
        }

        return value;
    }

    public object EvaluateHintedExpression(ExpressionSyntax expression)
    {
        if (_flowHints.TryGetValue(get_lineno(expression), out bool hint))
        {
            return hint;
        }
        return EvaluateBoolExpression(expression);
    }

    bool isSwitchVar(string varName)
    {
        return _varProcessor.VariableValues.IsSwitchVar(varName);
    }

    void setSwitchVar(string varName, bool isSwitch = true)
    {
        _varProcessor.VariableValues.SetSwitchVar(varName, isSwitch);
    }

    public void trace_block(BlockSyntax block, int start_idx = 0)
    {
        trace_block(block.Statements, start_idx);
    }

    public void trace_block(SyntaxList<StatementSyntax> statements, int start_idx = 0)
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
            bool skip = false;
            bool trace = true;

            if (stmt is LabeledStatementSyntax l0)
            {
                label = l0.Identifier.Text;
                stmt = l0.Statement;
            }

            if (Verbosity > 1)
            {
                Console.WriteLine($"[d] {get_lineno(stmt).ToString().PadLeft(6)}: {NodeTitle(stmt)}");
            }

            try
            {
                VariableProcessor.Expression ex;
                switch (stmt)
                {
                    case IfStatementSyntax ifStmt:
                        value = EvaluateHintedExpression(ifStmt.Condition);
                        if (value is not null)
                        {
                            comment = value.ToString();
                            if (!_flowHints.ContainsKey(get_lineno(ifStmt)))
                                skip = true;
                        }
                        break;

                    case ExpressionStatementSyntax:
                    case LocalDeclarationStatementSyntax:
                        ex = EvaluateExpressionEx(stmt);
                        value = ex.Result;
                        string valueStr = value?.ToString();
                        if (value is Boolean)
                            valueStr = valueStr.ToLower();

                        if (value != null && !stmt.ToString().Contains($"= {valueStr};"))
                        {
                            comment = value.ToString();
                        }

                        // TODO: move this 2 blocks to PostProcess()
                        if (ex.VarsRead.Count > 0 && ex.VarsRead.All(v => isSwitchVar(v)))
                        {
                            foreach (string v in ex.VarsWritten)
                                setSwitchVar(v);
                        }
                        if (ex.VarsWritten.Count > 0 && ex.VarsWritten.Any(v => isSwitchVar(v))) // questionable
                        {
                            foreach (string v in ex.VarsReferenced)
                                setSwitchVar(v);
                        }
                        //                        if (RemoveSwitchVars && ex.VarsWritten.Count > 0 && ex.VarsWritten.All(v => isSwitchVar(v)))
                        //                        {
                        //                            skip = true;
                        //                        }
                        break;

                    case SwitchStatementSyntax sw:
                        ex = EvaluateExpressionEx(sw.Expression);
                        value = ex.Result;
                        comment = value.ToString();
                        skip = true; // skip only if value is known
                        foreach (string varName in ex.VarsReferenced)
                        {
                            setSwitchVar(varName);
                        }
                        break;

                    case WhileStatementSyntax whileStmt:
                        if (whileStmt.ToString().Contains("switch"))
                        {
                            value = EvaluateHintedExpression(whileStmt.Condition);
                            comment = value.ToString();
                            skip = true; // skip only if value is known
                        }
                        else
                        {
                            // copy as-is
                            trace = false;
                        }
                        break;

                    case UsingStatementSyntax usingStmt:
                        value = EvaluateExpression(usingStmt.Expression);
                        comment = value.ToString();
                        break;

                    case GotoStatementSyntax:
                    case ContinueStatementSyntax:
                    case BreakStatementSyntax:
                    case BlockSyntax:
                    case DoStatementSyntax:
                        skip = true;
                        break;

                    case TryStatementSyntax tryStmt:
                        stmt = convert_try(tryStmt);
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

            int lineno = get_lineno(stmt);
            _visitedLines[lineno]++;
            if (_visitedLines[lineno] >= 1000)
            {
                throw new Exception($"Too many visits ({_visitedLines[lineno]}) to line {lineno}");
            }

            string line = NodeTitle(stmt);
            int nVisited = _visitedLines[lineno];
            if (nVisited > 1)
            {
                line = $"[{nVisited}] " + line;
            }

            if (line.Length > 99)
            {
                line = line.Substring(0, 99) + "…";
            }
            if (comment != "")
            {
                if (comment.Length > 99)
                {
                    comment = comment.Substring(0, 99) + "…";
                }
                line = line.PadRight(100) + " // " + comment;
            }

            if (Verbosity > 0)
            {
                if (skip)
                    Console.Write(ANSI_COLOR_GRAY);
                Console.Write($"{get_lineno(stmt).ToString().PadLeft(6)}: ");
                Console.Write(line);
                if (skip)
                    Console.Write(ANSI_COLOR_RESET);
                Console.WriteLine();
            }

            if (!skip)
            {
                _traceLog.entries.Add(new TraceEntry(stmt, value, _varProcessor.VariableValues, comment));
            }

            var state = new State(get_lineno(stmt), _varProcessor.VariableValues);
            if (_states.TryGetValue(state, out int idx))
            {
                string msg = $"Loop detected at line {get_lineno(stmt)}: {stmt} (state: {idx})";
                if (Verbosity > 0)
                    Console.WriteLine($"[d] {msg}");

                VarDict varValues = _varProcessor.VariableValues;
                while (idx > 0 && _traceLog.entries.Last().stmt == _traceLog.entries[idx].stmt)
                {
                    varValues = _traceLog.entries[_traceLog.entries.Count - 1].vars;
                    _traceLog.entries.RemoveAt(_traceLog.entries.Count - 1);
                    idx--;
                }

                SyntaxToken labelId;
                if (_traceLog.entries[idx].stmt is LabeledStatementSyntax labelStmt)
                {
                    labelId = labelStmt.Identifier;
                }
                else
                {
                    labelId = SyntaxFactory.Identifier($"l{get_lineno(_traceLog.entries[idx].stmt)}");
                    _traceLog.entries[idx].stmt = LabeledStatement(labelId, _traceLog.entries[idx].stmt);
                    //_traceLog.entries[idx].stmt = LabeledStatement(labelId, EmptyStatement());
                }

                _traceLog.entries.Add(
                        new TraceEntry(
                            GotoStatement(SyntaxKind.GotoStatement, IdentifierName(labelId)),
                            null,
                            varValues
                        )
                );
                throw new LoopException(msg, get_lineno(stmt));
            }
            else
            {
                _states[state] = _traceLog.entries.Count - 1;
            }

            try
            {
                switch (stmt)
                {
                    case TryStatementSyntax tryStmt:      // already handled above
                    case LocalDeclarationStatementSyntax: // already handled above
                    case ExpressionStatementSyntax:       // already handled above
                    case EmptyStatementSyntax:            // do nothing
                    case ForStatementSyntax:              // TODO: trace, copy as-is for now
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
                        if (trace)
                            trace_while(whileStmt);
                        break;

                    case DoStatementSyntax doStmt:
                        trace_do(doStmt);
                        break;

                    case IfStatementSyntax ifStmt:
                        trace_if(ifStmt, value);
                        break;

                    case BlockSyntax block:
                        trace_block(block); // TODO: local vars
                        break;

                    case UsingStatementSyntax usingStmt:
                        trace_block(usingStmt.Statement as BlockSyntax);
                        break;

                    case BreakStatementSyntax: throw new BreakException();
                    case ContinueStatementSyntax: throw new ContinueException();
                    case ReturnStatementSyntax: throw new ReturnException(null); // TODO: return value

                    default:
                        throw new NotImplementedException($"{get_lineno(stmt)}: Unhandled statement type: {stmt.GetType().ToString().Replace("Microsoft.CodeAnalysis.CSharp.Syntax.", "")}");
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

    public TraceLog TraceBlock(BlockSyntax block)
    {
        Queue<HintsDictionary> queue = new();
        queue.Enqueue(_flowHints);
        List<TraceLog> logs = new();

        while (queue.Count > 0)
        {
            HintsDictionary hints = queue.Dequeue();
            ControlFlowUnflattener clone = CloneWithHints(hints);
            if (Verbosity > 0)
                Console.WriteLine($"[d] >>> start tracing with hints: {String.Join(", ", hints.Select(kv => $"{kv.Key}:{(kv.Value ? 1 : 0)}"))}");
            try
            {
                clone.trace_block(block);
                logs.Add(clone._traceLog);
                if (Verbosity > 0)
                    Console.WriteLine($"[d] <<< end of block at line {get_lineno(clone._traceLog.entries.Last().stmt)}");
            }
            catch (ReturnException)
            {
                logs.Add(clone._traceLog);
                if (Verbosity > 0)
                    Console.WriteLine($"[d] <<< return at line {get_lineno(clone._traceLog.entries.Last().stmt)}");
            }
            catch (UndeterministicIfException e)
            {
                if (Verbosity > 0)
                    Console.WriteLine($"[d] <<< {e.Message}");

                HintsDictionary hints0 = new(hints);
                hints0[e.lineno] = false;
                queue.Enqueue(hints0);

                HintsDictionary hints1 = new(hints);
                hints1[e.lineno] = true;
                queue.Enqueue(hints1);
                continue;
            }
            catch (LoopException l)
            {
                Console.WriteLine($"[.] loop detected at line {l.lineno}");
                logs.Add(clone._traceLog);
            }

            //            // have successful trace
            //            foreach (var entry in log)
            //            {
            //                var stmt = entry.stmt;
            //                var value = entry.value;
            //                Console.WriteLine($"{get_lineno(stmt).ToString().PadLeft(6)}: {stmt} => {value}");
            //            }
        }

        if (Verbosity > 0)
            Console.WriteLine($"[=] got {logs.Count} traces: {String.Join(", ", logs.Select(l => l.entries.Count.ToString()))}");

        int nIter = 0;
        while (logs.Count > 1)
        {
            nIter++;
            if (nIter > 1000)
            {
                throw new Exception($"Too many iterations ({nIter}) while merging logs");
            }

            if (Verbosity > 0)
            {
                Console.WriteLine($"[d] logs:");
                for (int i = 0; i < logs.Count; i++)
                {
                    Console.WriteLine($"[d] - {i}: {logs[i].entries.Count} entries, hints: {String.Join(", ", logs[i].hints.Select(kv => $"{kv.Key}:{(kv.Value ? 1 : 0)}"))}");
                }
            }

            int maxHints = logs.Max(l => l.hints.Count);
            for (int i = 0; i < logs.Count - 1; i++)
            {
                if (logs[i].hints.Count != maxHints) // merge longest logs first
                    continue;

                int maxDiff = -1;
                int maxIdx = -1;
                for (int j = i + 1; j < logs.Count; j++)
                {
                    if (logs[j].hints.Count != maxHints)
                        continue;

                    // merge deeper diffs first
                    if (logs[i].CanMergeWith(logs[j]))
                    {
                        if (logs[i].diff1(logs[j]) > maxDiff)
                        {
                            maxDiff = logs[i].diff1(logs[j]);
                            maxIdx = j;
                        }
                    }
                }

                if (maxIdx != -1)
                {
                    if (Verbosity > 0)
                    {
                        Console.WriteLine($"[d] merging {i} and {maxIdx}");
                    }
                    logs[i] = logs[i].Merge(logs[maxIdx]);
                    logs.RemoveAt(maxIdx);
                    break;
                }
            }
        }

        return logs[0];
    }

    string indent(string line, int level)
    {
        string indent = new(' ', level * 4);
        return indent + line;
    }

    public string ReflowMethod(int lineno)
    {
        return ReflowMethod(GetMethod(lineno));
    }

    public string ReflowMethod(string methodName)
    {
        return ReflowMethod(GetMethod(methodName));
    }

    public BlockSyntax ReflowBlock(BlockSyntax block)
    {
        TraceLog log = TraceBlock(block);
        if (log.entries.Count > 0 && log.entries.Last().stmt.ToString() == "return;")
        {
            log.entries.RemoveAt(log.entries.Count - 1);
        }

        List<StatementSyntax> statements = new();
        foreach (var entry in log.entries)
        {
            var stmt = entry.stmt;
            var comment = entry.comment;
            if (!string.IsNullOrEmpty(comment) && AddComments)
            {
                stmt = stmt.WithTrailingTrivia(SyntaxFactory.Comment(" // " + comment));
            }
            statements.Add(stmt);
        }

        if (Verbosity > 0)
            Console.WriteLine("[d] switch vars: " + string.Join(", ", _varProcessor.VariableValues.SwitchVars()));

        PostProcessor postProcessor = new(_varProcessor);
        postProcessor.RemoveSwitchVars = RemoveSwitchVars;
        return postProcessor.PostProcessAll(SyntaxFactory.Block(statements));
    }

    public string ReflowMethod(CSharpSyntaxNode methodNode, string indentation = "", string eol = "")
    {
        BlockSyntax body = methodNode switch
        {
            BaseMethodDeclarationSyntax baseMethod => baseMethod.Body,
            LocalFunctionStatementSyntax localFunc => localFunc.Body,
            null => throw new ArgumentNullException(nameof(methodNode), "Method node cannot be null."),
            _ => throw new ArgumentException($"Unsupported method node type: {methodNode.GetType()}", nameof(methodNode))
        };

        string methodStr = methodNode.ToFullString();
        string linePrefix = "";

        if (eol == "")
        {
            eol = methodStr.Contains("\r\n") ? "\r\n" : "\n";
        }

        List<string> lines = methodStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        while (lines[0].Trim() == "")
            lines.RemoveAt(0);

        if (indentation == "")
        {
            indentation = methodStr.Contains("\n\t") ? "\t" : "    ";
        }

        while (lines[0].StartsWith(linePrefix + indentation))
        {
            linePrefix += indentation;
        }

        if (body == null)
            throw new InvalidOperationException("Method body cannot be null.");

        var newBody = ReflowBlock(body);

        SyntaxNode newMethodNode = methodNode switch
        {
            BaseMethodDeclarationSyntax baseMethod => baseMethod.WithBody(newBody),
            LocalFunctionStatementSyntax localFunc => localFunc.WithBody(newBody),
            _ => throw new ArgumentException("Unsupported method node type.", nameof(methodNode))
        };

        string result = newMethodNode.NormalizeWhitespace(eol: eol, indentation: indentation, elasticTrivia: true).ToFullString();
        if (linePrefix != "")
        {
            result = linePrefix + result.Replace(eol, eol + linePrefix);
        }
        return result;
    }

}
