namespace Bang.Components
{
    /// <summary>
    /// Attribute for components which must be kept on an entity 
    /// <see cref="Bang.Entities.Entity.Replace(IComponent[], List{ValueTuple{int, string}}, bool)"/> operation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class KeepOnReplaceAttribute : Attribute
    {
    }
}
