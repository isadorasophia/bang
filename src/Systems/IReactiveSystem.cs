using Bang.Entities;
using System.Collections.Immutable;

namespace Bang.Systems
{
    /// <summary>
    /// A reactive system that reacts to changes of certain components.
    /// </summary>
    public interface IReactiveSystem : ISystem
    {
        /// <summary>
        /// This is called at the end of the frame for all entities which were added one of the target
        /// components.
        /// This is not called if the entity died.
        /// </summary>
        public abstract void OnAdded(World world, ImmutableArray<Entity> entities);

        /// <summary>
        /// This is called at the end of the frame for all entities which removed one of the target
        /// components.
        /// </summary>
        public abstract void OnRemoved(World world, ImmutableArray<Entity> entities);

        /// <summary>
        /// This is called at the end of the frame for all entities which modified one of the target
        /// components.
        /// This is not called if the entity died.
        /// </summary>
        public abstract void OnModified(World world, ImmutableArray<Entity> entities);
    }
}
