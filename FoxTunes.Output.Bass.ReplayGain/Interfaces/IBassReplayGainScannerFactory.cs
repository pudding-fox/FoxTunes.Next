using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public interface IBassReplayGainScannerFactory : IStandardComponent
    {
        IBassReplayGainScanner CreateScanner(IEnumerable<ScannerItem> scannerItems);
    }
}
