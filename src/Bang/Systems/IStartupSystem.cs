using Bang.Contexts;

namespace Bang.Systems
{
    /// <summary>
    /// A system only called once when the world starts.
    /// </summary>
    public interface IStartupSystem : ISystem
    {
        /// <summary>
        /// This is called before any <see cref="IUpdateSystem.Update(Context)"/> call.
        /// </summary>
        public abstract void Start(Context context);
    }
}
