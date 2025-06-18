using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Runtime.CompilerServices;

public partial class VarProcessor : ICloneable
{
    public static VarDict Constants { get; private set; } = new();
    public int Verbosity = 0;

    VarDict _vars = new();
    Dictionary<string, bool> _traceVars = new(); // value is true if trace only unique
    Dictionary<string, HashSet<object>> _uniqValues = new(); // shared, r/w

    public VarProcessor(int verbosity = 0)
    {
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
        var clonedProcessor = new VarProcessor();
        clonedProcessor._vars = (VarDict)this._vars.Clone();
        clonedProcessor.Verbosity = this.Verbosity;
        clonedProcessor._traceVars = this._traceVars; // shared, r/o
        clonedProcessor._uniqValues = this._uniqValues; // shared, r/w
        return clonedProcessor;
    }

    class TraceScope : IDisposable
    {
        readonly VarProcessor _processor;
        readonly SyntaxNode? _node;
        readonly string _caller;
        readonly Dictionary<string, object> _original = null;

        public TraceScope(VarProcessor processor, SyntaxNode? node, [CallerMemberName] string caller = "")
        {
            if (processor._traceVars.Count > 0)
            {
                _caller = caller;
                _processor = processor;
                _node = node;
                _original = processor._vars
                    .Where(kvp => processor._traceVars.ContainsKey(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        public void Dispose()
        {
            if (_original == null)
                return;

            foreach (var (key, isUniq) in _processor._traceVars)
            {
                object? oldValue = null;
                if (_original.TryGetValue(key, out var value))
                    oldValue = value;
                object? newValue = null;
                if (_processor._vars.TryGetValue(key, out var newVal))
                    newValue = newVal;

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
                        _processor._uniqValues[key] = new HashSet<object> { oldValue, newValue };
                        log = true;
                    }
                }

                if (log)
                    Logger.log($"[d] {_node?.TitleWithLineNo() ?? _caller,-90} // {key}: {oldValue,-12} => {newValue,-12}");
            }
        }
    }

    public VarDict VariableValues() => _vars;
    public bool IsSwitchVar(string varName) => _vars.IsSwitchVar(varName);
    public void SetSwitchVar(string varName, bool value = true) => _vars.SetSwitchVar(varName, value);
    public void SetLoopVar(string varName, bool value = true) => _vars.SetLoopVar(varName, value);

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
            _vars.MergeExisting(other._vars);
        }
    }

    public void UpdateExisting(VarDict other)
    {
        using (new TraceScope(this, null))
        {
            _vars.UpdateExisting(other);
        }
    }

    public void UpdateExisting(VarProcessor other)
    {
        using (new TraceScope(this, null))
        {
            _vars.UpdateExisting(other._vars);
        }
    }

    public object EvaluateExpression(CSharpSyntaxNode node)
    {
        using (new TraceScope(this, node))
        {
            return new Expression(node, _vars)
                .SetVerbosity(Verbosity)
                .Evaluate();
        }
    }

    public Expression EvaluateExpressionEx(StatementSyntax stmt)
    {
        using (new TraceScope(this, stmt))
        {
            var e = new Expression(stmt, _vars);
            e.SetVerbosity(Verbosity);
            e.Evaluate();
            return e;
        }
    }

    public Expression EvaluateExpressionEx(ExpressionSyntax expr)
    {
        using (new TraceScope(this, expr))
        {
            var e = new Expression(expr, _vars);
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
            if (!_vars.ContainsKey(v.Identifier.ValueText))
                return false;
        }
        return true;
    }

    public void SetVar(string name, object? value)
    {
        _vars[name] = value;
    }

    public object? GetVar(string name)
    {
        if (_vars.TryGetValue(name, out var value))
            return value;

        return UnknownValue.Create();
    }

    public void SetVarTypes(LocalDeclarationStatementSyntax decl)
    {
        foreach (var v in decl.Declaration.Variables)
        {
            if (_vars.ContainsKey(v.Identifier.ValueText))
            {
                // TODO: check type
            }
            else
            {
                _vars[v.Identifier.ValueText] = UnknownValue.Create(decl.Declaration.Type);
            }
        }
    }

