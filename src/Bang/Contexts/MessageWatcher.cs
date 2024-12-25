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

        private int _contextSubscribed = -1;

        /// <summary>
        /// A watcher will target a single component.
        /// </summary>
        internal MessageWatcher(World world, int contextId, Type[] targetMessages)
        {
            World = world;

            List<int> builder = [];
            foreach (Type t in targetMessages)
            {
                Debug.Assert(typeof(IMessage).IsAssignableFrom(t));

                int id = world.ComponentsLookup.Id(t);
                builder.Add(id);
            }

            _targetMessages = [.. builder];

            // Calculate the hash based on the target messages and the context id.
            int messagesHash = HashExtensions.GetHashCodeImpl(builder);
            Id = HashCode.Combine(contextId, messagesHash);
        }

        internal void SubscribeToContext(Context context)
        {
            Debug.Assert(_contextSubscribed == -1);

            context.OnMessageSentForEntityInContext += OnMessageSent;
            _contextSubscribed = context.Id;
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