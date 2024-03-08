namespace Bang.Contexts
{
    /// <summary>
    /// When a system is watching for a component, this is the kind of notification currently fired.
    /// The order of the enumerator dictates the order that these will be called on the watcher systems.
    /// </summary>
    public enum WatcherNotificationKind
    {
        /// <summary>
        /// Component has been added. It is not called if the entity is dead.
        /// </summary>
        Added,

        /// <summary>
        /// Component was removed.
        /// </summary>
        Removed,

        /// <summary>
        /// Component was modified. It is not called if the entity is dead.
        /// </summary>
        Modified,

        /// <summary>
        /// Entity has been enabled, hence all its components. Called if an entity was
        /// previously disabled.
        /// </summary>
        Enabled,

        /// <summary>
        /// Entity has been disabled, hence all its components.
        /// </summary>
        Disabled
    }
}