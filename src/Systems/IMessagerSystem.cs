using Bang.Components;
using Bang.Entities;

namespace Bang.Systems
{
    /// <summary>
    /// A reactive system that reacts whenever a message gets added to an entity.
    /// </summary>
    public interface IMessagerSystem : ISystem
    {
        /// <summary>
        /// Called once a message is fired from <paramref name="entity"/>.
        /// </summary>
        public abstract ValueTask OnMessage(World world, Entity entity, IMessage message);
    }
}
