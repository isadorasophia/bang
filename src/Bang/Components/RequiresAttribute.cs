namespace Bang.Components
{
    /// <summary>
    /// Marks a component as requiring other components when being added to an entity.
    /// This is an attribute that tells that a given data requires another one of the same type.
    /// For example: a component requires another component when adding it to the entity,
    /// or a system requires another system when adding it to a world.
    /// If this is for a system, it assumes that the system that depends on the other one comes first.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RequiresAttribute : Attribute
    {
        /// <summary>
        /// System will target all entities that have this set of components.
        /// </summary>
        public Type[] Types { get; init; }

        /// <summary>
        /// Creates a new <see cref="RequiresAttribute"/>.
        /// </summary>
        /// <param name="types">List of dependencies for this component.</param>
        public RequiresAttribute(params Type[] types) =>
            Types = types;
    }
}