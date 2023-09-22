﻿using Microsoft.CodeAnalysis;
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

    private BangTypeSymbols(
        INamedTypeSymbol componentInterface,
        INamedTypeSymbol messageInterface,
        INamedTypeSymbol parentRelativeComponentInterface,
        INamedTypeSymbol stateMachineClass,
        INamedTypeSymbol interactionInterface,
        INamedTypeSymbol componentsLookupClass)
    {
        MessageInterface = messageInterface;
        StateMachineClass = stateMachineClass;
        ComponentInterface = componentInterface;
        InteractionInterface = interactionInterface;
        ComponentsLookupClass = componentsLookupClass;
        ParentRelativeComponentInterface = parentRelativeComponentInterface;
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

        return new(
            componentInterface,
            messageInterface,
            parentRelativeComponentInterface,
            stateMachineClass,
            interactionInterface,
            componentsLookupClass
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
        string FriendlyName,
        string FullyQualifiedName,
        bool IsParentRelativeComponent,
        ImmutableArray<ConstructorMetadata> Constructors
    ) : TypeMetadata;

    public sealed record Message(
        int Index,
        string TypeName,
        string FriendlyName,
        string FullyQualifiedName
    ) : TypeMetadata;

    // TODO: These can be turned into something like `GenericComponentConstrainedType` should we go the route 
    // to support arbitrary, user provided generic types.
    public sealed record StateMachine(
        string FullyQualifiedName
    ) : TypeMetadata;

    public sealed record Interaction(
        string FullyQualifiedName
    ) : TypeMetadata;
}
