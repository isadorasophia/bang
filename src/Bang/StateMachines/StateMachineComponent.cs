using Newtonsoft.Json;
using Bang.Components;
using Bang.Entities;

namespace Bang.StateMachines
{
    /// <summary>
    /// Implements a state machine component.
    /// </summary>
    public struct StateMachineComponent<T> : IStateMachineComponent, IModifiableComponent where T : StateMachine, new()
    {
        /// <summary>
        /// This will fire a notification whenever the state changes.
        /// </summary>
        public string State => _routine.Name;

        [JsonProperty]
        private readonly T _routine;

        /// <summary>
        /// Creates a new <see cref="StateMachineComponent{T}"/>.
        /// </summary>
        public StateMachineComponent() => _routine = new();

        /// <summary>
        /// Default constructor initialize a brand new routine.
        /// </summary>
        [JsonConstructor]
        public StateMachineComponent([JsonProperty("_routine")] T routine) => _routine = routine;

        /// <summary>
        /// Initialize the state machine with the world knowledge. Called before any tick.
        /// </summary>
        public void Initialize(World world, Entity e) => _routine.Initialize(world, e);

        /// <summary>
        /// Tick a yield operation in the state machine. The next tick will be called according to the returned <see cref="WaitKind"/>.
        /// </summary>
        public bool Tick(float seconds) => _routine.Tick(seconds * 1000);

        /// <summary>
        /// Called right before the component gets destroyed.
        /// </summary>
        public void OnDestroyed() => _routine.OnDestroyed();

        /// <summary>
        /// Stop listening to notifications on this component.
        /// </summary>
        public void Unsubscribe(Action notification) => _routine.OnModified -= notification;

        /// <summary>
        /// Subscribe for notifications on this component.
        /// </summary>
        public void Subscribe(Action notification) => _routine.OnModified += notification;
    }
}