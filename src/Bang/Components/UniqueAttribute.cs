namespace Bang.Components
{
    /// <summary>
    /// Marks a component as unique within our world.
    /// We should not expect two entities with the same component if it is declared as unique.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class UniqueAttribute : Attribute
    {
    }
}
