using Bang.Components;
using Bang.Systems;
using System.Diagnostics;

namespace Bang
{
    /// <summary>
    /// This will expose debug information used when creating a world.
    /// </summary>
    public partial class World
    {
#if DEBUG
        private static void CheckSystemsRequirements(IList<(ISystem system, bool isActive)> systems)
        {
            // First, list all the systems in the world according to their type, and map
            // to the order in which they appear.
            Dictionary<Type, int> systemTypes = new();
            for (int i = 0; i < systems.Count; i++)
            {
                Type t = systems[i].system.GetType();
                if (systemTypes.ContainsKey(t))
                {
                    Debug.Fail($"Why are we adding {t.Name} twice in the world!?");
                }

                systemTypes.Add(t, i);
            }

            foreach (var (t, index) in systemTypes)
            {
                if (Attribute.GetCustomAttribute(t, typeof(RequiresAttribute)) is RequiresAttribute requires)
                {
                    foreach (Type requiredSystem in requires.Types)
                    {
                        Debug.Assert(typeof(ISystem).IsAssignableFrom(requiredSystem),
                            "Why does the system requires a type that is not a system?");

                        if (systemTypes.TryGetValue(requiredSystem, out int order))
                        {
                            Debug.Assert(index > order,
                                $"Required system: {requiredSystem.Name} does not precede: {t.Name}.");
                        }
                        else
                        {
                            Debug.Fail($"Missing {requiredSystem.Name} required by {t.Name} in the world!");
                        }
                    }
                }
            }
        }
#endif
    }
}
