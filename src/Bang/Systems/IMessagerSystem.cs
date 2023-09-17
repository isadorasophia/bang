using Bang.Components;
using Bang.Entities;

namespace Bang.Systems
{
    /// <summary>
    /// A reactive system that reacts to messages getting added to an entity.
    /// </summary>
    public interface IMessagerSystem : ISystem
    {
        /// <summary>
        /// Called once a message is fired from <paramref name="entity"/>.
        /// </summary>
        public abstract void OnMessage(World world, Entity entity, IMessage message);
    }
}