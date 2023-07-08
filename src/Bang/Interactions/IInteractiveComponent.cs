using Bang.Components;
using Bang.Entities;

namespace Bang.Interactions
{
    /// <summary>
    /// Component that will interact with another entity.
    /// </summary>
    public interface IInteractiveComponent : IComponent
    {
        /// <summary>
        /// This is the logic which will be immediately called once the <paramref name="interactor"/> interacts with the
        /// <paramref name="interacted"/>.
        /// </summary>
        public void Interact(World world, Entity interactor, Entity? interacted);
    }
}
