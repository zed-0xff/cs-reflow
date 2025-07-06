using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

public static class MethodDeclarationSyntaxExtensions
{
    public static string ExplicitName(this MethodDeclarationSyntax method)
    {
        // If the method has an explicit interface implementation, return the full name
        if (method.ExplicitInterfaceSpecifier != null)
        {
            return $"{method.ExplicitInterfaceSpecifier.Name}.{method.Identifier.Text}";
        }

        // Otherwise, return just the method name
        return method.Identifier.Text;
    }

    public static string FullName(this MethodDeclarationSyntax method)
    {
        var sb = new StringBuilder();

        // Climb up the syntax tree to collect type/namespace nesting
        foreach (var ancestor in method.Ancestors())
        {
            switch (ancestor)
            {
                // case NamespaceDeclarationSyntax ns:
                //     sb.Insert(0, ns.Name + ".");
                //     break;
                // case FileScopedNamespaceDeclarationSyntax fns:
                //     sb.Insert(0, fns.Name + ".");
                //     break;
                case TypeDeclarationSyntax type:
                    sb.Insert(0, type.Identifier.Text + ".");
                    break;
            }
        }

        sb.Append(ExplicitName(method));
        return sb.ToString();
    }
}
