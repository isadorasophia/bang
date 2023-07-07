namespace Bang.Systems
{
    /// <summary>
    /// Indicates that a system will not be deactivated on pause.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class OnPauseAttribute : Attribute
    {
    }
}
