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

        // Bail if the current type declaration is not a system.
        var isSystem = typeSymbol.ImplementsInterface(bangSystemInterface);
        if (!isSystem)
            return;

        var filterAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.FilterAttribute);
        context.ReportDiagnosticIfLackingAttribute(typeSymbol, filterAttribute, FilterAttribute);

        var bangMessagerSystem = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.MessagerSystemInterface);
        if (bangMessagerSystem is not null)
        {
            var messagerAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.MessagerAttribute);
            if (typeSymbol.ImplementsInterface(bangMessagerSystem))
            {
                context.ReportDiagnosticIfLackingAttribute(typeSymbol, messagerAttribute, MessagerAttribute);
            }
            else
            {
                context.ReportDiagnosticIfAttributeExists(typeSymbol, messagerAttribute, NonApplicableMessagerAttribute);
            }
        }

        var bangReactiveSystem = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.ReactiveSystemInterface);
        if (bangReactiveSystem is not null)
        {
            var watchAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.WatchAttribute);
            if (typeSymbol.ImplementsInterface(bangReactiveSystem))
            {
                context.ReportDiagnosticIfLackingAttribute(typeSymbol, watchAttribute, WatchAttribute);
            }
            else
            {
                context.ReportDiagnosticIfAttributeExists(typeSymbol, watchAttribute, NonApplicableWatchAttribute);
            }
        }
    }
}