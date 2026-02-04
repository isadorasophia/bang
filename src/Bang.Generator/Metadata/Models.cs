using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Bang.Generator.Metadata;

public sealed class BangTypeSymbols
{
    public INamedTypeSymbol ComponentInterface { get; }
    public INamedTypeSymbol MessageInterface { get; }
    public INamedTypeSymbol ParentRelativeComponentInterface { get; }
    public INamedTypeSymbol StateMachineClass { get; }
    public INamedTypeSymbol InteractionInterface { get; }
    public INamedTypeSymbol ComponentsLookupClass { get; }
    public INamedTypeSymbol TransformInterface { get; }
    public INamedTypeSymbol UniqueAttribute { get; }

    private BangTypeSymbols(INamedTypeSymbol componentInterface,
        INamedTypeSymbol messageInterface,
        INamedTypeSymbol parentRelativeComponentInterface,
        INamedTypeSymbol stateMachineClass,
        INamedTypeSymbol interactionInterface,
        INamedTypeSymbol componentsLookupClass,
        INamedTypeSymbol transformInterface,
        INamedTypeSymbol uniqueAttribute)
    {
        MessageInterface = messageInterface;
        StateMachineClass = stateMachineClass;
        ComponentInterface = componentInterface;
        TransformInterface = transformInterface;
        InteractionInterface = interactionInterface;
        ComponentsLookupClass = componentsLookupClass;
        ParentRelativeComponentInterface = parentRelativeComponentInterface;
        UniqueAttribute = uniqueAttribute;
    }

    public static BangTypeSymbols? FromCompilation(Compilation compilation)
    {
        // Bail if IComponent is not resolvable.
        var componentInterface = compilation.GetTypeByMetadataName("Bang.Components.IComponent");
        if (componentInterface is null)
            return null;

        // Bail if IMessage is not resolvable.
        var messageInterface = compilation.GetTypeByMetadataName("Bang.Components.IMessage");
        if (messageInterface is null)
            return null;

        // Bail if IParentRelativeComponent is not resolvable.
        var parentRelativeComponentInterface = compilation.GetTypeByMetadataName("Bang.Components.IParentRelativeComponent");
        if (parentRelativeComponentInterface is null)
            return null;

        // Bail if StateMachine is not resolvable.
        var stateMachineClass = compilation.GetTypeByMetadataName("Bang.StateMachines.StateMachine");
        if (stateMachineClass is null)
            return null;

        // Bail if IInteraction is not resolvable.
        var interactionInterface = compilation.GetTypeByMetadataName("Bang.Interactions.IInteraction");
        if (interactionInterface is null)
            return null;

        // Bail if ComponentsLookup is not resolvable.
        var componentsLookupClass = compilation.GetTypeByMetadataName("Bang.ComponentsLookup");
        if (componentsLookupClass is null)
            return null;

        // Bail if PositionComponent is not resolvable.
        var transformComponentInterface = compilation.GetTypeByMetadataName("Bang.Components.PositionComponent");
        if (transformComponentInterface is null)
            return null;

        // This is not part of Bang, so it can be null.
        var murderTransformComponentInterface = compilation.GetTypeByMetadataName("Murder.Components.IMurderTransformComponent");

        // Bail if ITransformComponent is not resolvable.
        var uniqueAttribute = compilation.GetTypeByMetadataName("Bang.Components.UniqueAttribute");
        if (uniqueAttribute is null)
            return null;

        return new BangTypeSymbols(
            componentInterface,
            messageInterface,
            parentRelativeComponentInterface,
            stateMachineClass,
            interactionInterface,
            componentsLookupClass,
            transformComponentInterface,
            uniqueAttribute
        );
    }
}

public sealed record ConstructorParameter(
    string Name,
    string FullyQualifiedTypeName
);

public sealed record ConstructorMetadata(
    ImmutableArray<ConstructorParameter> Parameters
);

public abstract record TypeMetadata
{
    public sealed record Project(
        string ProjectName,
        string? ParentProjectName,
        string ParentProjectLookupClassName
    ) : TypeMetadata;

    public sealed record Component(
        int Index,
        bool IsInternal,
        string FriendlyName,
        string FullyQualifiedName,
        bool IsTransformComponent,
        bool IsParentRelativeComponent,
        bool IsUniqueComponent,
        ImmutableArray<ConstructorMetadata> Constructors
    ) : TypeMetadata;

    public sealed record Message(
        int Index,
        bool IsInternal,
        string TypeName,
        string FriendlyName,
        string FullyQualifiedName,
        ImmutableArray<ConstructorMetadata> Constructors
    ) : TypeMetadata;

    // TODO: These can be turned into something like `GenericComponentConstrainedType` should we go the route 
    // to support arbitrary, user provided generic types.
    public sealed record StateMachine(
        bool IsInternal,
        string FullyQualifiedName
    ) : TypeMetadata;

    public sealed record Interaction(
        bool IsInternal,
        string FullyQualifiedName
    ) : TypeMetadata;
}