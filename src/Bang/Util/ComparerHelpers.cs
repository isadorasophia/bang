using Bang.Contexts;

namespace Bang.Util;

/// <summary>
/// Helper class for comparing <see cref="WatcherNotificationKind"/> in descending order.
/// </summary>
internal class DescendingWatcherNotificationKindComparer : IComparer<WatcherNotificationKind>
{
    public static DescendingWatcherNotificationKindComparer Instance = new();

    private DescendingWatcherNotificationKindComparer() { }

    public int Compare(WatcherNotificationKind x, WatcherNotificationKind y)
    {
        int iX = (int)x;
        int iY = (int)y;

        return -iX.CompareTo(iY);
    }
}
