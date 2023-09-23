namespace Bang.Analyzers;

public static class TypeMetadataNames
{
    public const string ComponentInterface = "Bang.Components.IComponent";
    public const string SystemInterface = "Bang.Systems.ISystem";
    public const string FilterAttribute = "Bang.Systems.FilterAttribute";

    public const string ReactiveSystemInterface = "Bang.Systems.IReactiveSystem";
    public const string WatchAttribute = "Bang.Systems.WatchAttribute";

    public const string MessageInterface = "Bang.Components.IMessage";
    public const string MessagerSystemInterface = "Bang.Systems.IMessagerSystem";
    public const string MessagerAttribute = "Bang.Systems.MessagerAttribute";

    public const string InteractionInterface = "Bang.Interactions.IInteraction";

    public const string WorldType = "Bang.World";
    public const string UniqueAttribute = "Bang.Components.UniqueAttribute";
}