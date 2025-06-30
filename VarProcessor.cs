using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Runtime.CompilerServices;

public partial class VarProcessor : ICloneable
{
    public static Dictionary<string, object?> Constants { get; private set; } = new();
    public int Verbosity = 0;

    public readonly VarDB _varDB;
    VarDict _varDict;
    Dictionary<string, bool> _traceVars = new(); // value is true if trace only unique
    Dictionary<int, HashSet<object?>> _uniqValues = new(); // shared, r/w

    static readonly TaggedLogger _logger = new("VarProcessor");

    public VarProcessor(VarDB db, int verbosity = 0, VarDict? varDict = null) // vars arg is for tests
    {
        _varDB = db;
        _varDict = varDict ?? new VarDict(db);
        Verbosity = verbosity;
    }

    static VarProcessor()
    {
        Constants["string.Empty"] = string.Empty;
        Constants["int.MinValue"] = int.MinValue;
        Constants["int.MaxValue"] = int.MaxValue;
        Constants["uint.MinValue"] = uint.MinValue;
        Constants["uint.MaxValue"] = uint.MaxValue;

        Constants["Png_a7cb.BZh"] = 0x00685a42;
        Constants["Png_e0d5.IDAT"] = 0x54414449;
        Constants["Png_e0d5.IEND"] = 0x444e4549;
        Constants["Png_e0d5.IHDR"] = 0x52444849;
        Constants["Png_e0d5.PLTE"] = 0x45544c50;
        Constants["Png_e0d5.QRR"] = 0x00525251;
        Constants["Png_e0d5.tRNS"] = 0x534e5274;

        Constants["Structs_a7cb.BZh"] = 0x00685a42;
        Constants["Structs_e0d5.IDAT"] = 0x54414449;
        Constants["Structs_e0d5.IEND"] = 0x444e4549;
        Constants["Structs_e0d5.IHDR"] = 0x52444849;
        Constants["Structs_e0d5.PLTE"] = 0x45544c50;
        Constants["Structs_e0d5.QRR"] = 0x00525251;
        Constants["Structs_e0d5.tRNS"] = 0x534e5274;

        Constants["Type.EmptyTypes.LongLength"] = Type.EmptyTypes.LongLength;
    }

    public object Clone()
    {
        var clonedProcessor = new VarProcessor(_varDB, Verbosity);
        clonedProcessor._varDict = (VarDict)this._varDict.Clone();
        clonedProcessor._traceVars = this._traceVars; // shared, r/o
        clonedProcessor._uniqValues = this._uniqValues; // shared, r/w
        return clonedProcessor;
    }

    class TraceScope : IDisposable
    {
        readonly VarProcessor _processor;
        readonly SyntaxNode? _node;
        readonly string _caller;
        readonly Dictionary<int, object?>? _original;
        readonly Dictionary<int, bool> _resolvedTraceVars = new();

        static readonly TaggedLogger _logger = new("TraceScope");

        public TraceScope(VarProcessor processor, SyntaxNode? node, [CallerMemberName] string caller = "")
        {
            _caller = caller;
            _processor = processor;

            if (processor._traceVars.Count > 0)
            {
                _node = node;
                _original = new();
                foreach (var kvp in processor._varDict.ReadOnlyDict)
                {
                    string varName = processor._varDB[kvp.Key].Name;
                    if (processor._traceVars.TryGetValue(varName, out var isUniq))
                    {
                        _original[kvp.Key] = kvp.Value;
                        _resolvedTraceVars[kvp.Key] = isUniq;
                        _logger.debug($"varName: {varName}, key: {kvp.Key}, value: {kvp.Value}, isUniq: {isUniq}, caller: {_caller}");
                    }
                }
            }
            else
            {
                _original = null;
            }
        }

        public void Dispose()
        {
            if (_original == null)
                return;

            foreach (var (key, isUniq) in _resolvedTraceVars)
            {
                object? oldValue = null;
                if (_original.TryGetValue(key, out var value))
                    oldValue = value;
                object? newValue = null;
                if (_processor._varDict.TryGetValue(key, out var newVal))
                    newValue = newVal;

                _logger.debug(() => $"TraceScope: {key} old: {oldValue}, new: {newValue}, isUniq: {isUniq}, caller: {_caller}");

                if (Equals(oldValue, newValue))
                    continue;

                bool log = !isUniq;
                if (isUniq)
                {
                    if (_processor._uniqValues.TryGetValue(key, out var uniqSet))
                    {
                        if (uniqSet.Add(oldValue)) // cannot use '||' because both values may be unique
                            log = true;
                        if (uniqSet.Add(newValue))
                            log = true;
                    }
                    else
                    {
                        _processor._uniqValues[key] = new() { oldValue, newValue };
                        log = true;
                    }
                }

                if (log)
                {
                    var V = _processor._varDB[key];
                    Logger.log($"[d] {_node?.TitleWithLineNo() ?? _caller,-90} // {V}: {oldValue,-12} => {newValue,-12}");
                }
            }
        }
    }

    public VarDict VariableValues() => _varDict;

