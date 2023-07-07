namespace Bang.StateMachines
{
    /// <summary>
    /// Wait between state machine calls.
    /// </summary>
    public enum WaitKind
    {
        /// <summary>
        /// Stops the state machine execution.
        /// </summary>
        Stop,

        /// <summary>
        /// Wait for 'x' ms.
        /// </summary>
        Ms,

        /// <summary>
        /// Wait for a message to be fired.
        /// </summary>
        Message,

        /// <summary>
        /// Wait for 'x' frames.
        /// </summary>
        Frames,

        /// <summary>
        /// Redirect execution to another routine. This will resume once that's finished.
        /// </summary>
        Routine
    }
}
