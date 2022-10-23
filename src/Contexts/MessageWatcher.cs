using Bang.Components;
using Bang.Entities;
using Bang.Util;
using System.Diagnostics;

namespace Bang.Contexts
{
    /// <summary>
    /// A context may have a collection of watchers.
    /// </summary>
    internal class MessageWatcher
    {
        public readonly World World;

        internal readonly int Id;

        private readonly int _targetComponent;

        /// <summary>
        /// A watcher will target a single component.
        /// </summary>
        internal MessageWatcher(World world, int contextId, Type targetComponent)
        {
            World = world;

            Debug.Assert(typeof(IMessage).IsAssignableFrom(targetComponent));

            _targetComponent = world.ComponentsLookup.Id(targetComponent);
            Id = HashExtensions.GetHashCode(contextId, _targetComponent);
        }

        internal void SubscribeToContext(Context context)
        {
            context.OnMessageSentForEntityInContext += OnMessageSent;
        }

        private void OnMessageSent(Entity e, int index, IMessage message)
        {
            if (index != _targetComponent)
            {
                return;
            }

            World.OnMessage(Id, e, message);
        }
    }
}
