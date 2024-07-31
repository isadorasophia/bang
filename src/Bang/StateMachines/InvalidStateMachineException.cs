namespace Bang.StateMachines;

public class InvalidStateMachineException : InvalidOperationException
{
    public InvalidStateMachineException(string message) : base(message) { }
}