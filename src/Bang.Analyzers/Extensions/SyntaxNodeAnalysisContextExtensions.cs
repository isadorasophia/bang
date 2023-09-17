using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers.Extensions;

public static class SyntaxNodeAnalysisContextExtensions
{
    public static void ReportDiagnosticIfLackingAttribute(
        this SyntaxNodeAnalysisContext context,
        INamedTypeSymbol type,
        INamedTypeSymbol? attributeToCheck,
        DiagnosticDescriptor diagnosticDescriptor
    )
    {
        var hasAttribute = RecursivelyCheckForAttribute(type, attributeToCheck);
        context.ConditionallyReportDiagnostic(diagnosticDescriptor, !hasAttribute);
    }

    public static void ReportDiagnosticIfAttributeExists(
        this SyntaxNodeAnalysisContext context,
        INamedTypeSymbol type,
        INamedTypeSymbol? attributeToCheck,
        DiagnosticDescriptor diagnosticDescriptor
    )
    {
        var hasAttribute = RecursivelyCheckForAttribute(type, attributeToCheck);
        context.ConditionallyReportDiagnostic(diagnosticDescriptor, hasAttribute);
    }

    private static bool RecursivelyCheckForAttribute(
        INamedTypeSymbol type,
        ISymbol? attributeToCheck)
    {
        bool hasAttribute;
        var typeToCheck = type;
        do
        {
            hasAttribute = typeToCheck
                .GetAttributes()
                .Any(attr =>
                    attr.AttributeClass is not null && attr.AttributeClass.Equals(attributeToCheck,
                        SymbolEqualityComparer.IncludeNullability));

            if (!hasAttribute)
            {
                typeToCheck = typeToCheck.BaseType;
            }

        } while (!hasAttribute && typeToCheck != null);

        return hasAttribute;
    }

    private static void ConditionallyReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor diagnosticDescriptor,
        bool condition
    )
    {
        if (!condition)
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                diagnosticDescriptor,
                context.Node.GetLocation()
            )
        );
    }
}