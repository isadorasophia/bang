namespace Bang.Components
{
    /// <summary>
    /// This is an attribute that tells that a given component requires another component
    /// when adding it to the entity.
    /// This is an attribute that tells that a given data requires another one of the same type.
    /// For example: a component requires another component when adding it to the entity,
    /// or a system requires another system when additing it to a world.
    /// If this is for a system, it assumes that the system that depends on the other one comes first.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RequiresAttribute : Attribute
    {
        /// <summary>
        /// System will target all the entities that has all this set of components.
        /// </summary>
        public Type[] Types { get; init; }

        /// <summary>
        /// Creates a new <see cref="RequiresAttribute"/>.
        /// </summary>
        /// <param name="types">List of components which this depends on.</param>
        public RequiresAttribute(params Type[] types) =>
            Types = types;
    }
}
