using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IComponentRegistry
    {
        void AddComponents(params IBaseComponent[] components);

        void AddComponents(IEnumerable<IBaseComponent> components);

        IBaseComponent GetComponent(Type type);

        IBaseComponent GetComponent(string slot);

        IEnumerable<IBaseComponent> GetComponents(Type type);

        IEnumerable<IBaseComponent> GetComponents(string slot);

        T GetComponent<T>() where T : IBaseComponent;

        IEnumerable<T> GetComponents<T>();

        void RemoveComponent(IBaseComponent component);

        void ForEach(Action<IBaseComponent> action);

        void ForEach<T>(Action<T> action);

        bool IsDefault(IBaseComponent component);

        void Clear();
    }
}
