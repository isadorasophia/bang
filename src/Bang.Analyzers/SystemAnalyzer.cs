using System.Collections.Immutable;
using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor FilterAttribute = new(
        id: Diagnostics.Systems.MessagerAttribute.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(FilterAttribute),
        messageFormat: Diagnostics.Systems.MessagerAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Implementations of IMessagerSystem need to be annotated with MessagerAttribute."
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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(MessagerAttribute, WatchAttribute);

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
        var bangSystemInterface = context.Compilation.GetTypeByMetadataName("Bang.Systems.ISystem");
        if (bangSystemInterface is null)
            return;

        // Bail if the node we are checking is not a type declaration.
        if (context.ContainingSymbol is not INamedTypeSymbol typeSymbol)
            return;

        // Bail if the current type declaration is not a system.
        var isSystem = typeSymbol.ImplementsInterface(bangSystemInterface);
        if (!isSystem)
            return;

        var attributes = typeSymbol.GetAttributes();
        var filterAttribute = context.Compilation.GetTypeByMetadataName("Bang.Systems.FilterAttribute");
        ReportDiagnosticIfLackingAttribute(
            context: context,
            diagnosticDescriptor: FilterAttribute,
            attributes: attributes,
            attributeToCheck: filterAttribute
        );

        var bangMessagerSystem = context.Compilation.GetTypeByMetadataName("Bang.Systems.IMessagerSystem");
        if (bangMessagerSystem is not null && typeSymbol.ImplementsInterface(bangMessagerSystem))
        {
            var messagerAttribute = context.Compilation.GetTypeByMetadataName("Bang.Systems.MessagerAttribute");
            ReportDiagnosticIfLackingAttribute(
                context: context,
                diagnosticDescriptor: MessagerAttribute,
                attributes: attributes,
                attributeToCheck: messagerAttribute
            );
        }

        var bangReactiveSystem = context.Compilation.GetTypeByMetadataName("Bang.Systems.IReactiveSystem");
        if (bangReactiveSystem is not null && typeSymbol.ImplementsInterface(bangReactiveSystem))
        {
            var watchAttribute = context.Compilation.GetTypeByMetadataName("Bang.Systems.WatchAttribute");
            ReportDiagnosticIfLackingAttribute(
                context: context,
                diagnosticDescriptor: WatchAttribute,
                attributes: attributes,
                attributeToCheck: watchAttribute
            );
        }
    }

    private static void ReportDiagnosticIfLackingAttribute(
        SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor diagnosticDescriptor,
        ImmutableArray<AttributeData> attributes,
        INamedTypeSymbol? attributeToCheck
    )
    {
        var hasAttribute = attributes.Any(
            attr => attr.AttributeClass is not null && attr.AttributeClass.Equals(attributeToCheck, SymbolEqualityComparer.IncludeNullability));

        if (hasAttribute)
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                diagnosticDescriptor,
                context.Node.GetLocation()
            )
        );
    }
}