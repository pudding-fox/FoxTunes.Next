using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class BassStreamPipelineFactory : StandardComponent, IBassStreamPipelineFactory
    {
        protected virtual void OnQueryingPipeline(QueryingPipelineEventArgs e)
        {
            if (this.QueryingPipeline == null)
            {
                return;
            }
            this.QueryingPipeline(this, e);
        }

        public event QueryingPipelineEventHandler QueryingPipeline;

        protected virtual void OnCreatingPipeline(CreatingPipelineEventArgs e)
        {
            if (this.CreatingPipeline == null)
            {
                return;
            }
            this.CreatingPipeline(this, e);
        }

        public event CreatingPipelineEventHandler CreatingPipeline;

        public IBassStreamPipelineQueryResult QueryPipeline()
        {
            var e = new QueryingPipelineEventArgs();
            this.OnQueryingPipeline(e);
            return new BassStreamPipelineQueryResult(e.InputCapabilities, e.OutputCapabilities, e.OutputRates, e.OutputChannels);
        }

        public IBassStreamPipeline CreatePipeline(BassOutputStream stream)
        {
            var pipeline = new BassStreamPipeline();
            this.CreatePipeline(pipeline, stream);
            try
            {
                pipeline.Connect(stream);
                if (Logger.IsDebugEnabled(this))
                {
                    Logger.Write(
                        this,
                        LogLevel.Debug,
                        "Connected pipeline: {0}",
                        string.Join(" => ", pipeline.All.Select(component => string.Format("\"{0}\"", component.GetType().Name)))
                    );
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to create pipeline: {0}", e.Message);
                pipeline.Dispose();
                throw;
            }
            return pipeline;
        }

        protected virtual void CreatePipeline(IBassStreamPipeline pipeline, BassOutputStream stream)
        {
            var e = new CreatingPipelineEventArgs(pipeline, this.QueryPipeline(), stream);
            this.OnCreatingPipeline(e);
            if (e.Input == null)
            {
                throw new NotImplementedException("Failed to locate a suitable pipeline input.");
            }
            if (e.Output == null)
            {
                throw new NotImplementedException("Failed to locate a suitable pipeline output.");
            }
            pipeline.InitializeComponent(e.Input, e.Components, e.Output);
        }
    }
}
