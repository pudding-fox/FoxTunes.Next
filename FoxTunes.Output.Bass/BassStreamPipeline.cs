using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassStreamPipeline : BaseComponent, IBassStreamPipeline
    {
        public static readonly object SyncRoot = new object();

        public BassStreamPipeline()
        {
            this.Components = Enumerable.Empty<IBassStreamComponent>();
        }

        public IBassStreamInput Input { get; private set; }

        public IEnumerable<IBassStreamComponent> Components { get; private set; }

        public IBassStreamOutput Output { get; private set; }

        public IEnumerable<IBassStreamComponent> All
        {
            get
            {
                if (this.Input != null)
                {
                    yield return this.Input;
                }
                foreach (var component in this.Components)
                {
                    yield return component;
                }
                if (this.Output != null)
                {
                    yield return this.Output;
                }
            }
        }

        public IEnumerable<IBassStreamControllable> Controllable
        {
            get
            {
                return this.All.OfType<IBassStreamControllable>();
            }
        }

        public bool IsStarting
        {
            set
            {
                this.All.ForEach(component => component.IsStarting = value);
            }
        }

        public bool IsStopping
        {
            set
            {
                this.All.ForEach(component => component.IsStopping = value);
            }
        }

        public int BufferLength
        {
            get
            {
                return this.All.Sum(component => component.BufferLength);
            }
        }

        public void InitializeComponent(IBassStreamInput input, IEnumerable<IBassStreamComponent> components, IBassStreamOutput output)
        {
            this.Input = input;
            this.Components = components;
            this.Output = output;
        }

        public void Connect(BassOutputStream stream)
        {
            var previous = (IBassStreamComponent)this.Input;
            Logger.Write(this, LogLevel.Debug, "Connecting pipeline input: \"{0}\"", this.Input.GetType().Name);
            this.Input.Connect(stream);
            foreach (var component in this.Components)
            {
                Logger.Write(this, LogLevel.Debug, "Connecting pipeline component: \"{0}\"", component.GetType().Name);
                component.Connect(previous);
                previous = component;
            }
            Logger.Write(this, LogLevel.Debug, "Connecting pipeline output: \"{0}\"", this.Output.GetType().Name);
            this.Output.Connect(previous);
        }

        public void ClearBuffer()
        {
            Logger.Write(this, LogLevel.Debug, "Clearing pipeline buffer.");
            this.All.ForEach(component => component.ClearBuffer());
        }

        public void PreviewPlay()
        {
            this.Controllable.ForEach(component => component.PreviewPlay());
        }

        public void PreviewPause()
        {
            this.Controllable.Reverse().ForEach(component => component.PreviewPause());
        }

        public void PreviewResume()
        {
            this.Controllable.ForEach(component => component.PreviewResume());
        }

        public void PreviewStop()
        {
            this.Controllable.Reverse().ForEach(component => component.PreviewStop());
        }

        public void Play()
        {
            this.PreviewPlay();
            this.Controllable.ForEach(component => component.Play());
        }

        public void Pause()
        {
            this.PreviewPause();
            this.Controllable.Reverse().ForEach(component => component.Pause());
        }

        public void Resume()
        {
            this.PreviewResume();
            this.Controllable.ForEach(component => component.Resume());
        }

        public void Stop()
        {
            this.PreviewStop();
            this.Controllable.Reverse().ForEach(component => component.Stop());
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.All.ForEach(component =>
            {
                try
                {
                    component.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Error, "Pipeline component \"{0}\" could not be disposed: {1}", component.GetType().Name, e.Message);
                }
            });
            this.IsDisposed = true;
        }

        ~BassStreamPipeline()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
