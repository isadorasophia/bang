using Bang.Contexts;

namespace Bang.Systems;

/// <summary>
/// This is used for tracking when the system gets manually activated and deactivated.
/// </summary>
public interface IActivateAndDeactivateListenerSystem : ISystem
{
    /// <summary>
    /// Called once the system is activated. For now, this is not called on startup (should we?).
    /// </summary>
    public abstract void OnActivated(Context context);

    /// <summary>
    /// Called once the system is deactivated.
    /// </summary>
    public abstract void OnDeactivated(Context context);
}