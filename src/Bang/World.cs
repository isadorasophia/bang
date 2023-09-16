using Bang.Systems;
using Bang.Contexts;
using Bang.Components;
using Bang.Entities;
using System.Collections.Immutable;
using System.Diagnostics;
using Bang.Diagnostics;

namespace Bang
{
    /// <summary>
    /// This is the internal representation of a world within ECS.
    /// A world has the knowledge of all the entities and all the systems that exist within the game.
    /// This handles dispatching information and handling disposal of entities.
    /// </summary>
    public partial class World : IDisposable
    {
        /// <summary>
        /// Use this to set whether diagnostics should be pulled from the world run.
        /// </summary>
        public static bool DIAGNOSTICS_MODE = true;

        private record struct SystemInfo
        {
            public readonly int ContextId { get; init; }

            public readonly int[] Watchers { get; init; }

            public readonly int? Messager { get; init; }

            /// <summary>
            /// Executing order of the system.
            /// Unique within the world.
            /// </summary>
            public readonly int Order { get; init; }

            public bool IsActive;
        }

        private readonly object _notificationLock = new();

        // ***
        // Tracks all the active systems in the world!
        // -> ATTENTION <- Keep this as a collection! We need to preserve the order!
        // ***

        // TODO: Maybe we can make the async calls with a synchronous entrypoint.
        // We implement our own lock to make sure they won't initialize until all the activities 
        // have finished.

        /// <summary>
        /// The startup systems will be called the first time they are activated.
        /// We will keep the systems here even after they were deactivated.
        /// </summary>
        private readonly SortedList<int, (IStartupSystem System, int ContextId)> _cachedStartupSystems;
        private readonly SortedList<int, (IExitSystem System, int ContextId)> _cachedExitSystems;

        private readonly SortedList<int, (IFixedUpdateSystem System, int ContextId)> _cachedFixedExecuteSystems;
        private readonly SortedList<int, (IUpdateSystem System, int ContextId)> _cachedExecuteSystems;

        /// <summary>
        /// This must be called by engine implementations of Bang to handle with rendering.
        /// </summary>
        protected readonly SortedList<int, (IRenderSystem System, int ContextId)> _cachedRenderSystems;

        /// <summary>
        /// Tracks down all the watchers id that require a notification operation.
        /// </summary>
        private HashSet<int>? _watchersTriggered = null;

        /// <summary>
        /// Whether there are any pending watchers.
        /// </summary>
        private bool AnyPendingWatchers
        {
            get
            {
                lock (_notificationLock)
                {
                    return _watchersTriggered is not null;
                }
            }
        }

        /// <summary>
        /// Tracks down all the entities that received a message notification within the frame.
        /// </summary>
        private HashSet<int>? _entitiesTriggeredByMessage = null;

        /// <summary>
        /// Tracks all registered systems across the world, regardless if they are active or not.
        /// Maps: System order id -> (IsActive, ContextId).
        /// </summary>
        private readonly IDictionary<int, SystemInfo> _systems;

        /// <summary>
        /// Used when fetching systems based on its unique identifier.
        /// Maps: System order id -> System instance.
        /// </summary>
        public readonly ImmutableDictionary<int, ISystem> IdToSystem;

        /// <summary>
        /// Maps: System type -> System id.
        /// </summary>
        private readonly ImmutableDictionary<Type, int> _typeToSystems;

        /// <summary>
        /// Set of systems that will be paused. See <see cref="IsPauseSystem(ISystem)"/> for more information.
        /// </summary>
        private readonly ImmutableHashSet<int> _pauseSystems;

        /// <summary>
        /// Set of systems that will only be played once a pause occur.
        /// </summary>
        private readonly ImmutableHashSet<int> _playOnPauseSystems;

        /// <summary>
        /// List of systems that will be resumed after a pause.
        /// These are the systems which were deactivated due to <see cref="Pause"/>.
        /// </summary>
        private readonly HashSet<int> _systemsToResume = new();

        /// <summary>
        /// Maps all the context IDs with the context.
        /// We might add new ones if a system calls for a new context filter.
        /// </summary>
        protected readonly Dictionary<int, Context> Contexts;

        /// <summary>
        /// Maps all the watcher IDs.
        /// Maps: Watcher Ids -> (Watcher, Systems that subscribe to this watcher).
        /// </summary>
        private readonly ImmutableDictionary<int, (ComponentWatcher Watcher, SortedList<int, IReactiveSystem> Systems)> _watchers;

        /// <summary>
        /// Maps all the messagers IDs.
        /// Maps: Messager Ids -> (Messager, Systems that subscribe to this messager).
        /// </summary>
        private readonly ImmutableDictionary<int, (MessageWatcher Watcher, SortedList<int, IMessagerSystem> Systems)> _messagers;

