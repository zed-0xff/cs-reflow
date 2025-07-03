using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

public class VarDB
{
    int _var_id = 0;

    Dictionary<SyntaxAnnotation, Variable> _ann2vars = new();
    Dictionary<int, Variable> _vars = new();
    Dictionary<string, Variable> _name2vars = new();

    static readonly TaggedLogger _logger = new("VarDB");

    public Variable this[int id] => _vars[id];
    public Variable this[SyntaxAnnotation ann]
    {
        get
        {
            if (_ann2vars.TryGetValue(ann, out var variable))
                return variable;

            var data = ann.Data;
            if (data == null)
                throw new InvalidOperationException($"Annotation {ann} has no data.");

            if (int.TryParse(data, out int id))
            {
                if (_vars.TryGetValue(id, out Variable? V))
                {
                    _ann2vars[ann] = V;
                    return V;
                }
                throw new InvalidOperationException($"Annotation {ann} (Data={ann.Data}) refers to non-existing variable ID: {id}");
            }

            throw new InvalidOperationException($"Annotation {ann} has invalid data: {data}");
        }
    }

    public Variable? FindByName(string name) => _name2vars.TryGetValue(name, out var variable) ? variable : null;

    public bool TryGetValue(string varName, out Variable? variable) => _name2vars.TryGetValue(varName, out variable);
    public bool TryGetValue(SyntaxToken token, out Variable? variable)
    {
        var ann = token.VarID();
        if (ann == null)
        {
            _logger.warn_once($"Variable definition not found for {token}");
            return TryGetValue(token.ToString(), out variable);
        }
        return _ann2vars.TryGetValue(ann, out variable);
    }

    public Variable Add(ParameterSyntax node, string typeName) => Add(node.Identifier.ValueText, typeName);
    public Variable Add(VariableDeclaratorSyntax node, string typeName) => Add(node.Identifier.ValueText, typeName);
    public Variable Add(string name, string typeName)
    {
        int id = _var_id++;
        var v = new Variable(id, name, typeName);
        _ann2vars[v.Annotation] = v;
        _vars[id] = v;
        _name2vars[v.Name] = v;
        _logger.debug(() => $"{v}: ({typeName}) {name}");
        return v;
    }

    public Variable AddConst(string name, string typeName, object? value)
    {
        int id = _var_id++;
        var v = new Variable(id, name, typeName, value);
        _ann2vars[v.Annotation] = v;
        _vars[id] = v;
        _name2vars[v.Name] = v;
        _logger.debug(() => $"{v}: ({typeName}) {name}");
        return v;
    }

    public Variable Add(IFieldSymbol field) => field.IsConst ?
            AddConst(field.Name, field.Type.ToString()!, field.ConstantValue) :
            Add(field.Name, field.Type.ToString()!);

    public void SetLoopVar(int id) => _vars[id].Flags |= Variable.FLAG_LOOP;

    // same as Roslyn's ReadInside(), but handles ++/-- differently
    public (HashSet<int>, HashSet<int>, HashSet<int>) CollectVars(SyntaxNode rootNode)
    {
        var declared = rootNode.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(s => s.Declaration.Variables)
            .Select(v => v.VarID())
            .Where(v => v != null)
            .Select(v => this[v!].id)
            .ToHashSet();

        var written = new HashSet<int>();
        var read = new HashSet<int>();

        foreach (var idNode in rootNode.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var ann = idNode.VarID();
            if (ann == null)
                continue;
            int id = this[ann].id;

            var expr = idNode.Parent;
            if (expr is null)
                throw new InvalidOperationException($"Identifier {idNode.Identifier.Text} has no parent expression.");

            // ++x, --x, !x, ~x, ...
            if (expr is PrefixUnaryExpressionSyntax unaryPrefix)
            {
                if (expr.Parent is not ExpressionStatementSyntax) // y = ++x; / while(++x){ ... }
                    read.Add(id);
                if (unaryPrefix.IsKind(SyntaxKind.PreIncrementExpression) || unaryPrefix.IsKind(SyntaxKind.PreDecrementExpression))
                    written.Add(id);
                else
                    read.Add(id);
                continue;
            }

            // x++, x--
            if (expr is PostfixUnaryExpressionSyntax unaryPostfix)
            {
                if (expr.Parent is not ExpressionStatementSyntax)
                    read.Add(id);
                if (unaryPostfix.IsKind(SyntaxKind.PostIncrementExpression) || unaryPostfix.IsKind(SyntaxKind.PostDecrementExpression))
                    written.Add(id);
                else
                    read.Add(id);
                continue;
            }

            // =, +=, -=, *=, /=, %=, ...
            AssignmentExpressionSyntax? assExpr = expr.FirstAncestorOrSelfUntil<AssignmentExpressionSyntax, BlockSyntax>();
            if (assExpr != null)
            {
                var tokens = assExpr.Left.CollectTokens();
                if (tokens.Any(t => t.IsSameVar(ann)))
                {
                    _logger.debug(() => $"WRITE {ann.Data}");
                    written.Add(id); // intentionally do not interpret self-read as 'read'
                }
                else
                {
                    _logger.debug(() => $"READ  {ann.Data}");
                    read.Add(id);
                }
                continue;
            }

            LocalDeclarationStatementSyntax? declStmt = expr.FirstAncestorOrSelfUntil<LocalDeclarationStatementSyntax, BlockSyntax>();
            if (declStmt != null)
            {
                // local variable declaration: int x = 123 + [...]
                if (declStmt.IsSameVar(ann))
                {
                    _logger.debug(() => $"DECLARE {ann.Data}");
                    declared.Add(id);
                }
                else
                {
                    _logger.debug(() => $"READ  {ann.Data}");
                    read.Add(id); // variable is read in the other variable's initializer
                }
                continue;
            }

            // foo(x)
            ArgumentSyntax? argExpr = expr.FirstAncestorOrSelfUntil<ArgumentSyntax, BlockSyntax>();
            if (argExpr != null)
            {
                _logger.debug(() => $"READ  {ann.Data}");
                read.Add(id);
                if (!argExpr.RefOrOutKeyword.IsKind(SyntaxKind.None))
                    written.Add(id); // ref/out arguments are both read and written
                continue;
            }

            _logger.debug(() => $"unhandled expression: {expr.Kind()} at {expr.TitleWithLineNo()} => READ {ann.Data}");
            read.Add(id); // fallback: treat as read
        }

        return (declared, read, written);
    }
}
