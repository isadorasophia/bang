using Bang.Entities;
using System.Collections.Immutable;

namespace Bang.Contexts
{
    /// <summary>
    /// Base class for context. This shares implementation for any other class that decides to tweak
    /// the observer behavior (which hasn't happened yet).
    /// </summary>
    public abstract class Observer
    {
        /// <summary>
        /// World that it observes.
        /// </summary>
        public readonly World World;

        internal ComponentsLookup Lookup => World.ComponentsLookup;

        /// <summary>
        /// Entities that are currently watched in the world.
        /// </summary>
        public abstract ImmutableArray<Entity> Entities { get; }

        internal Observer(World world)
        {
            World = world;
        }

        /// <summary>
        /// Unique id of the context. 
        /// This is used when multiple systems share the same context.
        /// </summary>
        internal abstract int Id { get; }

        /// <summary>
        /// Filter an entity and observe any changes that happen to its components.
        /// </summary>
        internal abstract void FilterEntity(Entity entity);

        /// <summary>
        /// React to an entity that had some of its components added.
        /// </summary>
        internal abstract void OnEntityComponentAdded(Entity e, int index);

        /// <summary>
        /// React to an entity that had some of its components removed.
        /// </summary>
        internal abstract void OnEntityComponentRemoved(Entity e, int index, bool causedByDestroy);
    }
}
