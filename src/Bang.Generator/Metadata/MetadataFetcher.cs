using Bang.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Bang.Generator.Metadata;

public sealed class MetadataFetcher
{
    private readonly Compilation compilation;

    public MetadataFetcher(Compilation compilation)
    {
        this.compilation = compilation;
    }

    public IEnumerable<TypeMetadata> FetchMetadata(
        BangTypeSymbols bangTypeSymbols,
        ImmutableArray<TypeDeclarationSyntax> potentialComponents,
        ImmutableArray<ClassDeclarationSyntax> potentialStateMachines
    )
    {
        // Gets all potential components/messages from the assembly this generator is processing.
        var allValueTypesToBeCompiled = potentialComponents
            .SelectMany(ValueTypeFromTypeDeclarationSyntax(compilation))
            .ToImmutableArray();

        var componentIndexOffset = 0;
        var components = FetchComponents(bangTypeSymbols, allValueTypesToBeCompiled);
        foreach (var component in components)
        {
            yield return component;
            componentIndexOffset++;
        }

        var messages = FetchMessages(bangTypeSymbols, allValueTypesToBeCompiled, componentIndexOffset);
        foreach (var message in messages)
        {
            yield return message;
        }

        var stateMachines = FetchStateMachines(bangTypeSymbols, potentialStateMachines);
        foreach (var stateMachine in stateMachines)
        {
            yield return stateMachine;
        }

        var interactions = FetchInteractions(bangTypeSymbols, allValueTypesToBeCompiled);
        foreach (var interaction in interactions)
        {
            yield return interaction;
        }
    }

    private IEnumerable<TypeMetadata.Component> FetchComponents(
        BangTypeSymbols bangTypeSymbols,
        ImmutableArray<INamedTypeSymbol> allValueTypesToBeCompiled
    ) => allValueTypesToBeCompiled
            .Where(t => !t.IsGenericType && t.ImplementsInterface(bangTypeSymbols.ComponentInterface))
            .OrderBy(c => c.Name)
            .Select((component, index) => new TypeMetadata.Component(
                Index: index,
                FriendlyName: component.Name.ToCleanComponentName(),
                FullyQualifiedName: component.FullyQualifiedName(),
                IsParentRelativeComponent: component.ImplementsInterface(bangTypeSymbols.ParentRelativeComponentInterface),
                Constructors: component.Constructors
                    .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                    .Select(ConstructorMetadataFromConstructor)
                    .ToImmutableArray()
            ));

    private ConstructorMetadata ConstructorMetadataFromConstructor(IMethodSymbol methodSymbol) => new(
            methodSymbol.Parameters
                .Select(p => new ConstructorParameter(p.Name, p.Type.FullyQualifiedName()))
                .ToImmutableArray()
        );

    private IEnumerable<TypeMetadata.Message> FetchMessages(
        BangTypeSymbols bangTypeSymbols,
        ImmutableArray<INamedTypeSymbol> allValueTypesToBeCompiled,
        int componentIndexOffset
    ) => allValueTypesToBeCompiled
            .Where(t => !t.IsGenericType && t.ImplementsInterface(bangTypeSymbols.MessageInterface))
            .OrderBy(x => x.Name)
            .Select((message, index) => new TypeMetadata.Message(
                Index: index + componentIndexOffset,
                TypeName: message.Name,
                FriendlyName: message.Name.ToCleanComponentName(),
                FullyQualifiedName: message.FullyQualifiedName()
            ));

    private IEnumerable<TypeMetadata.StateMachine> FetchStateMachines(
        BangTypeSymbols bangTypeSymbols,
        ImmutableArray<ClassDeclarationSyntax> potentialStateMachines
    ) => potentialStateMachines
            .Select(GetTypeSymbol(compilation))
            .Where(t => t.IsSubtypeOf(bangTypeSymbols.StateMachineClass))
            .OrderBy(x => x.Name)
            .Select(s => new TypeMetadata.StateMachine(s.FullyQualifiedName()));

    private static IEnumerable<TypeMetadata.Interaction> FetchInteractions(
        BangTypeSymbols bangTypeSymbols,
        ImmutableArray<INamedTypeSymbol> allValueTypesToBeCompiled
    ) => allValueTypesToBeCompiled
        .Where(t => !t.IsGenericType && t.ImplementsInterface(bangTypeSymbols.InteractionInterface))
        .OrderBy(i => i.Name)
        .Select(i => new TypeMetadata.Interaction(i.FullyQualifiedName()));

    private Func<TypeDeclarationSyntax, IEnumerable<INamedTypeSymbol>> ValueTypeFromTypeDeclarationSyntax(Compilation compilation)
        => typeDeclarationSyntax =>
        {
            var semanticModel = compilation.GetSemanticModel(typeDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol potentialComponentTypeSymbol)
                return Enumerable.Empty<INamedTypeSymbol>();

            // Record classes cannot be components or messages.
            if (typeDeclarationSyntax is RecordDeclarationSyntax && !potentialComponentTypeSymbol.IsValueType)
                return Enumerable.Empty<INamedTypeSymbol>();

            return potentialComponentTypeSymbol.Yield();
        };

    private Func<ClassDeclarationSyntax, INamedTypeSymbol> GetTypeSymbol(Compilation compilation)
        => classDeclarationSyntax =>
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            return (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(classDeclarationSyntax)!;
        };
}