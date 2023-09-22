using Bang.Components;
using Bang.Interactions;
using Bang.StateMachines;

namespace Bang.Entities
{
    public static class Extensions
    {
        public static ITransformComponent GetTransform(this Entity e)
            => e.GetComponent<ITransformComponent>(BangComponentTypes.Transform);

        public static bool HasTransform(this Entity e)
            => e.HasComponent(BangComponentTypes.Transform);

        public static ITransformComponent? TryGetTransform(this Entity e)
            => e.HasTransform() ? e.GetTransform() : null;

        public static void SetTransform(this Entity e, ITransformComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.Transform);
        }

        public static Entity WithTransform(this Entity e, ITransformComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.Transform);
            return e;
        }

        public static bool RemoveTransform(this Entity e)
            => e.RemoveComponent(BangComponentTypes.Transform);

        public static IStateMachineComponent GetStateMachine(this Entity e)
            => e.GetComponent<IStateMachineComponent>(BangComponentTypes.StateMachine);

        public static bool HasStateMachine(this Entity e)
            => e.HasComponent(BangComponentTypes.StateMachine);

        public static IStateMachineComponent? TryGetStateMachine(this Entity e)
            => e.HasStateMachine() ? e.GetStateMachine() : null;

        public static void SetStateMachine(this Entity e, IStateMachineComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.StateMachine);
        }

        public static Entity WithStateMachine(this Entity e, IStateMachineComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.StateMachine);
            return e;
        }

        public static bool RemoveStateMachine(this Entity e)
            => e.RemoveComponent(BangComponentTypes.StateMachine);

        public static IInteractiveComponent GetInteractive(this Entity e)
            => e.GetComponent<IInteractiveComponent>(BangComponentTypes.Interactive);

        public static bool HasInteractive(this Entity e)
            => e.HasComponent(BangComponentTypes.Interactive);

        public static IInteractiveComponent? TryGetInteractive(this Entity e)
            => e.HasInteractive() ? e.GetInteractive() : null;

        public static void SetInteractive(this Entity e, IInteractiveComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.Interactive);
        }

        public static Entity WithInteractive(this Entity e, IInteractiveComponent component)
        {
            e.AddOrReplaceComponent(component, BangComponentTypes.Interactive);
            return e;
        }

        public static bool RemoveInteractive(this Entity e)
            => e.RemoveComponent(BangComponentTypes.Interactive);
    }
}