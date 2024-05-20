using Bang.Entities;

namespace Bang.Interactions
{
    /// <summary>
    /// Implements an interaction component which will be passed on to the entity.
    /// </summary>
    public struct InteractiveComponent<T> : IInteractiveComponent where T : struct, IInteraction
    {
        [Serialize]
        private readonly T _interaction;

        /// <summary>
        /// Default constructor, initializes a brand new interaction.
        /// </summary>
        public InteractiveComponent() => _interaction = new();

        /// <summary>
        /// Creates a new <see cref="InteractiveComponent{T}"/>.
        /// </summary>
        public InteractiveComponent(T interaction) => _interaction = interaction;

        /// <summary>
        /// Calls the inner interaction component.
        /// </summary>
        public void Interact(World world, Entity interactor, Entity? interacted)
            => _interaction.Interact(world, interactor, interacted);
    }
}