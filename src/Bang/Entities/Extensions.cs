using Bang.Components;
using Bang.Interactions;
using Bang.StateMachines;

namespace Bang.Entities
{
    /// <summary>
    /// Quality of life extensions for the default components declared in Bang.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets a component of type <see cref="ITransformComponent"/>.
        /// </summary>
        public static ITransformComponent GetTransform(this Entity e)
            => e.GetComponent<ITransformComponent>(BangComponentTypes.Transform);

        /// <summary>
        /// Checks whether this entity possesses a component of type <see cref="ITransformComponent"/> or not.
        /// </summary>
        public static bool HasTransform(this Entity e)
            => e.HasComponent(BangComponentTypes.Transform);

        /// <summary>
        /// Gets a <see cref="ITransformComponent"/> if the entity has one, otherwise returns null.
        /// </summary>
        public static ITransformComponent? TryGetTransform(this Entity e)
            => e.HasTransform() ? e.GetTransform() : null;

        /// <summary>
        /// Adds or replaces the component of type <see cref="ITransformComponent" />.
        /// </summary>
        public static void SetTransform(this Entity e, ITransformComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.Transform);
        }

        /// <summary>
        /// Adds or replaces the component of type <see cref="ITransformComponent" />.
        /// </summary>
        public static Entity WithTransform(this Entity e, ITransformComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.Transform);
            return e;
        }

        /// <summary>
        /// Removes the component of type <see cref="ITransformComponent" />.
        /// </summary>
        public static bool RemoveTransform(this Entity e)
            => e.RemoveComponent(BangComponentTypes.Transform);

        /// <summary>
        /// Gets a component of type <see cref="IStateMachineComponent"/>.
        /// </summary>
        public static IStateMachineComponent GetStateMachine(this Entity e)
            => e.GetComponent<IStateMachineComponent>(BangComponentTypes.StateMachine);

        /// <summary>
        /// Checks whether this entity possesses a component of type <see cref="IStateMachineComponent"/> or not.
        /// </summary>
        public static bool HasStateMachine(this Entity e)
            => e.HasComponent(BangComponentTypes.StateMachine);

        /// <summary>
        /// Gets a <see cref="IStateMachineComponent"/> if the entity has one, otherwise returns null.
        /// </summary>
        public static IStateMachineComponent? TryGetStateMachine(this Entity e)
            => e.HasStateMachine() ? e.GetStateMachine() : null;

        /// <summary>
        /// Adds or replaces the component of type <see cref="IStateMachineComponent" />.
        /// </summary>
        public static void SetStateMachine(this Entity e, IStateMachineComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.StateMachine);
        }

        /// <summary>
        /// Adds or replaces the component of type <see cref="IStateMachineComponent" />.
        /// </summary>
        public static Entity WithStateMachine(this Entity e, IStateMachineComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.StateMachine);
            return e;
        }

        /// <summary>
        /// Removes the component of type <see cref="IStateMachineComponent" />.
        /// </summary>
        public static bool RemoveStateMachine(this Entity e)
            => e.RemoveComponent(BangComponentTypes.StateMachine);

        /// <summary>
        /// Gets a component of type <see cref="IInteractiveComponent"/>.
        /// </summary>
        public static IInteractiveComponent GetInteractive(this Entity e)
            => e.GetComponent<IInteractiveComponent>(BangComponentTypes.Interactive);

        /// <summary>
        /// Checks whether this entity possesses a component of type <see cref="IInteractiveComponent"/> or not.
        /// </summary>
        public static bool HasInteractive(this Entity e)
            => e.HasComponent(BangComponentTypes.Interactive);

        /// <summary>
        /// Gets a <see cref="IInteractiveComponent"/> if the entity has one, otherwise returns null.
        /// </summary>
        public static IInteractiveComponent? TryGetInteractive(this Entity e)
            => e.HasInteractive() ? e.GetInteractive() : null;

        /// <summary>
        /// Adds or replaces the component of type <see cref="IInteractiveComponent" />.
        /// </summary>
        public static void SetInteractive(this Entity e, IInteractiveComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.Interactive);
        }

        /// <summary>
        /// Adds or replaces the component of type <see cref="IInteractiveComponent" />.
        /// </summary>
        public static Entity WithInteractive(this Entity e, IInteractiveComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.Interactive);
            return e;
        }

        /// <summary>
        /// Removes the component of type <see cref="IInteractiveComponent" />.
        /// </summary>
        public static bool RemoveInteractive(this Entity e)
            => e.RemoveComponent(BangComponentTypes.Interactive);
    }
}