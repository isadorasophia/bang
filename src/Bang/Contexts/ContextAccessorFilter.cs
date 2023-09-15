namespace Bang.Contexts
{
    /// <summary>
    /// Context accessor filter for a system.
    /// This will specify the kind of filter which will be performed on a certain list of component types.
    /// </summary>
    public enum ContextAccessorFilter
    {
        /// <summary>
        /// No filter is required. This won't be applied when filtering entities to a system.
        /// This is used when a system will, for example, add a new component to an entity but does
        /// not require such component.
        /// </summary>
        None = 1,

        /// <summary>
        /// Only entities which has all of the listed components will be fed to the system.
        /// </summary>
        AllOf = 2,

        /// <summary>
        /// Filter entities which has any of the listed components will be fed to the system.
        /// </summary>
        AnyOf = 3,

        /// <summary>
        /// Filter out entities that have the components listed.
        /// </summary>
        NoneOf = 4
    }
}
