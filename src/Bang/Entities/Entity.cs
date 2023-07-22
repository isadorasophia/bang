using Bang.Components;
using Bang.StateMachines;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Bang.Entities
{
    /// <summary>
    /// An entity is a collection of components within the world.
    /// This supports hierarchy (parent, children) 
    /// </summary>
    public partial class Entity : IDisposable
    {
        /// <summary>
        /// This will be fired whenever a new component has been added.
        /// </summary>
        public event Action<Entity, int>? OnComponentAdded;

        /// <summary>
        /// This will be fired whenever a new component has been removed.
        /// This will send the entity, the component id that was just removed and
        /// whether this was caused by a destroy.
        /// </summary>
        public event Action<Entity, int, bool>? OnComponentRemoved;

        /// <summary>
        /// This will be fired whenever any component has been replaced.
        /// </summary>
        public event Action<Entity, int>? OnComponentModified;

        /// <summary>
        /// This will be fired when the entity gets destroyed.
        /// </summary>
        public event Action<int>? OnEntityDestroyed;

        /// <summary>
        /// This will be fired when the entity gets activated, so it gets filtered
        /// back in the context listeners.
        /// </summary>
        public event Action<Entity>? OnEntityActivated;

        /// <summary>
        /// This will be fired when the entity gets deactivated, so it is filtered out
        /// from its context listeners.
        /// </summary>
        public event Action<Entity>? OnEntityDeactivated;

        /// <summary>
        /// Notifies listeners when a particular component has been modified.
        /// </summary>
        private readonly Dictionary<int, Action<int, IComponent>?> _trackedComponentsModified = new();

        /// <summary>
        /// Entity unique identifier.
        /// </summary>
        public int EntityId => _id;

        /// <summary>
        /// Components lookup. Unique per world that the entity was created.
        /// </summary>
        private readonly World _world;

        private readonly ComponentsLookup _lookup;

        private readonly int _id;

        private bool _isDestroyed = false;
        private bool _isDeactivated = false;

        /// <summary>
        /// Returns whether this entity has been destroyed (and probably recicled) or not.
        /// </summary>
        public bool IsDestroyed => _isDestroyed;

        public bool IsDeactivated => _isDeactivated;
        public bool IsActive => !_isDeactivated && !_isDestroyed;

        /// <summary>
        /// Maybe we want to expand this into various reasons an entity was deactivated?
        /// For now, track whether it was deactivated due to the parent.
        /// </summary>
        private bool _wasDeactivatedFromParent = false;

        /// <summary>
        /// Keeps track of all the components that are currently present.
        /// </summary>
        // TODO: Investigate trade-off between using this and a hash set.
        private bool[] _availableComponents;

        // TODO: I guess this can be an array. Eventually.
        private IDictionary<int, IComponent> _components;

        /// <summary>
        /// This is used for editor and serialization.
        /// TODO: Optimize this. For now, this is okay since it's only used once the entity is serialized.
        /// </summary>
        public ImmutableArray<IComponent> Components => _components
            .Where(kv => _availableComponents[kv.Key]).Select(kv => kv.Value).ToImmutableArray();

        // TODO: Optimize this. For now, this is okay since it's only used once the entity is initialized.
        internal ImmutableArray<int> ComponentsIndices => _components
            .Where(kv => _availableComponents[kv.Key]).Select(kv => kv.Key).ToImmutableArray();

        internal Entity(World world, int id, IComponent[] components)
        {
            _id = id;

            _world = world;
            _lookup = world.ComponentsLookup;

            _availableComponents = new bool[_lookup.TotalIndices];
            
            InitializeComponents(components);
        }

        /// <summary>
        /// Set an entity so it belongs to the world.
        /// </summary>
        [MemberNotNull(nameof(_components))]
        internal void InitializeComponents(IComponent[] components)
        {
            _components = new Dictionary<int, IComponent>();

            // Subscribe to each of the components that are modifiable and 
            // register the component as available.
            foreach (IComponent c in components)
            {
                int key = _lookup.Id(c.GetType());
                
                (c as IModifiableComponent)?.Subscribe(() => OnComponentModified?.Invoke(this, key));
                (c as IStateMachineComponent)?.Initialize(_world, this);

                AddComponentInternal(c, key);
            }
            
            if (World.DIAGNOSTICS_MODE)
            {
                CheckForRequiredComponents();
            }
        }

        /// <summary>
        /// This will check whether the entity has all the required components when set to the world.
        /// </summary>
        private void CheckForRequiredComponents()
        {
            Dictionary<int, Type> components = _components.Where(kv => _availableComponents[kv.Key])
                .ToDictionary(kv => kv.Key, kv => kv.Value.GetType());
            
            foreach ((int id, Type t) in components)
            {
                RequiresAttribute? requires = t.GetCustomAttributes(typeof(RequiresAttribute), inherit: true)
                    .FirstOrDefault() as RequiresAttribute;
                
                if (requires is not null)
                {
                    foreach (Type requiredType in requires.Types)
                    {
                        int requiredId = _lookup.Id(requiredType);
                        
                        Debug.Assert(typeof(IComponent).IsAssignableFrom(requiredType), 
                            "Why is a component requiring a type that is not a component?");

                        Debug.Assert(components.ContainsKey(requiredId),
                            $"Missing {requiredType.Name} required by {t.Name} in entity declaration!");
                    }
                }
            }
        }

        /// <summary>
        /// Whether this entity has a component of type T.
        /// </summary>
        public bool HasComponent<T>() where T : IComponent => HasComponent(GetComponentIndex<T>());

        /// <summary>
        /// Whether this entity has a component of type <paramref name="t"/>.
        /// </summary>
        public bool HasComponent(Type t) => HasComponent(GetComponentIndex(t));

        /// <summary>
        /// Try to get a component of type T. If none, returns false and null.
        /// </summary>
        /// <typeparam name="T">Type that inherits <see cref="IComponent"/>.</typeparam>
        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : IComponent
        {
            int index = GetComponentIndex<T>();
            if (TryGetComponent(index, out IComponent? result))
            {
                component = (T)result;
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Try to get a component of type T. If none, returns null.
        /// </summary>
        /// <typeparam name="T">Type that inherits <see cref="IComponent"/>.</typeparam>
        public T? TryGetComponent<T>() where T : struct, IComponent
        {
            // Since the method below doesn't assume that T is a struct,
            // the return value won't be a null, but rather the value of the struct
            // zero-ed out.
            int index = GetComponentIndex<T>();
            return TryGetComponent(index, out IComponent? value) ? (T)value : null;
        }

        private bool TryGetComponent(int index, [NotNullWhen(true)] out IComponent? component)
        {
            if (HasComponent(index))
            {
                component = _components[index];
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Fetch a component of type T. If the entity does not have that component, this method will assert and fail.
        /// </summary>
        /// <typeparam name="T">Type that inherits <see cref="IComponent"/>.</typeparam>
        public T GetComponent<T>() where T : IComponent
        {
            int index = GetComponentIndex<T>();
            return GetComponent<T>(index);
        }

        /// <summary>
        /// Fetch a component of type T with <paramref name="index"/>. 
        /// If the entity does not have that component, this method will assert and fail.
        /// </summary>
        /// <typeparam name="T">Type that inherits <see cref="IComponent"/>.</typeparam>
        public T GetComponent<T>(int index) where T : IComponent
        {
             Debug.Assert(HasComponent(index), $"The entity doesn't have a component of type '{typeof(T).Name}', maybe you should 'TryGetComponent'?");
             return (T)_components[index];
        }

        /// <summary>
        /// Add an empty component only once to the entity.
        /// </summary>
        /// <returns>
        /// Whether a new component was added.
        /// </returns>
        public bool AddComponentOnce<T>() where T : IComponent, new()
        {
            if (_lookup is null || HasComponent<T>())
            {
                Debug.Assert(_lookup is not null, "Method not implemented for unitialized components.");
                return false;
            }

            T c = new();

            int index = GetComponentIndex<T>();
            AddComponent(c, index);

            return true;
        }

        /// <summary>
        /// Add a component <paramref name="c"/> of type <paramref name="t"/>.
        /// </summary>
        /// <param name="c">Component.</param>
        /// <param name="t">Type of the component.</param>
        public void AddComponent(IComponent c, Type t)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(t));
            AddComponent(c, GetComponentIndex(t));
        }

        /// <summary>
        /// Add component <paramref name="c"/> to this entity.
        /// </summary>
        /// <typeparam name="T">Type that inherits from <see cref="IComponent"/>.</typeparam>
        /// <param name="c">Component.</param>
        /// <returns>Entity (this).</returns>
        public Entity AddComponent<T>(T c) where T : IComponent
        {
            if (_lookup is null)
            {
                // World has not been initialized yet, so add with an arbitrary index for the component.
                _components.Add(_components.Count, c);
                return this;
            }

            int index = GetComponentIndex<T>();
            AddComponent(c, index);

            return this;
        }

        /// <summary>
        /// Removes component of type <paramref name="t"/>.
        /// Do nothing if <paramref name="t"/> is not owned by this entity.
        /// </summary>
        /// <param name="t">Type of the component.</param>
        public void RemoveComponent(Type t)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(t));
            RemoveComponent(GetComponentIndex(t));
        }

        /// <summary>
        /// Removes component of type <typeparamref name="T"/>.
        /// Do nothing if <typeparamref name="T"/> is not owned by this entity.
        /// </summary>
        public bool RemoveComponent<T>() where T : IComponent
        {
            int index = GetComponentIndex<T>();
            return RemoveComponent(index);
        }

        /// <summary>
        /// Replace componenent of type <paramref name="t"/> with <paramref name="c"/>.
        /// This asserts if the component does not exist or is not assignable from <paramref name="t"/>.
        /// Do nothing if the entity has been destroyed.
        /// </summary>
        /// <param name="c">Component.</param>
        /// <param name="t">Target component type.</param>
        /// <param name="forceReplace">Whether the component will be forcefully replaced.</param>
        public void ReplaceComponent(IComponent c, Type t, bool forceReplace = false)
        {
            Debug.Assert(typeof(IComponent).IsAssignableFrom(t));
            ReplaceComponent(c, GetComponentIndex(t), forceReplace);
        }

        /// <summary>
        /// Replace componenent of type <typeparamref name="T"/> with <paramref name="c"/>.
        /// This asserts if the component does not exist or is not assignable from <typeparamref name="T"/>.
        /// Do nothing if the entity has been destroyed.
        /// </summary>
        /// <param name="c">Component.</param>
        public void ReplaceComponent<T>(T c) where T : IComponent
        {
            int index = GetComponentIndex<T>();
            ReplaceComponent(c, index);
        }

        /// <summary>
        /// Add or replace component of type <typeparamref name="T"/> with <paramref name="c"/>.
        /// Do nothing if the entity has been destroyed.
        /// </summary>
        /// <typeparam name="T">Target type that <paramref name="c"/> is assignable.</typeparam>
        /// <param name="c">Component.</param>
        public void AddOrReplaceComponent<T>(T c) where T : IComponent
        {
            int index = GetComponentIndex<T>();
            AddOrReplaceComponent(c, index);
        }

        /// <summary>
        /// Add or replace component of type <paramref name="t"/> with <paramref name="c"/>.
        /// Do nothing if the entity has been destroyed.
        /// </summary>
        /// <param name="c">Component.</param>
        /// <param name="t">Target type that <paramref name="c"/> is assignable.</param>
        public void AddOrReplaceComponent(IComponent c, Type t)
        {
            int index = GetComponentIndex(t);
            AddOrReplaceComponent(c, index);
        }

        /// <summary>
        /// Add or replace component of type <typeparamref name="T"/> with <paramref name="c"/>.
        /// Do nothing if the entity has been destroyed.
        /// </summary>
        /// <typeparam name="T">Target type that <paramref name="c"/> is assignable.</typeparam>
        /// <param name="c">Component.</param>
        /// <param name="index">Identifier ot the component type.</param>
        public void AddOrReplaceComponent<T>(T c, int index) where T : IComponent
        {
            if (HasComponent(index))
            {
                ReplaceComponent(c, index);
            }
            else
            {
                AddComponent(c, index);
            }
        }

        /// <summary>
        /// Checks whether an entity has a component.
        /// </summary>
        public bool HasComponent(int index) => index < _availableComponents.Length && _availableComponents[index];
        
        /// <summary>
        /// Checks whether an entity has a data attached to -- component or message.
        /// </summary>
        internal bool HasComponentOrMessage(int index) => HasComponent(index) || HasMessage(index);

        private int GetComponentIndex<T>() => GetComponentIndex(typeof(T));

        private int GetComponentIndex(Type t)
        {
            Debug.Assert(_lookup is not null, "Why are we modifying an entity without setting it to the world?");
            return _lookup.Id(t);
        }

        /// <summary>
        /// This simply adds a component to our lookup table. This won't do anything fancy other than
        /// booking it, if it happens to exceed the components length.
        /// </summary>
        private void AddComponentInternal<T>(T c, int index) where T : IComponent
        {
            if (_availableComponents.Length <= index)
            {
                // We might hit the scenario when a component that was not previously taken into account is added.
                // This may happen for components not tracked by a generator, usually when there is a project that
                // adds extra components. This shouldn't happen in the shipped engine, for example.

                // Double the lookup size.
                bool[] newLookup = new bool[_availableComponents.Length * 2];
                Array.Copy(_availableComponents, newLookup, _availableComponents.Length);

                _availableComponents = newLookup;
            }

            _components[index] = c;
            _availableComponents[index] = true;
        }

        /// <summary>
        /// Add a component to the entity.
        /// Returns true if the element existed and was replaced.
        /// </summary>
        public bool AddComponent<T>(T c, int index) where T : IComponent
        {
            if (_isDestroyed)
            {
                // TODO: Assert? The entity has been destroyed, so it's a no-op.
                return false;
            }

            if (HasComponent(index))
            {
                Debug.Fail("Why are we adding a component to an entity that already has one? Call ReplaceComponent(c) instead.");
                return false;
            }

            AddComponentInternal(c, index);
            NotifyAndSubscribeOnComponentAdded(index, c);

            return true;
        }

        /// <summary>
        /// Replace a component from the entity.
        /// Returns true if the element existed and was replaced.
        /// </summary>
        public bool ReplaceComponent<T>(T c, int index, bool forceReplace = false) where T : IComponent
        {
            if (_isDestroyed)
            {
                // TODO: Assert? The entity has been destroyed, so it's a no-op.
                return false;
            }

            if (!HasComponent(index))
            {
                Debug.Fail("Why are we replacing a component to an entity that does not have one? Call AddComponent(c) instead.");
                return false;
            }

            if (!forceReplace && c.Equals(_components[index]))
            {
                // Don't bother replacing if both components have the same value.
                return false;
            }

            // If this is a modifiable component, unsubscribe from it before actually replacing it.
            (_components[index] as IModifiableComponent)?.Unsubscribe(() => OnComponentModified?.Invoke(this, index));

            _components[index] = c;

            if (_parent is not null && c is IParentRelativeComponent relative && !relative.HasParent &&
                _parent.TryGetComponent(index, out IComponent? parentComponent))
            {
                // The parent is not provided to the new component. Make sure we set this again with the correct parent
                // before replacing that entity and notifying providers.
                OnParentModified(index, parentComponent);
                return true;
            }

            NotifyOnComponentReplaced(index, c);
            return true;
        }

        /// <summary>
        /// Remove a component from the entity.
        /// Returns true if the element existed and was removed.
        /// </summary>
        public bool RemoveComponent(int index)
        {
            if (!HasComponent(index))
            {
                // Redundant operation, just do a no-operation.
                return false;
            }

            (_components[index] as IModifiableComponent)?.Unsubscribe(() => OnComponentModified?.Invoke(this, index));
            
            _components[index] = default!;
            _availableComponents[index] = false;

            // Check whether this removal will cause the entity to be destroyed.
            // If no components are left, there is no use for this to exist.
            bool destroyAfterRemove = _components.Count == 0 && !IsDestroyed;

            OnComponentRemoved?.Invoke(this, index, destroyAfterRemove /* causedByDestroy */);
            _parent?.UntrackComponent(index, OnParentModified);

            if (destroyAfterRemove)
            {
                Destroy();
            }

            return true;
        }

        /// <summary>
        /// When adding a component:
        ///   1. If this is a modifiable component, we must subscribe to the new component.
        ///   2. If this is a state machine component, start it up.
        ///   3. Notify subscribers that the component has been added.
        ///   4. If this is a component that relies on the parent, make sure we are
        ///      tracking the parent changes.
        /// </summary>
        private void NotifyAndSubscribeOnComponentAdded(int index, IComponent c)
        {
            Debug.Assert(_world is not null);
            Debug.Assert(_lookup is not null);

            (c as IModifiableComponent)?.Subscribe(() => OnComponentModified?.Invoke(this, index));
            (c as IStateMachineComponent)?.Initialize(_world, this);

            OnComponentAdded?.Invoke(this, index);

            if (_lookup.IsRelative(index))
            {
                _parent?.TrackComponent(index, OnParentModified);
            }
        }

        /// <summary>
        /// When changing a component:
        ///   1. If this is a modifiable component, we have replaced the component with a new object.
        ///      Make sure we subcribe to the new component.
        ///   2. If this is a state machine component, start it up.
        ///   3. Notify subscribers that the component has been modified.
        ///   4. Notify any children about the value change.
        /// </summary>
        private void NotifyOnComponentReplaced(int index, IComponent c)
        {
            Debug.Assert(_world is not null);
            Debug.Assert(_lookup is not null);

            // First, subscribe to the newly replaced component.
            (c as IModifiableComponent)?.Subscribe(() => OnComponentModified?.Invoke(this, index));
            (c as IStateMachineComponent)?.Initialize(_world, this);

            // Now, notify all contexts that are observing this change.
            OnComponentModified?.Invoke(this, index);

            // Finally, notify any children who is listening to notifications.
            if (_trackedComponentsModified.TryGetValue(index, out Action<int, IComponent>? notification))
            {
                notification?.Invoke(index, c);
            }
        }

        /// <summary>
        /// Destroy the entity from the world.
        /// This will notify all components that it will be removed from the entity.
        /// At the end of the update of the frame, it will wipe this entity from the world.
        /// However, if someone still holds reference to an <see cref="Entity"/> (they shouldn't),
        /// they might see a zombie entity after this.
        /// </summary>
        public void Destroy()
        {
            foreach (int index in _components.Keys)
            {
                NotifyRemovalOnDestroy(index);
            }

            _isDestroyed = true;
            
            OnEntityDestroyed?.Invoke(EntityId);
        }

        /// <summary>
        /// Replace all the components of the entity. This is useful when you want to reuse
        /// the same entity id with new components.
        /// </summary>
        /// <param name="components">
        /// Components that will be placed in this entity.
        /// </param>
        /// <param name="children">
        /// Children in the world that will now have this entity as a parent.
        /// </param>
        /// <param name="wipe">
        /// Whether we want to wipe all trace of the current entity, including *destroying its children*.
        /// </param>
        public void Replace(IComponent[] components, List<(int, string)> children, bool wipe)
        {
            HashSet<int> replacedComponents = new();
            foreach (IComponent c in components)
            {
                int index = GetComponentIndex(c.GetType());
                replacedComponents.Add(index);

                if (HasComponent(index))
                {
                    ReplaceComponent(c, index, forceReplace: true);
                }
                else
                {
                    AddComponent(c, index);
                }
            }

            if (wipe)
            {
                ICollection<int> keys = _components.Keys;
                foreach (int index in keys)
                {
                    if (replacedComponents.Contains(index))
                    {
                        continue;
                    }

                    // TODO: Cache and optimize components that must be kept during r?
                    // As of today, a replace should happen so now and then that I will keep it like that for now.
                    if (HasComponent(index) && _components[index] is IComponent c &&
                        Attribute.IsDefined(c.GetType(), typeof(KeepOnReplaceAttribute)))
                    {
                        continue;
                    }

                    RemoveComponent(index);
                }
            }

            if (wipe && _children is not null)
            {
                int[] previousChildren = _children.Keys.ToArray();
                foreach (int c in previousChildren)
                {
                    // Crush and destroy the child dreams.
                    RemoveChild(c);

                    Entity e = _world.GetEntity(c);
                    e.Destroy();
                }
            }

            foreach ((int id, string name) in children)
            {
                AddChild(id, name);
            }
        }

        /// <summary>
        /// Entity has been destroyed due to <paramref name="_"/>.
        /// </summary>
        /// <param name="_">Parent id.</param>
        internal void Destroy(int _)
        {
            this.Destroy();
        }

        /// <summary>
        /// Dispose the entity.
        /// This will unparent and remove all components.
        /// It also removes subscription from all their contexts or entities.
        /// </summary>
        public void Dispose()
        {
            Unparent();
            
            foreach (var (index, _) in _components)
            {
                RemoveComponent(index);
            }

            OnComponentAdded = null;
            OnComponentModified = null;
            OnComponentRemoved = null;

            OnEntityDestroyed = null;
            OnEntityActivated = null;
            OnEntityDeactivated = null;

            OnMessage = null;
            _trackedComponentsModified.Clear();

            GC.SuppressFinalize(this);
        }

        private void ActivateFromParent(Entity _)
        {
            if (!_wasDeactivatedFromParent)
            {
                return;
            }

            Activate();
        }

        public void Activate()
        {
            if (!_isDeactivated)
            {
                // Already active.
                return;
            }

            _isDeactivated = false;
            _wasDeactivatedFromParent = false;

            _world.ActivateEntity(EntityId);

            OnEntityActivated?.Invoke(this);
        }

        private void DeactivateFromParent(Entity _)
        {
            if (_isDeactivated)
            {
                return;
            }

            _wasDeactivatedFromParent = true;
            Deactivate();
        }

        public void Deactivate()
        {
            if (_isDeactivated)
            {
                // Already deactivated.
                return;
            }

            _isDeactivated = true;
            _world.DeactivateEntity(EntityId);

            OnEntityDeactivated?.Invoke(this);
        }

        /// <summary>
        /// Notify that a component will be removed on the end of the frame due to a <see cref="Destroy(int)"/>.
        /// </summary>
        private bool NotifyRemovalOnDestroy(int index)
        {
            if (_isDestroyed)
            {
                // Entity was already destroyed, so we already notified any listeners.
                return false;
            }

            if (!HasComponent(index))
            {
                // Redundant operation, just do a no-operation.
                return false;
            }

            // Right now, I can't think of any other notifications that need to be notified as soon as 
            // the entity gets destroyed.
            // The rest of cleanup should be dealt with in the actual Dispose(), called by World at the
            // end of the frame.
            OnComponentRemoved?.Invoke(this, index, true /* causedByDestroyed */);

            return true;
        }

    }
}
