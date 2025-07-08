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
    protected List<SyntaxTree> _trees = new();

    // configuration
    const int DEFAULT_VERBOSITY = 0;

    public int Verbosity = DEFAULT_VERBOSITY;
    public bool ShowProgress = true;
    public bool ShowAnnotations = false;
    public int commentPadding = 100;

    // local
    protected Stopwatch _stopWatch = Stopwatch.StartNew();

    // for cloning
    protected SyntaxTreeProcessor() { }

    protected SyntaxTreeProcessor(OrderedDictionary<string, string> codes, int verbosity = DEFAULT_VERBOSITY, bool dummyClassWrap = false, bool showProgress = true)
    {
        ShowProgress = showProgress;
        Verbosity = verbosity;

        update_progress("parsing codes");
        foreach (var kv in codes)
        {
            var code = kv.Value;
            if (dummyClassWrap)
            {
                // without the dummy class methods are defined as LocalFunctions, and SemanticModel leaks variables from one method to another
                // not adding newlines to keep original line numbers
                code = "class DummyClass { " + code + " }";
            }
            _tree = CSharpSyntaxTree.ParseText(code);
            _trees.Add(_tree);
        }
        _trees.Remove(_tree); // remove last tree bc it will be modified
    }

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
            msg = $"[{ElapsedTime()}] {msg}..";
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
                    MethodDeclarationSyntax m => m.FullName(),
                    ConstructorDeclarationSyntax c => c.Identifier.Text,
                    DestructorDeclarationSyntax d => d.Identifier.Text,
                    LocalFunctionStatementSyntax l => l.Identifier.Text,
                    _ => "<unknown>"
                });

    public List<SyntaxNode> GetMethods(string methodName)
    {
        if (int.TryParse(methodName, out int lineno))
            return new List<SyntaxNode> { GetMethod(lineno) };

        return _tree.GetRoot().DescendantNodes()
            .Where(n =>
                    (n is MethodDeclarationSyntax m && m.FullName() == methodName) ||
                    //(n is LocalFunctionStatementSyntax l && l.Identifier.Text == methodName) ||
                    (n is ConstructorDeclarationSyntax c && c.Identifier.Text == methodName)
                  )
            .ToList();
    }

    public SyntaxNode GetMethod(string methodNameOrLineNo)
    {
        var methods = GetMethods(methodNameOrLineNo);

        switch (methods.Count())
        {
            case 0:
                throw new ArgumentException($"Method '{methodNameOrLineNo}' not found.");
            case 1:
                return (CSharpSyntaxNode)methods.First();
            default:
                throw new ArgumentException($"Multiple methods with the name '{methodNameOrLineNo}' found.");
        }
    }

    public SyntaxNode GetMethod(int lineno)
    {
        var line = _tree.GetText().Lines[lineno - 1];
        var result = _tree.GetRoot()
            .DescendantNodes()
            .OfType<BaseMethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Span.IntersectsWith(line.Span));

        if (result is null)
            throw new ArgumentException($"Method at line {lineno} not found.");

        return result;
    }
}
