using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using HintsDictionary = System.Collections.Generic.Dictionary<int, EHint>;
using ReturnsDictionary = System.Collections.Generic.Dictionary<string, int>;

public enum EHint
{
    None = 0,
    True = 1,
    False = 2,
    Unknown = 3
}

class CodeFmtInfo
{
    public string Indentation;
    public string LinePrefix;
    public string EOL;

    public CodeFmtInfo(string code)
    {
        EOL = code.Contains("\r\n") ? "\r\n" : "\n";
        List<string> lines = code.Substring(0, Math.Min(1024, code.Length)).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        while (lines[0].Trim() == "")
            lines.RemoveAt(0);

        Indentation = code.Contains("\n\t") ? "\t" : "    ";

        LinePrefix = "";
        while (lines[0].StartsWith(LinePrefix + Indentation))
            LinePrefix += Indentation;
    }
}

public class TaggedException : Exception
{
    public readonly string tag;
    public TaggedException(string tag, string message) : base(message)
    {
        this.tag = tag;
    }

    public override string ToString()
    {
        string str = base.ToString()
            .Replace("TaggedException: ", "")
            .Split("at System.CommandLine.Invocation.AnonymousCommandHandler.Invoke(InvocationContext context)")[0]
            .Trim();
        return $"{tag}: {str}";
    }
}

public class ControlFlowUnflattener : SyntaxTreeProcessor
{
    // used only by root Processor, i.e. in ReflowMethod() only
    HashSet<string> _keepVars = new();
    CodeFmtInfo? _fmt;

    // shared
    ControlFlowNode? _flowRoot;
    FlowDictionary _flowDict = null!;
    HashSet<string> _visitedLabels = new();
    DefaultDict<int, FlowInfo> _flowInfos = new();
    VarDB _varDB = new();
    HashSet<SyntaxNode> _condCache = new();

    // local
    TaggedLogger _logger = null!;
    FlowDictionary _localFlowDict = new();
    VarProcessor _varProcessor = null!;
    HintsDictionary _flowHints = new();
    TraceLog _traceLog = new();
    Dictionary<State, int> _states = new();
    Dictionary<int, List<State>> _condStates = new();
    DefaultDict<int, int> _visitedLines = new();
    ReturnsDictionary _parentReturns = new();
    ControlFlowUnflattener? _parent = null;
    string _status = "";

    // configuration
    const int DEFAULT_VERBOSITY = 0;

    public bool AddComments = true;
    public bool MoveDeclarations = true;
    public bool PostProcess = true;
    public bool PreProcess = true;
    public bool Reflow = true;
    public bool isClone = false;
    public string? dumpIntermediateLogs;

    public void Reset()
    {
        _condCache = new();
        _condStates = new();
        _flowHints = new();
        _flowInfos = new();
        _parentReturns = new();
        _states = new();
        _stopWatch = Stopwatch.StartNew();
        _varProcessor = new(_varDB);
        _visitedLines = new();

        _traceLog = new();
        _logger = create_logger();
    }

    private TaggedLogger create_logger() => new("ControlFlowUnflattener", prefix: $"{_traceLog.Id}: ");

    public class State
    {
        public readonly int lineno;
        public readonly VarDict vars;

        public State(int lineno, VarDict vars)
        {
            this.lineno = lineno;
            this.vars = (VarDict)vars.Clone();
        }

        public override bool Equals(object? obj)
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
        public readonly SyntaxNode? node;

        public FlowException(int lineno, string message) : base(message)
        {
            this.lineno = lineno;
        }

