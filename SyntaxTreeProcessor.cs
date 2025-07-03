using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System;

public class SyntaxTreeProcessor
{
    // shared
    protected SyntaxTree _tree = null!; // I know _tree is non-nullable, but I will assign it later, trust me.

    // configuration
    const int DEFAULT_VERBOSITY = 0;

    public int Verbosity = DEFAULT_VERBOSITY;
    public bool ShowProgress = true;

    // local
    protected Stopwatch _stopWatch = Stopwatch.StartNew();

    // for cloning
    protected SyntaxTreeProcessor() { }

    protected SyntaxTreeProcessor(string code, int verbosity = DEFAULT_VERBOSITY, bool dummyClassWrap = false)
    {
        Verbosity = verbosity;

        if (dummyClassWrap)
        {
            // without the dummy class methods are defined as LocalFunctions, and SemanticModel leaks variables from one method to another
            // not adding newlines to keep original line numbers
            code = "class DummyClass { " + code + " }";
        }
        update_progress("parsing code");
        _tree = CSharpSyntaxTree.ParseText(code);
    }

    public string ElapsedTime()
    {
        return _stopWatch.Elapsed.ToString(@"mm\:ss");
    }

    protected void update_progress(string msg)
    {
        if (Verbosity >= 0 && ShowProgress)
        {
            msg = $"[{ElapsedTime()}] {msg} ..";
            if (Verbosity == 0)
                Console.Error.Write(msg + ANSI.ERASE_TILL_EOL + "\r");
            else
                Console.WriteLine(msg);
        }
    }

    public Dictionary<int, string> Methods => _tree.GetRoot().DescendantNodes()
        .Where(n => n is BaseMethodDeclarationSyntax /*|| n is LocalFunctionStatementSyntax*/)
        .ToDictionary(
                n => _tree.GetLineSpan(new TextSpan(n.SpanStart, 0)).StartLinePosition.Line + 1,  // 1-based line number
                n => n switch
                {
                    MethodDeclarationSyntax m => m.Identifier.Text,
                    ConstructorDeclarationSyntax c => c.Identifier.Text,
                    DestructorDeclarationSyntax d => d.Identifier.Text,
                    LocalFunctionStatementSyntax l => l.Identifier.Text,
                    _ => "<unknown>"
                });

    public SyntaxNode GetMethod(string methodName)
    {
        if (int.TryParse(methodName, out int lineno))
            return GetMethod(lineno);

        var methods = _tree.GetRoot().DescendantNodes()
            .Where(n =>
                    (n is MethodDeclarationSyntax m && m.Identifier.Text == methodName) ||
                    //(n is LocalFunctionStatementSyntax l && l.Identifier.Text == methodName) ||
                    (n is ConstructorDeclarationSyntax c && c.Identifier.Text == methodName)
                  )
            .ToList();

        switch (methods.Count())
        {
            case 0:
                throw new ArgumentException($"Method '{methodName}' not found.");
            case 1:
                return (CSharpSyntaxNode)methods.First();
            default:
                throw new ArgumentException($"Multiple methods with the name '{methodName}' found.");
        }
    }

    public SyntaxNode GetMethod(int lineno)
    {
        var linePosition = _tree.GetText().Lines[lineno].Start;
        var result = _tree.GetRoot()
            .DescendantNodes()
            .OfType<BaseMethodDeclarationSyntax>()
            .FirstOrDefault(b => b.SpanStart <= linePosition && b.Span.End > linePosition);

        if (result == null)
            throw new ArgumentException($"Method at line {lineno} not found.");

        return result;
    }
}
