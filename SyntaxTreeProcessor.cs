using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;

public class SyntaxTreeProcessor
{
    protected const string ANSI_COLOR_RESET = "\x1b[0m";
    protected const string ANSI_COLOR_CYAN = "\x1b[36m";
    protected const string ANSI_COLOR_LIGHT_CYAN = "\x1b[96m";
    protected const string ANSI_COLOR_LIGHT_YELLOW = "\x1b[93m";
    protected const string ANSI_COLOR_BLUE = "\x1b[34m";
    protected const string ANSI_COLOR_LIGHT_BLUE = "\x1b[94m";
    protected const string ANSI_COLOR_MAGENTA = "\x1b[35m";
    protected const string ANSI_COLOR_LIGHT_MAGENTA = "\x1b[95m";
    protected const string ANSI_COLOR_RED = "\x1b[31m";
    protected const string ANSI_COLOR_LIGHT_RED = "\x1b[91m";

    protected string NodeTitle(SyntaxNode node)
    {
        return node.ToString().Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }
}
