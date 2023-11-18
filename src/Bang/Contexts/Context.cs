using Bang.Components;
using Bang.Entities;
using Bang.Systems;
using Bang.Util;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Bang.Contexts
{
    /// <summary>
    /// Context is the pool of entities accessed by each system that defined it.
    /// </summary>
    public class Context : Observer, IDisposable
    {
        /// <summary>
        /// List of entities that will be fed to the system of this context.
        /// </summary>
        private readonly Dictionary<int, Entity> _entities = new();

        /// <summary>
        /// Cached value of the immutable set of entities.
        /// </summary>
        private ImmutableArray<Entity>? _cachedEntities = null;

        /// <summary>
        /// Track the target components and what kind of filter should be performed for each.
        /// </summary>
        private readonly ImmutableDictionary<ContextAccessorFilter, ImmutableArray<int>> _targetComponentsIndex;

        /// <summary>
        /// Track the kind of operation the system will perform for each of the components.
        /// This is saved as a hash set since we will be using this to check if a certain component is set.
        /// </summary>
        private readonly ImmutableDictionary<ContextAccessorKind, ImmutableHashSet<int>> _componentsOperationKind;

        internal ImmutableHashSet<int> ReadComponents => _componentsOperationKind[ContextAccessorKind.Read];

        internal ImmutableHashSet<int> WriteComponents => _componentsOperationKind[ContextAccessorKind.Write];

        /// <summary>
        /// This will be fired when a component is added to an entity present in the system.
        /// </summary>
        internal event Action<Entity, int>? OnComponentAddedForEntityInContext;

        /// <summary>
        /// This will be fired when a component is removed from an entity present in the system.
        /// </summary>
        internal event Action<Entity, int, bool>? OnComponentRemovedForEntityInContext;

        /// <summary>
        /// This will be fired when a component is modified from an entity present in the system.
        /// </summary>
        internal event Action<Entity, int>? OnComponentModifiedForEntityInContext;

        /// <summary>
        /// This will be fired when an entity (which was previously disabled) gets enabled.
        /// </summary>
        internal event Action<Entity>? OnActivateEntityInContext;

        /// <summary>
        /// This will be fired when an entity (which was previously enabled) gets disabled.
        /// </summary>
        internal event Action<Entity>? OnDeactivateEntityInContext;

        /// <summary>
        /// This will be fired when a message gets added in an entity present in the system.
        /// </summary>
        internal event Action<Entity, int, IMessage>? OnMessageSentForEntityInContext;

        private readonly int _id;

        internal override int Id => _id;

        /// <summary>
        /// Returns whether this context does not have any filter and grab all entities instead.
        /// </summary>
        private bool IsNoFilter => _targetComponentsIndex.ContainsKey(ContextAccessorFilter.None);

        /// <summary>
        /// Entities that are currently active in the context.
        /// </summary>
        public override ImmutableArray<Entity> Entities
        {
            get
            {
                if (_cachedEntities is null)
                {
                    _cachedEntities = _entities.Values.ToImmutableArray();
                }

                return _cachedEntities.Value;
            }
        }

        /// <summary>
        /// Get the single entity present in the context.
        /// This assumes that the context targets a unique component.
        /// TODO: Add flag that checks for unique components within this context.
        /// </summary>
        public Entity Entity
        {
            get
            {
                Debug.Assert(_entities.Count == 1, "Getting an entity for a non-unique context?");

                return _entities.First().Value;
            }
        }

        /// <summary>
        /// Whether the context has any entity active.
        /// </summary>
        public bool HasAnyEntity => _entities.Count > 0;

        internal Context(World world, ISystem system) : base(world)
        {
            var filters = CreateFilterList(system);
            _targetComponentsIndex = CreateTargetComponents(filters);
            _componentsOperationKind = CreateAccessorKindComponents(filters);

            _id = CalculateId();
        }

        /// <summary>
        /// Initializes a context that is not necessarily tied to any system.
        /// </summary>
        internal Context(World world, ContextAccessorFilter filter, params int[] components) : base(world)
        {
            _targetComponentsIndex =
                new Dictionary<ContextAccessorFilter, ImmutableArray<int>> { { filter, components.ToImmutableArray() } }.ToImmutableDictionary();
            _componentsOperationKind = ImmutableDictionary<ContextAccessorKind, ImmutableHashSet<int>>.Empty;

            _id = CalculateId();
        }

        /// <summary>
        /// This gets the context unique identifier.
        /// This is important to get it right since it will be reused across different systems.
        /// It assumes that we won't get more than 1000 components declared. If this changes (oh! hello!), maybe we should
        /// reconsider this code.
        /// </summary>
        private int CalculateId()
        {
            List<int> allComponents = new();

            // Dictionaries by themselves do not guarantee any ordering.
            var orderedComponentsFilter = _targetComponentsIndex.OrderBy(kv => kv.Key);
            foreach (var (filter, collection) in orderedComponentsFilter)
            {
                // Add the filter identifier. This is negative so the hash can uniquely identify them.
                allComponents.Add(-(int)filter);
                allComponents.AddRange(collection.Sort());
            }

            return allComponents.GetHashCodeImpl();
        }

        private ImmutableArray<(FilterAttribute, ImmutableArray<int>)> CreateFilterList(ISystem system)
        {
            var lookup = (Type t) => Lookup.Id(t);

            var builder = ImmutableArray.CreateBuilder<(FilterAttribute, ImmutableArray<int>)>();

            // First, grab all the filters of the system.
            FilterAttribute[] filters = (FilterAttribute[])system
                .GetType().GetCustomAttributes(typeof(FilterAttribute), inherit: true);

            // Now, for each filter, populate our set of files.
            foreach (var filter in filters)
            {
                builder.Add((filter, filter.Types.Select(t => lookup(t)).ToImmutableArray()));
            }

            return builder.ToImmutableArray();
        }

        /// <summary>
        /// Create a list of which components we will be watching for when adding a new entity according to a
        /// <see cref="ContextAccessorFilter"/>.
        /// </summary>
        private ImmutableDictionary<ContextAccessorFilter, ImmutableArray<int>> CreateTargetComponents(
            ImmutableArray<(FilterAttribute, ImmutableArray<int>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<ContextAccessorFilter, ImmutableArray<int>>();

            foreach (var (filter, targets) in filters)
            {
                // Keep track of empty contexts.
                if (filter.Filter is ContextAccessorFilter.None)
                {
                    builder[filter.Filter] = ImmutableArray<int>.Empty;
                    continue;
                }

                if (targets.IsDefaultOrEmpty)
                {
                    // No-op, this is so we can watch for the accessor kind.
                    continue;
                }

                // We might have already added components for the filter for another particular kind of target,
                // so check if it has already been added in a previous filter.
                if (!builder.ContainsKey(filter.Filter))
                {
                    builder[filter.Filter] = targets;
                }
                else
                {
                    builder[filter.Filter] = builder[filter.Filter].Union(targets).ToImmutableArray();
                }
            }

            return builder.ToImmutableDictionary();
        }

        private ImmutableDictionary<ContextAccessorKind, ImmutableHashSet<int>> CreateAccessorKindComponents(
            ImmutableArray<(FilterAttribute, ImmutableArray<int>)> filters)
        {
            var builder = ImmutableDictionary.CreateBuilder<ContextAccessorKind, ImmutableHashSet<int>>();

            // Initialize both fields as empty, if there is none.
            builder[ContextAccessorKind.Read] = ImmutableHashSet<int>.Empty;
            builder[ContextAccessorKind.Write] = ImmutableHashSet<int>.Empty;

            foreach (var (filter, targets) in filters)
            {
                if (targets.IsDefaultOrEmpty || filter.Filter is ContextAccessorFilter.NoneOf)
                {
                    // No-op, this will never be consumed by the system.
                    continue;
                }

                ContextAccessorKind kind = filter.Kind;
                if (kind.HasFlag(ContextAccessorKind.Write))
                {
                    // If this is a read/write, just cache it as a write operation.
                    // Not sure if we can do anything with the information of a read...?
                    kind = ContextAccessorKind.Write;
                }

                // We might have already added components for the filter for another particular kind of target,
                // so check if it has already been added in a previous filter.
                if (builder[kind].IsEmpty)
                {
                    builder[kind] = targets.ToImmutableHashSet();
                }
                else
                {
                    builder[kind] = builder[kind].Union(targets).ToImmutableHashSet();
                }
            }

            return builder.ToImmutableDictionary();
        }

        /// <summary>
        /// Filter an entity for the first time in this context.
        /// This is called when the entity is first created an set into the world.
        /// </summary>
        internal override void FilterEntity(Entity entity)
        {
            if (IsNoFilter)
            {
                // No entities are caught by this context.
                return;
            }

            entity.OnComponentAdded += OnEntityComponentAdded;
            entity.OnComponentRemoved += OnEntityComponentRemoved;

            if (DoesEntityMatch(entity))
            {
                entity.OnComponentRemoved += OnComponentRemovedForEntityInContext;
                entity.OnComponentModified += OnComponentModifiedForEntityInContext;

                entity.OnMessage += OnMessageSentForEntityInContext;

                entity.OnEntityActivated += OnEntityActivated;
                entity.OnEntityDeactivated += OnEntityDeactivated;

                if (OnComponentAddedForEntityInContext is not null)
                {
                    if (!entity.IsDeactivated)
                    {
                        // TODO: Optimize this? We must notify all the reactive systems
                        // that the entity has been added.
                        foreach (int c in entity.ComponentsIndices)
                        {
                            OnComponentAddedForEntityInContext.Invoke(entity, c);
                        }
                    }

                    entity.OnComponentAdded += OnComponentAddedForEntityInContext;
                }

                if (!entity.IsDeactivated)
                {
                    _entities[entity.EntityId] = entity;
                    _cachedEntities = null;
                }
            }
        }

        /// <summary>
        /// Returns whether the entity matches the filter for this context.
        /// </summary>
        private bool DoesEntityMatch(Entity e)
        {
            if (_targetComponentsIndex.ContainsKey(ContextAccessorFilter.NoneOf))
            {
                foreach (var c in _targetComponentsIndex[ContextAccessorFilter.NoneOf])
                {
                    if (e.HasComponentOrMessage(c))
                    {
                        return false;
                    }
                }
            }

            if (_targetComponentsIndex.ContainsKey(ContextAccessorFilter.AllOf))
            {
                foreach (var c in _targetComponentsIndex[ContextAccessorFilter.AllOf])
                {
                    if (!e.HasComponentOrMessage(c))
                    {
                        return false;
                    }
                }
            }

            if (_targetComponentsIndex.ContainsKey(ContextAccessorFilter.AnyOf))
            {
                foreach (var c in _targetComponentsIndex[ContextAccessorFilter.AnyOf])
                {
                    if (e.HasComponentOrMessage(c))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        internal override void OnEntityComponentAdded(Entity e, int index)
        {
            if (e.IsDestroyed)
            {
                return;
            }

            OnEntityModified(e, index);
        }

        internal override void OnEntityComponentRemoved(Entity e, int index, bool causedByDestroy)
        {
            if (e.IsDestroyed)
            {
                if (_entities.ContainsKey(e.EntityId))
                {
                    // The entity was just destroyed, don't bother filtering it.
                    // Destroy it immediately.
                    StopWatchingEntity(e, index, causedByDestroy: true);
                }

                return;
            }

            OnEntityModified(e, index);
        }

        internal void OnEntityActivated(Entity e)
        {
            if (!_entities.ContainsKey(e.EntityId))
            {
                _entities.Add(e.EntityId, e);
                _cachedEntities = null;

                OnActivateEntityInContext?.Invoke(e);
            }
        }

        internal void OnEntityDeactivated(Entity e)
        {
            if (_entities.ContainsKey(e.EntityId))
            {
                _entities.Remove(e.EntityId);
                _cachedEntities = null;

                OnDeactivateEntityInContext?.Invoke(e);
            }
        }

        private void OnEntityModified(Entity e, int index)
        {
            bool isFiltered = DoesEntityMatch(e);
            bool isWatchingEntity = _entities.ContainsKey(e.EntityId);

            if (!isWatchingEntity && isFiltered)
            {
                StartWatchingEntity(e, index);
            }
            else if (isWatchingEntity && !isFiltered)
            {
                StopWatchingEntity(e, index, causedByDestroy: false);
            }
        }

        /// <summary>
        /// Tries to get a unique entity, if none is available, returns null
        /// </summary>
        /// <returns></returns>
        public Entity? TryGetUniqueEntity()
        {
            if (_entities.Count == 1)
            {
                return _entities.First().Value;
            }
            else
            {
                return null;
            }
        }

        private void StartWatchingEntity(Entity e, int index)
        {
            // Add any watchers from now on.
            e.OnComponentAdded += OnComponentAddedForEntityInContext;
            e.OnComponentRemoved += OnComponentRemovedForEntityInContext;
            e.OnComponentModified += OnComponentModifiedForEntityInContext;

            e.OnMessage += OnMessageSentForEntityInContext;

            e.OnEntityActivated += OnEntityActivated;
            e.OnEntityDeactivated += OnEntityDeactivated;

            if (!e.IsDeactivated)
            {
                // Notify immediately of the new added component.
                OnComponentAddedForEntityInContext?.Invoke(e, index);

                _entities.Add(e.EntityId, e);
                _cachedEntities = null;
            }
        }

        private void StopWatchingEntity(Entity e, int index, bool causedByDestroy)
        {
            // Remove any watchers.
            e.OnComponentAdded -= OnComponentAddedForEntityInContext;
            e.OnComponentRemoved -= OnComponentRemovedForEntityInContext;
            e.OnComponentModified -= OnComponentModifiedForEntityInContext;

            e.OnMessage -= OnMessageSentForEntityInContext;

            e.OnEntityActivated -= OnEntityActivated;
            e.OnEntityDeactivated -= OnEntityDeactivated;

            if (!e.IsDeactivated)
            {
                // Notify immediately of the removed component.
                OnComponentRemovedForEntityInContext?.Invoke(e, index, causedByDestroy);
            }
            else
            {
                Debug.Assert(!_entities.ContainsKey(e.EntityId),
                    "Why is a deactivate entity is in the collection?");
            }

            _entities.Remove(e.EntityId);
            _cachedEntities = null;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            OnComponentAddedForEntityInContext = null;
            OnComponentModifiedForEntityInContext = null;
            OnComponentRemovedForEntityInContext = null;

            OnActivateEntityInContext = null;
            OnDeactivateEntityInContext = null;
            OnMessageSentForEntityInContext = null;

            _entities.Clear();
        }
    }
}