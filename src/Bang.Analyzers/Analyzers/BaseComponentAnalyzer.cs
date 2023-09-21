using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Bang.Analyzers.Analyzers;

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
            action: c => Analyze(c, InterfaceName, DoNotUseClassesDiagnostic, ReadonlyDiagnostic),
            syntaxKinds: syntaxKinds
        );
    }

    protected virtual void PerformExtraAnalysis(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol typeSymbol,
        Location diagnosticLocation
    )
    { }

    private void Analyze(
        SyntaxNodeAnalysisContext context,
        string interfaceName,
        DiagnosticDescriptor classesAreNotValidDiagnostic,
        DiagnosticDescriptor readonlyDiagnostic
    )
    {
        // Bail if the interface we're analysing is not resolvable.
        var interfaceBeingAnalyzed = context.Compilation.GetTypeByMetadataName(interfaceName);
        if (interfaceBeingAnalyzed is null)
            return;

        // Bail if the node we are checking is not a type declaration.
        if (context.ContainingSymbol is not INamedTypeSymbol typeSymbol)
            return;

        // Bail if the current type declaration does not implement the interface we're analysing.
        var implementsInterface = typeSymbol.ImplementsInterface(interfaceBeingAnalyzed);
        if (!implementsInterface)
            return;

        var location = context.Node.GetLocation();

        if (context.Node is ClassDeclarationSyntax classDeclarationSyntax)
        {
            location = classDeclarationSyntax.Identifier.GetLocation();
            context.ReportDiagnostic(
                Diagnostic.Create(
                    classesAreNotValidDiagnostic,
                    classDeclarationSyntax.Identifier.GetLocation()
                )
            );
        }

        if (context.Node is StructDeclarationSyntax structDeclarationSyntax)
        {
            location = structDeclarationSyntax.Identifier.GetLocation();
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
            location = recordDeclarationSyntax.Identifier.GetLocation();
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
                        classesAreNotValidDiagnostic,
                        recordDeclarationSyntax.Identifier.GetLocation()
                    )
                );
            }
        }

        PerformExtraAnalysis(context, typeSymbol, location);
    }
}