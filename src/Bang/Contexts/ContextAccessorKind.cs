namespace Bang.Contexts
{
    /// <summary>
    /// Context accessor kind for a system.
    /// This will specify the kind of operation that each system will perform, so the world
    /// can parallelize efficiently each system execution.
    /// </summary>
    [Flags]
    public enum ContextAccessorKind
    {
        /// <summary>
        /// This will specify that the system implementation will only perform read operations.
        /// </summary>
        Read,

        /// <summary>
        /// This will specify that the system implementation will only perform write operations.
        /// </summary>
        Write
    }
}