using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamPipelineQueryResult
    {
        long InputCapabilities { get; }

        long OutputCapabilities { get; }

        IEnumerable<int> OutputRates { get; }

        int OutputChannels { get; }

        int GetNearestRate(int rate);
    }
}
