using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public partial class VarProcessor
{
    public class Expression
    {
        CSharpSyntaxNode node;
        VarDict _varDict;
        public int Verbosity = 0;

        private HashSet<int>? _varsRead;
        private HashSet<int>? _varsWritten;
        private HashSet<int>? _varsReferenced;

        public IEnumerable<int> VarsRead => _varsRead ?? calc_accessed_vars().read;
        public IEnumerable<int> VarsWritten => _varsWritten ?? calc_accessed_vars().written;
        public IEnumerable<int> VarsReferenced => _varsReferenced ?? calc_accessed_vars().referenced;

        public object? Result { get; private set; } = UnknownValue.Create();

        static readonly TaggedLogger _logger = new("Expression");

        public Expression(CSharpSyntaxNode node, VarDict varDict)
        {
            this.node = node;
            this._varDict = varDict;
        }

        (IEnumerable<int> read, IEnumerable<int> written, IEnumerable<int> referenced) calc_accessed_vars()
        {
            var (declared, read, written) = _varDict._varDB.CollectVars(node);
            _varsRead = read;
            _varsWritten = written;
            _varsReferenced = read.Union(written).ToHashSet();
            return (_varsRead, _varsWritten, _varsReferenced);
        }

        public Expression SetVerbosity(int verbosity)
        {
            Verbosity = verbosity;
            return this;
        }

        public object? Evaluate()
        {
            object? result = null;
            switch (node)
            {
                case ExpressionSyntax expression:
                    result = EvaluateExpression(expression);
                    break;
                case LocalDeclarationStatementSyntax localDeclaration:
                    ProcessLocalDeclaration(localDeclaration);
                    break;
                case ExpressionStatementSyntax expressionStatement:
                    result = EvaluateExpression(expressionStatement.Expression);
                    break;
                default:
                    throw new NotSupportedException($"Syntax node type '{node?.GetType()}' is not supported.");
            }
            if (result is IntConstExpr ice)
                result = ice.Materialize();
            Result = result;
            _logger.debug(() => $"{node.Title()} => ({result?.GetType()}) {result}");
            return result;
        }

        void ProcessLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration)
        {
            if (Verbosity > 2)
            {
                _logger.debug($"{localDeclaration}");
                if (Verbosity > 3)
                {
                    _logger.debug($".Declaration = {localDeclaration.Declaration}");
                    _logger.debug($".Declaration.Type = {localDeclaration.Declaration.Type}");
                    _logger.debug($".Declaration.Variables = {localDeclaration.Declaration.Variables}");
                }
            }

            foreach (var variable in localDeclaration.Declaration.Variables)
            {
                if (Verbosity > 2)
                    _logger.debug($"A Variable = {variable}");

                // Get the variable name (e.g., "num3")
                var varID = variable.Identifier;
                if (!_varDict.IsVariableRegistered(varID))
                {
                    _varDict.RegisterVariable(variable);
                }

                // if (string.IsNullOrEmpty(varName))
                //     throw new NotSupportedException($"Empty variable name in: {localDeclaration}");
                // 
                // VarsWritten.Add(varName);

                // Extract the right-hand side expression (e.g., "(num4 = (uint)(num2 ^ 0x76ED016F))")
                var initializerExpression = variable.Initializer?.Value;

                var value = _varDict.DefaultValue(varID);

                if (initializerExpression is not null)
                {
                    setVar(varID, value); // set to UnknownValue first, because EvaluateExpression() may throw an exception
                    value = EvaluateExpression(initializerExpression);
                }

                if (Verbosity > 2)
                    _logger.debug($"C varID={varID} value={value}");

                // narrow returned value type, when possible
                if (localDeclaration.Declaration.Type is not null && TypeDB.TryFind(localDeclaration.Declaration.Type.ToString()) is not null)
                {
                    switch (value)
                    {
                        case UnknownValue:
                            value = UnknownValue.Create(localDeclaration.Declaration.Type);
                            break;
                        case IntConstExpr ice:
                            value = ice.Cast(localDeclaration.Declaration.Type.ToString());
                            break;
                    }
                }

                if (Verbosity > 3)
                    _logger.debug($"D varID={varID} value={value}");

                // if (value is UnknownValueBase unk)
                //     value = unk.WithTag(varID);

                if (Verbosity > 3)
                    _logger.debug($"E varID={varID} value={value}");

                // do not overwrite existing variable values if initializerExpression is null
                if (initializerExpression is not null || !_varDict.ContainsKey(varID))
                {
                    setVar(varID, value);
                }

                if (Verbosity > 3)
                    _logger.debug($"F varID={varID} value={value}");

                // don't return 'value' bc it might be an UnknownValue from empty declaration, but vars may already have its value
                //return _varDict[varID];
            }
        }

        void setVar(IdentifierNameSyntax id, object? value) => setVar(id.Identifier, value);
        void setVar(SyntaxToken token, object? value) => _varDict.Set(token, value);

        static object cast_var(object value, TypeDB.IntType toType)
        {
            switch (value)
            {
                case UnknownValueBase uv:
                    return uv.Cast(toType);
                case IntConstExpr ice:
                    return ice.FakeCast(toType);
            }

            return toType.ConvertInt(value);
        }

        static object cast_var(object value, TypeSyntax toTypeSyntax)
        {
            var toType = TypeDB.TryFind(toTypeSyntax);
            if (toType is not null)
                return cast_var(value, toType);

            if (value is UnknownValueBase uv)
            {
                if (toTypeSyntax is PointerTypeSyntax pts && uv.IsPointer())
                {
                    var ptrType = TypeDB.TryFind(pts.ElementType);
                    if (ptrType is not null)
                        return uv.WithTag("ptr_cast", ptrType);
                }
                return new UnknownValue();
            }
            throw new NotSupportedException($"Type '{toTypeSyntax}' is not supported.");
        }

        [ThreadStatic]
        static bool _excLogged;

        [ThreadStatic]
        static int _evalDepth;

        object EvaluateExpression(ExpressionSyntax expr, bool resolveIdentifier = true)
        {
            _evalDepth++;
            try
            {
                var result = EvaluateExpression_(expr, resolveIdentifier);
                if (Verbosity > 0 || Logger.HasTag("Expression.EvaluateExpression"))
                {
                    Console.Error.WriteColor($"[.] ", ConsoleColor.DarkGray);
                    Console.Error.Write($"{expr,-50}");
                    Console.Error.WriteColor(" => ", ConsoleColor.DarkGray);
                    switch (result)
                    {
                        case UnknownValue:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        case UnknownTypedValue utv:
                            Console.ForegroundColor = utv.Cardinality() == 1 ? ConsoleColor.Green : ConsoleColor.Yellow;
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            break;
                    }
                    if (Verbosity > 1) Console.Error.Write($"({result?.GetType()}) ");
                    switch (result)
                    {
                        case IntConstExpr ice:
                            Console.Error.WriteLine($"{ice.Value,-11} (0x{ice.Value:X})");
                            break;
                        case int i:
                            Console.Error.WriteLine($"{i,-11} (0x{i:X})");
                            break;
                        case uint u:
                            Console.Error.WriteLine($"{u,-11} (0x{u:X})");
                            break;
                        default:
                            Console.Error.WriteLine($"{result}");
                            break;
                    }
                    Console.ResetColor();
                }
                return result!; // TODO: check nullability
            }
            catch (Exception ex)
            {
                if (ex is not NotSupportedException && !_excLogged) // log only from the deepest level
                {
                    string msg = $"Error evaluating expr '{expr.TitleWithLineNo()}': {ex.GetType()}";
                    var vars = _varDict.VarsFromNode(expr);
                    if (vars.Count > 0)
                        msg += $" ({vars})";

                    _logger.error(msg);
                    _excLogged = true;
                }
                throw;
            }
            finally
            {
                _evalDepth--;
                if (_evalDepth == 0 && _excLogged)
                {
                    // Reset the flag to avoid logging multiple times in the same thread
                    _excLogged = false;
                }
            }
        }

        void reset_tuple_vars(TupleExpressionSyntax tupleExpr)
        {
            // Reset all variables in the tuple expression
            foreach (var item in tupleExpr.Arguments)
            {
                var expr = item.Expression.StripParentheses();
                switch (expr)
                {
                    case IdentifierNameSyntax idName:
                        _varDict.ResetVar(idName.Identifier);
                        break;
                    case TupleExpressionSyntax nestedTuple:
                        reset_tuple_vars(nestedTuple);
                        break;
                    default:
                        // Ignore other types of expressions
                        _logger.warn_once($"ignoring {expr.Kind()} in {tupleExpr}");
                        break;
                }
            }
        }

        // calculate the result of an assignment expression, i.e. '+=', '-=', '*=', etc. (simple '=' too)
        object? calc_assignment_result(ExpressionSyntax left, SyntaxKind kind, ExpressionSyntax right)
        {
            switch (kind)
            {
                case SyntaxKind.SimpleAssignmentExpression:
                    return EvaluateExpression(right);

                default:
                    // +=, -=, etc
                    SyntaxKind binaryOperatorKind = kind switch
                    {
                        SyntaxKind.AddAssignmentExpression => SyntaxKind.AddExpression,
                        SyntaxKind.SubtractAssignmentExpression => SyntaxKind.SubtractExpression,
                        SyntaxKind.MultiplyAssignmentExpression => SyntaxKind.MultiplyExpression,
                        SyntaxKind.DivideAssignmentExpression => SyntaxKind.DivideExpression,
                        SyntaxKind.ModuloAssignmentExpression => SyntaxKind.ModuloExpression,
                        SyntaxKind.AndAssignmentExpression => SyntaxKind.BitwiseAndExpression,
                        SyntaxKind.OrAssignmentExpression => SyntaxKind.BitwiseOrExpression,
                        SyntaxKind.ExclusiveOrAssignmentExpression => SyntaxKind.ExclusiveOrExpression,
                        SyntaxKind.LeftShiftAssignmentExpression => SyntaxKind.LeftShiftExpression,
                        SyntaxKind.RightShiftAssignmentExpression => SyntaxKind.RightShiftExpression,
                        _ => throw new InvalidOperationException("Unsupported compound assignment operator")
                    };
                    return EvaluateBinaryExpression(BinaryExpression(binaryOperatorKind, left, right));
            }
        }

        void reset_var(ExpressionSyntax expr)
        {
            switch (expr)
            {
                case IdentifierNameSyntax idName:
                    _varDict.ResetVar(idName.Identifier);
                    return;

                case TupleExpressionSyntax tupleExpr:
                    reset_tuple_vars(tupleExpr);
                    return;
            }
        }

        object? var_set(IdentifierNameSyntax idNameLeft, SyntaxKind kind, ExpressionSyntax right)
        {
            SyntaxToken idLeft = idNameLeft.Identifier;
            // VarsWritten.Add(idLeft);

            try
            {
                var result = calc_assignment_result(idNameLeft, kind, right);
                setVar(idLeft, result);
                return _varDict[idLeft];
            }
            catch (Exception ex)
            {
                _logger.debug($"catched \"{ex.Message}\" in EvaluateExpression for {idLeft}");
                reset_var(idNameLeft);
                throw;
            }
        }

        object? arr_elem_set(ElementAccessExpressionSyntax elAccLeft, SyntaxKind kind, ExpressionSyntax right)
        {
            var accessor = element_access(elAccLeft);
            var result = calc_assignment_result(elAccLeft, kind, right);
            return accessor.SetValue(result);
        }

        // Handle assignment expressions (e.g., num3 = (num4 = (uint)(num2 ^ 0x76ED016F)))
        object? EvaluateAssignment(ExpressionSyntax left, SyntaxKind kind, ExpressionSyntax right)
        {
            left = left.StripParentheses();
            right = right.StripParentheses();

            if (left is TupleExpressionSyntax tupleL)
            {
                if (kind != SyntaxKind.SimpleAssignmentExpression)
                    throw new NotSupportedException($"Tuple assignment with '{kind}' is not supported");

                reset_tuple_vars(tupleL);
                if (right is TupleExpressionSyntax tupleR)
                {
                    if (tupleL.Arguments.Count != tupleR.Arguments.Count)
                        throw new NotSupportedException($"Tuple assignment mismatch: {tupleL.Arguments.Count} != {tupleR.Arguments.Count}");
                    for (int i = 0; i < tupleL.Arguments.Count; i++)
                    {
                        EvaluateAssignment(
                            tupleL.Arguments[i].Expression,
                            SyntaxKind.SimpleAssignmentExpression,
                            tupleR.Arguments[i].Expression
                        );
                    }
                }
                return UnknownValue.Create(); // TODO: return tuple?
            }

            return left switch
            {
                ElementAccessExpressionSyntax elAccLeft => arr_elem_set(elAccLeft, kind, right),
                IdentifierNameSyntax idNameLeft => var_set(idNameLeft, kind, right),
                _ => throw new NotSupportedException($"Assignment to '{left.Kind()}' is not supported")
            };
        }

        public bool TryGetConstValue(ExpressionSyntax expr, out object? value)
        {
            string key = expr.ToString();
            if (Constants.TryGetValue(key, out value))
                return true;

            if (expr is MemberAccessExpressionSyntax memberAccess)
            {
                // (Type.EmptyTypes).LongLength
                key = $"{memberAccess.Expression.StripParentheses()}.{memberAccess.Name}";
                if (Constants.TryGetValue(key, out value))
                    return true;

                if (_varDict._varDB.TryGetValue(memberAccess.Name.Identifier, out var V) && V!.IsConst)
                {
                    value = V.ConstValue;
                    return true;
                }
            }

            return false;
        }

        object create_array(ArrayCreationExpressionSyntax expr)
        {
            var rankSpecifier = expr.Type.RankSpecifiers.FirstOrDefault();
            var sizeExpr = rankSpecifier?.Sizes.FirstOrDefault();
            _logger.debug(() => $"rankSpecifier: {rankSpecifier}, sizeExpr: {sizeExpr}, initializer: {expr.Initializer}");

            var elType = TypeDB.ToSystemType(expr.Type.ElementType) ?? typeof(UnknownValue);

            // new int[3]
            if (sizeExpr is LiteralExpressionSyntax literalExpr)
            {
                int size = Convert.ToInt32(literalExpr.Token.Value);
                return new ArrayWrap(elType, size);
            }

            // new int[]{ 1, 2, 3 }
            if (expr.Initializer is not null)
            {
                int size = expr.Initializer.Expressions.Count;
                var arr = new ArrayWrap(elType, size);
                for (int i = 0; i < size; i++)
                {
                    var value = EvaluateExpression(expr.Initializer.Expressions[i]);
                    if (value is IntConstExpr ice)
                        value = ice.Materialize();
                    arr[i] = value;
                }
                return arr;
            }

            return UnknownValue.Create();
        }

        object? member_access(MemberAccessExpressionSyntax expr)
        {
            _logger.debug(() => $"member_access: {expr}");

            if (expr.Name is IdentifierNameSyntax idName)
            {
                switch (idName.Identifier.ValueText)
                {
                    case "Length":
                        var value = EvaluateExpression(expr.Expression);
                        if (value is ArrayWrap arr)
                            return arr.Length;
                        break;
                }
            }

            if (TryGetConstValue(expr, out var constantValue))
                return constantValue;
            else
                throw new NotSupportedException($"MemberAccessExpression is not supported for {expr}");
        }

        class ElementAccessor
        {
            public readonly ArrayWrap Array;
            public readonly int Index;

            public ElementAccessor(ArrayWrap array, int index)
            {
                Array = array;
                Index = index;
            }

            public object? GetValue() => Array[Index];
            public object? SetValue(object? value)
            {
                Logger.debug(() => $"Array={Array.GetType()}, Index={Index}, Value=({value?.GetType()}) {value}", "ElementAccessor.SetValue");

                var elType = Array.ValueType;
                if (elType is not null)
                {
                    var intType = TypeDB.TryFind(elType);
                    if (intType is not null)
                        value = value switch
                        {
                            IntConstExpr ice => ice.Materialize(intType),
                            _ => value
                        };
                }

                value = value switch
                {
                    IntConstExpr ice => ice.Materialize(),
                    _ => value
                };

                Array[Index] = value;
                return GetValue();
            }
        }

        // performs checks and returns ElementAccessor if all is ok, throws otherwise
        ElementAccessor element_access(ElementAccessExpressionSyntax expr)
        {
            _logger.debug(() => $"element_access: {expr}");

            var array = EvaluateExpression(expr.Expression);
            if (array is ArrayWrap arr)
            {
                // Evaluate the index expression
                var indexExpr = expr.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                if (indexExpr is not null)
                {
                    int index = TypeDB.ToInt32(EvaluateExpression(indexExpr));
                    return new ElementAccessor(arr, index);
                }
                throw new NotSupportedException($"ElementAccessExpression requires an index: {expr}");
            }

            throw new NotSupportedException($"ElementAccessExpression is not supported for {expr}");
        }

        object? EvaluateExpression_(ExpressionSyntax expression, bool resolveIdentifier = true)
        {
            switch (expression)
            {
                case AssignmentExpressionSyntax assignmentExpr:
                    try
                    {
                        return EvaluateAssignment(assignmentExpr.Left, assignmentExpr.Kind(), assignmentExpr.Right);
                    }
                    catch (Exception e)
                    {
                        _logger.debug($"\"{assignmentExpr}\" => {e.Message}");
                        _varDict.ResetVars(_varDict._varDB.CollectVars(assignmentExpr).written);
                        throw;
                    }

                case BinaryExpressionSyntax binaryExpr:
                    return EvaluateBinaryExpression(binaryExpr);

                case CastExpressionSyntax castExpr:               // (uint)num2
                    var value = EvaluateExpression(castExpr.Expression);
                    return cast_var(value, castExpr.Type);

                case ConditionalExpressionSyntax conditionalExpr: // ternary operator: num3 == 0 ? num4 : num5
                    var condition = EvaluateExpression(conditionalExpr.Condition);
                    try
                    {
                        if (Convert.ToBoolean(condition))
                            return EvaluateExpression(conditionalExpr.WhenTrue);
                        else
                            return EvaluateExpression(conditionalExpr.WhenFalse);
                    }
                    catch (InvalidCastException)
                    {
                        _logger.warn_once($"ConditionalExpression: cannot cast '{condition}' to bool in: {conditionalExpr}");
                        object whenTrue = EvaluateExpression(conditionalExpr.WhenTrue);
                        object whenFalse = EvaluateExpression(conditionalExpr.WhenFalse);
                        object result = (Equals(whenTrue, whenFalse)) ? whenTrue : VarProcessor.MergeVar("?", whenTrue, whenFalse);
                        _logger.warn_once($"ConditionalExpression: merged {whenTrue} and {whenFalse} => {result}");
                        return result;
                    }

                case DefaultExpressionSyntax defaultExpr: // "default(uint)"
                    return TypeDB.Default(defaultExpr.Type);

                case IdentifierNameSyntax id:
                    // If the expression is an identifier, fetch its value from the dictionary
                    // VarsRead.Add(id.Identifier);
                    return resolveIdentifier ? _varDict[id.Identifier] : id.Identifier;

                case LiteralExpressionSyntax literal:
                    if (literal.Token.Value is int i)
                        return new IntConstExpr(i);
                    return literal.Token.Value;

                case MemberAccessExpressionSyntax memberAccessExpr:
                    return member_access(memberAccessExpr);

                case ElementAccessExpressionSyntax elementAccessExpr:
                    return element_access(elementAccessExpr).GetValue();

                case ParenthesizedExpressionSyntax parenExpr:
                    return EvaluateExpression(parenExpr.Expression);

                case CheckedExpressionSyntax checkedExpr: // TODO: simulate exception on overflow?
                    return EvaluateExpression(checkedExpr.Expression);

                case PrefixUnaryExpressionSyntax prefixExpr:
                    return EvaluatePrefixExpression(prefixExpr);

                case PostfixUnaryExpressionSyntax postfixExpr:
                    return EvaluatePostfixExpression(postfixExpr);

                case SizeOfExpressionSyntax sizeOfExpr:
                    return TypeDB.SizeOf(sizeOfExpr.Type);

                case ArrayCreationExpressionSyntax arrayCreationExpr:
                    return create_array(arrayCreationExpr);

                default:
                    throw new NotSupportedException($"{expression.Kind()} is not supported.");
            }
        }

        static INumber<T> eval_prefix<T>(T value, SyntaxKind kind) where T : INumber<T>, IBitwiseOperators<T, T, T>
        {
            return kind switch
            {
                SyntaxKind.BitwiseNotExpression => ~value,
                SyntaxKind.UnaryPlusExpression => value,
                SyntaxKind.UnaryMinusExpression => -value,
                SyntaxKind.PreDecrementExpression => value - T.One,
                SyntaxKind.PreIncrementExpression => value + T.One,
                _ => throw new NotSupportedException($"Unary operator '{kind}' is not supported for {typeof(T)}.")
            };
        }

        static bool eval_prefix(bool value, SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.LogicalNotExpression => !value,
                _ => throw new NotSupportedException($"Unary operator '{kind}' is not supported for bool.")
            };
        }

        static object eval_prefix(object value, SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.AddressOfExpression: // "&x"
                    return UnknownTypedValue.Create(TypeDB.NInt).WithTag("pointee", value); // XXX returning NInt for all pointer types

                case SyntaxKind.PointerIndirectionExpression: // "*x" => Dereference a pointer
                    if (value is UnknownTypedValue utv && utv.TryGetTag("pointee", out var pointee))
                    {
                        if (utv.TryGetTag("ptr_cast", out var ptrCast) && ptrCast is TypeDB.IntType ptrType)
                        {
                            return cast_var(pointee!, ptrType);
                        }
                        return pointee!;
                    }
                    break;
            }

            return value switch
            {
                bool b => eval_prefix(b, kind),
                byte b => eval_prefix(b, kind),
                int i => eval_prefix(i, kind),
                long l => eval_prefix(l, kind),
                nint ni => eval_prefix(ni, kind),
                nuint nu => eval_prefix(nu, kind),
                sbyte sb => eval_prefix(sb, kind),
                short s => eval_prefix(s, kind),
                uint u => eval_prefix(u, kind),
                ulong ul => eval_prefix(ul, kind),
                ushort us => eval_prefix(us, kind),
                IntConstExpr ice => eval_prefix(ice, kind),
                UnknownValueBase unk => unk.UnaryOp(kind),
                _ => throw new NotSupportedException($"Unary operator '{kind}' is not supported for {value.GetType()}.")
            };
        }

        // !x
        // -x
        // +x
        // ~x
        // --x
        // ++x
        object EvaluatePrefixExpression(PrefixUnaryExpressionSyntax expr)
        {
            var value = EvaluateExpression(expr.Operand);
            var retValue = eval_prefix(value, expr.Kind());

            if (expr.Kind() == SyntaxKind.PreDecrementExpression || expr.Kind() == SyntaxKind.PreIncrementExpression)
            {
                switch (expr.Operand)
                {
                    case IdentifierNameSyntax id:
                        int var_id = _varDict.Set(id, retValue);
                        // VarsWritten.Add(var_id);
                        // VarsRead.Add(var_id);
                        break;
                    case LiteralExpressionSyntax num:
                        retValue = value;
                        _logger.warn_once($"Prefix operator '{expr.TitleWithLineNo()}' on literal '{num}'. Returning {retValue}");
                        break;
                    default:
                        throw new NotSupportedException($"Prefix operator '{expr.Kind()}' is not supported for {expr.Operand.Kind()}.");
                }
            }

            return retValue;
        }

        static INumber<T> eval_postfix<T>(T value, SyntaxKind kind) where T : INumber<T>, IBitwiseOperators<T, T, T>
        {
            return kind switch
            {
                SyntaxKind.PostDecrementExpression => value - T.One,
                SyntaxKind.PostIncrementExpression => value + T.One,
                _ => throw new NotSupportedException($"Postfix operator '{kind}' is not supported for {typeof(T)}.")
            };
        }

        // x--
        // x++
        object EvaluatePostfixExpression(PostfixUnaryExpressionSyntax expr)
        {
            var value = EvaluateExpression(expr.Operand);
            var retValue = value;
            var kind = expr.Kind();
            object newValue = value switch
            {
                byte b => eval_postfix(b, kind),
                int i => eval_postfix(i, kind),
                long l => eval_postfix(l, kind),
                nint ni => eval_postfix(ni, kind),
                nuint nu => eval_postfix(nu, kind),
                sbyte sb => eval_postfix(sb, kind),
                short s => eval_postfix(s, kind),
                uint u => eval_postfix(u, kind),
                ulong ul => eval_postfix(ul, kind),
                ushort us => eval_postfix(us, kind),
                UnknownValueBase u => u.UnaryOp(kind),
                _ => throw new NotSupportedException($"Unary postfix operator '{kind}' is not supported for {value.GetType()}.")
            };
            if (expr.Operand is IdentifierNameSyntax idName)
            {
                _varDict.Set(idName, newValue);
                // VarsWritten.Add(var_id);
                // VarsRead.Add(var_id);
            }
            else
            {
                throw new NotSupportedException($"Postfix operand '{expr.Operand.Kind()}' is not supported.");
            }
            return retValue;
        }

        BinaryExpressionSyntax? extract_common_factors(ExpressionSyntax left, ExpressionSyntax right)
        {
            left = left.StripParentheses();
            right = right.StripParentheses();
            {
                if (
                        left is BinaryExpressionSyntax lb && lb.IsKind(SyntaxKind.MultiplyExpression) &&
                        right is BinaryExpressionSyntax rb && rb.IsKind(SyntaxKind.MultiplyExpression)
                   )
                {
                    var lbLeft = lb.Left.StripParentheses();
                    var lbRight = lb.Right.StripParentheses();
                    var rbLeft = rb.Left.StripParentheses();
                    var rbRight = rb.Right.StripParentheses();

                    // Case A1: (lit1 * id) + (lit2 * id)
                    if (lbLeft is LiteralExpressionSyntax lc1 && lbRight is IdentifierNameSyntax lv1 &&
                            rbLeft is LiteralExpressionSyntax rc1 && rbRight is IdentifierNameSyntax rv1 &&
                            lv1.IsSameVar(rv1))
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv1,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc1, rc1)
                                    )
                                );
                    }

                    // Case A2: (id * lit1) + (lit2 * id)
                    if (lbLeft is IdentifierNameSyntax lv2 && lbRight is LiteralExpressionSyntax lc2 &&
                            rbLeft is LiteralExpressionSyntax rc2 && rbRight is IdentifierNameSyntax rv2 &&
                            lv2.IsSameVar(rv2))
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv2,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc2, rc2)
                                    )
                                );
                    }

                    // Case A3: (lit1 * id) + (id * lit2)
                    if (lbLeft is LiteralExpressionSyntax lc3 && lbRight is IdentifierNameSyntax lv3 &&
                            rbLeft is IdentifierNameSyntax rv3 && rbRight is LiteralExpressionSyntax rc3 &&
                            lv3.IsSameVar(rv3))
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv3,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc3, rc3)
                                    )
                                );
                    }

                    // Case A4: (id * lit1) + (id * lit2)
                    if (lbLeft is IdentifierNameSyntax lv4 && lbRight is LiteralExpressionSyntax lc4 &&
                            rbLeft is IdentifierNameSyntax rv4 && rbRight is LiteralExpressionSyntax rc4 &&
                            lv4.IsSameVar(rv4))
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv4,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc4, rc4)
                                    )
                                );
                    }
                }
            }

            {
                if (
                        left is BinaryExpressionSyntax lb && lb.IsKind(SyntaxKind.MultiplyExpression) &&
                        right is IdentifierNameSyntax rid
                   )
                {
                    var lbLeft = lb.Left.StripParentheses();
                    var lbRight = lb.Right.StripParentheses();

                    // Case B1: (lit1 * id) + id
                    if (lbLeft is LiteralExpressionSyntax lc1 && lbRight is IdentifierNameSyntax lv1 && lv1.IsSameVar(rid))
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv1,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc1, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                    )
                                );
                    }

                    // Case A2: (id * lit1) + id
                    if (lbLeft is IdentifierNameSyntax lv2 && lbRight is LiteralExpressionSyntax lc2 && lv2.IsSameVar(rid))
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                lv2,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, lc2, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                    )
                                );
                    }
                }
            }

            {
                if (
                        left is IdentifierNameSyntax lid &&
                        right is BinaryExpressionSyntax rb && rb.IsKind(SyntaxKind.MultiplyExpression)
                   )
                {
                    var rbLeft = rb.Left.StripParentheses();
                    var rbRight = rb.Right.StripParentheses();

                    // Case C1: id + (lit1 * id)
                    if (rbLeft is LiteralExpressionSyntax rc1 && rbRight is IdentifierNameSyntax rv1 && rv1.IsSameVar(lid))
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                rv1,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, rc1, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                    )
                                );
                    }

                    // Case C2: id + (id * lit1)
                    if (rbLeft is IdentifierNameSyntax rv2 && rbRight is LiteralExpressionSyntax rc2 && rv2.IsSameVar(lid))
                    {
                        return BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                rv2,
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.AddExpression, rc2, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                    )
                                );
                    }
                }
            }

            return null;
        }

        bool is_always_false(BinaryExpressionSyntax binaryExpr)
        {
            {
                if (binaryExpr.Left is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.IsKind(SyntaxKind.BitwiseNotExpression) &&
                        unaryExpr.Operand is IdentifierNameSyntax lid && binaryExpr.Right is IdentifierNameSyntax rid &&
                        lid.IsSameVar(rid) && binaryExpr.OperatorToken.Text == "==")
                    return true; // ~x == x
            }

            {
                if (binaryExpr.Right is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.IsKind(SyntaxKind.BitwiseNotExpression) &&
                        unaryExpr.Operand is IdentifierNameSyntax rid && binaryExpr.Left is IdentifierNameSyntax lid &&
                        lid.IsSameVar(rid) && binaryExpr.OperatorToken.Text == "==")
                    return true; // x == ~x
            }

            return false;
        }

        bool is_always_true(BinaryExpressionSyntax binaryExpr)
        {
            {
                if (binaryExpr.Left is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.IsKind(SyntaxKind.BitwiseNotExpression) &&
                        unaryExpr.Operand is IdentifierNameSyntax lid && binaryExpr.Right is IdentifierNameSyntax rid &&
                        lid.IsSameVar(rid) && binaryExpr.OperatorToken.Text == "!=")
                    return true; // ~x != x
            }

            {
                if (binaryExpr.Right is PrefixUnaryExpressionSyntax unaryExpr && unaryExpr.IsKind(SyntaxKind.BitwiseNotExpression) &&
                        unaryExpr.Operand is IdentifierNameSyntax rid && binaryExpr.Left is IdentifierNameSyntax lid &&
                        lid.IsSameVar(rid) && binaryExpr.OperatorToken.Text == "!=")
                    return true; // x != ~x
            }

            return false;
        }

        // TODO: check performance
        // bool IsPromotionRequired_fast<TL, TR>(TL l, TR r)
        //     where TL : INumber<TL>, IBitwiseOperators<TL, TL, TL>
        //     where TR : INumber<TR>, IBitwiseOperators<TR, TR, TR>
        // {
        //     // promote & materialize IntConstExprs, if any
        //     if ((l is IntConstExpr && r is not IntConstExpr) || (r is IntConstExpr))
        //         return true;
        // 
        //     int sizeL = Unsafe.SizeOf<TL>();
        //     if (sizeL < 4)
        //         return true;
        // 
        //     int sizeR = Unsafe.SizeOf<TR>();
        //     if (sizeR < 4)
        //         return true;
        // 
        //     if (sizeL != sizeR)
        //         return true;
        // 
        //     return typeof(TL) != typeof(TR);
        // }

        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12473-binary-numeric-promotions
        bool IsPromotionRequired(object l, object r)
        {
            // promote & materialize IntConstExprs, if any
            if ((l is IntConstExpr && r is not IntConstExpr) || (r is IntConstExpr))
                return true;

            if (l is null || r is null)
                return false; // null is not a number, so no promotion is needed

            if (ReferenceEquals(l.GetType(), r.GetType()))
            {
                if (l is int or uint or long or ulong)
                    return false; // fast path, same type and >= 4 bytes
            }

            int sizeL = TypeDB.SizeOf(l);
            if (sizeL < 4)
                return true;

            int sizeR = TypeDB.SizeOf(r);
            if (sizeR < 4)
                return true;

            if (sizeL != sizeR)
                return true;

            return l.GetType() != r.GetType();
        }

        object eval_binary<TL, TR>(TL l, SyntaxKind kind, TR r)
            where TL : INumber<TL>, IBitwiseOperators<TL, TL, TL>
            where TR : INumber<TR>, IBitwiseOperators<TR, TR, TR>
        {
            _logger.debug(() => $"[d] eval_binary: ({l.GetType()}) {l} {kind} ({r.GetType()}) {r}");

            return kind switch
            {
                SyntaxKind.AddExpression => (dynamic)l + (dynamic)r,
                SyntaxKind.SubtractExpression => (dynamic)l - (dynamic)r,
                SyntaxKind.MultiplyExpression => (dynamic)l * (dynamic)r,
                SyntaxKind.DivideExpression => (dynamic)l / (dynamic)r,
                SyntaxKind.ModuloExpression => (dynamic)l % (dynamic)r,
                SyntaxKind.BitwiseAndExpression => (dynamic)l & (dynamic)r,
                SyntaxKind.BitwiseOrExpression => (dynamic)l | (dynamic)r,
                SyntaxKind.ExclusiveOrExpression => (dynamic)l ^ (dynamic)r,
                SyntaxKind.LeftShiftExpression => (dynamic)l << (int)(dynamic)r,
                SyntaxKind.RightShiftExpression => (dynamic)l >> (int)(dynamic)r,
                SyntaxKind.UnsignedRightShiftExpression => (l, r) switch
                {
                    // FIXME: add more types?
                    (int li, int ru) => li >>> ru,
                    (uint lu, int ru) => lu >>> ru,
                    (ulong lu, int ru) => lu >>> ru,
                    (nuint lu, int ru) => lu >>> ru,
                    _ => throw new NotSupportedException(">>> is only supported for uint, ulong, nuint with int shift count")
                },
                SyntaxKind.EqualsExpression => l.Equals(r),
                SyntaxKind.NotEqualsExpression => !l.Equals(r),
                SyntaxKind.LessThanExpression => (dynamic)l < (dynamic)r,
                SyntaxKind.LessThanOrEqualExpression => (dynamic)l <= (dynamic)r,
                SyntaxKind.GreaterThanExpression => (dynamic)l > (dynamic)r,
                SyntaxKind.GreaterThanOrEqualExpression => (dynamic)l >= (dynamic)r,
                SyntaxKind.LogicalAndExpression => Convert.ToBoolean(l) && Convert.ToBoolean(r),
                SyntaxKind.LogicalOrExpression => Convert.ToBoolean(l) || Convert.ToBoolean(r),
                _ => throw new NotSupportedException($"Binary operator '{kind}' is not supported for {typeof(TL)} and {typeof(TR)}.")
            };
        }

        object eval_binary_and(BinaryExpressionSyntax binaryExpr, object lValue)
        {
            if (lValue is UnknownValueBase luvb)
                lValue = luvb.Cast(TypeDB.Bool);

            switch (lValue)
            {
                case bool b:
                    return b ? EvaluateExpression(binaryExpr.Right) : false;
                case UnknownValueBase: // nor true nor false
                    var rValue = EvaluateExpression(binaryExpr.Right);
                    if (rValue is UnknownValueBase ruvb)
                        rValue = ruvb.Cast(TypeDB.Bool);
                    if (rValue is bool rb && rb == false)
                        return false; // a whole expression is always false because 2nd operand is always false
                    break;
                default:
                    throw new NotSupportedException($"Left operand '{lValue?.GetType()}' is not supported for '&&' operator.");
            }
            return UnknownValue.Create(TypeDB.Bool);
        }

        object eval_binary_or(BinaryExpressionSyntax binaryExpr, object lValue)
        {
            if (lValue is UnknownValueBase luvb)
                lValue = luvb.Cast(TypeDB.Bool);

            switch (lValue)
            {
                case bool b:
                    return b ? true : EvaluateExpression(binaryExpr.Right);
                case UnknownValueBase: // nor true nor false
                    var rValue = EvaluateExpression(binaryExpr.Right);
                    if (rValue is UnknownValueBase ruvb)
                        rValue = ruvb.Cast(TypeDB.Bool);
                    if (rValue is bool rb && rb == true)
                        return true; // a whole expression is always true because 2nd operand is always true
                    break;
                default:
                    throw new NotSupportedException($"Left operand '{lValue?.GetType()}' is not supported for '&&' operator.");
            }
            return UnknownValue.Create(TypeDB.Bool);
        }

        static readonly TypeSyntax NINT_TYPE = ParseTypeName("nint");
        static readonly TypeSyntax NUINT_TYPE = ParseTypeName("nuint");

        object EvaluateBinaryExpression(BinaryExpressionSyntax binaryExpr)
        {
            // handle falsely misinterpreted "(nint)-17648"
            // only 'nint' and 'nuint' are affected because they are not in PredefinedTypes
            if (binaryExpr.Left is ParenthesizedExpressionSyntax parenExpr
                    && binaryExpr.OperatorToken.Text == "-"
                    && binaryExpr.Right.StripParentheses() is LiteralExpressionSyntax litR)
            {
                string ls = parenExpr.StripParentheses().ToString();
                switch (ls)
                {
                    case "nint":
                        return cast_var(EvaluatePrefixExpression(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, litR)), NINT_TYPE);
                    case "nuint":
                        return cast_var(EvaluatePrefixExpression(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, litR)), NUINT_TYPE);
                }
            }

            if (is_always_false(binaryExpr))
                return false;

            if (is_always_true(binaryExpr))
                return true;

            var kind = binaryExpr.Kind();

            // convert (4 * x + x * 4) => (4 + 4) * x
            // bc its easier then to evaluate when checking unknown values
            if (kind == SyntaxKind.AddExpression)
            {
                var factoredExpr = extract_common_factors(binaryExpr.Left, binaryExpr.Right);
                if (factoredExpr is not null)
                    return EvaluateBinaryExpression(factoredExpr);
            }

            object? lValue = EvaluateExpression(binaryExpr.Left); // always evaluated

            switch (kind)
            {
                case SyntaxKind.LogicalAndExpression:
                    return eval_binary_and(binaryExpr, lValue);
                case SyntaxKind.LogicalOrExpression:
                    return eval_binary_or(binaryExpr, lValue);
            }

            // evaluate rValue, handle everything else
            object? rValue = EvaluateExpression(binaryExpr.Right); // NOT always evaluated

            if (lValue is null || rValue is null)
            {
                return kind switch
                {
                    SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression => UnknownValue.Create(TypeDB.Bool),
                    _ => UnknownValue.Create()
                };
            }

            if (lValue is UnknownValueBase luv)
                return luv.BinaryOp(kind, rValue);

            if (rValue is UnknownValueBase ruv)
                return ruv.InverseBinaryOp(kind, lValue);

            if (IsPromotionRequired(lValue, rValue))
            {
                var (lp, rp) = VarProcessor.PromoteInts(lValue, rValue);
                return eval_binary((dynamic)lp, binaryExpr.Kind(), (dynamic)rp);
            }

            return eval_binary((dynamic)lValue, binaryExpr.Kind(), (dynamic)rValue);
        }
    }
}