        /// <summary>
        /// Cache all the unique contexts according to the component type.
        /// Maps: Unique component type -> Context id.
        /// </summary>
        private readonly Dictionary<Type, int> _cacheUniqueContexts = new();

        /// <summary>
        /// Entities that exist within our world.
        /// TODO: Do we want some sort of object pooling here?
        /// Right now, if we remove entities, we will set the id to null.
        /// </summary>
        private readonly Dictionary<int, Entity> _entities = new();

        /// <summary>
        /// Entities which have been temporarily deactivated in the world.
        /// </summary>
        private readonly Dictionary<int, Entity> _deactivatedEntities = new();

        /// <summary>
        /// Entities that we will destroy within the world.
        /// </summary>
        private readonly HashSet<int> _pendingDestroyEntities = new();

        /// <summary>
        /// Systems which will be either activate or deactivated at the end of the frame.
        /// </summary>
        private readonly Dictionary<int, bool> _pendingActivateSystems = new();

        /// <summary>
        /// Entity count, used for generating the next id.
        /// </summary>
        private int _nextEntityId;

        /// <summary>
        /// Whether the world has been queried to be on pause or not.
        /// See <see cref="Pause"/>.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Map of all the components index across the world.
        /// </summary>
        internal readonly ComponentsLookup ComponentsLookup;

        /// <summary>
        /// Initialize the world!
        /// </summary>
        /// <param name="systems">List of systems and whether they are currently active in the world.</param>
        /// <exception cref="ArgumentException">If no systems are passed to the world.</exception>
        public World(IList<(ISystem system, bool isActive)> systems)
        {
            if (systems.Count == 0)
            {
                throw new ArgumentException("Cannot create a world without any systems.");
            }

            ComponentsLookup = FindLookupImplementation();

            var watchBuilder = ImmutableDictionary.CreateBuilder<int, (ComponentWatcher Watcher, SortedList<int, IReactiveSystem> Systems)>();
            var messageBuilder = ImmutableDictionary.CreateBuilder<int, (MessageWatcher Watcher, SortedList<int, IMessagerSystem> Systems)>();
            var pauseSystems = ImmutableHashSet.CreateBuilder<int>();
            var playOnPauseSystems = ImmutableHashSet.CreateBuilder<int>();

            var idToSystems = ImmutableDictionary.CreateBuilder<int, ISystem>();

            Contexts = new Dictionary<int, Context>();
            _systems = new Dictionary<int, SystemInfo>();

            for (int i = 0; i < systems.Count; i++)
            {
                var (s, isActive) = (systems[i].system, systems[i].isActive);

                Context c = new(this, s);
                if (Contexts.ContainsKey(c.Id))
                {
                    // Grab the correct context reference when adding events to it.
                    c = Contexts[c.Id];
                }
                else
                {
                    Contexts.Add(c.Id, c);
                }

                // If this is a reactive system, get all the watch components.
                List<int> componentWatchers = new();
                foreach (ComponentWatcher watcher in GetWatchComponentsForSystem(s, c))
                {
                    // Did we already created a watcher with the same id for another system?
                    if (!watchBuilder.ContainsKey(watcher.Id))
                    {
                        // First time! You shall be allowed to access the context.
                        watcher.SubscribeToContext(c);
                        watchBuilder.Add(watcher.Id, (watcher, new()));
                    }

                    if (isActive)
                    {
                        watchBuilder[watcher.Id].Systems.Add(i, (IReactiveSystem)s);
                    }

                    componentWatchers.Add(watcher.Id);
                }

                int? messageWatcher = null;
                if (TryGetMessagerForSystem(s, c) is MessageWatcher messager)
                {
                    if (!messageBuilder.ContainsKey(messager.Id))
                    {
                        messager.SubscribeToContext(c);
                        messageBuilder.Add(messager.Id, (messager, new()));
                    }

                    if (isActive)
                    {
                        messageBuilder[messager.Id].Systems.Add(i, (IMessagerSystem)s);
                    }

                    messageWatcher = messager.Id;
                }

                if (IsPlayOnPauseSystem(s))
                {
                    isActive = false;
                    playOnPauseSystems.Add(i);
                }
                else if (IsPauseSystem(s))
                {
                    pauseSystems.Add(i);
                }

                idToSystems.Add(i, s);
                _systems.Add(i, new SystemInfo { ContextId = c.Id, Watchers = componentWatchers.ToArray(), Messager = messageWatcher, Order = i, IsActive = isActive });
            }

            _watchers = watchBuilder.ToImmutable();
            _messagers = messageBuilder.ToImmutable();

            // Track the systems.
            IdToSystem = idToSystems.ToImmutable();
            _typeToSystems = idToSystems.ToImmutableDictionary(sv => sv.Value.GetType(), s => s.Key);
            _pauseSystems = pauseSystems.ToImmutable();
            _playOnPauseSystems = playOnPauseSystems.ToImmutable();

            _cachedStartupSystems = new(_systems.Where(kv => kv.Value.IsActive && IdToSystem[kv.Key] is IStartupSystem)
                .ToDictionary(kv => kv.Value.Order, kv => ((IStartupSystem)IdToSystem[kv.Key], kv.Value.ContextId)));

            _cachedExitSystems = new(_systems.Where(kv => kv.Value.IsActive && IdToSystem[kv.Key] is IExitSystem)
                .ToDictionary(kv => kv.Value.Order, kv => ((IExitSystem)IdToSystem[kv.Key], kv.Value.ContextId)));

            _cachedFixedExecuteSystems = new(_systems.Where(kv => kv.Value.IsActive && IdToSystem[kv.Key] is IFixedUpdateSystem)
                .ToDictionary(kv => kv.Value.Order, kv => ((IFixedUpdateSystem)IdToSystem[kv.Key], kv.Value.ContextId)));

            _cachedExecuteSystems = new(_systems.Where(kv => kv.Value.IsActive && IdToSystem[kv.Key] is IUpdateSystem)
                .ToDictionary(kv => kv.Value.Order, kv => ((IUpdateSystem)IdToSystem[kv.Key], kv.Value.ContextId)));

            _cachedRenderSystems = new(_systems.Where(kv => kv.Value.IsActive && IdToSystem[kv.Key] is IRenderSystem)
                .ToDictionary(kv => kv.Value.Order, kv => ((IRenderSystem)IdToSystem[kv.Key], kv.Value.ContextId)));

            if (DIAGNOSTICS_MODE)
            {
                CheckSystemsRequirements(systems);
                InitializeDiagnosticsCounters();
            }
        }

