using Bang.Components;
using System.Diagnostics;

namespace Bang.Systems
{
    /// <summary>
    /// Indicates a messager attribute for a system.
    /// This must be implemented by all the systems that inherits from <see cref="IMessagerSystem"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class MessagerAttribute : Attribute
    {
        /// <summary>
        /// System will target all the entities that has all this set of components.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Creates a new <see cref="MessagerAttribute"/>.
        /// </summary>
        /// <param name="type">Message which will be fired to this system.</param>
        public MessagerAttribute(Type type)
        {
            Debug.Assert(type.IsValueType || type == typeof(IMessage),
                "Why are we adding a watcher attribute for a non-struct? This won't be notified when the value changes.");

            Type = type;
        }
    }
}
