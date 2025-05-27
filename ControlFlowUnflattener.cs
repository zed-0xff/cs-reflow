using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using HintsDictionary = System.Collections.Generic.Dictionary<int, bool>;
using ReturnsDictionary = System.Collections.Generic.Dictionary<string, int>;

public class ControlFlowUnflattener : SyntaxTreeProcessor
{
    // shared
    SyntaxTree _tree;
    DefaultDict<int, FlowInfo> _flowInfos = new();

    // local
    VariableProcessor _varProcessor = new();
    HintsDictionary _flowHints = new();
    TraceLog _traceLog = new();
    Dictionary<State, int> _states = new();
    Dictionary<int, List<State>> _condStates = new();
    DefaultDict<int, int> _visitedLines = new();
    ReturnsDictionary _parentReturns = new();
    Stopwatch _stopWatch = Stopwatch.StartNew();

    // configuration
    public int Verbosity = 0;
    public bool RemoveSwitchVars = true;
    public bool AddComments = true;
    public bool PostProcess = true;
    public bool isClone = false;
    public bool showIntermediateLogs = false;
    public int commentPadding = 90;

    public void Reset()
    {
        _varProcessor = new();
        _flowHints = new();
        _traceLog = new();
        _states = new();
        _condStates = new();
        _visitedLines = new();
        _parentReturns = new();
        _stopWatch = Stopwatch.StartNew();
    }

    class FlowInfo
    {
        public int id = -1;
        public SyntaxKind Kind = SyntaxKind.None;
        public bool hasTrue = false;
        public bool hasFalse = false;
        public bool hasUnknown = false;
        public bool hasBreak = false;
        public bool hasContinue = false;
        public bool hasReturn = false;
        public bool hasOutGoto = false;
        public bool hasInterCaseGoto = false; // goto from one case into a middle of another
        public HashSet<string> loopVars = new();
        public HashSet<object> values = new();

        public FlowInfo() { }

        public FlowInfo(SyntaxKind kind, int id = -1)
        {
            this.id = id;
            this.Kind = kind;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<FlowInfo");
            if (id != -1)
                sb.Append($" @{id}");
            if (Kind != SyntaxKind.None)
                sb.Append($" {Kind}");
            if (hasTrue)
                sb.Append(" hasTrue");
            if (hasFalse)
                sb.Append(" hasFalse");
            if (hasUnknown)
                sb.Append(" hasUnknown");
            if (hasBreak)
                sb.Append(" hasBreak");
            if (hasContinue)
                sb.Append(" hasContinue");
            if (hasOutGoto)
                sb.Append(" hasOutGoto");
            if (hasInterCaseGoto)
                sb.Append(" hasInterCaseGoto");
            if (hasReturn)
                sb.Append(" hasReturn");
            if (loopVars.Count > 0)
                sb.Append($" loopVars=[{String.Join(", ", loopVars)}]");
            if (values.Count > 0)
            {
                if (values.Count > 5)
                    sb.Append($" values=[{String.Join(", ", values.Take(5))}, …][{values.Count}]");
                else if (values.Count == 1 && values.First() is bool)
                {
                    // skip
                }
                else
                {
                    sb.Append($" values=[{String.Join(", ", values)}]");
                }
            }
            sb.Append(">");

            return sb.ToString();
        }
    }

    //    class Context
    //    {
    //        VarDict vars;
    //        int lineno;
    //        TraceLog traceLog;
    //        // TODO: stack
    //    }

    public class State
    {
        public readonly int lineno;
        public readonly VarDict vars;

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

        public override string ToString()
        {
            return $"<State lineno={lineno}, vars={vars}>";
        }
    };

    // control flow exceptions

    abstract class FlowException : Exception
    {
        public readonly int lineno;
        public FlowException(int lineno, string message) : base(message)
        {
            this.lineno = lineno;
        }
    }

    class ReturnException : FlowException
    {
        public ReturnException(int lineno) : base(lineno, "return") { }
    }

    class GotoException : FlowException
    {
        public readonly string label;
        public GotoException(int lineno, string label) : base(lineno, $"goto label '{label}'")
        {
            this.label = label;
        }
    }

    class GotoCaseException : FlowException
    {
        public readonly object value;
        public GotoCaseException(int lineno, object value) : base(lineno, $"goto case {value}")
        {
            this.value = value;
        }
    }

    class GotoDefaultCaseException : FlowException
    {
        public GotoDefaultCaseException(int lineno) : base(lineno, "goto default case")
        {
        }
    }

