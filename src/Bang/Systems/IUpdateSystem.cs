using Bang.Contexts;

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
        public abstract void Update(Context context);
    }
}