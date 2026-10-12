using Bang.Entities;
using Bang.Util;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Bang.Contexts
{
    /// <summary>
    /// A context may have a collection of watchers.
    /// </summary>
    internal class ComponentWatcher
    {
        public readonly World World;

        internal readonly int Id;

        private readonly int _targetComponent;

        private readonly object _lock = new();

        /// <summary>
        /// Tracks the total of entities to notify.
        /// This will make sure that, even if the same entity has an operation multiple times,
        /// it will only be passed on once per update.
        /// Maps:
        ///  [Notification kind -> [Entity id, Entity]]
        /// </summary>
        private readonly Dictionary<WatcherNotificationKind, Dictionary<int, Entity>> _entitiesToNotify = [];

        // [Perf]
        private readonly Action<Entity, int> _onEntityComponentAddedCache;
        private readonly Action<Entity, int, bool> _onEntityComponentRemovedCache;
        private readonly Action<Entity, int> _onEntityComponentReplacedCache;
        private readonly Action<Entity> _onEntityActivatedCache;
        private readonly Action<Entity> _onEntityDeactivatedCache;

        /// <summary>
        /// Get the entities that will be notified.
        /// This will immediately clear the notification list.
        /// </summary>
        public Dictionary<WatcherNotificationKind, ImmutableArray<Entity>> PopNotifications()
        {
            lock (_lock)
            {
                if (_entitiesToNotify.Count != 0)
                {
                    // We will only filter entities that have not been destroyed or are being passed over to a 
                    // remove watch system.
                    Dictionary<WatcherNotificationKind, ImmutableArray<Entity>> result = [];
                    foreach ((WatcherNotificationKind kind, Dictionary<int, Entity> affectedEntities) in _entitiesToNotify)
                    {
                        var builder = ImmutableArray.CreateBuilder<Entity>();
                        foreach ((int entityId, Entity e) in affectedEntities)
                        {
                            if (e.IsActive || kind == WatcherNotificationKind.Removed || kind == WatcherNotificationKind.Disabled)
                            {
                                builder.Add(e);
                            }
                        }

                        if (affectedEntities.Count == 0)
                        {
                            continue;
                        }

                        result[kind] = builder.ToImmutable();
                    }

                    _entitiesToNotify.Clear();
                    return result;
                }

                Debug.Fail($"Why are we getting the entities for watcher {Id} without any notification?");
                return [];
            }
        }

        private void QueueEntityNotification(WatcherNotificationKind kind, Entity entity)
        {
            lock (_lock)
            {
                if (!_entitiesToNotify.TryGetValue(kind, out Dictionary<int, Entity>? affectedEntities))
                {
                    affectedEntities = [];
                    _entitiesToNotify.Add(kind, affectedEntities);

                    World.QueueWatcherNotification(Id);
                }

                affectedEntities[entity.EntityId] = entity;
            }
        }

        /// <summary>
        /// A watcher will target a single component.
        /// </summary>
        internal ComponentWatcher(World world, int contextId, Type targetComponent)
        {
            World = world;

            _targetComponent = world.ComponentsLookup.Id(targetComponent);
            Id = HashCode.Combine(contextId, _targetComponent);

            // Initialize and cache delegators for performance improvement.
            _onEntityComponentAddedCache = OnEntityComponentAdded;
            _onEntityComponentRemovedCache = OnEntityComponentRemoved;
            _onEntityComponentReplacedCache = OnEntityComponentReplaced;
            _onEntityActivatedCache = OnEntityActivated;
            _onEntityDeactivatedCache = OnEntityDeactivated;
        }

        internal void SubscribeToContext(Context context)
        {
            context.OnComponentAddedForEntityInContext += _onEntityComponentAddedCache;
            context.OnComponentRemovedForEntityInContext += _onEntityComponentRemovedCache;
            context.OnComponentModifiedForEntityInContext += _onEntityComponentReplacedCache;

            context.OnActivateEntityInContext += _onEntityActivatedCache;
            context.OnDeactivateEntityInContext += _onEntityDeactivatedCache;
        }

        private void OnEntityComponentAdded(Entity e, int index)
        {
            if (index != _targetComponent)
            {
                return;
            }

            if (!e.IsActive)
            {
                return;
            }

            QueueEntityNotification(WatcherNotificationKind.Added, e);
        }

        private void OnEntityComponentRemoved(Entity e, int index, bool causedByDestroy)
        {
            if (index != _targetComponent)
            {
                return;
            }

            if (e.IsDestroyed)
            {
                // entity has already been notified prior to this call.
                return;
            }

            if (!e.IsActive)
            {
                return;
            }

            if (_entitiesToNotify.TryGetValue(WatcherNotificationKind.Added, out var notificationOnAdded) &&
                notificationOnAdded.ContainsKey(e.EntityId))
            {
                // This was previously added. But now it's removed! So let's clean up this list.
                // We do this here because the order matters. If it was removed then added, we want to keep both.
                notificationOnAdded.Remove(e.EntityId);
            }

            QueueEntityNotification(WatcherNotificationKind.Removed, e);
        }

        private void OnEntityComponentReplaced(Entity e, int index)
        {
            if (index != _targetComponent)
            {
                return;
            }

            if (!e.IsActive)
            {
                return;
            }

            QueueEntityNotification(WatcherNotificationKind.Modified, e);
        }

        private void OnEntityActivated(Entity e)
        {
            QueueEntityNotification(WatcherNotificationKind.Enabled, e);
        }

        private void OnEntityDeactivated(Entity e)
        {
            if (_entitiesToNotify.TryGetValue(WatcherNotificationKind.Added, out var notificationOnAdded) &&
                notificationOnAdded.ContainsKey(e.EntityId))
            {
                // This entity was literally just added this frame. For such scenario, don't trigger Added *or* Deactivated.
                // It was born into anonymity. Leave it that way.
                notificationOnAdded.Remove(e.EntityId);
                return;
            }

            QueueEntityNotification(WatcherNotificationKind.Disabled, e);
        }

        internal void OnFinish()
        {
            lock (_lock)
            {
                _entitiesToNotify.Clear();
            }
        }
    }
}