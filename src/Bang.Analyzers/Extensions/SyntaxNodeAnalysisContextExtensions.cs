using System.Collections.Immutable;
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
        var hasAttribute = type.GetAttributes().Any(
            attr => attr.AttributeClass is not null && attr.AttributeClass.Equals(attributeToCheck, SymbolEqualityComparer.IncludeNullability));

        context.ConditionallyReportDiagnostic(diagnosticDescriptor, !hasAttribute);
    }

    public static void ReportDiagnosticIfAttributeExists(
        this SyntaxNodeAnalysisContext context,
        INamedTypeSymbol type,
        INamedTypeSymbol? attributeToCheck,
        DiagnosticDescriptor diagnosticDescriptor
    )
    {
        var hasAttribute = type.GetAttributes().Any(
            attr => attr.AttributeClass is not null && attr.AttributeClass.Equals(attributeToCheck, SymbolEqualityComparer.IncludeNullability));

        context.ConditionallyReportDiagnostic(diagnosticDescriptor, hasAttribute);
    }

    public static void ConditionallyReportDiagnostic(
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