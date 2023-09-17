using Bang.Contexts;

namespace Bang.Systems
{
    /// <summary>
    /// Indicates characteristics of a system that was implemented on our ECS system.
    /// This must be implemented by all the systems that inherits from <see cref="ISystem"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class FilterAttribute : Attribute
    {
        /// <summary>
        /// System will target all the entities that has all this set of components.
        /// </summary>
        public Type[] Types { get; init; } = Array.Empty<Type>();

        /// <summary>
        /// This is the kind of accessor that will be made on this component.
        /// This can be leveraged once we parallelize update frames (which we don't yet), so don't bother with this just yet.
        /// </summary>
        public ContextAccessorKind Kind { get; init; } = ContextAccessorKind.Read | ContextAccessorKind.Write;

        /// <summary>
        /// This is how the system will filter the entities. See <see cref="ContextAccessorFilter"/>.
        /// </summary>
        public ContextAccessorFilter Filter { get; init; } = ContextAccessorFilter.AllOf;

        /// <summary>
        /// Creates a system filter with custom accessors.
        /// </summary>
        public FilterAttribute(ContextAccessorFilter filter, ContextAccessorKind kind, params Type[] types)
        {
            (Filter, Kind, Types) = (filter, kind, types);
        }

        /// <summary>
        /// Create a system filter with default accessor of <see cref="Kind"/> for <paramref name="types"/>.
        /// </summary>
        public FilterAttribute(ContextAccessorFilter filter, params Type[] types) :
            this(filter, kind: ContextAccessorKind.Read | ContextAccessorKind.Write, types)
        { }

        /// <summary>
        /// Create a system filter with default accessor of <see cref="Filter"/> for <paramref name="types"/>.
        /// </summary>
        public FilterAttribute(ContextAccessorKind kind, params Type[] types) :
            this(filter: ContextAccessorFilter.AllOf, kind, types)
        { }

        /// <summary>
        /// Create a system filter with default accessors for <paramref name="types"/>.
        /// </summary>
        public FilterAttribute(params Type[] types) :
            this(filter: ContextAccessorFilter.AllOf, kind: ContextAccessorKind.Read | ContextAccessorKind.Write, types)
        { }
    }
}