namespace Bang.Components;

/// <summary>
/// This will tag a component such that it should not do an equals operation
/// prior to replacing it.
/// </summary>
public interface IDoNotCheckOnReplaceTag
{
    /// <summary>
    /// Whether this should also ignore any notifications on component added, modified or removed.
    /// </summary>
    public bool SkipNotificationsOnComponent { get; }
}