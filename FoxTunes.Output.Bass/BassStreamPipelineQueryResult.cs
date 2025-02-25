using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassStreamPipelineQueryResult : IBassStreamPipelineQueryResult
    {
        public BassStreamPipelineQueryResult(long inputCapabilities, long outputCapabilities, IEnumerable<int> outputRates, int outputChannels)
        {
            this.InputCapabilities = inputCapabilities;
            this.OutputCapabilities = outputCapabilities;
            this.OutputRates = outputRates;
            this.OutputChannels = outputChannels;
        }

        public long InputCapabilities { get; private set; }

        public long OutputCapabilities { get; private set; }

        public IEnumerable<int> OutputRates { get; private set; }

        public int OutputChannels { get; private set; }

        public int GetNearestRate(int rate)
        {
            //Find the closest supported rate.
            foreach (var supportedRate in this.OutputRates)
            {
                if (supportedRate >= rate)
                {
                    return supportedRate;
                }
            }
            //Ah. The minimum supported rate is not enough.
            return this.OutputRates.LastOrDefault();
        }
    }
}
