using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

public partial class VarProcessor : ICloneable
{
    public VarDict VariableValues { get; private set; } = new();
    public static VarDict Constants { get; private set; } = new();
    public int Verbosity = 0;

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
        clonedProcessor.VariableValues = (VarDict)this.VariableValues.Clone();
        clonedProcessor.Verbosity = this.Verbosity;
        clonedProcessor._traceVars = this._traceVars; // shared, r/o
        clonedProcessor._uniqValues = this._uniqValues; // shared, r/w
        return clonedProcessor;
    }

    class TraceScope : IDisposable
    {
        readonly VarProcessor _processor;
        readonly SyntaxNode _node;
        readonly Dictionary<string, object> _original = null;

        public TraceScope(VarProcessor processor, SyntaxNode node)
        {
            if (processor._traceVars.Count > 0)
            {
                _processor = processor;
                _node = node;
                _original = processor.VariableValues
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
                if (_processor.VariableValues.TryGetValue(key, out var newVal))
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
                    Logger.log($"[d] {_node.TitleWithLineNo(),-90} // {key}: {oldValue,-12} => {newValue,-12}");
            }
        }
    }

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

    public object EvaluateExpression(CSharpSyntaxNode node)
    {
        using (new TraceScope(this, node))
        {
            return new Expression(node, VariableValues)
                .SetVerbosity(Verbosity)
                .Evaluate();
        }
    }

    public Expression EvaluateExpressionEx(StatementSyntax stmt)
    {
        using (new TraceScope(this, stmt))
        {
            var e = new Expression(stmt, VariableValues);
            e.SetVerbosity(Verbosity);
            e.Evaluate();
            return e;
        }
    }

    public Expression EvaluateExpressionEx(ExpressionSyntax expr)
    {
        using (new TraceScope(this, expr))
        {
            var e = new Expression(expr, VariableValues);
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
            if (!VariableValues.ContainsKey(v.Identifier.ValueText))
                return false;
        }
        return true;
    }

    public void SetVarTypes(LocalDeclarationStatementSyntax decl)
    {
        foreach (var v in decl.Declaration.Variables)
        {
            if (VariableValues.ContainsKey(v.Identifier.ValueText))
            {
                // TODO: check type
            }
            else
            {
                VariableValues[v.Identifier.ValueText] = UnknownValue.Create(decl.Declaration.Type);
            }
        }
    }
}
