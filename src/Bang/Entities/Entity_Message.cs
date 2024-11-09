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
        /// This should be avoided since it highly depends on the order of the systems
        /// being fired and can lead to several bugs.
        /// For example, if we check for that on the state machine, it will depend on the order
        /// of the entities in the world.
        /// </summary>
        public bool HasMessage<T>() where T : IMessage
        {
            return HasMessage(GetComponentIndex(typeof(T)));
        }

        /// <summary>
        /// Whether entity has a message of index <paramref name="index"/>.
        /// This should be avoided since it highly depends on the order of the systems
        /// being fired and can lead to several bugs.
        /// For example, if we check for that on the state machine, it will depend on the order
        /// of the entities in the world.
        /// </summary>
        public bool HasMessage(int index) => _messages.Contains(index);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> for any system watching it.
        /// </summary>
        public void SendMessage<T>() where T : IMessage, new() => SendMessage(new T());

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> for any system watching it.
        /// </summary>
        public void SendMessage<T>(T message) where T : IMessage
        {
            int index = GetComponentIndex(message.GetType());
            SendMessage(index, message);
        }

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> for any system watching it.
        /// This will also send a body message through <paramref name="message"/>.
        /// </summary>
        public void SendMessage<T>(int index, T message) where T : IMessage
        {
            Debug.Assert(_world is not null);

            _messages.Add(index);

            // Notify messagers. We only use the message to notify all the messagers,
            // but we will not save any of its data afterwards.
            OnMessage?.Invoke(this, index, message);

            // Notify world that a message has been sent for this entity.
            _world.OnMessage(this);
        }

        /// <summary>
        /// Clear all pending messages.
        /// </summary>
        internal void ClearMessages()
        {
            // We no longer send notification to systems upon clearing messages.
            // Filters should NOT track messages, this just has too much overhead.
            // _ = _messages.ToImmutableArray();
            _messages.Clear();
        }

        /// <summary>
        /// This removes a message from the entity. This is used when the message must be removed within
        /// this frame.
        /// </summary>
        public bool RemoveMessage(int index)
        {
            bool removed = _messages.Remove(index);
            OnComponentRemoved?.Invoke(this, index, false /* causedByDestroy */);

            return removed;
        }
    }
}