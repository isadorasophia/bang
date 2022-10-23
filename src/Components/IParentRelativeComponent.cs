using Bang.Entities;

namespace Bang.Components
{
    /// <summary>
    /// This represents a component which is relative to the parent.
    /// This will be notified each time that the tracking component of the parent changes.
    /// </summary>
    public interface IParentRelativeComponent : IComponent
    {
        /// <summary>
        /// Creates a copy of the component without any parent.
        /// </summary>
        public IParentRelativeComponent WithoutParent();

        /// <summary>
        /// Whether the component has a parent that it's tracking.
        /// </summary>
        public bool HasParent { get; }

        /// <summary>
        /// Called when a parent gets the <paramref name="parentComponent"/> modified.
        /// </summary>
        /// <param name="parentComponent">Component of the parent.</param>
        /// <param name="childEntity">Child entity tracking the parent.</param>
        public void OnParentModified(IComponent parentComponent, Entity childEntity);
    }
}
