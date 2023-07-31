using System.Collections.Immutable;
using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor ClassesCannotBeComponents = new(
        id: Diagnostics.Components.ClassesCannotBeComponents.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(ClassesCannotBeComponents),
        messageFormat: Diagnostics.Components.ClassesCannotBeComponents.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Bang Components should be declared as readonly structs."
    );

    public static readonly DiagnosticDescriptor StructsMustBeReadonly = new(
        id: Diagnostics.Components.StructsMustBeReadonly.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(StructsMustBeReadonly),
        messageFormat: Diagnostics.Components.StructsMustBeReadonly.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Bang Components should be declared as readonly structs."
    );
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
        => ImmutableArray.Create(ClassesCannotBeComponents, StructsMustBeReadonly);

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
        // Bail if IComponent is not resolvable.
        var bangComponentInterface = context.Compilation.GetTypeByMetadataName("Bang.Components.IComponent");
        if (bangComponentInterface is null)
            return;
     
        // Bail if the node we are checking is not a type declaration.
        if (context.ContainingSymbol is not INamedTypeSymbol typeSymbol) 
            return;
        
        // Bail if the current type declaration is not a component.
        var isComponent = typeSymbol.ImplementsInterface(bangComponentInterface);
        if (!isComponent)
            return;

        if (context.Node is ClassDeclarationSyntax classDeclarationSyntax)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    ClassesCannotBeComponents,
                    classDeclarationSyntax.Identifier.GetLocation()
                )
            );
        }

        if (context.Node is StructDeclarationSyntax structDeclarationSyntax)
        {
            if (!typeSymbol.IsReadOnly)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        StructsMustBeReadonly,
                        structDeclarationSyntax.Identifier.GetLocation()
                    )
                );
            }
        }

        if (context.Node is RecordDeclarationSyntax recordDeclarationSyntax)
        {
            // This checks if the record is a struct.
            if (typeSymbol.IsValueType)
            {
                if (!typeSymbol.IsReadOnly)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            StructsMustBeReadonly,
                            recordDeclarationSyntax.Identifier.GetLocation()
                        )
                    );
                }
            }
            else
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ClassesCannotBeComponents,
                        recordDeclarationSyntax.Identifier.GetLocation()
                    )
                );
            }
        }
    }
}