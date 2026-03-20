using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IAIContext : IDisposable
    {
        Task<IEnumerable<string>> Chat(string prompt);
    }
}