    class ContinueException : FlowException
    {
        public ContinueException(int lineno) : base(lineno, "continue") { }
    }

    class BreakException : FlowException
    {
        public BreakException(int lineno) : base(lineno, "break") { }
    }

    class UndeterministicIfException : FlowException
    {
        public UndeterministicIfException(string condition, int lineno) : base(lineno, $"undeterministic if({condition}) at line {lineno}")
        {
        }
    }

    class LoopException : FlowException
    {
        public readonly int idx;
        public LoopException(string message, int lineno, int idx) : base(lineno, message)
        {
            this.idx = idx;
        }
    }

    class ConditionalLoopException : FlowException
    {
        public ConditionalLoopException(string message, int lineno) : base(lineno, message) { }
    }

    // end of exceptions

    public void DropVars(List<string> vars)
    {
        foreach (var varName in vars)
        {
            setSwitchVar(varName, true);
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
            trace_block_inline(GetMethodBody(methodName));
        }
        catch (ReturnException)
        {
        }

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

    public ControlFlowUnflattener Clone()
    {
        var clone = new ControlFlowUnflattener();
        clone.isClone = true;
        clone.PostProcess = false;
        clone._flowHints = new(_flowHints);
        clone._varProcessor = (VariableProcessor)_varProcessor.Clone();

        clone.Verbosity = Verbosity;
        clone.RemoveSwitchVars = RemoveSwitchVars;
        clone.AddComments = AddComments;
        clone.showIntermediateLogs = showIntermediateLogs;

        clone._tree = _tree;     // shared, r/o
        clone._parentReturns = _parentReturns; // shared, r/o
        clone._flowInfos = _flowInfos;   // shared, r/w

        return clone;
    }

    public void SetHints(HintsDictionary flowHints)
    {
        _flowHints = new(flowHints);
    }

    public ControlFlowUnflattener WithHints(HintsDictionary flowHints)
    {
        foreach (var hint in flowHints)
        {
            _flowHints[hint.Key] = hint.Value;
        }
        _traceLog.hints = new(_flowHints);
        return this;
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

    void trace_while(WhileStatementSyntax whileStmt)
    {
        int loop_id = whileStmt.LineNo();
        var condition = whileStmt.Condition;
        var body = whileStmt.Statement;

        while (true)
        {
            object value;
            if (_flowHints.TryGetValue(loop_id, out bool hintValue))
            {
                value = hintValue;
            }
            else
            {
                var ex = EvaluateExpressionEx(condition);
                flow_info(whileStmt).loopVars.UnionWith(ex.VarsRead);
                value = ex.Result;
            }

            update_flow_info(whileStmt, value);
            switch (value)
            {
                case bool b:
                    if (b)
                    {
                        try
                        {
                            trace_block_inline(body as BlockSyntax); // TODO: single-statement body
                        }
                        catch (BreakException)
                        {
                            flow_info(whileStmt).hasBreak = true;
                            return;
                        }
                        catch (ContinueException)
                        {
                            flow_info(whileStmt).hasContinue = true;
                            break;
                        }
                        catch (ReturnException)
                        {
                            flow_info(whileStmt).hasReturn = true;
                            throw;
                        }
                        catch (GotoException)
                        {
                            flow_info(whileStmt).hasOutGoto = true;
                            throw;
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;

                default:
                    throw new NotImplementedException($"While condition result ({value?.GetType()}) '{value}' is not supported.");
            }
        }
    }

    void trace_do(DoStatementSyntax doStmt)
    {
        var body = doStmt.Statement as BlockSyntax; // TODO: single-statement body
        var condition = doStmt.Condition;
        int lastCount = _traceLog.entries.Count;
        TraceEntry? lastEntry = lastCount > 0 ? _traceLog.entries[lastCount - 1] : null;

        while (true)
        {
            try
            {
                trace_block_inline(body);
            }
            catch (BreakException)
            {
                flow_info(doStmt).hasBreak = true;
                return;
            }
            catch (ContinueException)
            {
                flow_info(doStmt).hasContinue = true;
                // continue jumps to condition eval
            }
            catch (ReturnException)
            {
                flow_info(doStmt).hasReturn = true;
                throw;
            }
            catch (GotoException)
            {
                flow_info(doStmt).hasOutGoto = true;
                throw;
            }

            var value = EvaluateBoolExpression(condition);
            log_stmt(condition, value.ToString(), skip: true, prefix: "while ( ", suffix: " )");
            update_flow_info(doStmt, value);
            switch (value)
            {
                case bool b:
                    if (b)
                        continue;
                    else
                        return;

                case UnknownValueBase unk:
                    if (lastCount > 0)
                    {
                        if (_traceLog.entries[lastCount - 1] != lastEntry)
                            throw new Exception($"Do-while loop at line {doStmt.LineNo()} has changed since last evaluation. Last entry: {lastEntry}, current entry: {_traceLog.entries.Last()}");
                    }
                    // TraceLog loopLog = _traceLog.CutFrom(lastCount);
                    //                    var newDoStmt = doStmt
                    //                        .WithStatement(ReflowBlock(body, log: loopLog))
                    //                        .WithAdditionalAnnotations(new SyntaxAnnotation("OriginalLineNo", doStmt.LineNo().ToString()));
                    //
                    //                    loopLog.entries.Last().comment = unk.ToString();
                    //                    _traceLog.entries.Add(new TraceEntry(newDoStmt, unk, _varProcessor.VariableValues));
                    // _traceLog.entries.Add(new TraceEntry(doStmt, unk, _varProcessor.VariableValues));
                    return;

                default:
                    throw new NotImplementedException($"Not supported Do condition type '{value}'");
            }
        }
    }

    StatementSyntax convert_for(ForStatementSyntax forStmt)
    {
        throw new NotImplementedException($"For statement at line {forStmt.LineNo()} is not supported yet.");
    }

    StatementSyntax convert_foreach(ForEachStatementSyntax forEachStmt)
    {
        throw new NotImplementedException($"ForEach statement at line {forEachStmt.LineNo()} is not supported yet.");
    }

    void trace_switch(SwitchStatementSyntax switchStmt, object value)
    {
        if (value is VariableProcessor.Expression ex)
        {
            value = ex.Result;
            flow_info(switchStmt).loopVars.UnionWith(ex.VarsRead);
        }
        if (value is not GotoDefaultCaseException)
            update_flow_info(switchStmt, value);

        if (value == null)
            throw new NotImplementedException($"{switchStmt.LineNo()}: Switch statement with null value.");
        if (value is UnknownValueBase)
            throw new NotImplementedException($"{switchStmt.LineNo()}: Switch statement with UnknownValue.");

        SwitchLabelSyntax swLabel = null, defaultLabel = null;

        if (value is GotoDefaultCaseException)
        {
            swLabel = switchStmt.Sections
                .SelectMany(s => s.Labels)
                .OfType<DefaultSwitchLabelSyntax>()
                .FirstOrDefault();
        }
        else
        {
            foreach (var s in switchStmt.Sections)
            {
                foreach (var l in s.Labels)
                {
                    if (l is CaseSwitchLabelSyntax caseLabel)
                    {
                        var caseValue = EvaluateExpression(caseLabel.Value);
                        if (caseValue.GetType() != value.GetType())
                            throw new NotSupportedException($"Switch case value type mismatch: case {caseValue.GetType()} vs actual {value.GetType()}");
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
                if (swLabel != null)
                    break;
            }
        }

        if (swLabel == null)
            swLabel = defaultLabel;

        if (swLabel == null) // no matching case or default label found
            return;

        //        Console.WriteLine($"{get_lineno(swLabel).ToString().PadLeft(6)}: {swLabel}");
        SwitchSectionSyntax section = swLabel.Parent as SwitchSectionSyntax;
        int start_idx = 0;

        while (true)
        {
            try
            {
                trace_block_inline(section.Statements, start_idx); // TODO: fallthrough
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
            catch (ReturnException e)
            {
                flow_info(switchStmt).hasReturn = true;
                throw;
            }
            catch (ContinueException e)
            {
                flow_info(switchStmt).hasContinue = true;
                throw;
            }
            catch (GotoException e)
            {
                // real-life example - jump from one case into a middle of another
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
                            flow_info(switchStmt).hasInterCaseGoto = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    flow_info(switchStmt).hasOutGoto = true;
                    throw;
                }
            }
        }
    }

    void update_flow_info(StatementSyntax stmt, object value)
    {
        int id = stmt.LineNo();
        var flowInfo = flow_info(stmt);

        if (_flowHints.ContainsKey(id))
            return;

        flowInfo.values.Add(value);

        switch (value)
        {
            case bool b when b:
                flowInfo.hasTrue = true;
                break;
            case bool b when !b:
                flowInfo.hasFalse = true;
                break;
            case UnknownValueBase:
                flowInfo.hasUnknown = true;
                break;
        }
    }

    FlowInfo flow_info(StatementSyntax stmt)
    {
        int id = stmt.LineNo();
        if (!_flowInfos.ContainsKey(id))
            _flowInfos[id] = new FlowInfo(kind: stmt.Kind(), id: id);
        return _flowInfos[id];
    }

    void trace_if(IfStatementSyntax ifStmt, object value)
    {
        var condition = ifStmt.Condition;
        int lineno = ifStmt.LineNo();

        update_flow_info(ifStmt, value);
        switch (value)
        {
            case bool b:
                if (b)
                {
                    trace_block_inline(ifStmt.Statement as BlockSyntax); // TODO: single-statement body
                }
                else
                {
                    if (ifStmt.Else != null)
                    {
                        switch (ifStmt.Else.Statement)
                        {
                            case BlockSyntax block:
                                trace_block_inline(block);
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

            case UnknownValueBase:
                throw new UndeterministicIfException(condition.ToString(), lineno);

            default:
                throw new NotImplementedException($"If condition type '{value?.GetType()}' is not supported.");
        }
    }

    ControlFlowUnflattener WithParentReturns(ReturnsDictionary retLabels)
    {
        _parentReturns = retLabels;
        return this;
    }

    TryStatementSyntax convert_try(TryStatementSyntax tryStmt, ReturnsDictionary retLabels)
    {
        var clone = Clone().WithParentReturns(retLabels);
        var newBlock = clone.ReflowBlock(tryStmt.Block);
        _varProcessor.UpdateExistingVars(clone._varProcessor);

        var newCatches = SyntaxFactory.List(
                tryStmt.Catches.Select(c =>
                    {
                        return c.WithBlock(Clone().WithParentReturns(retLabels).ReflowBlock(c.Block));
                    })
                );

        return tryStmt
            .WithBlock(newBlock)
            .WithCatches(newCatches)
            .WithFinally(tryStmt.Finally?.WithBlock(Clone().WithParentReturns(retLabels).ReflowBlock(tryStmt.Finally.Block)))
            .WithAdditionalAnnotations(
                    new SyntaxAnnotation("OriginalLineNo", tryStmt.LineNo().ToString())
                    );
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
        if (_flowHints.TryGetValue(expression.LineNo(), out bool hint))
            return hint;

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

    public void trace_block_inline(BlockSyntax block, int start_idx = 0)
    {
        trace_block_inline(block.Statements, start_idx);
    }

    void log_stmt(SyntaxNode stmt, string comment = "", bool skip = false, string prefix = "", string suffix = "")
    {
        if (Verbosity <= 0)
            return;

        string line = prefix + NodeTitle(stmt) + suffix;
        int nVisited = _visitedLines[stmt.LineNo()];
        if (nVisited > 1)
        {
            line = $"[{nVisited}] " + line;
        }

        int lineno = stmt.LineNo();
        if (_flowInfos.ContainsKey(lineno))
        {
            comment = String.IsNullOrEmpty(comment) ? "" : (comment + "; ");
            comment += _flowInfos[lineno].ToString();
        }

        if (!String.IsNullOrEmpty(comment))
        {
            if (line.Length > commentPadding - 1)
            {
                line = line.Substring(0, commentPadding - 1) + "…";
            }
            if (comment.Length > commentPadding - 1)
            {
                comment = comment.Substring(0, commentPadding - 1) + "…";
            }
            line = line.PadRight(commentPadding) + ANSI_COLOR_GRAY + " // " + comment + ANSI_COLOR_RESET;
        }

        if (skip)
            Console.Write(ANSI_COLOR_GRAY);
        Console.Write($"{stmt.LineNo().ToString().PadLeft(6)}: ");
        Console.Write(line);
        if (skip)
            Console.Write(ANSI_COLOR_RESET);
        Console.WriteLine();
        if (Verbosity > 1)
            Console.WriteLine($"    vars: {_varProcessor.VariableValues}");
    }

    // trace block as main flow
    public void trace_block_inline(SyntaxList<StatementSyntax> statements, int start_idx = 0)
    {
        Dictionary<string, int> labels = new();
        ReturnsDictionary retLabels = new();

        foreach (var stmt in statements.OfType<LabeledStatementSyntax>())
        {
            var labelName = stmt.Identifier.Text;
            labels[stmt.Identifier.Text] = statements.IndexOf(stmt) - 1;
            if (stmt.Statement is ReturnStatementSyntax)
            {
                retLabels[labelName] = statements.IndexOf(stmt) - 1;
            }
        }

        // Console.WriteLine($"[d] {statements.First().LineNo()}: labels: {String.Join(", ", labels.Keys)}");
        VariableProcessor.Expression? ex = null;

        // main loop
        for (int i = start_idx; i < statements.Count; i++)
        {
            StatementSyntax stmt = statements[i];
            string comment = "";
            string label = "";
            object? value = UnknownValue.Create();
            bool skip = false;
            bool trace = true;

            if (stmt is LabeledStatementSyntax l0)
            {
                label = l0.Identifier.Text;
                stmt = l0.Statement;
            }
            int lineno = stmt.LineNo();

            if (Verbosity > 2)
            {
                Console.WriteLine($"[d] {stmt.LineNo().ToString().PadLeft(6)}: {NodeTitle(stmt)}");
            }

            try
            {
                ex = null;
                switch (stmt)
                {
                    case IfStatementSyntax ifStmt:
                        _condStates.TryAdd(lineno, new List<State>());
                        _condStates[lineno].Add(new State(lineno, _varProcessor.VariableValues));
                        if (NodeTitle(ifStmt).Contains("calli with instance method signature not support") && !_flowHints.ContainsKey(lineno))
                            value = UnknownValue.Create();
                        else
                            value = EvaluateHintedExpression(ifStmt.Condition);
                        //if (value != null and value is not UnknownValue)
                        {
                            comment = value.ToString();
                            if (_flowHints.ContainsKey(lineno))
                                comment += " (hint)";
                            else
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

                        comment = valueStr;
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
                        value = EvaluateHintedExpression(whileStmt.Condition);
                        comment = value.ToString();
                        skip = true; // skip only if value is known
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
                        stmt = convert_try(tryStmt, retLabels);
                        break;

                    case ForEachStatementSyntax forEachStmt:
                        stmt = convert_foreach(forEachStmt);
                        break;

                    case ForStatementSyntax forStmt:
                        stmt = convert_for(forStmt);
                        break;
                }
            }
            catch (NotSupportedException e)
            {
                comment = e.Message;
            }
            //            catch (VariableProcessor.VarNotFoundException e2)
            //            {
            //                //                comment = e2.Message;
            //            }

            _visitedLines[lineno]++;
            if (_visitedLines[lineno] >= 100)
            {
                if (_visitedLines[lineno] >= 1000)
                {
                    throw new Exception($"Too many visits ({_visitedLines[lineno]}) to line {lineno}");
                }
                else if (_flowHints.ContainsKey(lineno))
                {
                    throw new ConditionalLoopException($"Conditional loop at line {lineno}: {_visitedLines[lineno]} visits", lineno);
                }
            }

            log_stmt(stmt, comment, skip);
            if (!skip)
                _traceLog.entries.Add(new TraceEntry(stmt, value, _varProcessor.VariableValues, comment));

            var state = new State(stmt.LineNo(), _varProcessor.VariableValues);
            if (_states.TryGetValue(state, out int idx))
            {
                throw new LoopException($"Loop detected at line {stmt.LineNo()}: {stmt} (state: {idx})", stmt.LineNo(), idx);
            }
            else
            {
                _states[state] = _traceLog.entries.Count - 1;
            }

            try
            {
                switch (stmt)
                {
                    case EmptyStatementSyntax:            // do nothing
                    case ExpressionStatementSyntax:       // already handled above
                    case ForEachStatementSyntax:          // already handled above
                    case ForStatementSyntax:              // already handled above
                    case LocalDeclarationStatementSyntax: // already handled above
                    case ThrowStatementSyntax:            // copy as-is
                    case TryStatementSyntax tryStmt:      // already handled above
                        break;

                    case GotoStatementSyntax gotoStmt:
                        switch (gotoStmt.CaseOrDefaultKeyword.Kind())
                        {
                            case SyntaxKind.CaseKeyword:
                                throw new GotoCaseException(gotoStmt.LineNo(), EvaluateExpression(gotoStmt.Expression));

                            case SyntaxKind.DefaultKeyword:
                                throw new GotoDefaultCaseException(gotoStmt.LineNo());

                            default:
                                // goto label_123
                                if (gotoStmt.Expression is IdentifierNameSyntax labelId)
                                {
                                    if (labels.TryGetValue(labelId.Identifier.Text, out i))
                                    {
                                        // local block goto
                                        continue;
                                    }
                                    else if (_parentReturns.TryGetValue(labelId.Identifier.Text, out i))
                                    {
                                        // return label
                                        continue;
                                    }
                                    else
                                    {
                                        // jump to outer block
                                        throw new GotoException(gotoStmt.LineNo(), labelId.Identifier.Text);
                                    }
                                }
                                break;
                        }
                        throw new ArgumentException($"Label '{gotoStmt.Expression}' not found. ({gotoStmt.CaseOrDefaultKeyword})");

                    case SwitchStatementSyntax switchStmt:
                        trace_switch(switchStmt, ex);
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
                        trace_block_inline(block); // TODO: local vars
                        break;

                    case UsingStatementSyntax usingStmt:
                        trace_block_inline(usingStmt.Statement as BlockSyntax);
                        break;

                    case BreakStatementSyntax: throw new BreakException(lineno);
                    case ContinueStatementSyntax: throw new ContinueException(lineno);
                    case ReturnStatementSyntax: throw new ReturnException(lineno); // TODO: return value

                    default:
                        throw new NotImplementedException($"{stmt.LineNo()}: Unhandled statement type: {stmt.GetType().ToString().Replace("Microsoft.CodeAnalysis.CSharp.Syntax.", "")}");
                } // switch stmt

            }
            catch (GotoException e)
            {
                //                Console.WriteLine($"[d] Goto '{e.label}' at line {stmt.LineNo()}, labels: {String.Join(", ", labels.Keys)}");
                if (labels.TryGetValue(e.label, out i))
                {
                    // local block goto
                    continue;
                }
                else if (_parentReturns.TryGetValue(e.label, out i))
                {
                    // return label
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

    public List<string> find_loop_vars(int lineno)
    {
        List<string> loopVars = find_loop_vars(_condStates[lineno][^3..]);
        if (loopVars.Count != 0)
            return loopVars;

        var states = _condStates[lineno][^3..];
        VarDict diffVars = (VarDict)_condStates[lineno][^3].vars.Clone();
        foreach (var state in states.Skip(1))
        {
            foreach (var kv in state.vars)
            {
                if (diffVars.TryGetValue(kv.Key, out var val))
                {
                    if (val == null || kv.Value == null || val.Equals(kv.Value))
                        diffVars.Remove(kv.Key);
                }
            }
        }
        var emptyObject = default(object);
        foreach (var kv in diffVars)
        {
            if (kv.Value == null || (kv.Value != null && kv.Equals(emptyObject)))
                diffVars.Remove(kv.Key);
        }
        foreach (var state in states)
        {
            var stateDiffVars = state.vars.Where(kv => diffVars.ContainsKey(kv.Key)).ToList();
            Console.WriteLine($"[d] {state.lineno}: {String.Join(", ", stateDiffVars.Select(kv => $"{kv.Key}={kv.Value}"))}");
        }
        throw new Exception($"Loop var not found at line {lineno}");
    }

    public static List<string> find_loop_vars(List<State> states)
    {
        if (states.Count < 2)
            return new();

        // Ensure all states have the same lineno
        var lineno = states[0].lineno;
        if (states.Any(s => s.lineno != lineno))
            return new();

        // Initialize the common variable set from the first state
        var baseVars = new Dictionary<string, object>(states[0].vars);

        foreach (var state in states.Skip(1))
        {
            foreach (var key in baseVars.Keys.ToList())
            {
                if (!state.vars.TryGetValue(key, out var val))
                {
                    baseVars.Remove(key);
                    continue;
                }

                var baseVal = baseVars[key];

                if (
                    baseVal is null || val is null ||
                    baseVal.Equals(val) || key == "_"
                )
                {
                    baseVars.Remove(key);
                }
            }
        }

        // At this point baseVars contains candidates that differ
        if (baseVars.Count == 0)
            return new();

        // Get distinct values for each key from all states
        var grouped = baseVars.Keys.ToDictionary(k => k, k => states.Select(s => s.vars[k]).Distinct().Count());

        // select only vars that have eactly count(states) distinct values
        return grouped.Where(kv => kv.Value == states.Count).Select(kv => kv.Key).ToList();
    }

    public string ElapsedTime()
    {
        return _stopWatch.Elapsed.ToString(@"mm\:ss");
    }

    public TraceLog TraceBlock(BlockSyntax block)
    {
        Queue<HintsDictionary> queue = new();
        queue.Enqueue(_flowHints);
        List<TraceLog> logs = new();
        FlowInfo flowInfo = new FlowInfo(SyntaxKind.Block, block.LineNo());

        while (queue.Count > 0)
        {
            HintsDictionary hints = queue.Dequeue();

            while (true)
            {
                ControlFlowUnflattener clone = Clone().WithHints(hints);
                if (Verbosity > -1)
                {
                    string msg = $"[{ElapsedTime()}] tracing branches: {logs.Count}/{logs.Count + queue.Count}";
                    if (Verbosity == 0)
                    {
                        if (!isClone)
                            Console.Error.Write(msg + "\r");
                    }
                    else
                    {
                        Console.WriteLine($"{msg} @ line {block.LineNo()} with {hints.Count} hints");
                    }
                }

                try
                {
                    clone.trace_block_inline(block);
                    logs.Add(clone._traceLog);
                    if (Verbosity > 0)
                        Console.WriteLine($"<<< end of block at line {clone._traceLog.entries.Last().stmt.LineNo()}\n");
                }
                catch (ReturnException retExc)
                {
                    flowInfo.hasReturn = true;
                    logs.Add(clone._traceLog);
                    if (Verbosity > 0)
                        Console.WriteLine($"<<< return at line {retExc.lineno}\n");
                }
                catch (BreakException breakExc)
                {
                    flowInfo.hasBreak = true;
                    logs.Add(clone._traceLog);
                    if (Verbosity > 0)
                        Console.WriteLine($"<<< break at line {breakExc.lineno}\n");
                }
                catch (ContinueException contExc)
                {
                    flowInfo.hasContinue = true;
                    logs.Add(clone._traceLog);
                    if (Verbosity > 0)
                        Console.WriteLine($"<<< continue at line {contExc.lineno}\n");
                }
                catch (GotoException gotoExc) // goto to outer block
                {
                    flowInfo.hasOutGoto = true;
                    logs.Add(clone._traceLog);
                    if (Verbosity > 0)
                        Console.WriteLine($"<<< goto {gotoExc.label} from line {gotoExc.lineno}\n");
                }
                catch (UndeterministicIfException e)
                {
                    if (Verbosity > 0)
                        Console.WriteLine($"<<< {e.Message}\n");

                    HintsDictionary hints0 = new(hints);
                    hints0[e.lineno] = false;
                    queue.Enqueue(hints0);

                    HintsDictionary hints1 = new(hints);
                    hints1[e.lineno] = true;
                    queue.Enqueue(hints1);
                }
                catch (LoopException l)
                {
                    int idx = l.idx;
                    VarDict varValues = clone._varProcessor.VariableValues;
                    while (idx > 0 && clone._traceLog.entries.Last().stmt == clone._traceLog.entries[idx].stmt)
                    {
                        varValues = clone._traceLog.entries.Last().vars;
                        clone._traceLog.entries.RemoveAt(clone._traceLog.entries.Count - 1);
                        idx--;
                    }

                    var targetStmt = clone._traceLog.entries[idx].stmt;
                    if (Verbosity > 0)
                        Console.WriteLine($"[.] loop at line {l.lineno} -> lbl_{targetStmt.LineNo()} (idx={l.idx}, log_len={clone._traceLog.entries.Count})");


                    SyntaxToken labelId;
                    if (clone._traceLog.entries[idx].stmt is LabeledStatementSyntax labelStmt)
                    {
                        labelId = labelStmt.Identifier;
                    }
                    else
                    {
                        int label_lineno = targetStmt.LineNo();
                        if (label_lineno == 1 && idx > 0)
                        {
                            label_lineno = clone._traceLog.entries[idx - 1].stmt.LineNo() + 1;
                        }
                        labelId = SyntaxFactory.Identifier($"lbl_{label_lineno}");
                        targetStmt = targetStmt
                            .WithAdditionalAnnotations(
                                    new SyntaxAnnotation("OriginalLineNo", label_lineno.ToString())
                                    );
                        clone._traceLog.entries[idx].stmt = LabeledStatement(labelId, targetStmt)
                            .WithAdditionalAnnotations(
                                    new SyntaxAnnotation("OriginalLineNo", label_lineno.ToString())
                                    );
                    }

                    clone._traceLog.entries.Add(
                            new TraceEntry(
                                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(labelId)),
                                null,
                                varValues
                                )
                            );

                    logs.Add(clone._traceLog);
                }
                catch (ConditionalLoopException e)
                {
                    List<string> loopVars = clone.find_loop_vars(e.lineno);
                    foreach (var loopVar in loopVars)
                    {
                        if (Verbosity > 0)
                            Console.WriteLine($"[.] conditional loop at line {e.lineno}, loopVar: {loopVar}");
                        _varProcessor.VariableValues.SetLoopVar(loopVar);
                    }
                    continue;
                }

                break;
            }
        }

        if (Verbosity == 0 && !isClone)
            Console.Error.WriteLine();

        if (Verbosity > 0)
            Console.WriteLine($"[=] got {logs.Count} traces: {String.Join(", ", logs.Select(l => l.entries.Count.ToString()))}");

        int nIter = 0;
        var nLogs = logs.Count;
        int nPrevLogs = logs.Count + 1;
        while (logs.Count < nPrevLogs)
        {
            nPrevLogs = logs.Count;
            nIter++;

            if (Verbosity >= 0)
            {
                string msg = $"[{ElapsedTime()}] merging trace logs: {nIter}/{nLogs}          ";
                if (Verbosity == 0)
                {
                    if (!isClone)
                        Console.Error.Write(msg + "\r");
                }
                else
                {
                    Console.WriteLine($"[d] logs:");
                    for (int i = 0; i < logs.Count; i++)
                    {
                        Console.WriteLine($"[d] - {i}: {logs[i].entries.Count} entries, hints: [{String.Join(", ", logs[i].hints.Select(kv => $"{kv.Key}:{(kv.Value ? 1 : 0)}"))}]");
                    }
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
                        var diff1 = logs[i].diff1(logs[j]);
                        if (logs[i].hints.Keys.ToList().IndexOf(diff1) > maxDiff)
                        {
                            maxDiff = logs[i].hints.Keys.ToList().IndexOf(diff1);
                            maxIdx = j;
                        }
                    }
                }

                if (maxIdx != -1)
                {
                    if (Verbosity > 0)
                    {
                        Console.WriteLine($"[d] merging {i} and {maxIdx} => {i}");
                        if (Verbosity > 2)
                        {
                            foreach (var entry in logs[i].entries)
                            {
                                Console.WriteLine($"[d] A{(Verbosity > 3 ? entry.StmtWithLineNo() : entry.TitleWithLineNo())}");
                            }
                            Console.WriteLine();

                            foreach (var entry in logs[maxIdx].entries)
                            {
                                Console.WriteLine($"[d] B{(Verbosity > 3 ? entry.StmtWithLineNo() : entry.TitleWithLineNo())}");
                            }
                            Console.WriteLine();
                        }
                    }

                    if (showIntermediateLogs)
                    {
                        logs[maxIdx].Print("A");
                        logs[i].Print("B");
                    }

                    logs[i] = logs[i].Merge(logs[maxIdx], Verbosity);

                    if (showIntermediateLogs)
                        logs[i].Print("C");

                    logs.RemoveAt(maxIdx);
                    break;
                }
            }
        }

        if (Verbosity == 0 && !isClone)
            Console.Error.WriteLine();

        if (logs.Count != 1)
            throw new Exception($"Cannot merge logs: {logs.Count} logs left");

        if (Verbosity > 0)
            Console.WriteLine($"[=] final log: {logs[0]}");

        return logs[0];
    }

    public string ReflowMethod(int lineno)
    {
        return ReflowMethod(GetMethod(lineno));
    }

    public string ReflowMethod(string methodName)
    {
        return ReflowMethod(GetMethod(methodName));
    }

    // trace block and reflow it, returning a new BlockSyntax
    // does not alter _traceLog
    public BlockSyntax ReflowBlock(BlockSyntax block, TraceLog? log = null, bool isMethod = false)
    {
        if (block.Statements.Count == 0) // i.e. empty catch {}
            return block;

        log ??= TraceBlock(block);
        if (isMethod && log.entries.Count > 0 && log.entries.Last().stmt.ToString() == "return;")
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

        BlockSyntax result = SyntaxFactory.Block(statements);
        return result;
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

        var newBody = ReflowBlock(body, isMethod: true);
        if (PostProcess)
        {
            if (Verbosity >= 0)
            {
                string msg = $"[{ElapsedTime()}] post-processing ..";
                if (Verbosity == 0)
                {
                    Console.Error.WriteLine(msg);
                }
                else
                {
                    Console.WriteLine(msg);
                }
            }
            PostProcessor postProcessor = new(_varProcessor);
            postProcessor.RemoveSwitchVars = RemoveSwitchVars;
            newBody = postProcessor.PostProcessAll(newBody);
        }

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

    public void DumpFlowInfos()
    {
        Console.WriteLine("Flow infos:");
        foreach (var key in _flowInfos.Keys.OrderBy(k => k))
        {
            Console.WriteLine($"{key.ToString().PadLeft(6)}: {_flowInfos[key]}");
        }
    }
}
