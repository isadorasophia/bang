using Bang.Components;
using System.Diagnostics;

namespace Bang.Systems
{
    /// <summary>
    /// Marks a messager attribute for a system.
    /// This must be implemented by all the systems that inherit <see cref="IMessagerSystem"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class MessagerAttribute : Attribute
    {
        /// <summary>
        /// System will target all the entities that has all this set of components.
        /// </summary>
        public Type[] Types { get; }

        /// <summary>
        /// Creates a new <see cref="MessagerAttribute"/>.
        /// </summary>
        /// <param name="types">Messages that will be fired to this system.</param>
        public MessagerAttribute(params Type[] types)
        {
            if (World.DIAGNOSTICS_MODE)
            {
                foreach (Type t in types)
                {
                    Debug.Assert(t.IsValueType || t == typeof(IMessage),
                        "Why are we adding a watcher attribute for a non-struct? This won't be notified when the value changes.");
                }
            }

            Types = types;
        }
    }
}