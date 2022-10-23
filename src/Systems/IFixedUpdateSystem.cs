using Bang.Contexts;

namespace Bang.Systems
{
    /// <summary>
    /// System which will be called in fixed intervals.
    /// </summary>
    public interface IFixedUpdateSystem : ISystem
    {
        /// <summary>
        /// Update calls which will be called in fixed intervals.
        /// </summary>
        /// <param name="context">Context which will filter the entities.</param>
        public abstract ValueTask FixedUpdate(Context context);
    }
}
