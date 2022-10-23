using Bang.Systems;
using System.Reflection;

namespace Bang
{
    /// <summary>
    /// Reflection helper utility to access the world.
    /// </summary>
    public partial class World
    {
        private ComponentsLookup FindLookupImplementation()
        {
            Type lookup = typeof(ComponentsLookup);

            var isLookup = (Type t) => !t.IsInterface && !t.IsAbstract && lookup.IsAssignableFrom(t);

            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly s in allAssemblies)
            {
                foreach (Type t in s.GetTypes())
                {
                    if (isLookup(t))
                    {
                        return (ComponentsLookup)Activator.CreateInstance(t)!;
                    }
                }
            }

            throw new InvalidOperationException("A generator is required to be run before running the game!");
        }

        /// <summary>
        /// Returns whether a system is eligible to be paused.
        /// This means that:
        ///   - it is an update system;
        ///   - it does not have the DoNotPauseAttribute.
        /// </summary>
        private static bool IsPauseSystem(ISystem s)
        {
            if (s is IRenderSystem)
            {
                // do not pause render systems.
                return false;
            }

            if (s is not IFixedUpdateSystem && s is not IUpdateSystem)
            {
                // only pause update systems.
                return false;
            }

            if (Attribute.IsDefined(s.GetType(), typeof(DoNotPauseAttribute)))
            {
                return false;
            }

            return true;
        }
    }
}
