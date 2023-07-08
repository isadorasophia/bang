﻿using Bang.Contexts;
using Bang.Diagnostics;
using Bang.StateMachines;
using System.Diagnostics;

namespace Bang.Systems
{
    /// <summary>
    /// Indicates a watcher attribute for a system.
    /// This must be implemented by all the systems that inherit <see cref="IReactiveSystem"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class WatchAttribute : Attribute
    {
        /// <summary>
        /// System will target all the entities that has all this set of components.
        /// </summary>
        public Type[] Types { get; }

        /// <summary>
        /// Creates a new <see cref="WatchAttribute"/> with a set of target types.
        /// </summary>
        /// <param name="types">Component types that will fire a notification once they are modified.</param>
        public WatchAttribute(params Type[] types)
        {
            if (World.DIAGNOSTICS_MODE)
            {
                // Verify that all the attribute types are IComponents and a struct
                foreach (Type t in types)
                {
                    Assert.Verify(t.IsValueType || t == typeof(IStateMachineComponent) || t.IsInterface,
                        "Why are we adding a watcher attribute for a non-struct? This won't be notified when the value changes.");
                }
            }

            Types = types;
        }
    }
}