    public static (dynamic, dynamic) PromoteInts(dynamic l, dynamic r)
    {
        var iceL = l as IntConstExpr;
        var iceR = r as IntConstExpr;
        var ltype = (iceL != null) ? iceL.IntType.Type : l.GetType();
        var rtype = (iceR != null) ? iceR.IntType.Type : r.GetType();
        // [floats skipped]
        // if either operand is of type ulong, the OTHER OPERAND is converted to type ulong,
        // or a binding-time error occurs if the other operand is of type sbyte, short, int, or long.
        if (ltype == typeof(ulong) || rtype == typeof(ulong))
        {
            if (ltype != typeof(ulong))
                l = (iceL != null) ? iceL.Cast(TypeDB.ULong) : Convert.ToUInt64(l);
            if (rtype != typeof(ulong))
                r = (iceR != null) ? iceR.Cast(TypeDB.ULong) : Convert.ToUInt64(r);
        }

        // Otherwise, if either operand is of type long, the OTHER OPERAND is converted to type long.
        else if (ltype == typeof(long) || rtype == typeof(long))
        {
            if (ltype != typeof(long))
                l = (iceL != null) ? iceL.Cast(TypeDB.Long) : Convert.ToInt64(l);
            if (rtype != typeof(long))
                r = (iceR != null) ? iceR.Cast(TypeDB.Long) : Convert.ToInt64(r);
        }

        // Otherwise, if either operand is of type uint and the other operand is of type sbyte, short, or int, BOTH OPERANDS are converted to type long.
        // XXX not always true XXX
        else if (
                (ltype == typeof(uint) && (rtype == typeof(sbyte) || rtype == typeof(short) || rtype == typeof(int))) ||
                (rtype == typeof(uint) && (ltype == typeof(sbyte) || ltype == typeof(short) || ltype == typeof(int)))
                )
        {
            // if left is uint and right is int, but can fit in uint => right is converted to uint
            if (ltype == typeof(uint) && rtype == typeof(int) && iceR != null && iceR!.Value >= 0)
            {
                r = iceR!.Cast(TypeDB.UInt);
            }
            else if (rtype == typeof(uint) && ltype == typeof(int) && iceL != null && iceL!.Value >= 0)
            {
                l = iceL!.Cast(TypeDB.UInt);
            }
            else
            {
                l = (iceL != null) ? iceL.Cast(TypeDB.Long) : Convert.ToInt64(l);
                r = (iceR != null) ? iceR.Cast(TypeDB.Long) : Convert.ToInt64(r);
            }
        }

        // Otherwise, if either operand is of type uint, the OTHER OPERAND is converted to type uint.
        else if (ltype == typeof(uint) || rtype == typeof(uint))
        {
            if (ltype != typeof(uint))
                l = (iceL != null) ? iceL.Cast(TypeDB.UInt) : Convert.ToUInt32(l);
            if (rtype != typeof(uint))
                r = (iceR != null) ? iceR.Cast(TypeDB.UInt) : Convert.ToUInt32(r);
        }
        else
        {
            // Otherwise, BOTH OPERANDS are converted to type int.
            l = (iceL != null) ? iceL.Cast(TypeDB.Int) : Convert.ToInt32(l);
            r = (iceR != null) ? iceR.Cast(TypeDB.Int) : Convert.ToInt32(r);
        }

        if (l is IntConstExpr iceL2)
            l = iceL2.TypedValue();

        if (r is IntConstExpr iceR2)
            r = iceR2.TypedValue();

        return (l, r);
    }


    // input: value1 != value2 and both of them are not null
    public static object MergeVar(string key, object value1, object value2, int depth = 0)
    {
        if (value2 is UnknownValueBase && value1 is not UnknownValueBase)
        {
            return MergeVar(key, value2, value1); // Ensure UnknownValueBase is always first
        }

        object result = value1 switch
        {
            byte b1 when value2 is byte b2 => new UnknownValueList(TypeDB.Byte, new() { b1, b2 }),
            sbyte sb1 when value2 is sbyte sb2 => new UnknownValueList(TypeDB.SByte, new() { sb1, sb2 }),
            int i1 when value2 is int i2 => new UnknownValueList(TypeDB.Int, new() { i1, i2 }),
            uint ui1 when value2 is uint ui2 => new UnknownValueList(TypeDB.UInt, new() { ui1, ui2 }),
            short s1 when value2 is short s2 => new UnknownValueList(TypeDB.Short, new() { s1, s2 }),
            ushort us1 when value2 is ushort us2 => new UnknownValueList(TypeDB.UShort, new() { us1, us2 }),
            long l1 when value2 is long l2 => new UnknownValueList(TypeDB.Long, new() { l1, l2 }),
            ulong ul1 when value2 is ulong ul2 => throw new NotImplementedException("Merging ulong values is not implemented."),
            UnknownValueBase unk1 => unk1.Merge(value2),
            _ => PromoteAndMergeVar(key, value1, value2, depth)
        };

        Logger.debug(() => $" {key,-10} {value1,-20} {value2,-20} => {result}");

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
