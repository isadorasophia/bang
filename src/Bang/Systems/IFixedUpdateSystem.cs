using Bang.Contexts;

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
        public abstract void FixedUpdate(Context context);
    }
}