        public FlowException(SyntaxNode node, string message) : base(message)
        {
            this.node = node;
            this.lineno = node.LineNo();
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

    class GotoCaseExceptionBase : FlowException
    {
        public GotoCaseExceptionBase(int lineno, string message) : base(lineno, message)
        {
        }
    }

    class GotoCaseException : GotoCaseExceptionBase
    {
        public readonly object? value;
        public GotoCaseException(int lineno, object? value) : base(lineno, $"goto case {value}")
        {
            this.value = value;
        }
    }

    class GotoDefaultCaseException : GotoCaseExceptionBase
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
        public BreakException(SyntaxNode node) : base(node, "break") { }
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

    class CannotConvertSwitchException : FlowException
    {
        public CannotConvertSwitchException(SyntaxNode node, string message) : base(node, message)
        {
        }
    }

    // end of exceptions

    public void KeepVars(List<string> vars)
    {
        _keepVars = new(vars);
    }

    public void TraceVars(List<string> vars) => _varProcessor.TraceVars(vars);
    public void TraceUniqVars(List<string> vars) => _varProcessor.TraceUniqVars(vars);

    public BlockSyntax GetMethodBody(string methodName)
    {
        var body = GetMethod(methodName) switch
        {
            BaseMethodDeclarationSyntax baseMethod => baseMethod.Body,
            LocalFunctionStatementSyntax localFunc => localFunc.Body,
            null => throw new ArgumentNullException(nameof(methodName), "Method name cannot be null."),
            _ => throw new ArgumentException($"Unsupported method node type: {methodName.GetType()}", nameof(methodName))
        };

        if (body is not BlockSyntax block)
            throw new InvalidOperationException($"Method '{methodName}' body is not a block: {body?.GetType()}");

        return block;
    }

    public TraceLog TraceMethod(string methodName)
    {
        try
        {
            trace_statements_inline(GetMethodBody(methodName));
        }
        catch (ReturnException)
        {
        }

        return _traceLog;
    }

    public ControlFlowUnflattener(OrderedDictionary<string, string> codes, int verbosity = DEFAULT_VERBOSITY, HintsDictionary? flowHints = null, bool dummyClassWrap = false, bool showProgress = true)
        : base(codes, verbosity, dummyClassWrap, showProgress)
    {
        string code = codes.Last().Value;
        _fmt = new(code); // needs to be initialized without dummy class wrap

        if (flowHints is not null)
            _flowHints = new(flowHints);

        _varProcessor = new(_varDB);
        _varProcessor.Verbosity = Verbosity;
        _logger = create_logger();
    }

    // private empty constructor for cloning exclusively
    private ControlFlowUnflattener() { }

    public ControlFlowUnflattener Clone()
    {
        var clone = new ControlFlowUnflattener();
        clone.isClone = true;
        clone._logger = create_logger();
        clone._parent = this;
        clone.PreProcess = false;
        clone.PostProcess = false;
        clone._flowHints = new(_flowHints);
        clone._varProcessor = (VarProcessor)_varProcessor.Clone();

        clone.Verbosity = Verbosity;
        clone.ShowProgress = ShowProgress;
        clone.AddComments = AddComments;
        clone.ShowAnnotations = ShowAnnotations;
        clone.MoveDeclarations = MoveDeclarations;
        clone.dumpIntermediateLogs = dumpIntermediateLogs;

        clone._tree = _tree;                   // shared, r/o
        clone._flowRoot = _flowRoot;           // shared, r/o, but may have clone's own override(s)
        clone._parentReturns = _parentReturns; // shared, r/o
        clone._varDB = _varDB;                 // shared, mostly r/o, may update flags

        clone._flowDict = _flowDict;           // shared, r/w 
        clone._flowInfos = _flowInfos;         // shared, r/w
        clone._visitedLabels = _visitedLabels; // shared, r/w
        clone._condCache = _condCache; // shared, r/w

        clone._localFlowDict = _localFlowDict.Clone(); // TODO: check if this clone method is correct
        return clone;
    }

    public void SetHints(HintsDictionary flowHints)
    {
        _flowHints = new(flowHints);
    }

    private ControlFlowUnflattener WithFlowDictOverride(SyntaxNode node, Action<ControlFlowNode> action)
    {
        var nodeClone = _flowDict[node].Clone();
        action(nodeClone);
        _localFlowDict[node] = nodeClone;
        return this;
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

    void trace_while(WhileStatementSyntax whileStmt)
    {
        _logger.debug(() => $"[{_visitedLines[whileStmt.LineNo()]}] {whileStmt.TitleWithLineNo()}");

        int loop_id = whileStmt.LineNo();
        var condition = whileStmt.Condition;
        var body = whileStmt.Statement;

        while (true)
        {
            object? value;
            if (_flowHints.TryGetValue(loop_id, out EHint hintValue))
            {
                value = hintValue switch
                {
                    EHint.True => true,
                    EHint.False => false,
                    EHint.Unknown => UnknownValue.Create(),
                    _ => throw new ArgumentOutOfRangeException(nameof(hintValue), hintValue, "Unknown hint value")
                };
            }
            else
            {
                var ex = EvaluateExpressionEx(condition);
                flow_info(whileStmt).loopVars.UnionWith(ex.VarsRead);
                value = ex.Result;
            }

            // Console.WriteLine($"[d] {_traceLog.Id} trace_while: {whileStmt.TitleWithLineNo()}: value={value}");

            update_flow_info(whileStmt, value);
            switch (value)
            {
                case bool b:
                    if (b)
                    {
                        try
                        {
                            trace_statements_inline(body);
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
        var body = doStmt.Statement is BlockSyntax block ? block.Statements : SingletonList<StatementSyntax>(doStmt.Statement);
        var condition = doStmt.Condition;
        int lastCount = _traceLog.entries.Count;
        TraceEntry? lastEntry = lastCount > 0 ? _traceLog.entries[lastCount - 1] : null;

        while (true)
        {
            try
            {
                trace_statements_inline(body);
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
            log_stmt(condition, value?.ToString(), skip: true, prefix: "while ( ", suffix: " )");
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
                    //                        .WithStatement(ReflowBlock(body, log: loopLog));
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
        Logger.warn_once($"For statement is not supported yet");
        return forStmt;
    }

    StatementSyntax convert_foreach(ForEachStatementSyntax forEachStmt, ReturnsDictionary retLabels)
    {
        var clone = Clone().WithParentReturns(retLabels);
        BlockSyntax block = forEachStmt.Statement as BlockSyntax ?? Block(SingletonList(forEachStmt.Statement));
        var newBlock = clone.ReflowBlock(block);
        _varProcessor.MergeExisting(clone._varProcessor);

        return forEachStmt
            .WithStatement(newBlock);
    }

    SwitchStatementSyntax convert_switch(SwitchStatementSyntax switchStmt)
    {
        _logger.debug(() => $"{switchStmt.TitleWithLineNo()}");
        _flowDict[switchStmt].keep = true; // should be before throw

        if (_visitedLines[switchStmt.LineNo()] > 0)
            throw new CannotConvertSwitchException(switchStmt, $"Switch statement {switchStmt.TitleWithLineNo()} was already visited {_visitedLines[switchStmt.LineNo()]} times, cannot convert it.");

        foreach (var gotoStmt in switchStmt.DescendantNodes().OfType<GotoStatementSyntax>())
        {
            switch (gotoStmt.CaseOrDefaultKeyword.Kind())
            {
                case SyntaxKind.CaseKeyword:
                case SyntaxKind.DefaultKeyword:
                    if (gotoStmt.Ancestors().OfType<SwitchStatementSyntax>().FirstOrDefault() == switchStmt)
                        _flowDict[gotoStmt].keep = true;
                    break;
            }
        }

        // convert switch statement to a new one with reflowed sections
        var newSections = switchStmt.Sections.Select(s =>
        {
            var clone = Clone();
            var log = clone.TraceBlock(s.Statements);
            return s.WithStatements(log.ToSyntaxList());
        });

        return switchStmt.WithSections(List(newSections));
    }

    void trace_switch(SwitchStatementSyntax switchStmt, object? value)
    {
        _logger.debug(() => $"[{_visitedLines[switchStmt.LineNo()]}] {switchStmt.TitleWithLineNo()} with value {value}");
        if (value is VarProcessor.Expression ex)
        {
            value = ex.Result;
            flow_info(switchStmt).loopVars.UnionWith(ex.VarsRead);
        }
        if (value is not GotoDefaultCaseException)
            update_flow_info(switchStmt, value);

        if (value is null)
            throw new NotImplementedException($"Switch statement with null value: {switchStmt.TitleWithLineNo()}");

        // XXX if the switch was already visited any number of times, we need to roll all them back
        if (value is UnknownValueBase)
        {
            // Logger.error($"vars: {_varProcessor.VariableValues().VarsFromNode(switchStmt.Expression)}");
            Logger.warn_once($"switch statement with {value}: {switchStmt.TitleWithLineNo()}");
            _traceLog.entries.Add(new TraceEntry(convert_switch(switchStmt), value, _varProcessor.VariableValues()));
            return;
        }

        SwitchLabelSyntax? swLabel = null, defaultLabel = null;

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
                        if (caseValue?.GetType() != value.GetType())
                            throw new NotSupportedException($"Switch case value type mismatch: case {caseValue?.GetType()} vs actual {value.GetType()}: {caseLabel.TitleWithLineNo()}");
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
                if (swLabel is not null)
                    break;
            }
        }

        if (swLabel is null)
            swLabel = defaultLabel;

        if (swLabel is null) // no matching case or default label found
            return;

        //        Console.WriteLine($"{get_lineno(swLabel).ToString().PadLeft(6)}: {swLabel}");
        SwitchSectionSyntax? section = swLabel.Parent as SwitchSectionSyntax;
        if (section is null)
            throw new InvalidOperationException($"Switch label {swLabel.TitleWithLineNo()} is not a part of a switch section");

        int start_idx = 0;

        while (true)
        {
            try
            {
                trace_statements_inline(section.Statements, start_idx); // TODO: fallthrough
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
            catch (ReturnException)
            {
                flow_info(switchStmt).hasReturn = true;
                throw;
            }
            catch (ContinueException)
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
                            _visitedLabels.Add(label);
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

    void update_flow_info(StatementSyntax stmt, object? value)
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

    void trace_if(IfStatementSyntax ifStmt, object? value)
    {
        var condition = ifStmt.Condition;
        int lineno = ifStmt.LineNo();

        update_flow_info(ifStmt, value);

        if (value is UnknownValueBase unk)
            value = unk.Cast(TypeDB.Bool);

        switch (value)
        {
            case bool b:
                if (b)
                {
                    trace_statements_inline(ifStmt.Statement);
                }
                else if (ifStmt.Else is not null)
                {
                    trace_statements_inline(ifStmt.Else.Statement);
                }
                break;

            case UnknownValueBase:
                throw new UndeterministicIfException(condition.ToString(), lineno);

            default:
                throw new NotImplementedException($"If condition type '{value?.GetType()}' is not supported.");
        }
    }

    void trace_block(BlockSyntax block)
    {
        var start_block_marker = new TraceEntry(block.ToEmptyStmt(), null, _varProcessor.VariableValues());
        _traceLog.entries.Add(start_block_marker);

        trace_statements_inline(block); // TODO: local vars

        // if we are here then block was not interrupted by a break/continue/goto/return
        // so insert it as a block for the sake of variable scopes preservation
        int start_idx = _traceLog.entries.IndexOf(start_block_marker);
        if (start_idx < 0)
            throw new InvalidOperationException($"Start block marker not found in trace log for block {block.TitleWithLineNo()}");

        _traceLog.entries[start_idx].stmt = _traceLog.ToBlock(start_idx + 1);
        _traceLog.CutFrom(start_idx + 1);

        // var end_block_marker = new TraceEntry(
        //         EmptyStatement()
        //         .WithComment("}")
        //         .WithAdditionalAnnotations(
        //             new SyntaxAnnotation("LineNo", (block.GetLocation().GetLineSpan().EndLinePosition.Line + 1).ToString())
        //             ),
        //         null, _varProcessor.VariableValues());
        // _traceLog.entries.Add(end_block_marker);
    }

    ControlFlowUnflattener WithParentReturns(ReturnsDictionary retLabels)
    {
        _parentReturns = retLabels;
        return this;
    }

    WhileStatementSyntax convert_while(WhileStatementSyntax whileStmt, ReturnsDictionary retLabels)
    {
        _logger.debug(whileStmt.TitleWithLineNo());

        var block = whileStmt.Statement as BlockSyntax;
        if (block is null)
            block = Block(SingletonList(whileStmt.Statement));

        return whileStmt
            .WithStatement(ReflowBlock(block)); // XXX retLabels not used
    }

    // returns
    //  a) new WhileStatement if the 'while' should be kept
    //  b) null if the 'while' should be inlined
    WhileStatementSyntax? maybe_convert_while(WhileStatementSyntax whileStmt, ReturnsDictionary retLabels)
    {
        if (_localFlowDict.TryGetValue(whileStmt, out ControlFlowNode? flowNode) && flowNode.forceInline)
        {
            _logger.debug($"{whileStmt.TitleWithLineNo()} => null  [local forceInline]");
            return null;
        }

        if (_flowDict[whileStmt].forceInline)
        {
            _logger.debug($"{whileStmt.TitleWithLineNo()} => null  [global forceInline]");
            return null;
        }

        bool keep = _flowDict[whileStmt].keep;
        if (keep)
        {
            _logger.debug($"{whileStmt.TitleWithLineNo()} => keep  [cached]");
        }
        else
        {
            var clone = Clone()
                .WithFlowDictOverride(whileStmt, (node) => node.forceInline = true)
                .WithParentReturns(retLabels);
            _logger.debug($"created clone: {clone._traceLog.Id}");

            // all breaks/continues/returns need to be catched!
            var log = clone.TraceBlock(SingletonList<StatementSyntax>(whileStmt));
            keep = log.entries.FirstOrDefault() is TraceEntry te && te.stmt is LabeledStatementSyntax;
            _logger.debug($"{whileStmt.TitleWithLineNo()} => {(keep ? "keep" : "inline")}  --   {log.Id}: {log.entries.FirstOrDefault()?.stmt}");
        }

        if (keep)
        {
            _flowDict[whileStmt].keep = true;
            return convert_while(whileStmt, retLabels);
        }
        else
        {
            _flowDict[whileStmt].forceInline = true;
            return null;
        }
    }

    TryStatementSyntax convert_try(TryStatementSyntax tryStmt, ReturnsDictionary retLabels)
    {
        bool hasSingleReturnCatch = false;
        var newCatches = SyntaxFactory.List(
                tryStmt.Catches.Select(c =>
                    {
                        var clone = Clone().WithParentReturns(retLabels);
                        var newBlock = clone.ReflowBlock(c.Block);
                        hasSingleReturnCatch = (newBlock.Statements.Count == 1 && newBlock.Statements.First() is ReturnStatementSyntax);
                        _varProcessor.MergeExisting(clone._varProcessor);
                        return c.WithBlock(newBlock);
                    })
                );

        var clone = Clone().WithParentReturns(retLabels);
        var newBlock = clone.ReflowBlock(tryStmt.Block);
        // TODO: handle break/continue/goto/return

        if (newCatches.Count == 1 && hasSingleReturnCatch)
            _varProcessor.UpdateExisting(clone._varProcessor);
        else
            _varProcessor.MergeExisting(clone._varProcessor);

        FinallyClauseSyntax? newFinally = null;
        if (tryStmt.Finally is not null)
        {
            var cloneF = Clone().WithParentReturns(retLabels);
            newFinally = tryStmt.Finally.WithBlock(cloneF.ReflowBlock(tryStmt.Finally.Block));
            // 'finally' always get executed, so all variables set there overwrite existing ones
            _varProcessor.UpdateExisting(cloneF._varProcessor);
        }

        return tryStmt
            .WithBlock(newBlock)
            .WithCatches(newCatches)
            .WithFinally(newFinally);
    }

    UsingStatementSyntax convert_using(UsingStatementSyntax usingStmt, ReturnsDictionary retLabels)
    {
        var clone = Clone().WithParentReturns(retLabels);
        BlockSyntax block = usingStmt.Statement as BlockSyntax ?? Block(SingletonList(usingStmt.Statement));
        var newBlock = clone.ReflowBlock(block);
        _varProcessor.UpdateExisting(clone._varProcessor);

        return usingStmt
            .WithStatement(newBlock);
    }

    public object? EvaluateParsedString()
    {
        var root = _tree.GetRoot();
        if (PreProcess)
            root = PreProcessBlock(root);
        return _varProcessor.EvaluateParsedString(root);
    }

    public object? EvaluateExpression(ExpressionSyntax expression) => _varProcessor.EvaluateExpression(expression);
    public VarProcessor.Expression EvaluateExpressionEx(StatementSyntax expression) => _varProcessor.EvaluateExpressionEx(expression);
    public VarProcessor.Expression EvaluateExpressionEx(ExpressionSyntax expression) => _varProcessor.EvaluateExpressionEx(expression);

    public object? EvaluateBoolExpression(ExpressionSyntax expression)
    {
        var value = _varProcessor.EvaluateExpression(expression);
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
            case UnknownValueBase unk:
                value = unk.Cast(TypeDB.Bool);
                break;
        }

        return value;
    }

    public object? EvaluateHintedBoolExpression(ExpressionSyntax expression)
    {
        if (expression is AssignmentExpressionSyntax assignExpr)
        {
            throw new NotSupportedException("TBD");
        }
        if (_flowHints.TryGetValue(expression.LineNo(), out EHint hint))
        {
            switch (hint)
            {
                case EHint.True:
                    return true;
                case EHint.False:
                    return false;
                case EHint.Unknown:
                    return UnknownValue.Create();
                default:
                    throw new ArgumentOutOfRangeException(nameof(hint), hint, "Unknown hint value");
            }
        }

        return EvaluateBoolExpression(expression);
    }

    public void trace_statements_inline(StatementSyntax stmt)
    {
        if (stmt is BlockSyntax block)
        {
            trace_statements_inline(block);
        }
        else
        {
            // single-statement bodies of if/for/while/etc
            trace_statements_inline(SingletonList(stmt));
        }
    }

    public void trace_statements_inline(BlockSyntax block, int start_idx = 0)
    {
        trace_statements_inline(block.Statements, start_idx);
    }

    void log_stmt(SyntaxNode stmt, string? comment = null, bool skip = false, string prefix = "", string suffix = "")
    {
        if (Verbosity <= 0)
            return;

        string line = prefix + stmt.Title() + suffix;
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

        // get comment from stmt, set by UnusedLocalsRemover when run as pre-processor
        if (stmt is EmptyStatementSyntax && string.IsNullOrEmpty(comment))
        {
            var cmt = stmt
                .GetTrailingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia))
                .ToString();

            line += ANSI.COLOR_GRAY + cmt;
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
            line = line.PadRight(commentPadding) + ANSI.COLOR_GRAY + " // " + comment + ANSI.COLOR_RESET;
        }

        if (skip)
            Console.Write(ANSI.COLOR_GRAY);
        Console.Write(_traceLog.Id);
        Console.Write($"{stmt.LineNo().ToString().PadLeft(6)}: ");
        Console.Write(line);
        if (skip)
            Console.Write(ANSI.COLOR_RESET);
        Console.WriteLine();
        if (Verbosity > 1)
            Console.WriteLine($"    vars: {_varProcessor.VariableValues().ToString(Verbosity > 2)}");
    }

    // return: bool skip?
    bool check_flow_stmt(StatementSyntax stmt)
    {
        var flowNode = _flowDict[stmt];
        if (flowNode.keep)
        {
            flowNode.kept = true;
            return false;
        }
        else
        {
            bool keep = stmt switch
            {
                ContinueStatementSyntax _ => flowNode.FindParent(n => n.IsContinuable())!.keep,
                BreakStatementSyntax _ => flowNode.FindParent(n => n.IsBreakable())!.keep,
                _ => false
            };

            if (keep)
            {
                flowNode.kept = true;
                return false;
            }

            return true;
        }
    }

    // trace block as main flow
    public void trace_statements_inline(SyntaxList<StatementSyntax> statements, int start_idx = 0)
    {
        // scan labels
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

        // main loop
        for (int i = start_idx; i < statements.Count; i++)
        {
            VarProcessor.Expression? ex = null;
            StatementSyntax stmt = statements[i];
            string comment = "";
            object? value = UnknownValue.Create();
            bool skip = false;
            bool trace = true;

            _flowDict.TryGetValue(stmt, out var flowNode);
            flowNode ??= new();

            if (stmt is LabeledStatementSyntax l0)
            {
                _visitedLabels.Add(l0.Identifier.Text);
                stmt = l0.Statement;
                if (flowNode.keep)
                {
                    if (l0.Statement is not EmptyStatementSyntax)
                    {
                        l0 = l0.WithStatement(EmptyStatement());
                    }
                    _traceLog.entries.Add(new TraceEntry(l0, null, _varProcessor.VariableValues()));
                    flowNode.kept = true;
                }
            }
            int lineno = stmt.LineNo();

            if (Verbosity > 2)
                Console.WriteLine($"[d] {stmt.LineNo().ToString().PadLeft(6)}: {stmt.Title()}");

            try
            {
                ex = null;
                switch (stmt)
                {
                    case IfStatementSyntax ifStmt:
                        _condStates.TryAdd(lineno, new List<State>());
                        _condStates[lineno].Add(new State(lineno, _varProcessor.VariableValues()));
                        if (ifStmt.Title().Contains("calli with instance method signature not support") && !_flowHints.ContainsKey(lineno))
                            value = UnknownValue.Create();
                        else
                            value = EvaluateHintedBoolExpression(ifStmt.Condition);

                        comment = value?.ToString() ?? "";
                        if (_flowHints.ContainsKey(lineno))
                            comment += " (hint)";
                        else
                            skip = true;
                        break;

                    case ExpressionStatementSyntax:
                    case LocalDeclarationStatementSyntax:
                        // if (stmt is LocalDeclarationStatementSyntax localDecl
                        //         && _visitedLines[lineno] > 0
                        //         && localDecl.Declaration.Variables.All(v => v.Initializer is null)
                        //         && _varProcessor.HasVar(localDecl)
                        //     )
                        // {
                        //     skip = true; // skip repeating local decl
                        // }
                        ex = EvaluateExpressionEx(stmt);
                        value = ex.Result;
                        string valueStr = value?.ToString() ?? "";
                        if (value is Boolean)
                            valueStr = valueStr.ToLower();
                        comment = valueStr;
                        break;

                    case SwitchStatementSyntax sw:
                        if (_flowDict.TryGetValue(sw, out ControlFlowNode? flowNodeSw) && flowNodeSw.keep)
                        {
                            stmt = convert_switch(sw);
                            skip = false; // keep the switch, so don't skip
                            trace = false;
                        }
                        else
                        {
                            ex = EvaluateExpressionEx(sw.Expression);
                            value = ex.Result;
                            comment = value?.ToString() ?? "";
                            skip = true; // skip only if value is known
                        }
                        break;

                    case WhileStatementSyntax whileStmt:
                        // if (!_condCache.Contains(whileStmt.Condition))
                        // {
                        //     _condCache.Add(whileStmt.Condition);
                        //     foreach (int var_id in _varDB.CollectVars(whileStmt.Condition).read)
                        //         _varDB.SetLoopVar(var_id);
                        // }

                        value = EvaluateHintedBoolExpression(whileStmt.Condition);
                        comment = value?.ToString() ?? "";
                        switch (value)
                        {
                            case true:
                                WhileStatementSyntax? newWhile = maybe_convert_while(whileStmt, retLabels);
                                //Console.WriteLine($"[d] maybe_convert_while: {whileStmt.TitleWithLineNo()} => {newWhile?.TitleWithLineNo()}");
                                if (newWhile is null)
                                {
                                    skip = true;
                                    trace = true; // inline it
                                }
                                else
                                {
                                    stmt = newWhile;
                                    skip = false;
                                    trace = false;
                                }
                                break;
                            case false:
                                skip = true;
                                trace = false;
                                break;
                            default:
                                skip = false;
                                trace = false;
                                stmt = convert_while(whileStmt, retLabels);
                                break;
                        }
                        break;

                    case UsingStatementSyntax usingStmt:
                        stmt = convert_using(usingStmt, retLabels);
                        skip = false;
                        trace = false;
                        break;

                    case GotoStatementSyntax:
                    case ContinueStatementSyntax:
                    case BreakStatementSyntax:
                        trace = true; // always trace bc they should throw a FlowException
                        skip = check_flow_stmt(stmt);
                        break;

                    case BlockSyntax:
                    case DoStatementSyntax:
                        skip = true;
                        break;

                    case TryStatementSyntax tryStmt:
                        stmt = convert_try(tryStmt, retLabels);
                        break;

                    case ForEachStatementSyntax forEachStmt:
                        stmt = convert_foreach(forEachStmt, retLabels);
                        break;

                    case ForStatementSyntax forStmt:
                        stmt = convert_for(forStmt);
                        break;

                    case LocalFunctionStatementSyntax localFunc: // nested function
                        stmt = localFunc.WithBody(
                                ReflowBlock(localFunc.Body!, isMethod: true)
                                );
                        trace = false;
                        break;
                }
            }
            catch (NotSupportedException e)
            {
                comment = e.Message;
            }

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
            {
                // TODO: store more precise variablevalues, now they're stored after processing if/while/etc condition, if any,
                // but before processing the main block
                _traceLog.entries.Add(new TraceEntry(stmt, value, _varProcessor.VariableValues(), comment));
            }

            var state = new State(stmt.LineNo(), _varProcessor.VariableValues());
            if (_states.TryGetValue(state, out int idx))
            {
                throw new LoopException($"Loop detected at line {stmt.LineNo()}: \"{stmt.Title()}\" (idx: {idx}/{_traceLog.entries.Count})", stmt.LineNo(), idx);
            }
            else
            {
                _states[state] = _traceLog.entries.Count - 1;
            }

            if (!trace)
                continue;

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
                                throw new GotoCaseException(gotoStmt.LineNo(), EvaluateExpression(gotoStmt.Expression!));

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
                        trace_while(whileStmt);
                        break;

                    case DoStatementSyntax doStmt:
                        trace_do(doStmt);
                        break;

                    case IfStatementSyntax ifStmt:
                        trace_if(ifStmt, value);
                        break;

                    case BlockSyntax block:
                        trace_block(block);
                        break;

                    case UsingStatementSyntax usingStmt:
                        trace_statements_inline(usingStmt.Statement);
                        break;

                    case BreakStatementSyntax: throw new BreakException(stmt);
                    case ContinueStatementSyntax: throw new ContinueException(lineno);
                    case ReturnStatementSyntax: throw new ReturnException(lineno); // TODO: return value?

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

    public List<int> find_loop_vars(int lineno)
    {
        _logger.debug($"line {lineno}");
        List<int> loopVars = find_loop_vars(_condStates[lineno][^3..]);
        if (loopVars.Count != 0)
            return loopVars;

        var states = _condStates[lineno][^3..];
        VarDict diffVars = (VarDict)_condStates[lineno][^3].vars.Clone();
        foreach (var state in states.Skip(1))
        {
            foreach (var kv in state.vars.ReadOnlyDict)
            {
                if (diffVars.TryGetValue(kv.Key, out var val))
                {
                    if (val is null || kv.Value is null || val.Equals(kv.Value))
                        diffVars.Remove(kv.Key);
                }
            }
        }
        var emptyObject = default(object);
        foreach (var key in diffVars.ReadOnlyDict
                .Where(kv => kv.Value is null || kv.Value.Equals(emptyObject))
                .Select(kv => kv.Key)
                .ToList())
        {
            diffVars.Remove(key);
        }

        foreach (var state in states)
        {
            var stateDiffVars = state.vars.ReadOnlyDict.Where(kv => diffVars.ContainsKey(kv.Key)).ToList();
            Console.WriteLine($"[d] {state.lineno}: {String.Join(", ", stateDiffVars.Select(kv => $"{kv.Key}={kv.Value}"))}");
        }
        throw new Exception($"Loop var not found at line {lineno}");
    }

    public static List<int> find_loop_vars(List<State> states)
    {
        if (states.Count < 2)
            return new();

        // Ensure all states have the same lineno
        var lineno = states[0].lineno;
        if (states.Any(s => s.lineno != lineno))
            return new();

        // Initialize the common variable set from the first state
        var baseVars = states[0].vars.ShallowClone();

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
                if (baseVal is null || val is null || baseVal.Equals(val) /* || key == "_" */)
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

    // does not update variables nor _traceLog
    public TraceLog TraceBlock(BlockSyntax block, int start_idx = 0)
    {
        return TraceBlock(block.Statements, start_idx, block.LineNo());
    }

    string nested_statuses()
    {
        List<string> statuses = new();
        ControlFlowUnflattener? processor = this;
        while (processor is not null)
        {
            if (!string.IsNullOrEmpty(processor._status))
                statuses.Add(processor._status);
            processor = processor._parent;
        }
        statuses.Reverse();
        return String.Join(" ", statuses);
    }

    // does not update variables nor _traceLog
    public TraceLog TraceBlock(SyntaxList<StatementSyntax> statements, int start_idx = 0, int lineno = -1)
    {
        Logger.info(statements.First().TitleWithLineNo());

        if (lineno == -1)
            lineno = statements.First().LineNo();

        Queue<HintsDictionary> queue = new();
        queue.Enqueue(_flowHints);
        List<TraceLog> logs = new();
        FlowInfo flowInfo = new FlowInfo(SyntaxKind.Block, lineno);

        while (queue.Count > 0)
        {
            HintsDictionary hints = queue.Dequeue();

            while (true)
            {
                ControlFlowUnflattener clone = Clone().WithHints(hints);
                if (Verbosity > -1 && ShowProgress)
                {
                    string msg = $"[{ElapsedTime()}] tracing branches: ";
                    _status = $"{logs.Count + 1}/{logs.Count + queue.Count + 1}";
                    msg += nested_statuses();
                    if (Verbosity == 0)
                    {
                        Console.Error.Write(msg + ANSI.ERASE_TILL_EOL + "\r");
                    }
                    else
                    {
                        Console.WriteLine($"{msg} @ line {lineno} with {hints.Count} hints");
                    }
                }

                try
                {
                    clone.trace_statements_inline(statements, start_idx);
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
                catch (GotoCaseExceptionBase gotoCaseExc)
                {
                    logs.Add(clone._traceLog);
                    if (Verbosity > 0)
                        Console.WriteLine($"<<< {gotoCaseExc} from line {gotoCaseExc.lineno}\n");
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
                    hints0[e.lineno] = EHint.False;
                    queue.Enqueue(hints0);

                    HintsDictionary hints1 = new(hints);
                    hints1[e.lineno] = EHint.True;
                    queue.Enqueue(hints1);
                }
                catch (LoopException l)
                {
                    Logger.debug($"[d] {l.GetType()}: {l.Message}", "ControlFlowUnflattener.LoopException");
                    if (Verbosity > 1)
                        clone._traceLog.Print("LOOP");

                    int idx = l.idx;
                    VarDict varValues = clone._varProcessor.VariableValues();
                    while (idx >= 0 && clone._traceLog.entries.Last().stmt.IsSameStmt(clone._traceLog.entries[idx].stmt))
                    {
                        // expecting that clone._traceLog last entry always be a duplicate entry that has to be removed
                        Logger.debug(() => $"removing {clone._traceLog.entries.Last()}", "ControlFlowUnflattener.LoopException");
                        varValues = clone._traceLog.entries.Last().vars;
                        clone._traceLog.entries.RemoveAt(clone._traceLog.entries.Count - 1);
                        idx--;
                    }
                    idx++;

                    var targetStmt = clone._traceLog.entries[idx].stmt;
                    Logger.debug($"[.] loop at line {l.lineno} -> lbl_{targetStmt.LineNo()}: {targetStmt.Title()}", "ControlFlowUnflattener.LoopException");

                    SyntaxToken labelId;
                    if (clone._traceLog.entries[idx].stmt is LabeledStatementSyntax labelStmt)
                    {
                        labelId = labelStmt.Identifier;
                        _visitedLabels.Add(labelId.Text);
                    }
                    else
                    {
                        int label_lineno = targetStmt.LineNo();
                        if (label_lineno == 1 && idx > 0)
                        {
                            label_lineno = clone._traceLog.entries[idx - 1].stmt.LineNo() + 1;
                        }
                        labelId = SyntaxFactory.Identifier($"lbl_{label_lineno}");
                        clone._traceLog.entries[idx].stmt = LabeledStatement(labelId, targetStmt)
                            .WithAdditionalAnnotations(
                                    new SyntaxAnnotation("LineNo", label_lineno.ToString())
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
                    List<int> loopVars = clone.find_loop_vars(e.lineno);
                    foreach (int loopVar in loopVars)
                    {
                        if (Verbosity > 0)
                            Console.WriteLine($"[.] conditional loop at line {e.lineno}, loopVar: {loopVar}");
                        _varDB.SetLoopVar(loopVar);
                    }
                    continue;
                }
                catch (CannotConvertSwitchException e)
                {
                    // should work 2nd time because 'keep' flag is now set
                    if (e.node is not null && _flowDict.TryGetValue(e.node, out ControlFlowNode? flowNodeSw) && flowNodeSw.keep)
                        continue;
                    throw new TaggedException("ControlFlowUnflattener", $"Cannot convert switch at line {lineno}: {e.Message}");
                }
                catch (FlowException e)
                {
                    Console.Error.WriteLine($"[!] uncatched {e.GetType()} at line {lineno}: {e.Message}".Red());
                    Console.Error.WriteLine(e.StackTrace);
                    throw new TaggedException("ControlFlowUnflattener", $"Uncatched FlowException at line {lineno}: {e.Message}");
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

            if (Verbosity >= 0 && ShowProgress)
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
                        Console.WriteLine($"[d] - {i}: {logs[i]} [{String.Join(", ", logs[i].hints.Select(kv => $"{kv.Key}:{(kv.Value)}"))}]");
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
                        Console.WriteLine($"[d] merging {logs[i].Id} and {logs[maxIdx].Id} => {i}");

                    if (!string.IsNullOrEmpty(dumpIntermediateLogs))
                    {
                        logs[maxIdx].DumpTo(dumpIntermediateLogs);
                        logs[i].DumpTo(dumpIntermediateLogs);
                    }

                    try
                    {
                        logs[i] = logs[i].Merge(logs[maxIdx], Verbosity);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"[!] Error merging logs {logs[i].Id} and {logs[maxIdx].Id}");
                        throw;
                    }

                    if (!string.IsNullOrEmpty(dumpIntermediateLogs))
                        logs[i].DumpTo(dumpIntermediateLogs);

                    logs.RemoveAt(maxIdx);
                    break;
                }
            }
        }

        if (Verbosity == 0 && !isClone)
            Console.Error.WriteLine();

        if (logs.Count != 1)
            throw new Exception($"Cannot merge logs: {logs.Count} logs left");

        var finalLog = logs[0];
        finalLog.flowInfo = flowInfo;

        if (Verbosity > 1 && finalLog.entries.Count > 0)
            Console.WriteLine($"[=] final vars: {finalLog.entries[^1].vars}");
        if (Verbosity > 0)
            Console.WriteLine($"[=] final log: {finalLog}");

        if (!string.IsNullOrEmpty(dumpIntermediateLogs))
            finalLog.DumpTo(dumpIntermediateLogs);

        return finalLog;
    }

    public string ReflowMethod(int lineno) => ReflowMethod(GetMethod(lineno));
    public string ReflowMethod(string methodName) => ReflowMethod(GetMethod(methodName));

    // trace block and reflow it, returning a new BlockSyntax
    // does not alter _traceLog
    // updates _varProcessor.VariableValues
    public BlockSyntax ReflowBlock(BlockSyntax block, TraceLog? log = null, bool isMethod = false)
    {
        _logger.debug(() => $"[{_visitedLines[block.LineNo()]}] {block.TitleWithLineNo()}");

        if (block.Statements.Count == 0) // i.e. empty catch {}
            return block;

        // collect all local decls to dict name => syntaxNode
        Dictionary<string, VariableDeclarationSyntax> localDecls0 = new(StringComparer.Ordinal);
        foreach (var variable in block.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .SelectMany(ld => ld.Declaration.Variables))
        {
            var key = variable.VarID()?.Data;
            if (key is null)
                throw new ArgumentException($"variable '{variable.Identifier}' has no 'VarID' annotation data: {variable.Parent?.TitleWithLineNo()}");

            var value = variable.Parent as VariableDeclarationSyntax;
            if (localDecls0.TryGetValue(key, out var existingDecl))
            {
                if (existingDecl.Type.ToString() != value?.Type.ToString())
                    throw new ArgumentException($"Conflicting local variable declaration for '{key}' at {existingDecl.LineNo()} and {value?.LineNo()}");

                if (Verbosity > 0)
                    Console.Error.WriteLine($"[?] Duplicate variable decl: '{key}'");
                continue;
            }

            localDecls0[key] = value!;
        }

        log ??= TraceBlock(block);
        if (log.entries.Count > 0)
        {
            var lastEntry = log.entries.Last();
            if (log.entries.Count == 1 && lastEntry.stmt is ReturnStatementSyntax)
            {
                // single return statement, no variables to update
            }
            else
            {
                _varProcessor.UpdateExisting(lastEntry.vars);
            }
        }

        List<StatementSyntax> statements = new();
        foreach (var entry in log.entries)
        {
            var stmt = entry.stmt;
            var comment = entry.comment;
            while (AddComments && !string.IsNullOrEmpty(comment))
            {
                // do not make obvious comments
                if (stmt.ToString().EndsWith($" = {comment};"))
                    break;
                if ((comment == "True" || comment == "False") && stmt.Title().ToLower().Contains($"({comment.ToLower()})")) // e.g. while (true) { ... }
                    break;

                stmt = stmt.WithComment(comment);
                break;
            }

            statements.Add(stmt);
        }

        var labels = block.DescendantNodes()
            .OfType<LabeledStatementSyntax>()
            .Where(l => _flowDict.TryGetValue(l, out ControlFlowNode? flowNode) && flowNode.keep && !flowNode.kept)
            .ToList();

        foreach (var label in labels)
        {
            var lastStmt = getLastStmt(statements);
            if (lastStmt is null || !lastStmt.IsTerminal())
            {
                log.Print();
                throw new NotSupportedException($"Labeled statement at line {label.LineNo()} does not end with a terminal statement: {lastStmt}");
            }
            statements.Add(
                label.WithComment("XXX: not fully implemented")
            );
        }

        if (isMethod && statements.Count > 1 && statements.Last().ToString() == "return;")
            statements.RemoveAt(statements.Count - 1);

        BlockSyntax result = SyntaxFactory.Block(statements);

        // collect remaining decls
        var localDecls1 = result.DescendantNodesAndSelf()
            .OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(ld => ld.Declaration.Variables)
            .Select(v => v.VarID()?.Data)
            .ToHashSet();

        if (Verbosity > 2)
        {
            Console.Error.WriteLine($"[d] localDecls0: {String.Join(", ", localDecls0.Keys)}");
            Console.Error.WriteLine($"[d] localDecls1: {String.Join(", ", localDecls1)}");
        }

        // check if any local declarations are used, but were removed
        var newDecls = new List<LocalDeclarationStatementSyntax>();
        foreach (var id in result.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            var ann = id.VarID();
            if (ann is null)
            {
                if (Verbosity > 1)
                    Logger.once($"[?] Identifier '{id}' has no 'VarID' annotation"); // TODO: check why
                continue;
            }
            string? key = ann.Data;
            if (key is null)
                throw new ArgumentException($"Identifier '{id}' has no 'VarID' annotation data");

            if (localDecls1.Contains(key))
                continue;
            if (!localDecls0.TryGetValue(key, out var decl0))
                continue;

            if (Verbosity > 0)
                Console.Error.WriteLine($"[d] adding back decl \"{decl0.Type} {id};\" used at {id.Parent?.TitleWithLineNo()}");
            localDecls1.Add(key);
            newDecls.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(decl0.Type, SingletonSeparatedList(
                                VariableDeclarator(id.Identifier)
                                .WithInitializer(null)
                                .WithAdditionalAnnotations(
                                    ann,
                                    new SyntaxAnnotation("LineNo", id.LineNo().ToString())
                                    )
                                )
                            )
                        )
                    );
        }

        if (newDecls.Count > 0)
        {
            statements.InsertRange(0, newDecls);
            result = SyntaxFactory.Block(statements);
        }

        return result;
    }

    CSharpSyntaxNode? getLastStmt(object? obj)
    {
        return obj switch
        {
            List<StatementSyntax> list => list.LastOrDefault(),
            LabeledStatementSyntax labeledStmt => getLastStmt(labeledStmt.Statement),
            BlockSyntax block => getLastStmt(block.Statements.LastOrDefault()),
            StatementSyntax stmt => stmt,
            _ => throw new ArgumentException($"Unsupported type: {obj?.GetType()}", nameof(obj))
        };
    }

    public BlockSyntax GetMethodBody(SyntaxNode methodNode)
    {
        var body = methodNode switch
        {
            BaseMethodDeclarationSyntax baseMethod => baseMethod.Body,
            LocalFunctionStatementSyntax localFunc => localFunc.Body,
            null => throw new ArgumentNullException(nameof(methodNode), "Method node cannot be null."),
            _ => throw new ArgumentException($"Unsupported method node type: {methodNode.GetType()}", nameof(methodNode))
        };

        if (body is null)
            throw new InvalidOperationException("Method body cannot be null.");

        return body;
    }

    SyntaxNode PreProcessBlock(SyntaxNode node)
    {
        var node_ = new PureArithmeticsEvaluator().Visit(node);
        if (!node_!.IsEquivalentTo(node))
            node = node.ReplaceWith(node_);

        for (int i = 0; i < 100; i++)
        {
            update_progress("pre-processing" + new string('.', i));
            // remove unused vars _before_ main processing
            node_ = new UnusedLocalsRemover(_varDB, _trees, Verbosity, _keepVars).Process(node);
            if (node_.IsEquivalentTo(node))
                break;
            else
                node = node.ReplaceWith(node_);
        }

        return node;
    }

    public string ReflowMethod(SyntaxNode methodNode)
    {
        // collect all declarations from body prior to any processing
        // bc ReflowBlock() definitely may remove code blocks containing var initial declaration
        var tracker = new VarTracker(_varDB);
        var trackedMethod = tracker.Track(methodNode, _trees); // tracker has to be run on method itself (not only body) to capture method arguments
        methodNode = methodNode.ReplaceWith(trackedMethod!);

        BlockSyntax body = GetMethodBody(methodNode);

        // annotate body with original line numbers
        body = body.ReplaceWith((BlockSyntax)new OriginalLineNoAnnotator().Visit(body)!);

        if (_fmt is null)
            throw new InvalidOperationException("Formatter is not set.");

        if (PreProcess)
            body = (BlockSyntax)PreProcessBlock(body)!;

        var collector = new ControlFlowTreeCollector();
        collector.Process(body!);
        _flowRoot = collector.Root;
        _flowDict = collector.Root.ToDictionary();

        if (Reflow)
        {
            var body_ = ReflowBlock(body, isMethod: true);
            body = body.ReplaceWith(body_);
        }

        if (MoveDeclarations)
        {
            var body_ = (BlockSyntax)tracker.MoveDeclarations(body);
            if (!body_.IsEquivalentTo(body))
                body = body.ReplaceWith(body_);
        }

        while (PostProcess)
        {
            update_progress("post-processing");
            var body2 = new UnusedLocalsRemover(_varDB, _trees, Verbosity, _keepVars).Process(body) as BlockSyntax;
            PostProcessor postProcessor = new(_varDB);
            var body3 = postProcessor.PostProcessAll(body2!); // removes empty finally{} after UnusedLocalsRemover removed some locals
            if (body3.IsEquivalentTo(body))
            {
                break;
            }
            else
            {
                body = body.ReplaceWith(body3);
            }
        }

        if (body.Statements.Count > 0 && body.Statements.Last() is ReturnStatementSyntax retStmt && retStmt.Expression is null)
        {
            // remove trailing "return;"
            body = body.ReplaceWith(
                    body.WithStatements(body.Statements.RemoveAt(body.Statements.Count - 1))
                    );
        }

        if (ShowAnnotations)
        {
            var body_ = new VarTracker.ShowAnnotationsRewriter().Visit(body);
            body = body.ReplaceWith(body_!);
        }

        SyntaxNode? newMethodNode = body;
        while (newMethodNode is not null)
        {
            if (newMethodNode is BaseMethodDeclarationSyntax)
                break;
            if (newMethodNode is LocalFunctionStatementSyntax)
                break;
            newMethodNode = newMethodNode.Parent;
        }

        if (newMethodNode is null)
            throw new InvalidOperationException("Could not find method node in the syntax tree.");

        newMethodNode = newMethodNode.NormalizeWhitespace(eol: _fmt.EOL, indentation: _fmt.Indentation, elasticTrivia: true);
        string result = PostProcessor.ExpandTabs(GotoSpacer.Process(newMethodNode.ToFullString()));

        // align comments
        var newTree = CSharpSyntaxTree.ParseText(result);
        newMethodNode = new CommentAligner(newTree.GetText(), commentPadding).Visit(newTree.GetRoot());
        result = newMethodNode.ToFullString();

        if (_fmt.LinePrefix != "")
            result = _fmt.LinePrefix + result.Replace(_fmt.EOL, _fmt.EOL + _fmt.LinePrefix);
        return result;
    }

    public void DumpFlowInfos()
    {
        Console.WriteLine("Flow infos:");
        foreach (var key in _flowInfos.Keys.OrderBy(k => k))
        {
            Console.WriteLine($"{key.ToString().PadLeft(6)}: {_flowInfos[key]}");
        }
        Console.WriteLine();
        Console.WriteLine("Processed Control Flow Tree:");
        _flowRoot?.PrintTree();
    }
}