    public void TraceVars(List<string> vars)
    {
        foreach (var varName in vars)
            _traceVars[varName] = false; // trace all occurrences
    }

    public void TraceUniqVars(List<string> vars)
    {
        foreach (var varName in vars)
            _traceVars[varName] = true; // trace only unique occurrences
    }

    public void MergeExisting(VarProcessor other)
    {
        using (new TraceScope(this, null))
        {
            _varDict.MergeExisting(other._varDict);
        }
    }

    public void UpdateExisting(VarDict other)
    {
        using (new TraceScope(this, null))
        {
            _varDict.UpdateExisting(other);
        }
    }

    public void UpdateExisting(VarProcessor other)
    {
        using (new TraceScope(this, null))
        {
            _varDict.UpdateExisting(other._varDict);
        }
    }

    public object? EvaluateString(string expr)
    {
        var tree = CSharpSyntaxTree.ParseText(expr);

        var tracker = new VarTracker(_varDB);
        var trackedRoot = tracker.Track(tree.GetRoot());

        object? result = null;
        foreach (var stmt in trackedRoot!.DescendantNodes().OfType<GlobalStatementSyntax>())
        {
            result = EvaluateExpression(stmt.Statement);
        }
        return result;
    }

    public object? EvaluateExpression(CSharpSyntaxNode node)
    {
        using (new TraceScope(this, node))
        {
            return new Expression(node, _varDict)
                .SetVerbosity(Verbosity)
                .Evaluate();
        }
    }

    public Expression EvaluateExpressionEx(StatementSyntax stmt)
    {
        using (new TraceScope(this, stmt))
        {
            var e = new Expression(stmt, _varDict);
            e.SetVerbosity(Verbosity);
            e.Evaluate();
            return e;
        }
    }

    public Expression EvaluateExpressionEx(ExpressionSyntax expr)
    {
        using (new TraceScope(this, expr))
        {
            var e = new Expression(expr, _varDict);
            e.SetVerbosity(Verbosity);
            e.Evaluate();
            return e;
        }
    }

    public bool HasVar(LocalDeclarationStatementSyntax decl)
    {
        foreach (var v in decl.Declaration.Variables)
        {
            // TODO: check type
            if (!_varDict.ContainsKey(v.Identifier))
                return false;
        }
        return true;
    }

    // also materialize IntConstExpr
    public static (object, object) PromoteInts(object l, object r)
    {
        var (tl, tr) = TypeDB.Promote(l, r);
        if (tl != null || tr != null)
            _logger.debug(() => $"PromoteInts: ({l.GetType()}) {l} and ({r.GetType()}) ({r}) => ({tl}, {tr})");
        l = l is IntConstExpr iceL ? iceL.Materialize(tl) : tl != null ? tl.ConvertInt(l) : l;
        r = r is IntConstExpr iceR ? iceR.Materialize(tr) : tr != null ? tr.ConvertInt(r) : r;
        return (l, r);
    }

    // input: value1 != value2 and both of them are not null
    public static object MergeVar(string key, object value1, object value2, int depth = 0)
    {
        if (value2 is UnknownValueBase && value1 is not UnknownValueBase)
            return MergeVar(key, value2, value1); // Ensure UnknownValueBase is always first

        // _logger.debug(() => $" {key,-10} ({value1?.GetType()}) {value1,-20} ({value2?.GetType()}) {value2,-20}");

        object result = value1 switch
        {
            byte b1 when value2 is byte b2 => new UnknownValueSet(TypeDB.Byte, new() { b1, b2 }),
            sbyte sb1 when value2 is sbyte sb2 => new UnknownValueSet(TypeDB.SByte, new() { sb1, sb2 }),
            int i1 when value2 is int i2 => new UnknownValueSet(TypeDB.Int, new() { i1, i2 }),
            uint ui1 when value2 is uint ui2 => new UnknownValueSet(TypeDB.UInt, new() { ui1, ui2 }),
            short s1 when value2 is short s2 => new UnknownValueSet(TypeDB.Short, new() { s1, s2 }),
            ushort us1 when value2 is ushort us2 => new UnknownValueSet(TypeDB.UShort, new() { us1, us2 }),
            long l1 when value2 is long l2 => new UnknownValueSet(TypeDB.Long, new() { l1, l2 }),
            ulong ul1 when value2 is ulong ul2 => throw new NotImplementedException("Merging ulong values is not implemented."),
            UnknownValueBase uvb1 => uvb1.Merge(value2),
            _ => PromoteAndMergeVar(key, value1, value2, depth)
        };

        _logger.debug(() => $" {key,-10} {value1,-20} {value2,-20} => {result}");

        return result;
    }

    public static object PromoteAndMergeVar(string key, object value1, object value2, int depth = 0)
    {
        if (depth >= 2)
            throw new NotImplementedException($"cannot merge ({value1?.GetType()}) {value1} and ({value2?.GetType()}) {value2}");

        var (l, r) = PromoteInts(value1, value2);
        return MergeVar(key, l, r, depth + 1);
    }
}
