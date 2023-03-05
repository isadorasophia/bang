using Newtonsoft.Json;
using Bang.Components;
using Bang.Entities;

namespace Bang.Interactions
{
    /// <summary>
    /// Implements an interaction component which will be passed on to the entity.
    /// </summary>
    public struct InteractiveComponent<T> : IInteractiveComponent, IModifiableComponent where T : Interaction, new()
    {
        [JsonProperty]
        private readonly T _interaction;

        /// <summary>
        /// Default constructor initialize a brand new interaction.
        /// </summary>
        public InteractiveComponent() => _interaction = new();

        /// <summary>
        /// Creates a new <see cref="InteractiveComponent{T}"/>.
        /// </summary>
        public InteractiveComponent(T interaction) => _interaction = interaction;

        /// <summary>
        /// This calls the inner interaction component.
        /// </summary>
        public void Interact(World world, Entity interactor, Entity? interacted)
            => _interaction.Interact(world, interactor, interacted);

        /// <summary>
        /// Stop listening to notifications on this component.
        /// </summary>
        public void Unsubscribe(Action notification) { }

        /// <summary>
        /// Subscribe for notifications on this component.
        /// </summary>
        public void Subscribe(Action notification) { }
    }
}
