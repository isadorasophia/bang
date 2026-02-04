namespace Bang.Entities
{
    /// <summary>
    /// Ids reserved for special Bang components.
    /// </summary>
    public static class BangComponentTypes
    {
        /// <summary>
        /// Unique Id used for the lookup of components with type <see cref="Bang.StateMachines.IStateMachineComponent"/>.
        /// </summary>
        public const int StateMachine = 0;

        /// <summary>
        /// Unique Id used for the lookup of components with type <see cref="Bang.Interactions.IInteractiveComponent"/>.
        /// </summary>
        public const int Interactive = 1;

        /// <summary>
        /// Unique Id used for the lookup of components with type <see cref="Bang.Components.PositionComponent"/>.
        /// </summary>
        public const int Position = 2;
    }
}