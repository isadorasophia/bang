namespace Bang.Components
{
    /// <summary>
    /// Marks a component that generates another component in runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class GeneratesAttribute : Attribute
    {
        /// <summary>
        /// Component which will be generated from the component that has this attribute.
        /// </summary>
        public Type Type { get; init; }

        /// <summary>
        /// Creates a new <see cref="GeneratesAttribute"/>.
        /// </summary>
        /// <param name="type">Component which will be generated from the component that has this attribute.</param>
        public GeneratesAttribute(Type type) =>
            Type = type;
    }
}