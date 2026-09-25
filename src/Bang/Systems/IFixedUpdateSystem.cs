using Bang.Contexts;
using Bang.Entities;

namespace Bang.Systems
{
    /// <summary>
    /// A system called in fixed intervals.
    /// </summary>
    public interface IFixedUpdateSystem : ISystem
    {
        /// <summary>
        /// Update calls that will be called in fixed intervals.
        /// </summary>
        /// <param name="context">Context that will filter the entities.</param>
        public void FixedUpdate(Context context);
    }

    /// <summary>
    /// Simple implementation of <see cref="IFixedUpdateSystem"/> that calls
    /// <see cref="PerformFixedUpdate"/> for each entity matching the filter.
    /// </summary>
    public abstract class SimpleFixedUpdateSystem : IFixedUpdateSystem
    {
        /// <inheritdoc />
        public void FixedUpdate(Context context)
        {
            foreach (var entity in context.Entities)
            {
                PerformFixedUpdate(entity, context);
            }
        }

        /// <summary>
        /// Called on a set interval for every entity that matches the <see cref="FilterAttribute" />.
        /// </summary>
        /// <param name="entity">Entity being processed.</param>
        /// <param name="context">Context for the world.</param>
        protected abstract void PerformFixedUpdate(Entity entity, Context context);
    }
}