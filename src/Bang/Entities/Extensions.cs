using Bang.Components;
using Bang.Interactions;
using Bang.StateMachines;
using System.Numerics;

namespace Bang.Entities
{
    /// <summary>
    /// Quality of life extensions for the default components declared in Bang.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Checks whether this entity possesses a component of type <see cref="PositionComponent"/> or not.
        /// </summary>
        public static bool HasPosition(this Entity e) => e.HasComponent(BangComponentTypes.Position);

        /// <summary>
        /// Gets a <see cref="PositionComponent"/> if the entity has one, otherwise returns null.
        /// </summary>
        public static PositionComponent? TryGetPosition(this Entity e) => e.HasPosition() ? e.GetPosition() : null;

        /// <summary>
        /// Adds or replaces the component of type <see cref="PositionComponent" />.
        /// </summary>
        public static void SetPosition(this Entity e, PositionComponent component) => 
            e.AddOrReplaceComponent(component, BangComponentTypes.Position);

        /// <summary>
        /// Adds or replaces the component of type <see cref="PositionComponent" />.
        /// </summary>
        public static void SetPosition(this Entity e, Vector2 position) =>
            e.AddOrReplaceComponent(new PositionComponent(position), BangComponentTypes.Position);

        /// <summary>
        /// Adds or replaces the component of type <see cref="PositionComponent" />.
        /// </summary>
        public static void SetPosition(this Entity e, float x, float y) =>
            e.AddOrReplaceComponent(new PositionComponent(x, y), BangComponentTypes.Position);

        /// <summary>
        /// Removes the component of type <see cref="PositionComponent" />.
        /// </summary>
        public static bool RemovePosition(this Entity e) => e.RemoveComponent(BangComponentTypes.Position);

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