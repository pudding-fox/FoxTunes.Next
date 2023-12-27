using System;
namespace FoxTunes.Interfaces
{
    public interface IScriptingContext : IDisposable
    {
        void SetValue(string name, object value);

        object GetValue(string name);

        object Run(string script);
    }
}
