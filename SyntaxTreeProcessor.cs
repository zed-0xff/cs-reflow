using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;

public class SyntaxTreeProcessor
{
    protected string NodeTitle(SyntaxNode node)
    {
        return node.ToString().Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }
}
