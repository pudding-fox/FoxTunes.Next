using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class ComponentRegistry : IComponentRegistry
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        private ComponentRegistry()
        {
            this.Clear();
        }

        private IDictionary<Type, IBaseComponent> ComponentsByType { get; set; }

        private IDictionary<Type, IList<IBaseComponent>> ComponentsByInterface { get; set; }

        private IDictionary<string, IList<IBaseComponent>> ComponentsBySlot { get; set; }

        public void AddComponents(params IBaseComponent[] components)
        {
            this.AddComponents(components.AsEnumerable());
        }

        public void AddComponents(IEnumerable<IBaseComponent> components)
        {
            foreach (var component in components)
            {
                if (component == null)
                {
                    continue;
                }
                this.AddComponent(component);
            }
        }

        public void AddComponent(IBaseComponent component)
        {
            var componentType = component.GetType();
            if (!this.ComponentsByType.TryAdd(componentType, component))
            {
                Logger.Write(typeof(ComponentRegistry), LogLevel.Warn, "Cannot register component type \"{0}\", it was already registered.", componentType.FullName);
            }
            foreach (var componentInterface in componentType.GetInterfaces())
            {
                this.ComponentsByInterface.GetOrAdd(componentInterface, () => new List<IBaseComponent>()).Add(component);
            }
            var attribute = componentType.GetCustomAttribute<ComponentAttribute>();
            if (attribute == null || string.IsNullOrEmpty(attribute.Slot) || string.Equals(attribute.Slot, ComponentSlots.None, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            this.ComponentsBySlot.GetOrAdd(attribute.Slot, () => new List<IBaseComponent>()).Add(component);
        }

        public IBaseComponent GetComponent(Type type)
        {
            return this.GetComponents(type).FirstOrDefault();
        }

        public IBaseComponent GetComponent(string slot)
        {
            return this.GetComponents(slot).FirstOrDefault();
        }

        public IEnumerable<IBaseComponent> GetComponents(Type type)
        {
            if (type.IsClass)
            {
                var component = default(IBaseComponent);
                if (this.ComponentsByType.TryGetValue(type, out component))
                {
                    return new[] { component };
                }
            }
            if (type.IsInterface)
            {
                var components = default(IList<IBaseComponent>);
                if (this.ComponentsByInterface.TryGetValue(type, out components))
                {
                    return components;
                }
            }
            return Enumerable.Empty<IBaseComponent>();
        }

        public IEnumerable<IBaseComponent> GetComponents(string slot)
        {
            var components = default(IList<IBaseComponent>);
            if (this.ComponentsBySlot.TryGetValue(slot, out components))
            {
                return components;
            }
            return Enumerable.Empty<IBaseComponent>();
        }

        public T GetComponent<T>() where T : IBaseComponent
        {
            return this.GetComponents<T>().FirstOrDefault();
        }

        public IEnumerable<T> GetComponents<T>()
        {
            return this.GetComponents(typeof(T)).OfType<T>();
        }

        public void RemoveComponent(IBaseComponent component)
        {
            var components = default(IList<IBaseComponent>);
            var componentType = component.GetType();
            this.ComponentsByType.Remove(componentType);
            foreach (var componentInterface in componentType.GetInterfaces())
            {
                if (this.ComponentsByInterface.TryGetValue(componentInterface, out components))
                {
                    components.Remove(component);
                }
            }
            var attribute = componentType.GetCustomAttribute<ComponentAttribute>();
            if (attribute == null || string.IsNullOrEmpty(attribute.Slot) || string.Equals(attribute.Slot, ComponentSlots.None, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (this.ComponentsBySlot.TryGetValue(attribute.Slot, out components))
            {
                components.Remove(component);
            }
        }

        public void ForEach(Action<IBaseComponent> action)
        {
            foreach (var pair in this.ComponentsByType)
            {
                action(pair.Value);
            }
        }

        public void ForEach<T>(Action<T> action)
        {
            foreach (var component in this.GetComponents<T>())
            {
                action(component);
            }
        }

        public bool IsDefault(IBaseComponent component)
        {
            var type = component.GetType();
            var attribute = type.GetCustomAttribute<ComponentPreferenceAttribute>();
            if (attribute != null && attribute.IsDefault)
            {
                return true;
            }
            return false;
        }

        public void Clear()
        {
            this.ComponentsByType = new Dictionary<Type, IBaseComponent>();
            this.ComponentsByInterface = new Dictionary<Type, IList<IBaseComponent>>();
            this.ComponentsBySlot = new Dictionary<string, IList<IBaseComponent>>(StringComparer.OrdinalIgnoreCase);
        }

        public static readonly IComponentRegistry Instance = new ComponentRegistry();
    }
}