        /// <summary>
        /// Add a new empty entity to the world. 
        /// This will map the instance to the world.
        /// Any components added after this entity has been created will be notified to any reactive systems.
        /// </summary>
        public Entity AddEntity() => AddEntity(default, Array.Empty<IComponent>());

        /// <summary>
        /// Add a single entity to the world (e.g. collection of <paramref name="components"/>). 
        /// This will map the instance to the world.
        /// </summary>
        public Entity AddEntity(params IComponent[] components) => AddEntity(default, components);

        /// <summary>
        /// Add a single entity to the world (e.g. collection of <paramref name="components"/>). 
        /// This will map the instance to the world and add an entity with an existing id.
        /// </summary>
        public Entity AddEntity(int? id, IComponent[] components)
        {
            Entity e = new(this, CheckEntityId(id), components);

            AddEntity(e);
            return e;
        }

        /// <summary>
        /// Add a single entity to the world. This will map the instance to the world.
        /// Any components added after this entity has been created will be notified to any reactive systems.
        /// </summary>
        internal World AddEntity(Entity entity)
        {
            _entities.Add(entity.EntityId, entity);

            // Track end of the entity lifetime.
            entity.OnEntityDestroyed += RegisterToRemove;

            // Filter the entity across all active contexts.
            foreach (var (_, context) in Contexts)
            {
                context.FilterEntity(entity);
            }

            return this;
        }

        /// <summary>
        /// This will take <paramref name="id"/> and provide an entity id
        /// that has not been used by any other entity in the world.
        /// </summary>
        internal int CheckEntityId(int? id = default)
        {
            if (id is not null && _entities.ContainsKey(id.Value))
            {
                id = default;
            }

            if (id is null)
            {
                // Look for the next id available.
                id = _nextEntityId++;
                while (_entities.ContainsKey(id.Value) || _deactivatedEntities.ContainsKey(id.Value))
                {
                    id = _nextEntityId++;
                }

                return id.Value;
            }

            return id.Value;
        }

        /// <summary>
        /// Register that an entity must be removed in the end of the frame.
        /// </summary>
        private void RegisterToRemove(int id)
        {
            _pendingDestroyEntities.Add(id);
        }

        /// <summary>
        /// Destroy all the pending entities within the frame.
        /// </summary>
        private void DestroyPendingEntities()
        {
            if (_pendingDestroyEntities.Count == 0)
            {
                return;
            }

            ImmutableArray<int> entitiesToRemove = _pendingDestroyEntities.ToImmutableArray();
            _pendingDestroyEntities.Clear();

            foreach (int id in entitiesToRemove)
            {
                RemoveEntity(id);
            }
        }

        /// <summary>
        /// Activate and deactivate all pending systems.
        /// </summary>
        private void ActivateOrDeactivatePendingSystems()
        {
            if (_pendingActivateSystems.Count == 0)
            {
                return;
            }

            ImmutableDictionary<int, bool> systems = _pendingActivateSystems.ToImmutableDictionary();
            _pendingActivateSystems.Clear();

            foreach ((int id, bool activate) in systems)
            {
                if (activate)
                {
                    ActivateSystem(id, immediately: true);
                }
                else
                {
                    DeactivateSystem(id, immediately: true);
                }
            }
        }

