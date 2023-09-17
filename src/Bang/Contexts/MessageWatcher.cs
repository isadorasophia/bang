using Bang.Components;
using Bang.Entities;
using Bang.Util;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Bang.Contexts
{
    /// <summary>
    /// A context may have a collection of watchers.
    /// </summary>
    internal class MessageWatcher
    {
        public readonly World World;

        internal readonly int Id;
        private readonly ImmutableHashSet<int> _targetMessages;

        /// <summary>
        /// A watcher will target a single component.
        /// </summary>
        internal MessageWatcher(World world, int contextId, Type[] targetMessages)
        {
            World = world;

            var builder = ImmutableHashSet.CreateBuilder<int>();
            foreach (Type t in targetMessages)
            {
                Debug.Assert(typeof(IMessage).IsAssignableFrom(t));

                int id = world.ComponentsLookup.Id(t);
                builder.Add(id);
            }

            _targetMessages = builder.ToImmutableHashSet();

            // Calculate the hash based on the target messages and the context id.
            int messagesHash = HashExtensions.GetHashCodeImpl(_targetMessages);
            Id = HashExtensions.GetHashCode(contextId, messagesHash);
        }

        internal void SubscribeToContext(Context context)
        {
            context.OnMessageSentForEntityInContext += OnMessageSent;
        }

        private void OnMessageSent(Entity e, int index, IMessage message)
        {
            if (!_targetMessages.Contains(index))
            {
                return;
            }

            World.OnMessage(Id, e, message);
        }
    }
}