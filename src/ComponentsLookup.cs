using Bang.Components;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Bang
{
    /// <summary>
    /// Implemented by generators in order to provide a mapping of all the types to their respective id.
    /// </summary>
    public abstract class ComponentsLookup
    {
        /// <summary>
        /// Maps all the components to their unique id.
        /// </summary>
        protected abstract ImmutableDictionary<Type, int> ComponentsIndex { get; }

        /// <summary>
        /// Maps all the messages to their unique id.
        /// </summary>
        protected abstract ImmutableDictionary<Type, int> MessagesIndex { get; }

        /// <summary>
        /// List of all the unique id of the components that inherit from <see cref="IParentRelativeComponent"/>.
        /// </summary>
        public abstract ImmutableHashSet<int> RelativeComponents { get; }

        /// <summary>
        /// Get the id for <paramref name="t"/> component type.
        /// </summary>
        /// <param name="t">Type.</param>
        public int Id(Type t)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(t) || typeof(IMessage).IsAssignableFrom(t),
                "Why are we receiving a type that is not an IComponent?");

            if (typeof(IMessage).IsAssignableFrom(t))
            {
                return MessagesIndex[t];
            }

            return ComponentsIndex[t];
        }

        /// <summary>
        /// Returns whether a <paramref name="id"/> is relative to its parent.
        /// </summary>
        public bool IsRelative(int id)
        {
            return RelativeComponents.Contains(id);
        }
        
        internal int TotalIndices => ComponentsIndex.Count + MessagesIndex.Count;
    }
}
