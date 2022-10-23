namespace Bang.Components
{
    /// <summary>
    /// This is for a component that can be modified and is not an actual immutable.
    /// </summary>
    public interface IModifiableComponent : IComponent
    {
        /// <summary>
        /// Subscribe to receive notifications when the component gets modified.
        /// </summary>
        public void Subscribe(Action notification);

        /// <summary>
        /// Unsubscribe to receive notifications when the component gets modified.
        /// </summary>
        public void Unsubscribe(Action notification);
    }
}
