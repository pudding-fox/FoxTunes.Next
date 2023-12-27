using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FoxTunes
{
    public interface IBassReplayGainScanner : IBaseComponent, IDisposable
    {
        Process Process { get; }

        IEnumerable<ScannerItem> ScannerItems { get; }

        void Scan();

        void Update();

        void Cancel();
    }
}
