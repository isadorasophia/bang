using Bang.Entities;

namespace Bang.Interactions
{
    /// <summary>
    /// An interaction is any logic which will be immediately sent to another entity.
    /// </summary>
    public interface IInteraction
    {
        /// <summary>
        /// Contract immediately performed once <paramref name="interactor"/> interacts with <paramref name="interacted"/>.
        /// </summary>
        public abstract void Interact(World world, Entity interactor, Entity? interacted);
    }
}