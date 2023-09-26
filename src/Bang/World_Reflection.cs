using Bang.Systems;
using System.Reflection;

namespace Bang
{
    /// <summary>
    /// Reflection helper utility to access the world.
    /// </summary>
    public partial class World
    {
        /// <summary>
        /// Look for an implementation for the lookup table of components.
        /// </summary>
        public static ComponentsLookup FindLookupImplementation()
        {
            Type lookup = typeof(ComponentsLookup);

            var isLookup = (Type t) => !t.IsInterface && !t.IsAbstract && lookup.IsAssignableFrom(t);

            // We might find more than one lookup implementation, when inheriting projects with a generator.
            List<Type> candidateLookupImplementations = new();

            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly s in allAssemblies)
            {
                foreach (Type t in s.GetTypes())
                {
                    if (isLookup(t))
                    {
                        candidateLookupImplementations.Add(t);
                    }
                }
            }

            var target = candidateLookupImplementations.MaxBy(NumberOfParentClasses);
            if (target is not null)
            {
                return (ComponentsLookup)Activator.CreateInstance(target)!;
            }

            throw new InvalidOperationException("A generator is required to be run before running the game!");

            static int NumberOfParentClasses(Type type)
                => type.BaseType is null ? 0 : 1 + NumberOfParentClasses(type.BaseType);
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

        /// <summary>
        /// Returns whether a system is only expect to play when the game is paused.
        /// This is useful when defining systems that still track the game stack, even if paused.
        /// </summary>
        private static bool IsPlayOnPauseSystem(ISystem s)
        {
            return Attribute.IsDefined(s.GetType(), typeof(OnPauseAttribute));
        }
    }
}