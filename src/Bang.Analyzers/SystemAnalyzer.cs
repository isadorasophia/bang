using System.Collections.Immutable;
using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MessagerAttribute = new(
        id: Diagnostics.Systems.MessagerAttribute.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(MessagerAttribute),
        messageFormat: Diagnostics.Systems.MessagerAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Implementations of IMessagerSystem need to be annotated with MessagerAttribute."
    );

    public static readonly DiagnosticDescriptor WatchAttribute = new(
        id: Diagnostics.Systems.WatchAttribute.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(WatchAttribute),
        messageFormat: Diagnostics.Systems.WatchAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
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
        
        var bangMessagerSystem = context.Compilation.GetTypeByMetadataName("Bang.Systems.IMessagerSystem");
        if (bangMessagerSystem is not null && typeSymbol.ImplementsInterface(bangMessagerSystem))
        {
            var messagerAttribute = context.Compilation
                .GetTypeByMetadataName("Bang.Systems.MessagerAttribute");
            
            var attributes = typeSymbol.GetAttributes();
            var hasModuleAttribute = attributes.Any(
                attr => attr.AttributeClass is not null && attr.AttributeClass.Equals(messagerAttribute, SymbolEqualityComparer.IncludeNullability));
            
            if (!hasModuleAttribute)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MessagerAttribute,
                        context.Node.GetLocation()
                    )
                );
            }   
        }

        var bangReactiveSystem = context.Compilation.GetTypeByMetadataName("Bang.Systems.IReactiveSystem");
        if (bangReactiveSystem is not null && typeSymbol.ImplementsInterface(bangReactiveSystem))
        {
            var watchAttribute = context.Compilation.GetTypeByMetadataName("Bang.Systems.WatchAttribute");
            
            var attributes = typeSymbol.GetAttributes();
            var hasModuleAttribute = attributes.Any(
                attr => attr.AttributeClass is not null && attr.AttributeClass.Equals(watchAttribute, SymbolEqualityComparer.IncludeNullability));
            
            if (!hasModuleAttribute)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        WatchAttribute,
                        context.Node.GetLocation()
                    )
                );
            }
        }
    }
}