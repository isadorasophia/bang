using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace Bang.Generator.Extensions;

public static class IncrementalGeneratorExtensions
{
    public static IncrementalValuesProvider<TypeDeclarationSyntax> PotentialComponents(
        this IncrementalGeneratorInitializationContext context
    ) => context.SyntaxProvider.CreateSyntaxProvider(
        (node, _) => node.IsStructOrRecordWithSubtypes(),
        (c, _) => (TypeDeclarationSyntax)c.Node
    );

    public static IncrementalValuesProvider<ClassDeclarationSyntax> PotentialStateMachines(
        this IncrementalGeneratorInitializationContext context
    ) => context.SyntaxProvider.CreateSyntaxProvider(
        (node, _) => node.IsClassWithSubtypes(),
        (c, _) => (ClassDeclarationSyntax)c.Node
    );

    public static bool IsClassWithSubtypes(this SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 };

    // Returns true for structs that implement an interface and records with base types.
    // We only check if a record is a value type later on in the chain because we need a TypeSymbol.
    public static bool IsStructOrRecordWithSubtypes(this SyntaxNode node)
        => node is
            RecordDeclarationSyntax { BaseList.Types.Count: > 0 } or
            StructDeclarationSyntax { BaseList.Types.Count: > 0 };
}