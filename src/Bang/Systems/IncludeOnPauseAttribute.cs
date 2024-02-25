namespace Bang.Systems
{
    /// <summary>
    /// Indicates that a system will be included when the world is paused.
    /// This will override <see cref="DoNotPauseAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class IncludeOnPauseAttribute : Attribute
    {
    }
}