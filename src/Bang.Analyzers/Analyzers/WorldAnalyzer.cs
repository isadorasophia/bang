using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WorldAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MarkUniqueComponentAsUnique = new(
        id: Diagnostics.World.MarkUniqueComponentAsUnique.Id,
        title: nameof(WorldAnalyzer) + "." + nameof(MarkUniqueComponentAsUnique),
        messageFormat: Diagnostics.World.MarkUniqueComponentAsUnique.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When retrieving components using GetUniqueEntity, consider marking that entity as [Unique]."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MarkUniqueComponentAsUnique);

    public override void Initialize(AnalysisContext context)
    {
        var syntaxKind = ImmutableArray.Create(SyntaxKind.InvocationExpression);

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(Analyze, syntaxKind);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        // Bail if World is not resolvable.
        var world = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.WorldType);
        if (world is null)
            return;

        // Bail if UniqueAttribute is not resolvable.
        var uniqueAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.UniqueAttribute);
        if (uniqueAttribute is null)
            return;

        var invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;
        if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            return;

        // Bail if the methods are not the 4 methods in Bang.World that we are looking for.
        if (!memberAccessExpression.Name.ToString().Contains("GetUnique"))
            return;

        // Bail out this call is not for the type Bang.World.
        var memberAccessNode = memberAccessExpression.ChildNodes().FirstOrDefault();
        if (memberAccessNode is null)
            return;

        if (context.SemanticModel.GetTypeInfo(memberAccessNode).Type is not INamedTypeSymbol typeInfo ||
            !typeInfo.Equals(world, SymbolEqualityComparer.IncludeNullability))
            return;

        // Bail out if not one of the methods we are verifying 

        // TODO: Bail if not part of za warudo
        var genericArgument = invocationExpressionSyntax
            .DescendantNodes()
            .OfType<GenericNameSyntax>()
            .FirstOrDefault()
            ?.TypeArgumentList
            .Arguments
            .FirstOrDefault();

        if (genericArgument is null)
            return;

        if (context.SemanticModel.GetSymbolInfo(genericArgument).Symbol is not ITypeSymbol genericArgumentTypeSymbol)
            return;

        var hasUniqueAttribute =
            genericArgumentTypeSymbol
                .GetAttributes()
                .Any(a => a.AttributeClass?.Equals(uniqueAttribute, SymbolEqualityComparer.IncludeNullability) ?? false);

        if (hasUniqueAttribute)
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                MarkUniqueComponentAsUnique,
                genericArgument.GetLocation()
            )
        );
    }
}