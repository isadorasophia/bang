using Bang.Components;
using Bang.Entities;
using Bang.Interactions;
using Bang.StateMachines;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Bang
{
    /// <summary>
    /// Implemented by generators in order to provide a mapping of all the types to their respective id.
    /// </summary>
    public abstract class ComponentsLookup
    {
        /// <summary>
        /// Tracks the last id this particular implementation is tracking plus one.
        /// </summary>
        public const int NextLookupId = 3;

        /// <summary>
        /// Maps all the components to their unique id.
        /// </summary>
        protected ImmutableDictionary<Type, int> ComponentsIndex { get; init; } = new Dictionary<Type, int>
        {
            { typeof(IStateMachineComponent), BangComponentTypes.StateMachine },
            { typeof(IInteractiveComponent), BangComponentTypes.Interactive },
            { typeof(PositionComponent), BangComponentTypes.Position }
        }.ToImmutableDictionary();

        /// <summary>
        /// Maps all the messages to their unique id.
        /// </summary>
        protected ImmutableDictionary<Type, int> MessagesIndex { get; init; } = new Dictionary<Type, int>().ToImmutableDictionary();

        /// <summary>
        /// List of all the unique id of the components that inherit from <see cref="IParentRelativeComponent"/>.
        /// </summary>
        public ImmutableHashSet<int> RelativeComponents { get; protected init; } =
            ImmutableHashSet.Create(BangComponentTypes.Position);

        /// <summary>
        /// Tracks components and messages without a generator. This query will have a lower performance.
        /// </summary>
        private readonly Dictionary<Type, int> _untrackedIndices = new();

        /// <summary>
        /// Tracks relative components without a generator. This query will have a lower performance.
        /// </summary>
        private readonly HashSet<int> _untrackedRelativeComponents = new();

        private int? _nextUntrackedIndex;

        /// <summary>
        /// Get the id for <paramref name="t"/> component type.
        /// </summary>
        /// <param name="t">Type.</param>
        public int Id(Type t)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(t) || typeof(IMessage).IsAssignableFrom(t),
                "Why are we receiving a type that is not an IComponent?");

            int index;

            if (typeof(IMessage).IsAssignableFrom(t) && MessagesIndex.TryGetValue(t, out index))
            {
                return index;
            }

            if (ComponentsIndex.TryGetValue(t, out index))
            {
                return index;
            }

            if (_untrackedIndices.TryGetValue(t, out index))
            {
                return index;
            }

            return AddUntrackedIndexForComponentOrMessage(t);
        }

        /// <summary>
        /// Returns whether a <paramref name="id"/> is relative to its parent.
        /// </summary>
        public bool IsRelative(int id)
        {
            return RelativeComponents.Contains(id);
        }

        internal int TotalIndices => ComponentsIndex.Count + MessagesIndex.Count + _untrackedIndices.Count;

        private int AddUntrackedIndexForComponentOrMessage(Type t)
        {
            int? id = null;

            if (!t.IsInterface)
            {
                if (typeof(IStateMachineComponent).IsAssignableFrom(t))
                {
                    id = Id(typeof(IStateMachineComponent));
                }
                else if (typeof(IInteractiveComponent).IsAssignableFrom(t))
                {
                    id = Id(typeof(IInteractiveComponent));
                }
            }
            else
            {
                Debug.Assert(t != typeof(IComponent), "Why are we doing a lookup for an IComponent itself?");
            }

            if (id is null)
            {
                _nextUntrackedIndex ??= ComponentsIndex.Count + MessagesIndex.Count;

                id = _nextUntrackedIndex++;
            }

            _untrackedIndices.Add(t, id.Value);

            if (typeof(IParentRelativeComponent).IsAssignableFrom(t))
            {
                _untrackedRelativeComponents.Add(id.Value);
            }

            return id.Value;
        }
    }
}