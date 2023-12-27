using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamPipelineFactory : IStandardComponent
    {
        event QueryingPipelineEventHandler QueryingPipeline;

        IBassStreamPipelineQueryResult QueryPipeline();

        event CreatingPipelineEventHandler CreatingPipeline;

        IBassStreamPipeline CreatePipeline(BassOutputStream stream);
    }

    public delegate void QueryingPipelineEventHandler(object sender, QueryingPipelineEventArgs e);

    public class QueryingPipelineEventArgs : EventArgs
    {
        public long InputCapabilities { get; set; }

        public long OutputCapabilities { get; set; }

        public IEnumerable<int> OutputRates { get; set; }

        public int OutputChannels { get; set; }
    }

    public delegate void CreatingPipelineEventHandler(object sender, CreatingPipelineEventArgs e);

    public class CreatingPipelineEventArgs : EventArgs
    {
        private CreatingPipelineEventArgs()
        {
            this.Components = new List<IBassStreamComponent>();
        }

        public CreatingPipelineEventArgs(IBassStreamPipeline pipeline, IBassStreamPipelineQueryResult query, BassOutputStream stream) : this()
        {
            this.Pipeline = pipeline;
            this.Query = query;
            this.Stream = stream;
        }

        public IBassStreamPipeline Pipeline { get; private set; }

        public IBassStreamPipelineQueryResult Query { get; private set; }

        public BassOutputStream Stream { get; private set; }

        public IBassStreamInput Input { get; set; }

        public ICollection<IBassStreamComponent> Components { get; private set; }

        public IBassStreamOutput Output { get; set; }
    }
}
