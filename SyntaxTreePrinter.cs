using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System;

class SyntaxTreePrinter : SyntaxTreeProcessor
{
    VarProcessor variableProcessor = new VarProcessor(new VarDB()); // XXX empty vardb

    private SyntaxTree tree;
    private SyntaxNode root;
    public int Verbosity;

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
        this.tree = tree;
        this.root = tree.GetRoot();
    }

    public SyntaxTreePrinter(string code)
    {
        tree = CSharpSyntaxTree.ParseText(code);
        root = tree.GetRoot();
    }

    public void Print(SyntaxNode node, int indent = 0)
    {
        // Add indentation based on the level
        string indent_str = new string(' ', indent * 2);  // 2 spaces per level of indentation
        string line = NodeTitle(node);

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
        if (color != null)
        {
            Console.Write(color);
        }
        Console.WriteLine($"{lineNumber.ToString().PadRight(8)}{indent_str}{node.GetType().Name}: {line}");
        if (color != null)
        {
            Console.Write(ANSI.COLOR_RESET);
        }

        if (node is ExpressionStatementSyntax && Verbosity < 1)
            return; // Skip printing child nodes for expression statements if verbosity is low

        foreach (var child in node.ChildNodes())
        {
            Print(child, indent + 1);
        }
    }

    private Dictionary<string, LabeledStatementSyntax> _labels = new();

    public List<StatementSyntax> TraceMethod(string methodName)
    {
        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == methodName);

        if (method == null || method.Body == null)
            throw new ArgumentException($"Method '{methodName}' not found or has no body.");

        return TraceBlock(method.Body);
    }

    public List<string> Methods => root.DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(m => m.Identifier.Text)
        .ToList();

    public List<StatementSyntax> TraceBlock(BlockSyntax block)
    {
        var traced = new List<StatementSyntax>();
        var visited = new HashSet<StatementSyntax>();

        // Index labels for quick lookup
        foreach (var stmt in block.Statements.OfType<LabeledStatementSyntax>())
        {
            var labelName = stmt.Identifier.Text;
            _labels[labelName] = stmt;
        }

        // Start tracing from the top
        StatementSyntax? current = block.Statements.FirstOrDefault();
        while (current != null)
        {
            if (!visited.Add(current))
                break; // prevent infinite loops

            var lineno = current.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            Console.WriteLine($"{lineno.ToString().PadLeft(6)}: {NodeTitle(current)}");
            traced.Add(current);

            switch (current)
            {
                case GotoStatementSyntax gotoStmt:
                    if (gotoStmt.Expression is IdentifierNameSyntax labelId &&
                            _labels.TryGetValue(labelId.Identifier.Text, out var targetLabel))
                    {
                        current = targetLabel.Statement;
                        continue;
                    }
                    throw new ArgumentException($"Label '{gotoStmt.Expression}' not found.");

                case LabeledStatementSyntax labeled:
                    current = labeled.Statement;
                    continue;

                case WhileStatementSyntax whileStmt:
                    var cond = whileStmt.Condition.ToString();
                    if ((cond == "true" || cond == "1") && whileStmt.Statement is BlockSyntax whileBlk)
                    {
                        TraceBlock(whileBlk);
                        continue;
                    }
                    throw new NotImplementedException("While statements are not supported yet.");

                case IfStatementSyntax ifStmt:
                    // Optionally handle branching here, or assume fall-through
                    //current = GetNextStatement(current, block);
                    //continue;
                    throw new NotImplementedException("If statements are not supported yet.");

                case ReturnStatementSyntax:
                    throw new ReturnException(current.LineNo());

                default:
                    current = GetNextStatement(current, block);
                    continue;
            }
        }

        return traced;
    }

    private StatementSyntax? GetNextStatement(StatementSyntax current, BlockSyntax block)
    {
        var statements = block.Statements;
        int index = statements.IndexOf(current);
        return index >= 0 && index + 1 < statements.Count ? statements[index + 1] : null;
    }

    public void PrintMethod(string methodName)
    {
        var methodNode = root.DescendantNodes()
            .FirstOrDefault(n =>
                    (n is MethodDeclarationSyntax m && m.Identifier.Text == methodName) ||
                    (n is LocalFunctionStatementSyntax l && l.Identifier.Text == methodName));

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
        if (method.Body != null)
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
        if (method.Body != null)
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
        Print(root);
    }
}
