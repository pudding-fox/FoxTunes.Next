using System;
using System.Windows.Input;

namespace FoxTunes.Interfaces
{
    public interface IKeyBindingsBehaviour : IStandardBehaviour, IDisposable
    {
        bool Add(string id, string keys, ICommand command);

        bool Remove(string id);
    }
}
