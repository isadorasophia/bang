using Bang.Components;
using Bang.Entities;

namespace Bang.StateMachines
{
    /// <summary>
    /// See <see cref="StateMachine"/> for more details. This is the component implementation.
    /// </summary>
    public interface IStateMachineComponent : IComponent
    {
        /// <summary>
        /// Initialize the state machine with the world knowledge. Called before any tick.
        /// </summary>
        public void Initialize(World world, Entity e);

        /// <summary>
        /// Name of the state machine. This is mostly used to debug.
        /// </summary>
        public string State { get; }

        /// <summary>
        /// Initialize all state machines.
        /// </summary>
        public void Start();

        /// <summary>
        /// Tick a yield operation in the state machine. The next tick will be called according to the returned <see cref="WaitKind"/>.
        /// </summary>
        public bool Tick(float dt);

        /// <summary>
        /// Called right before the component gets destroyed.
        /// </summary>
        public void OnDestroyed();
    }
}