        /// <summary>
        /// Removes an entity with <paramref name="id"/> from the world.
        /// </summary>
        private void RemoveEntity(int id)
        {
            if (_deactivatedEntities.ContainsKey(id))
            {
                _deactivatedEntities[id]?.Dispose();
                _entities.Remove(id);

                return;
            }

            Debug.Assert(_entities.ContainsKey(id), "Why are we removing an entity that has never been added?");

            _entities[id]?.Dispose();
            _entities.Remove(id);
        }

        /// <summary>
        /// Activates an entity in the world.
        /// Only called by an <see cref="Entity"/>.
        /// </summary>
        internal bool ActivateEntity(int id)
        {
            if (_deactivatedEntities.TryGetValue(id, out Entity? e))
            {
                _entities.Add(id, e);
                _deactivatedEntities.Remove(id);

                Debug.Assert(!e.IsDeactivated, $"Entity {id} should have been activated when calling this.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deactivate an entity in the world.
        /// Only called by an <see cref="Entity"/>.
        /// </summary>
        internal bool DeactivateEntity(int id)
        {
            if (_entities.TryGetValue(id, out Entity? e))
            {
                _deactivatedEntities.Add(id, e);
                _entities.Remove(id);

                Debug.Assert(e.IsDeactivated, $"Entity {id} should have been deactivated when calling this.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get an entity with the specific id.
        /// </summary>
        public Entity GetEntity(int id)
        {
            if (_deactivatedEntities.TryGetValue(id, out Entity? entity) && entity is not null)
            {
                // We consider looking up deactivated entities for this call.
                return entity;
            }

            Debug.Assert(_entities.ContainsKey(id), $"Expected to have entity with id: {id}.");

            return _entities[id];
        }

        /// <summary>
        /// Tries to get an entity with the specific id.
        /// If the entity is no longer among us, return null.
        /// </summary>
        public Entity? TryGetEntity(int id)
        {
            if (_entities.TryGetValue(id, out Entity? entity) && entity is not null)
            {
                return entity;
            }

            if (_deactivatedEntities.TryGetValue(id, out entity) && entity is not null)
            {
                return entity;
            }

            return default;
        }

        /// <summary>
        /// This should be used very cautiously! I hope you know what you are doing.
        /// It fetches all the entities within the world and return them.
        /// </summary>
        public ImmutableArray<Entity> GetAllEntities()
        {
            var entities = _entities.Values;
            var deactivatedEntities = _deactivatedEntities.Values;

            IEnumerable<Entity> allEntities = entities.Concat(deactivatedEntities);
            return allEntities.ToImmutableArray();
        }

        public int EntityCount => _entities.Count;

        /// <summary>
        /// Whether a system is active within the world.
        /// </summary>
        public bool IsSystemActive(Type t)
        {
            if (!_typeToSystems.TryGetValue(t, out int id))
            {
                // Most likely the system is simply not available.
                return false;
            }

            return _systems[id].IsActive;
        }

        /// <summary>
        /// Activate a system within our world.
        /// </summary>
        public bool ActivateSystem<T>()
        {
            return ActivateSystem(typeof(T));
        }

        /// <summary>
        /// Activate a system of type <paramref name="t"/> within our world.
        /// </summary>
        /// <returns>
        /// Whether the system is found and has been activated.
        /// </returns>
        public bool ActivateSystem(Type t)
        {
            if (!_typeToSystems.TryGetValue(t, out int id))
            {
                // Most likely the system is simply not available.
                return false;
            }

            return ActivateSystem(id);
        }

        /// <summary>
        /// Deactivate a system within our world.
        /// </summary>
        public bool DeactivateSystem<T>()
        {
            return DeactivateSystem(typeof(T));
        }

        /// <summary>
        /// Deactivate a system within our world.
        /// </summary>
        public bool DeactivateSystem(Type t)
        {
            if (!_typeToSystems.TryGetValue(t, out int id))
            {
                return false;
            }

            return DeactivateSystem(id);
        }

        /// <summary>
        /// Pause all the set of systems that qualify in <see cref="IsPauseSystem"/>.
        /// A paused system will no longer be called on any <see cref="Update"/> calls.
        /// </summary>
        public virtual void Pause()
        {
            IsPaused = true;

            // Start by activating all systems that wait for a pause.
            foreach (int id in _playOnPauseSystems)
            {
                ActivateSystem(id);
            }

            _systemsToResume.Clear();

            foreach (int id in _pauseSystems)
            {
                if (_systems[id].IsActive)
                {
                    _systemsToResume.Add(id);
                    DeactivateSystem(id);
                }
            }
        }

        /// <summary>
        /// This will resume all paused systems.
        /// </summary>
        public virtual void Resume()
        {
            IsPaused = false;

            foreach (int id in _systemsToResume)
            {
                ActivateSystem(id);
            }

            _systemsToResume.Clear();

            foreach (int id in _playOnPauseSystems)
            {
                if (_systems[id].IsActive)
                {
                    _systemsToResume.Add(id);
                    DeactivateSystem(id);
                }
            }
        }

        /// <summary>
        /// Activate a system within our world.
        /// </summary>
        private bool ActivateSystem(int id, bool immediately = false)
        {
            if (_pendingActivateSystems.TryGetValue(id, out bool active))
            {
                if (active)
                {
                    // System *will be* activated.
                    return false;
                }
            }
            else if (_systems[id].IsActive)
            {
                return false;
            }

            if (!immediately)
            {
                _pendingActivateSystems[id] = true;
                return true;
            }

            _systems[id] = _systems[id] with { IsActive = true };

            int context = _systems[id].ContextId;

            ISystem system = IdToSystem[id];
            if (system is IStartupSystem startupSystem && !_cachedStartupSystems.ContainsKey(id))
            {
                _cachedStartupSystems.Add(id, (startupSystem, context));

                // System has never started before. Start them here!
                startupSystem.Start(Contexts[context]);
            }

            if (system is IUpdateSystem updateSystem) _cachedExecuteSystems.Add(id, (updateSystem, context));
            if (system is IFixedUpdateSystem fixedUpdateSystem) _cachedFixedExecuteSystems.Add(id, (fixedUpdateSystem, context));
            if (system is IRenderSystem renderSystem) _cachedRenderSystems.Add(id, (renderSystem, context));

            if (system is IReactiveSystem reactiveSystem)
            {
                foreach (var watcherId in _systems[id].Watchers)
                {
                    _watchers[watcherId].Systems.Add(id, reactiveSystem);
                }
            }

            if (system is IMessagerSystem messagerSystem)
            {
                int messagerId = _systems[id].Messager!.Value;
                _messagers[messagerId].Systems.Add(id, messagerSystem);
            }

            return true;
        }

        /// <summary>
        /// Deactivate a system within our world.
        /// </summary>
        public bool DeactivateSystem(int id, bool immediately = false)
        {
            if (_pendingActivateSystems.TryGetValue(id, out bool active))
            {
                if (!active)
                {
                    // System *will be* deactivated.
                    return false;
                }
            }
            else if (!_systems[id].IsActive)
            {
                // System was already deactivated.
                return false;
            }

            if (!immediately)
            {
                _pendingActivateSystems[id] = false;
                return true;
            }

            if (DIAGNOSTICS_MODE)
            {
                UpdateDiagnosticsOnDeactivateSystem(id);
            }

            _systems[id] = _systems[id] with { IsActive = false };

            // We do not remove it from the list of startup systems, since it was already initialized.

            ISystem system = IdToSystem[id];
            if (system is IUpdateSystem) _cachedExecuteSystems.Remove(id);
            if (system is IFixedUpdateSystem) _cachedFixedExecuteSystems.Remove(id);
            if (system is IRenderSystem) _cachedRenderSystems.Remove(id);

            if (system is IReactiveSystem)
            {
                foreach (var watcherId in _systems[id].Watchers)
                {
                    _watchers[watcherId].Systems.Remove(id);
                }
            }

            if (system is IMessagerSystem)
            {
                int messagerId = _systems[id].Messager!.Value;
                _messagers[messagerId].Systems.Remove(id);
            }

            return true;
        }

        /// <summary>
        /// Activate all systems across the world.
        /// </summary>
        /// TODO: Optimize?
        public void ActivateAllSystems()
        {
            foreach (var (_, info) in _systems)
            {
                ActivateSystem(info.Order);
            }
        }

        /// <summary>
        /// Deactivate all systems across the world.
        /// </summary>
        /// TODO: Optimize?
        public void DeactivateAllSystems(params Type[] skip)
        {
            foreach (var (s, info) in _systems)
            {
                if (IsSystemOfType(IdToSystem[s], skip)) continue;

                DeactivateSystem(info.Order);
            }
        }

        /// <summary>
        /// Returns whether a system inherits from a given type.
        /// </summary>
        private static bool IsSystemOfType(ISystem system, Type[] skip)
        {
            for (int i = 0; i < skip.Length; i++)
            {
                Type t = skip[i];
                Type systemType = system.GetType();

                if (t == systemType || t.IsAssignableFrom(systemType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the unique component within an entity <typeparamref name="T"/>.
        /// </summary>
        public T GetUnique<T>() where T : struct, IComponent
        {
            T? component = TryGetUnique<T>();
            if (component is null)
            {
                throw new InvalidOperationException($"How do we not have a '{typeof(T).Name}' component within our world?");
            }

            return component.Value;
        }

        /// <summary>
        /// Try to get a unique entity that owns <typeparamref name="T"/>.
        /// </summary>
        /// <returns>
        /// The unique component <typeparamref name="T"/>.
        /// </returns>
        public T? TryGetUnique<T>() where T : struct, IComponent
        {
            if (TryGetUniqueEntity<T>() is Entity e)
            {
                return e.GetComponent<T>();
            }

            return default;
        }

        /// <summary>
        /// Get an entity with the unique component <typeparamref name="T"/>.
        /// </summary>
        public Entity GetUniqueEntity<T>() where T : struct, IComponent
        {
            Entity? entity = TryGetUniqueEntity<T>();
            if (entity is null)
            {
                throw new InvalidOperationException($"How do we not have the unique component of type '{typeof(T).Name}' within our world?");
            }

            return entity;
        }

        /// <summary>
        /// Try to get a unique entity that owns <typeparamref name="T"/>.
        /// </summary>
        public Entity? TryGetUniqueEntity<T>() where T : IComponent
        {
            if (!_cacheUniqueContexts.TryGetValue(typeof(T), out int contextId))
            {
                // Get the context for acquiring the unique component.
                contextId = GetOrCreateContext(ContextAccessorFilter.AnyOf, ComponentsLookup.Id(typeof(T)));

                _cacheUniqueContexts.Add(typeof(T), contextId);
            }

            Context context = Contexts[contextId];

            // We expect more than one entity if the remaining ones have been destroyed
#if DEBUG
            int nonDestroyedCount = 0;
            if (context.Entities.Length > 1)
            {
                nonDestroyedCount = context.Entities.Where(e => !e.IsDestroyed).Count();
                Debug.Assert(nonDestroyedCount == 1, "Why are there more than one entity with an unique component?");
            }
#endif

            Entity? e = context.Entities.LastOrDefault();
            return e is null || e.IsDestroyed ? null : e;
        }

        /// <summary>
        /// Retrieve a context for the specified filter and components.
        /// </summary>
        public ImmutableArray<Entity> GetEntitiesWith(params Type[] components)
        {
            return GetEntitiesWith(ContextAccessorFilter.AllOf, components);
        }

        /// <summary>
        /// Retrieve a context for the specified filter and components.
        /// </summary>
        public ImmutableArray<Entity> GetEntitiesWith(ContextAccessorFilter filter, params Type[] components)
        {
            int id = GetOrCreateContext(filter, components.Select(t => ComponentsLookup.Id(t)).ToArray());
            return Contexts[id].Entities;
        }

        /// <summary>
        /// Get or create a context id for the specified filter and components.
        /// </summary>
        private int GetOrCreateContext(ContextAccessorFilter filter, params int[] components)
        {
            Context context = new(this, filter, components);
            if (Contexts.ContainsKey(context.Id))
            {
                // Context already exists within our cache. Just return the id.
                return context.Id;
            }

            // Otherwise, we need to introduce the context to the world! Filter each entity.
            foreach (var (_, entity) in _entities)
            {
                context.FilterEntity(entity);
            }

            // Add new context to our cache.
            Contexts.Add(context.Id, context);

            return context.Id;
        }

        /// <summary>
        /// Call start on all systems.
        /// This is called before any updates and will notify any reactive systems by the end of it.
        /// </summary>
        public void Start()
        {
            foreach (var (_, (system, contextId)) in _cachedStartupSystems)
            {
                system.Start(Contexts[contextId]);
            }

            NotifyReactiveSystems();
        }

        /// <summary>
        /// Call to end all systems.
        /// This is called right before shutting down or switching scenes.
        /// </summary>
        public void Exit()
        {
            foreach (var (_, (system, contextId)) in _cachedExitSystems)
            {
                system.Exit(Contexts[contextId]);
            }
        }

        /// <summary>
        /// Calls update on all <see cref="IUpdateSystem"/> systems.
        /// At the end of update, it will notify all reactive systems of any changes made to entities
        /// they were watching.
        /// Finally, it destroys all pending entities and clear all messages.
        /// </summary>
        public void Update()
        {
            foreach (var (systemId, (system, contextId)) in _cachedExecuteSystems)
            {
                if (DIAGNOSTICS_MODE)
                {
                    _stopwatch.Reset();
                    _stopwatch.Start();
                }

                Context context = Contexts[contextId];
                system.Update(context);

                if (DIAGNOSTICS_MODE)
                {
                    InitializeDiagnosticsCounters();

                    _stopwatch.Stop();
                    UpdateCounters[systemId].Update(_stopwatch.Elapsed.TotalMicroseconds, context.Entities.Length);
                }
            }

            NotifyReactiveSystems();
            DestroyPendingEntities();
            ActivateOrDeactivatePendingSystems();

            // Clear the messages after the update so we can persist messages sent during Start().
            ClearMessages();
        }

        /// <summary>
        /// Calls update on all <see cref="IFixedUpdateSystem"/> systems.
        /// This will be called on fixed intervals.
        /// </summary>
        public void FixedUpdate()
        {
            foreach (var (systemId, (system, contextId)) in _cachedFixedExecuteSystems)
            {
                if (DIAGNOSTICS_MODE)
                {
                    _stopwatch.Reset();
                    _stopwatch.Start();
                }

                // TODO: We want to run systems which do not cross components in parallel.
                system.FixedUpdate(Contexts[contextId]);

                if (DIAGNOSTICS_MODE)
                {
                    InitializeDiagnosticsCounters();

                    _stopwatch.Stop();
                    FixedUpdateCounters[systemId].Update(_stopwatch.Elapsed.TotalMicroseconds, Contexts[contextId].Entities.Length);
                }
            }
        }

        /// <summary>
        /// Notify all reactive systems of any change that happened during the update.
        /// </summary>
        private void NotifyReactiveSystems()
        {
            ImmutableArray<int> watchersTriggered;

            lock (_notificationLock)
            {
                if (_watchersTriggered is null)
                {
                    if (DIAGNOSTICS_MODE)
                    {
                        // Make sure we update each reactive system with our nothing-ness.
                        foreach ((int systemId, SmoothCounter counter) in ReactiveCounters)
                        {
                            ReactiveCounters[systemId].Update(0, 0);
                        }
                    }

                    // Nothing to notified, just go away.
                    return;
                }

                watchersTriggered = _watchersTriggered.ToImmutableArray();
                _watchersTriggered = null;
            }

            // This will map:
            // [System ID] => ([Notification => Entities], System)
            // This is so we can track any duplicate entities reported for watchers of multiple components.
            Dictionary<int, (Dictionary<WatcherNotificationKind, Dictionary<int, Entity>> Notifications, IReactiveSystem System)> systemsToNotify = new();

            // First, iterate over each watcher and clean up their notification queue.
            foreach (int watcherId in watchersTriggered)
            {
                var (watcher, systems) = _watchers[watcherId];

                Dictionary<WatcherNotificationKind, Dictionary<int, Entity>> currentNotifications = watcher.PopNotifications();

                // Pass that notification for each system that it targets.
                foreach (var (systemId, system) in systems)
                {
                    // Ok, if no previous systems had any notifications, that's easy, just add right away.
                    if (!systemsToNotify.TryGetValue(systemId, out var notificationsAndSystem))
                    {
                        systemsToNotify.Add(systemId, (currentNotifications, system));
                        continue;
                    }

                    // Otherwise, things got tricky... Let us start by checking the notification kind.
                    foreach (var (kind, currentEntities) in currentNotifications)
                    {
                        // If the system did not have this notification previously, that's easy, just add right away then!
                        if (!notificationsAndSystem.Notifications.TryGetValue(kind, out var entities))
                        {
                            notificationsAndSystem.Notifications.Add(kind, currentEntities);
                            continue;
                        }

                        // Uh-oh, we got a conflicting notification kind. Merge them into the entities for the notification.
                        foreach (var (entityId, entity) in currentEntities)
                        {
                            if (!entities.ContainsKey(entityId))
                            {
                                entities.Add(entityId, entity);
                            }
                        }
                    }
                }
            }

            // This is used when DIAGNOSTICS_MODE is set to update reactive systems that were
            // not triggered.
            HashSet<int> triggeredSystems = new();

            // Now, iterate over each watcher and actually notify the systems based on their pending notifications.
            // This must be done *afterwards* since the reactive systems may add further notifications on their implementation.
            foreach (var (systemId, notificationsAndSystem) in systemsToNotify)
            {
                if (DIAGNOSTICS_MODE)
                {
                    _stopwatch.Reset();
                    _stopwatch.Start();
                }

                IReactiveSystem system = notificationsAndSystem.System;

                // Make sure we make this in order. Some components are added *and* removed in the same frame.
                // If this is the case, make sure we first call remove and *then* add.
                var orderedNotifications = notificationsAndSystem.Notifications.OrderByDescending(kv => (int)kv.Key);
                foreach (var (kind, entities) in orderedNotifications)
                {
                    if (entities.Count == 0)
                    {
                        // This might happen if all the entities were destroyed and no longer relevante to be passed on.
                        // Skip notifying in such cases.
                        continue;
                    }

                    ImmutableArray<Entity> entitiesInput = entities.Values.ToImmutableArray();

                    switch (kind)
                    {
                        case WatcherNotificationKind.Added:
                            system.OnAdded(this, entitiesInput);
                            break;

                        case WatcherNotificationKind.Removed:
                            system.OnRemoved(this, entitiesInput);
                            break;

                        case WatcherNotificationKind.Modified:
                            system.OnModified(this, entitiesInput);
                            break;

                        case WatcherNotificationKind.Enabled:
                            system.OnActivated(this, entitiesInput);
                            break;

                        case WatcherNotificationKind.Disabled:
                            system.OnDeactivated(this, entitiesInput);
                            break;
                    }
                }

                if (DIAGNOSTICS_MODE)
                {
                    InitializeDiagnosticsCounters();

                    _stopwatch.Stop();

                    ReactiveCounters[systemId].Update(
                        _stopwatch.Elapsed.TotalMicroseconds, totalEntities: notificationsAndSystem.Notifications.Sum(n => n.Value.Count));

                    triggeredSystems.Add(systemId);
                }
            }

            if (DIAGNOSTICS_MODE)
            {
                foreach ((int systemId, SmoothCounter counter) in ReactiveCounters)
                {
                    if (!triggeredSystems.Contains(systemId))
                    {
                        ReactiveCounters[systemId].Update(0, 0);
                    }
                }
            }

            // If the reactive systems triggered other operations, trigger that again.
            if (AnyPendingWatchers)
            {
                NotifyReactiveSystems();
            }
        }

        /// <summary>
        /// This will clear any messages received by the entities within a frame.
        /// </summary>
        private void ClearMessages()
        {
            ImmutableArray<int> entitiesTriggered;
            lock (_notificationLock)
            {
                if (_entitiesTriggeredByMessage is null)
                {
                    return;
                }

                entitiesTriggered = _entitiesTriggeredByMessage.ToImmutableArray();
                _entitiesTriggeredByMessage = null;
            }

            foreach (int entityId in entitiesTriggered)
            {
                if (_entities.TryGetValue(entityId, out Entity? e))
                {
                    // This will make sure that the entity has not been deleted.
                    e.ClearMessages();
                }
            }
        }

        internal void QueueWatcherNotification(int watcherId)
        {
            lock (_notificationLock)
            {
                _watchersTriggered ??= new();
                _watchersTriggered.Add(watcherId);
            }
        }

        /// <summary>
        /// Notify that a message has been received for a <paramref name="entity"/>.
        /// </summary>
        internal void OnMessage(Entity entity)
        {
            lock (_notificationLock)
            {
                _entitiesTriggeredByMessage ??= new();
                _entitiesTriggeredByMessage.Add(entity.EntityId);
            }
        }

        /// <summary>
        /// Notify that a message has been received for a <paramref name="entity"/>.
        /// This will notify all systems immediately and clear the message at the end of the update.
        /// </summary>
        internal void OnMessage(int messagerId, Entity entity, IMessage message)
        {
            OnMessage(entity);

            // Immediately notify all systems tied to this messager.
            foreach (var (_, system) in _messagers[messagerId].Systems)
            {
                system.OnMessage(this, entity, message);
            }
        }

        private IEnumerable<ComponentWatcher> GetWatchComponentsForSystem(ISystem system, Context context)
        {
            if (system is not IReactiveSystem)
            {
                Debug.Assert(!Attribute.IsDefined(system.GetType(), typeof(WatchAttribute)),
                    "Watch attribute for a non-reactive system. Attribute will be dropped.");
                yield break;
            }

            if (system.GetType().GetCustomAttributes(typeof(WatchAttribute), inherit: true).FirstOrDefault()
                is WatchAttribute attribute)
            {
                foreach (var t in attribute.Types)
                {
                    yield return new ComponentWatcher(this, context.Id, t);
                }
            }
        }

        private MessageWatcher? TryGetMessagerForSystem(ISystem system, Context context)
        {
            if (system is not IMessagerSystem)
            {
                Debug.Assert(!Attribute.IsDefined(system.GetType(), typeof(MessagerAttribute)),
                    "Messager attribute for a non-messager system. Attribute will be dropped.");
                return default;
            }

            if (system.GetType().GetCustomAttributes(typeof(MessagerAttribute), inherit: true).FirstOrDefault()
                is MessagerAttribute attribute)
            {
                return new MessageWatcher(this, context.Id, attribute.Types);
            }

            return default;
        }

        /// <summary>
        /// This will first call all <see cref="IExitSystem"/> to cleanup each system. 
        /// It will then call Dispose on each of the entities on the world and clear all the collections.
        /// </summary>
        public void Dispose()
        {
            Exit();

            foreach (Entity e in _entities.Values)
            {
                e.Dispose();
            }

            _entities.Clear();
            _deactivatedEntities.Clear();

            foreach (Context c in Contexts.Values)
            {
                c.Dispose();
            }

            Contexts.Clear();
        }
    }
}
