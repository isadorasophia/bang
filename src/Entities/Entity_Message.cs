using Bang.Components;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Bang.Entities
{
    public partial class Entity
    {
        /// <summary>
        /// This will be fired when a message gets sent to the entity.
        /// </summary>
        public event Action<Entity, int, IMessage>? OnMessage;

        /// <summary>
        /// Track message components. This will be added within an update.
        /// </summary>
        private readonly HashSet<int> _messages = new();

        /// <summary>
        /// Whether entity has a message of type <typeparamref name="T"/>.
        /// </summary>
        public bool HasMessage<T>() where T : IMessage
        {
            return HasMessage(GetComponentIndex(typeof(T)));
        }

        /// <summary>
        /// Whether entity has a message of index <paramref name="index"/>.
        /// </summary>
        public bool HasMessage(int index) => _messages.Contains(index);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> for any system watching it.
        /// </summary>
        public void SendMessage<T>() where T : IMessage, new() => SendMessage(new T());

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> for any system watching it.
        /// This will also send a body message through <paramref name="message"/>.
        /// </summary>
        public void SendMessage<T>(T message) where T : IMessage
        {
            Debug.Assert(_world is not null);

            int index = GetComponentIndex(typeof(T));

            _messages.Add(index);

            // Notify messagers. We only use the message to notify all the messagers,
            // but we will not save any of its data afterwards.
            OnMessage?.Invoke(this, index, message);

            // Notify systems that may filter by this message.
            OnComponentAdded?.Invoke(this, index);

            // Notify world that a message has been sent for this entity.
            _world.OnMessage(this);
        }

        /// <summary>
        /// Clear all pending messages.
        /// </summary>
        internal void ClearMessages()
        {
            // First, keep a reference of all the removed messages and clear them.
            // We only notify the contexts afterwards -- since they will check whether
            // the message is available when updating their filters.
            ImmutableArray<int> allRemovedMessages = _messages.ToImmutableArray();
            _messages.Clear();

            foreach (int index in allRemovedMessages)
            {
                // Notify systems that may filter by this message.
                OnComponentRemoved?.Invoke(this, index, false /* causedByDestroy */);
            }
        }
    }
}
