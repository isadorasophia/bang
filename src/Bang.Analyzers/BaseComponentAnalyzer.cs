using System.Collections.Immutable;
using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers;

public abstract class BaseComponentAnalyzer : DiagnosticAnalyzer
{
    protected abstract string InterfaceName { get; }
    protected abstract DiagnosticDescriptor DoNotUseClassesDiagnostic { get; }
    protected abstract DiagnosticDescriptor ReadonlyDiagnostic { get; }

    public override void Initialize(AnalysisContext context)
    {
        var syntaxKinds = ImmutableArray.Create(
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration
        );

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(
            action: context => Analyze(context, InterfaceName, DoNotUseClassesDiagnostic, ReadonlyDiagnostic),
            syntaxKinds: syntaxKinds
        );
    }

    private static void Analyze(
        SyntaxNodeAnalysisContext context,
        string interfaceName,
        DiagnosticDescriptor classesAreNotvalidDiagnostic,
        DiagnosticDescriptor readonlyDiagnostic
    )
    {
        // Bail if IComponent is not resolvable.
        var bangComponentInterface = context.Compilation.GetTypeByMetadataName(interfaceName);
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
                    classesAreNotvalidDiagnostic,
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
                        readonlyDiagnostic,
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
                            readonlyDiagnostic,
                            recordDeclarationSyntax.Identifier.GetLocation()
                        )
                    );
                }
            }
            else
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        classesAreNotvalidDiagnostic,
                        recordDeclarationSyntax.Identifier.GetLocation()
                    )
                );
            }
        }
    }
}