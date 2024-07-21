namespace Bang.Components
{
    /// <summary>
    /// A set of components will define an entity. This can be any sort of game abstraction.
    /// Bang follows the convention of only defining components for readonly structs.
    /// </summary>
    public interface IComponent
    {
        public static bool Equals(IComponent? a, IComponent? b)
        {
            if (a is null || b is null)
            {
                return a is null && b is null;
            }

            if (a is IEquatable<IComponent> iA)
            {
                return iA.Equals(b);
            }

            return a.Equals(b);
        }
    }
}