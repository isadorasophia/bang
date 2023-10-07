using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Bang.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor FilterAttribute = new(
        id: Diagnostics.Systems.FilterAttribute.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(FilterAttribute),
        messageFormat: Diagnostics.Systems.FilterAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Implementations of ISystem need to be annotated with FilterAttribute."
    );

    public static readonly DiagnosticDescriptor MessagerAttribute = new(
        id: Diagnostics.Systems.MessagerAttribute.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(MessagerAttribute),
        messageFormat: Diagnostics.Systems.MessagerAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Implementations of IMessagerSystem need to be annotated with MessagerAttribute."
    );

    public static readonly DiagnosticDescriptor WatchAttribute = new(
        id: Diagnostics.Systems.WatchAttribute.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(WatchAttribute),
        messageFormat: Diagnostics.Systems.WatchAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Implementations of IReactiveSystem need to be annotated with WatchAttribute."
    );

    public static readonly DiagnosticDescriptor NonApplicableMessagerAttribute = new(
        id: Diagnostics.Systems.NonApplicableMessagerAttribute.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(NonApplicableMessagerAttribute),
        messageFormat: Diagnostics.Systems.NonApplicableMessagerAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "MessagerAttribute will be ignored since this class does not implement IMessagerSystem."
    );

    public static readonly DiagnosticDescriptor NonApplicableWatchAttribute = new(
        id: Diagnostics.Systems.NonApplicableWatchAttribute.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(NonApplicableWatchAttribute),
        messageFormat: Diagnostics.Systems.NonApplicableWatchAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "WatchAttribute will be ignored since this class does not implement IReactiveSystem."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        FilterAttribute,
        MessagerAttribute,
        WatchAttribute,
        NonApplicableMessagerAttribute,
        NonApplicableWatchAttribute
    );

    public override void Initialize(AnalysisContext context)
    {
        var syntaxKind = ImmutableArray.Create(
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration
        );

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(Analyze, syntaxKind);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        // Bail if ISystem is not resolvable.
        var bangSystemInterface = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.SystemInterface);
        if (bangSystemInterface is null)
            return;

        // Bail if the node we are checking is not a type declaration.
        if (context.ContainingSymbol is not INamedTypeSymbol typeSymbol)
            return;

        if (context.Node is not TypeDeclarationSyntax typeDeclarationSyntax)
            return;

        // Bail if the current type declaration is not a system.
        var isSystem = typeSymbol.ImplementsInterface(bangSystemInterface);
        if (!isSystem)
            return;

        // Abstract types don't need to be annotated and can instead delegate their filters to subclasses.
        if (typeSymbol.IsAbstract)
            return;

        // IReactiveSystem and IMessagerSystem don't need the filter attribute.
        var filterIsOptional = false;

        var bangMessagerSystem = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.MessagerSystemInterface);
        if (bangMessagerSystem is not null)
        {
            var messagerAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.MessagerAttribute);
            if (typeSymbol.ImplementsInterface(bangMessagerSystem))
            {
                var hasAttribute = typeSymbol.RecursivelyCheckForAttribute(messagerAttribute);
                if (!hasAttribute)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            MessagerAttribute,
                            typeDeclarationSyntax.Identifier.GetLocation()
                        )
                    );
                }

                filterIsOptional = true;
            }
            else if (typeSymbol.HasAttribute(messagerAttribute))
            {
                // TODO: Check using aliases, full attribute name and normal attribute name
                var attributeSyntax = context.Node
                    .DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .FirstOrDefault(attributeSyntax =>
                    {
                        var attributeData = GetAttributeDataForSyntax(attributeSyntax, typeSymbol);
                        return attributeData.AttributeClass?.Equals(messagerAttribute, SymbolEqualityComparer.IncludeNullability) ?? false;
                    });

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        NonApplicableMessagerAttribute,
                        attributeSyntax?.GetLocation() ?? typeDeclarationSyntax.Identifier.GetLocation()
                    )
                );
            }
        }

        var bangReactiveSystem = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.ReactiveSystemInterface);
        if (bangReactiveSystem is not null)
        {
            var watchAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.WatchAttribute);
            if (typeSymbol.ImplementsInterface(bangReactiveSystem))
            {
                var hasAttribute = typeSymbol.RecursivelyCheckForAttribute(watchAttribute);
                if (!hasAttribute)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            WatchAttribute,
                            typeDeclarationSyntax.Identifier.GetLocation()
                        )
                    );
                }
                filterIsOptional = true;
            }
            else if (typeSymbol.HasAttribute(watchAttribute))
            {
                var attributeSyntax = context.Node
                    .DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .FirstOrDefault(attributeSyntax =>
                    {
                        var attributeData = GetAttributeDataForSyntax(attributeSyntax, typeSymbol);
                        return attributeData.AttributeClass?.Equals(watchAttribute, SymbolEqualityComparer.IncludeNullability) ?? false;
                    });

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        NonApplicableWatchAttribute,
                        attributeSyntax?.GetLocation() ?? typeDeclarationSyntax.Identifier.GetLocation()
                    )
                );
            }
        }

        if (filterIsOptional)
            return;

        var filterAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.FilterAttribute);
        var hasFilterAttribute = typeSymbol.RecursivelyCheckForAttribute(filterAttribute);
        if (!hasFilterAttribute)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    FilterAttribute,
                    typeDeclarationSyntax.Identifier.GetLocation()
                )
            );
        }
    }

    private static AttributeData GetAttributeDataForSyntax(
        AttributeSyntax attributeSyntax,
        ISymbol annotatedTypeSymbol
    ) => annotatedTypeSymbol
        .GetAttributes()
        .Single(a => a.ApplicationSyntaxReference!.GetSyntax() == attributeSyntax);
}