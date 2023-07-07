namespace Bang.Components
{
    /// <summary>
    /// This is an attribute that tells that a given component is unique within our world.
    /// We should not expect two entities with the same component if it is declared as unique.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class UniqueAttribute : Attribute
    {
    }
}
