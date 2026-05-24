using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FoxTunes
{
    public interface IMoodBar : IBaseComponent, IDisposable
    {
        Process Process { get; }

        IEnumerable<MoodBarItem> MoodBarItems { get; }

        Task Create();

        void Update();

        void Cancel();

        void Prune();

        event EventHandler Updated;
    }
}
