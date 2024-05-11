using Bang.Components;
using Bang.Entities;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Bang.StateMachines
{
    /// <summary>
    /// This is a basic state machine for an entity.
    /// It is sort-of anti-pattern of ECS at this point. This is a trade-off
    /// between adding content and using ECS at the core of the game.
    /// </summary>
    public abstract class StateMachine
    {
        /// <summary>
        /// This is the only property of the state machine we will actually persist.
        /// This will keep track of the last state of the state machine.
        /// </summary>
        [Serialize]
        private string? _cachedPersistedState;

        /// <summary>
        /// World of the state machine.
        /// Initialized in <see cref="Initialize(Bang.World,Entities.Entity)"/>.
        /// </summary>
        protected World World = null!;

        /// <summary>
        /// Entity of the state machine.
        /// Initialized in <see cref="Initialize(Bang.World, Entities.Entity)"/>.
        /// </summary>
        protected Entity Entity = null!;

        /// <summary>
        /// Name of the active state. Used for debug.
        /// </summary>
        [JsonIgnore]
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Called when the state changes.
        /// Should only be called by the state machine component, see <see cref="StateMachineComponent{T}"/>.
        /// </summary>
        internal event Action? OnModified;

        /// <summary>
        /// Current state, represented by <see cref="Name"/>.
        /// Tracked if we ever need to reset to the start of the state.
        /// </summary>
        private Func<IEnumerator<Wait>>? CurrentState { get; set; }

        /// <summary>
        /// The routine the entity is currently executing.
        /// </summary>
        private IEnumerator<Wait>? Routine { get; set; }

        /// <summary>
        /// Track any wait time before calling the next Tick.
        /// </summary>
        private float? _waitTime = null;

        /// <summary>
        /// Track any amount of frames before calling the next Tick.
        /// </summary>
        private int? _waitFrames = null;

        /// <summary>
        /// Routine which we might be currently waiting on, before resuming to <see cref="Routine"/>.
        /// </summary>
        private readonly Stack<IEnumerator<Wait>> _routinesOnWait = new();

        /// <summary>
        /// Track the message we are waiting for.
        /// </summary>
        private int? _waitForMessage = null;

        /// <summary>
        /// Target entity for <see cref="_waitForMessage"/>.
        /// </summary>
        private Entity? _waitForMessageTarget = null;

        /// <summary>
        /// Tracks whether a message which was waited has been received.
        /// </summary>
        private bool _isMessageReceived = false;

        /// <summary>
        /// Whether this was the first time a tick was executed.
        /// Used to call <see cref="OnStart"/>.
        /// </summary>
        private bool _isFirstTick = true;

        /// <summary>
        /// Whether the state machine active state should be persisted on serialization.
        /// </summary>
        protected virtual bool PersistStateOnSave => true;

        /// <summary>
        /// Initialize the state machine. 
        /// Should only be called by the entity itself when it is registered in the world.
        /// </summary>
        [MemberNotNull(nameof(World))]
        [MemberNotNull(nameof(Entity))]
        internal virtual void Initialize(World world, Entity e)
        {
            Debug.Assert(Routine is not null, "Have you called State() before starting this state machine?");

            (World, Entity) = (world, e);

            // If the default state is not the same as the one we persisted, track it again, if we can!
            if (_cachedPersistedState is not null && Name != _cachedPersistedState && PersistStateOnSave)
            {
                MethodInfo? method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == _cachedPersistedState);
                if (method is not null)
                {
                    State((Func<IEnumerator<Wait>>)Delegate.CreateDelegate(typeof(Func<IEnumerator<Wait>>), this, method));
                }
            }

            e.OnMessage += OnMessageSent;
        }

        /// <summary>
        /// Initialize the state machine. Called before the first <see cref="Tick(float)"/> call.
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// Tick an update.
        /// Should only be called by the state machine component, see <see cref="StateMachineComponent{T}"/>.
        /// </summary>
        internal bool Tick(float dt)
        {
            Debug.Assert(World is not null && Entity is not null, "Why are we ticking before starting first?");

            if (_waitTime is not null)
            {
                _waitTime -= dt;

                if (_waitTime > 0)
                {
                    return true;
                }

                _waitTime = null;
            }

            if (_waitFrames is not null)
            {
                if (--_waitFrames > 0)
                {
                    return true;
                }

                _waitFrames = null;
            }

            if (_waitForMessage is not null)
            {
                if (!_isMessageReceived)
                {
                    return true;
                }

                _waitForMessage = null;
                _isMessageReceived = false;
            }

            Wait r = Tick();
            switch (r.Kind)
            {
                case WaitKind.Stop:
                    Finish();
                    return false;

                case WaitKind.Ms:
                    _waitTime = r.Value!.Value;
                    return true;

                case WaitKind.Frames:
                    _waitFrames = r.Value!.Value;
                    return true;

                case WaitKind.Message:
                    Entity target = r.Target ?? Entity;
                    int messageId = World.ComponentsLookup.Id(r.Component!);

                    if (target.HasMessage(messageId))
                    {
                        // The entity might already have the message within the frame.
                        // If that is the case, skip the wait and resume in the next frame.
                        _waitFrames = 1;
                        return true;
                    }

                    _waitForMessage = messageId;

                    // Do extra setup on custom targets.
                    if (r.Target is not null)
                    {
                        _waitForMessageTarget = r.Target;
                        target.OnMessage += OnMessageSent;
                    }
                    else
                    {
                        _waitForMessageTarget = null;
                    }

                    return true;

                case WaitKind.Routine:
                    _routinesOnWait.Push(r.Routine!);

                    // When we wait for a routine, immediately run it first.
                    return Tick(dt);
            }

            return true;
        }

        private Wait Tick()
        {
            if (Routine is null)
            {
                Debug.Assert(Routine is not null, "Have you called State() before ticking this state machine?");

                // Instead of embarrassingly crashing, send a stop wait message.
                return Wait.Stop;
            }

            if (_isFirstTick)
            {
                OnStart();
                _isFirstTick = false;
            }

            // If there is a wait routine, go for that instead.
            while (_routinesOnWait.Count != 0)
            {
                if (_routinesOnWait.Peek().MoveNext())
                {
                    return _routinesOnWait.Peek().Current ?? Wait.Stop;
                }
                else
                {
                    _routinesOnWait.Pop();
                }
            }

            if (!Routine.MoveNext())
            {
                return Wait.Stop;
            }

            return Routine?.Current ?? Wait.Stop;
        }

        internal void Finish()
        {
            OnDestroyed();

            if (Entity.TryGetComponent(out IStateMachineComponent? c) && c.GetType().GenericTypeArguments.Length > 0 &&
                c.GetType().GenericTypeArguments[0] == GetType() && c.State == Name)
            {
                Entity?.RemoveStateMachine();
            }
        }

        /// <summary>
        /// Clean up right before the state machine gets cleaned up.
        /// Callers must call the base implementation.
        /// </summary>
        public virtual void OnDestroyed()
        {
            Entity.OnMessage -= OnMessageSent;

            Routine?.Dispose();

            Routine = null;
            CurrentState = null;
            OnModified = null;

            Name = string.Empty;
        }

        /// <summary>
        /// This resets the current state of the state machine back to the beginning of that same state.
        /// </summary>
        protected void Reset()
        {
            Routine = CurrentState?.Invoke();
        }

        /// <summary>
        /// Redirects the state machine to a new <paramref name="routine"/>.
        /// </summary>
        /// <param name="routine">Target routine (new state).</param>
        protected virtual Wait GoTo(Func<IEnumerator<Wait>> routine)
        {
            SwitchState(routine);

            return Tick();
        }

        /// <summary>
        /// Redirects the state machine to a new <paramref name="routine"/> without doing
        /// a tick.
        /// </summary>
        /// <param name="routine">Target routine (new state).</param>
        protected virtual void Transition(Func<IEnumerator<Wait>> routine)
        {
            SwitchState(routine);
        }

        /// <summary>
        /// Redirects the state machine to a new <paramref name="routine"/> without doing
        /// a tick.
        /// </summary>
        /// <param name="routine">Target routine (new state).</param>
        protected void SwitchState(Func<IEnumerator<Wait>> routine)
        {
            // Also resets any pending wait state.
            // This is important if this happened to be called while interrupting an
            // ongoing state machine.
            _waitTime = null;
            _waitFrames = null;
            _waitForMessage = null;

            State(routine);
        }

        /// <summary>
        /// Set the current state of the state machine with <paramref name="routine"/>.
        /// </summary>
        protected void State(Func<IEnumerator<Wait>> routine)
        {
            Routine?.Dispose();

            CurrentState = routine;
            Routine = routine.Invoke();

            Name = routine.Method.Name;
            _cachedPersistedState = Name;

            OnModified?.Invoke();
        }

        private void OnMessageSent(Entity e, int index, IMessage message)
        {
            if (e.EntityId == Entity.EntityId)
            {
                OnMessage(message);
            }

            if (_waitForMessage is null ||
                (_waitForMessageTarget is not null && e.EntityId != _waitForMessageTarget.EntityId))
            {
                return;
            }

            if (index != _waitForMessage.Value)
            {
                return;
            }

            _isMessageReceived = true;

            if (_waitForMessageTarget is not null)
            {
                _waitForMessageTarget.OnMessage -= OnMessageSent;
                _waitForMessageTarget = null;
            }
        }

        /// <summary>
        /// Implemented by state machine implementations that want to listen to message
        /// notifications from outer systems.
        /// </summary>
        protected virtual void OnMessage(IMessage message) { }
    }
}