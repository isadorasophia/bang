namespace Bang.Components
{
    /// <summary>
    /// A special type of component that can be modified.
    /// </summary>
    public interface IModifiableComponent : IComponent
    {
        /// <summary>
        /// Subscribe to receive notifications when the component gets modified.
        /// </summary>
        public void Subscribe(Action notification);

        /// <summary>
        /// Unsubscribe to stop receiving notifications when the component gets modified.
        /// </summary>
        public void Unsubscribe(Action notification);
    }
}