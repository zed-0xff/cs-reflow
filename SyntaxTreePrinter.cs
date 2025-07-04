using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System;

class SyntaxTreePrinter : SyntaxTreeProcessor
{
    VarProcessor variableProcessor = new VarProcessor(new VarDB()); // XXX empty vardb

    class ReturnException : Exception
    {
        public object result;
        public ReturnException(object result) : base($"return {result}")
        {
            this.result = result;
        }
    }

    public SyntaxTreePrinter(SyntaxTree tree)
    {
        _tree = tree;
    }

    public SyntaxTreePrinter(string code) : base(code)
    {
    }

    public void Print(SyntaxNode node, int indent = 0)
    {
        // Add indentation based on the level
        string indent_str = new string(' ', indent * 2);  // 2 spaces per level of indentation
        string line = node.Title();

        var lineSpan = node.GetLocation().GetLineSpan();
        int lineNumber = lineSpan.StartLinePosition.Line + 1;
        string color = "";

        try
        {
            switch (node)
            {
                case LocalDeclarationStatementSyntax l:
                    color = ANSI.COLOR_LIGHT_BLUE;
                    variableProcessor.EvaluateExpression(l);
                    break;
                case ExpressionStatementSyntax e:
                    color = ANSI.COLOR_LIGHT_CYAN;
                    var r1 = variableProcessor.EvaluateExpression(e.Expression);
                    line += $" => {r1}";
                    break;
                case SwitchStatementSyntax sw:
                    color = ANSI.COLOR_LIGHT_MAGENTA;
                    var r2 = variableProcessor.EvaluateExpression(sw.Expression);
                    line += $" => {r2}";
                    break;
            }
        }
        catch (Exception ex)
        {
            line += $" => {ANSI.COLOR_LIGHT_RED}{ex.Message}{ANSI.COLOR_RESET}";
        }

        // Print the node type and its text representation
        if (color is not null)
            Console.Write(color);

        line = $"{lineNumber.ToString().PadRight(8)}{indent_str}{node.GetType().Name}: {line}";
        Console.Write(line);

        if (color is not null)
            Console.Write(ANSI.COLOR_RESET);

        if (ShowAnnotations)
        {
            var ann_str = node.AnnotationsAsString();
            if (ann_str is not null)
            {
                string pad = new string(' ', Math.Max(0, commentPadding - line.Length));
                Console.Write(pad + $"// {ann_str}".Gray());
            }
        }
        Console.WriteLine();

        if (node is ExpressionStatementSyntax && Verbosity < 1)
            return; // Skip printing child nodes for expression statements if verbosity is low

        foreach (var child in node.ChildNodes())
        {
            Print(child, indent + 1);
        }
    }

    private Dictionary<string, LabeledStatementSyntax> _labels = new();

    public void PrintMethod(string methodName)
    {
        var methodNode = GetMethod(methodName);

        switch (methodNode)
        {
            case MethodDeclarationSyntax method:
                PrintMethod(method);
                break;
            case LocalFunctionStatementSyntax localFunction:
                PrintMethod(localFunction);
                break;
            default:
                throw new InvalidOperationException($"Node '{methodName}' is not a method or local function.");
        }
    }

    public void PrintMethod(LocalFunctionStatementSyntax method)
    {
        Console.WriteLine($"{method.Identifier}()");

        // Check if the method has a body
        if (method.Body is not null)
        {
            Print(method.Body, 1);  // Start from indent level 1 for the method body
        }
        else
        {
            Console.WriteLine("This method doesn't have a body (it might be an abstract or interface method).");
        }
    }

    public void PrintMethod(MethodDeclarationSyntax method)
    {
        Console.WriteLine($"{method.Identifier}()");

        // Check if the method has a body
        if (method.Body is not null)
        {
            Print(method.Body, 1);  // Start from indent level 1 for the method body
        }
        else
        {
            Console.WriteLine("This method doesn't have a body (it might be an abstract or interface method).");
        }
    }

    public void Print()
    {
        Print(_tree.GetRoot());
    }
}
