using Bang.Components;
using Bang.Entities;

namespace Bang.StateMachines
{
    /// <summary>
    /// A message fired to communicate the current state of the state machine.
    /// </summary>
    public record Wait
    {
        /// <summary>
        /// When should the state machine be called again.
        /// </summary>
        public readonly WaitKind Kind;

        /// <summary>
        /// Integer value, if kind is <see cref="WaitKind.Ms"/> or <see cref="WaitKind.Frames"/>.
        /// </summary>
        public int? Value;

        /// <summary>
        /// Used for <see cref="WaitKind.Message"/>.
        /// </summary>
        public Type? Component;

        /// <summary>
        /// Used for <see cref="WaitKind.Message"/> when waiting on another entity that is not the owner of the state machine.
        /// </summary>
        public Entity? Target;
        
        /// <summary>
        /// Used for <see cref="WaitKind.Routine"/>.
        /// </summary>
        public IEnumerator<Wait>? Routine;

        /// <summary>
        /// No longer execute the state machine.
        /// </summary>
        public static readonly Wait Stop = new();

        /// <summary>
        /// Wait for <paramref name="ms"/>.
        /// </summary>
        public static Wait ForMs(int ms) => new(WaitKind.Ms, ms);

        /// <summary>
        /// Wait for <paramref name="seconds"/>.
        /// </summary>
        public static Wait ForSeconds(float seconds) => new(WaitKind.Ms, (int)(seconds * 1000));

        /// <summary>
        /// Wait until message of type <typeparamref name="T"/> is fired.
        /// </summary>
        public static Wait ForMessage<T>() where T : IMessage => new(typeof(T));

        /// <summary>
        /// Wait until message of type <typeparamref name="T"/> is fired from <paramref name="target"/>.
        /// </summary>
        public static Wait ForMessage<T>(Entity target) where T : IMessage => new(typeof(T), target);

        /// <summary>
        /// Wait until <paramref name="frames"/> have occurred.
        /// </summary>
        public static Wait ForFrames(int frames) => new(WaitKind.Frames, frames);

        /// <summary>
        /// Wait until the next frame.
        /// </summary>
        public static Wait NextFrame => new(WaitKind.Frames, 0);

        /// <summary>
        /// Wait until <paramref name="routine"/> finishes.
        /// </summary>
        public static Wait ForRoutine(IEnumerator<Wait> routine) => new(routine);

        private Wait() => Kind = WaitKind.Stop;
        private Wait(WaitKind kind, int value) => (Kind, Value) = (kind, value);
        private Wait(Type messageType) => (Kind, Component) = (WaitKind.Message, messageType);
        private Wait(Type messageType, Entity target) => (Kind, Component, Target) = (WaitKind.Message, messageType, target);
        private Wait(IEnumerator<Wait> routine) => (Kind, Routine) = (WaitKind.Routine, routine);
    }
}
