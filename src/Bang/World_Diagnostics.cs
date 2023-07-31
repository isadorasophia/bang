using Bang.Components;
using Bang.Diagnostics;
using Bang.Systems;
using System.Diagnostics;

namespace Bang
{
    // This file contains the code responsible for debug information used when creating a world.
    public partial class World
    {
        private bool _initializedDiagnostics = false;

        protected readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        protected readonly Stopwatch _overallStopwatch = Stopwatch.StartNew();

        /// <summary>
        /// This has the duration of each update system (id) to its corresponding time (in ms).
        /// See <see cref="IdToSystem"/> on how to fetch the actual system.
        /// </summary>
        public readonly Dictionary<int, SmoothCounter> UpdateCounters = new();

        /// <summary>
        /// This has the duration of each fixed update system (id) to its corresponding time (in ms).
        /// See <see cref="IdToSystem"/> on how to fetch the actual system.
        /// </summary>
        public readonly Dictionary<int, SmoothCounter> FixedUpdateCounters = new();

        /// <summary>
        /// This has the duration of each reactive system (id) to its corresponding time (in ms).
        /// See <see cref="IdToSystem"/> on how to fetch the actual system.
        /// </summary>
        public readonly Dictionary<int, SmoothCounter> ReactiveCounters = new();

        /// <summary>
        /// Initialize the performance counters according to the systems present in the world.
        /// </summary>
        protected void InitializeDiagnosticsCounters()
        {
            Debug.Assert(DIAGNOSTICS_MODE, 
                "Why are we initializing diagnostics out of diagnostic mode?");
            
            if (_initializedDiagnostics)
            {
                // Already initialized.
                return;
            }
            
            _initializedDiagnostics = true;
            
            foreach (var (systemId, system) in IdToSystem)
            {
                if (system is IUpdateSystem)
                {
                    UpdateCounters[systemId] = new();
                }

                if (system is IFixedUpdateSystem)
                {
                    FixedUpdateCounters[systemId] = new();
                }

                if (system is IReactiveSystem)
                {
                    ReactiveCounters[systemId] = new();
                }

                InitializeDiagnosticsForSystem(systemId, system);
            }
        }
        
        private void UpdateDiagnosticsOnDeactivateSystem(int id) 
        {
            if (UpdateCounters.TryGetValue(id, out var value)) value.Clear();
            if (FixedUpdateCounters.TryGetValue(id, out value)) value.Clear();
            if (ReactiveCounters.TryGetValue(id, out value)) value.Clear();

            ClearDiagnosticsCountersForSystem(id);
        }

        /// <summary>
        /// Implemented by custom world in order to clear diagnostic information about the world.
        /// </summary>
        /// <param name="systemId"></param>
        protected virtual void ClearDiagnosticsCountersForSystem(int systemId) { }

        /// <summary>
        /// Implemented by custom world in order to express diagnostic information about the world.
        /// </summary>
        protected virtual void InitializeDiagnosticsForSystem(int systemId, ISystem system) { }
        
        private static void CheckSystemsRequirements(IList<(ISystem system, bool isActive)> systems)
        {
            // First, list all the systems in the world according to their type, and map
            // to the order in which they appear.
            Dictionary<Type, int> systemTypes = new();
            for (int i = 0; i < systems.Count; i++)
            {
                Type t = systems[i].system.GetType();
                
                Assert.Verify(!systemTypes.ContainsKey(t),
                    $"Why are we adding {t.Name} twice in the world!?");

                systemTypes.Add(t, i);
            }

            foreach (var (t, index) in systemTypes)
            {
                if (Attribute.GetCustomAttribute(t, typeof(RequiresAttribute)) is RequiresAttribute requires)
                {
                    foreach (Type requiredSystem in requires.Types)
                    {
                        Assert.Verify(typeof(ISystem).IsAssignableFrom(requiredSystem),
                            "Why does the system requires a type that is not a system?");
                        
                        if (systemTypes.TryGetValue(requiredSystem, out int order))
                        {
                            Assert.Verify(index > order,
                                $"Required system: {requiredSystem.Name} does not precede: {t.Name}.");
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Missing {requiredSystem.Name} required by {t.Name} in the world!");
                        }
                    }
                }
            }
        }
    }
}
