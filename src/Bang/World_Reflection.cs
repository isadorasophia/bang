using Bang.Systems;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bang
{
    /// <summary>
    /// Reflection helper utility to access the world.
    /// </summary>
    public partial class World
    {
        /// <summary>
        /// Cache the lookup implementation for this game.
        /// </summary>
        private static ComponentsLookup? _lookupImplementation = null;

        public static void InitializeLookupComponents(ComponentsLookup lookup)
        {
            _lookupImplementation = lookup;
        }

        /// <summary>
        /// Look for an implementation for the lookup table of components.
        /// </summary>
        public static ComponentsLookup FindLookupImplementation()
        {
            if (_lookupImplementation is not null)
            {
                return _lookupImplementation;
            }

            _lookupImplementation = SlowFetchComponentsLookupImplementation();
            if (_lookupImplementation is null)
            {
                throw new InvalidOperationException("A generator is required to be run before running the game!");
            }

            return _lookupImplementation;
        }

        [UnconditionalSuppressMessage("AOT", "IL2026:System.Reflection.Assembly.GetTypes() can break functionality when trimming application code. Types might be removed.", Justification = "Target assemblies scanned are not trimmed.")]
        [UnconditionalSuppressMessage("AOT", "IL2072:Public constructor might have been removed when scanning the candidate type.", Justification = "Target assemblies scanned are not trimmed.")]
        private static ComponentsLookup? SlowFetchComponentsLookupImplementation()
        {
            Type lookup = typeof(ComponentsLookup);

            var isLookup = (Type t) => !t.IsInterface && !t.IsAbstract && lookup.IsAssignableFrom(t);

            // We might find more than one lookup implementation, when inheriting projects with a generator.
            List<Type> candidateLookupImplementations = [];

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

            Type? result = candidateLookupImplementations.MaxBy(NumberOfParentClasses);
            if (result is not null)
            {
                return Activator.CreateInstance(result) as ComponentsLookup;
            }

            return null;

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
            if (Attribute.IsDefined(s.GetType(), typeof(IncludeOnPauseAttribute)))
            {
                return true;
            }

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