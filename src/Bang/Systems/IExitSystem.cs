using Bang.Contexts;

namespace Bang.Systems
{
    /// <summary>
    /// A system called when the world is shutting down.
    /// </summary>
    public interface IExitSystem : ISystem
    {
        /// <summary>
        /// Called when everything is turning off (this is your last chance).
        /// </summary>
        public abstract void Exit(Context context);
    }
}
