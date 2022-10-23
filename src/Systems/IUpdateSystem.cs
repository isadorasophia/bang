using Bang.Contexts;

namespace Bang.Systems
{
    /// <summary>
    /// This is the update system and consists of a single update call.
    /// </summary>
    public interface IUpdateSystem : ISystem
    {
        /// <summary>
        /// Update method. Called once each frame.
        /// </summary>
        public abstract ValueTask Update(Context context);
    }
}
