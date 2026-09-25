using Bang.Contexts;
using Bang.Entities;

namespace Bang.Systems
{
    /// <summary>
    /// A system that consists of a single update call.
    /// </summary>
    public interface IUpdateSystem : ISystem
    {
        /// <summary>
        /// Update method. Called once each frame.
        /// </summary>
        public void Update(Context context);
    }

    /// <summary>
    /// Simple implementation of <see cref="IUpdateSystem"/> that calls
    /// <see cref="PerformUpdate"/> for each entity matching the filter.
    /// </summary>
    public abstract class SimpleUpdateSystem : IUpdateSystem
    {
        /// <inheritdoc />
        public void Update(Context context)
        {
            foreach (var entity in context.Entities)
            {
                PerformUpdate(entity, context);
            }
        }

        /// <summary>
        /// Called each frame for every entity that matches the <see cref="FilterAttribute" />.
        /// </summary>
        /// <param name="entity">Entity being processed.</param>
        /// <param name="context">Context for the world.</param>
        protected abstract void PerformUpdate(Entity entity, Context context);
    }
}