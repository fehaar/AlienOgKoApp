namespace Gosuman.EntitySystem
{
    /// <summary>
    /// This is the base class of all entities
    /// </summary>
    [Serializable]
    public abstract class Entity
    {
        public string Id { get; set; } = Ulid.NewUlid().ToString();

        private Dictionary<Type, IComponent>? _components;

        public void AddComponent(IComponent component)
        {
            if (_components == null)
            {
                _components = new Dictionary<Type, IComponent>();
            }
            _components[component.GetType()] = component;
        }

        public bool HasComponent<T>() where T : IComponent
        {
            if (_components == null)
            {
                return false;
            }
            return _components.ContainsKey(typeof(T));
        }

        public T GetComponent<T>() where T : IComponent
        {
            if (_components != null && _components.ContainsKey(typeof(T)))
            {
                return (T)_components[typeof(T)];
            }
            throw new Exception($"Component {typeof(T).Name} not found on entity {this}");
        }

        public T GetOrCreateComponent<T>() where T : IComponent
        {
            if (_components != null && _components.ContainsKey(typeof(T)))
            {
                return (T)_components[typeof(T)];
            }
            var component = Activator.CreateInstance<T>();
            AddComponent(component);
            return component;
        }

        public bool TryGetComponent<T>(out T? component) where T : IComponent
        {
            if (_components != null && _components.ContainsKey(typeof(T)))
            {
                component = (T)_components[typeof(T)];
                return true;
            }
            component = default;
            return false;
        }

        public void RemoveComponent(IComponent component)
        {
            if (_components != null)
            {
                _components.Remove(component.GetType());
            }
        }

        public void RemoveComponent<T>()
        {
            if (_components != null)
            {
                _components.Remove(typeof(T));
            }
        }

        public IEnumerable<IComponent> Components
        {
            get
            {
                if (_components == null)
                {
                    return Array.Empty<IComponent>();
                }
                return _components.Values;
            }
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
