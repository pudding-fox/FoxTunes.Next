using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class BassOutputDataSource : StandardComponent, IOutputDataSource, IDisposable
    {
        public IBassStreamOutput Output { get; private set; }

        public IBassStreamPipelineManager PipelineManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PipelineManager = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineManager>();
            this.PipelineManager.Created += this.OnPipelineManagerCreated;
            this.PipelineManager.Destroyed += this.OnPipelineManagerDestroyed;
            base.InitializeComponent(core);
        }

        protected virtual void OnPipelineManagerCreated(object sender, EventArgs e)
        {
            this.PipelineManager.WithPipeline(pipeline => this.Output = pipeline.Output);
            this.OnCanGetDataChanged();
        }

        protected virtual void OnPipelineManagerDestroyed(object sender, EventArgs e)
        {
            this.Output = null;
            this.OnCanGetDataChanged();
        }

        #region IOutputDataSource

        public bool GetOutputFormat(out int rate, out int channels, out OutputStreamFormat format)
        {
            if (this.Output == null)
            {
                rate = 0;
                channels = 0;
                format = OutputStreamFormat.None;
                return false;
            }
            var flags = default(BassFlags);
            if (!this.Output.GetFormat(out rate, out channels, out flags))
            {
                rate = 0;
                channels = 0;
                format = OutputStreamFormat.None;
                return false;
            }
            if (flags.HasFlag(BassFlags.DSDRaw))
            {
                format = OutputStreamFormat.DSDRaw;
            }
            else if (flags.HasFlag(BassFlags.Float))
            {
                format = OutputStreamFormat.Float;
            }
            else
            {
                format = OutputStreamFormat.Short;
            }
            return true;
        }

        public bool GetOutputChannelMap(out IDictionary<int, OutputChannel> channels)
        {
            var _rate = default(int);
            var _channels = default(int);
            var _format = default(OutputStreamFormat);
            if (!this.GetOutputFormat(out _rate, out _channels, out _format))
            {
                channels = default(IDictionary<int, OutputChannel>);
                return false;
            }
            channels = BassChannelMap.GetChannelMap(_channels);
            return true;
        }

        public bool CanGetData
        {
            get
            {
                if (this.Output == null)
                {
                    return false;
                }
                return this.Output.CanGetData;
            }
        }

        protected virtual void OnCanGetDataChanged()
        {
            if (this.CanGetDataChanged == null)
            {
                return;
            }
            this.CanGetDataChanged(this, EventArgs.Empty);
        }

        public event EventHandler CanGetDataChanged;

        public bool GetDataFormat(out int rate, out int channels, out OutputStreamFormat format)
        {
            if (this.Output == null)
            {
                rate = 0;
                channels = 0;
                format = OutputStreamFormat.None;
                return false;
            }
            var flags = default(BassFlags);
            if (!this.Output.GetDataFormat(out rate, out channels, out flags))
            {
                rate = 0;
                channels = 0;
                format = OutputStreamFormat.None;
                return false;
            }
            if (flags.HasFlag(BassFlags.DSDRaw))
            {
                format = OutputStreamFormat.DSDRaw;
            }
            else if (flags.HasFlag(BassFlags.Float))
            {
                format = OutputStreamFormat.Float;
            }
            else
            {
                format = OutputStreamFormat.Short;
            }
            return true;
        }

        public bool GetDataChannelMap(out IDictionary<int, OutputChannel> channels)
        {
            var _rate = default(int);
            var _channels = default(int);
            var _format = default(OutputStreamFormat);
            if (!this.GetDataFormat(out _rate, out _channels, out _format))
            {
                channels = default(IDictionary<int, OutputChannel>);
                return false;
            }
            channels = BassChannelMap.GetChannelMap(_channels);
            return true;
        }

        public T[] GetBuffer<T>(TimeSpan duration) where T : struct
        {
            if (this.Output == null)
            {
                return null;
            }
            var length = Convert.ToInt32(
                Bass.ChannelSeconds2Bytes(this.Output.ChannelHandle, duration.TotalSeconds)
            );
            if (typeof(T) == typeof(short))
            {
                length /= sizeof(short);
            }
            else if (typeof(T) == typeof(float))
            {
                length /= sizeof(float);
            }
            else
            {
                throw new NotImplementedException();
            }
            return new T[length];
        }

        public int GetData(short[] buffer)
        {
            if (this.Output == null)
            {
                return 0;
            }
            return this.Output.GetData(buffer);
        }

        public int GetData(float[] buffer)
        {
            if (this.Output == null)
            {
                return 0;
            }
            return this.Output.GetData(buffer);
        }

        public float[] GetBuffer(int fftSize, bool individual = false)
        {
            var length = default(int);
            switch (fftSize)
            {
                case BassFFT.FFT256:
                    length = BassFFT.FFT256_SIZE;
                    break;
                case BassFFT.FFT512:
                    length = BassFFT.FFT512_SIZE;
                    break;
                case BassFFT.FFT1024:
                    length = BassFFT.FFT1024_SIZE;
                    break;
                case BassFFT.FFT2048:
                    length = BassFFT.FFT2048_SIZE;
                    break;
                case BassFFT.FFT4096:
                    length = BassFFT.FFT4096_SIZE;
                    break;
                case BassFFT.FFT8192:
                    length = BassFFT.FFT8192_SIZE;
                    break;
                case BassFFT.FFT16384:
                    length = BassFFT.FFT16384_SIZE;
                    break;
                case BassFFT.FFT32768:
                    length = BassFFT.FFT32768_SIZE;
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (individual)
            {
                var rate = default(int);
                var channels = default(int);
                var format = default(OutputStreamFormat);
                if (!this.GetDataFormat(out rate, out channels, out format))
                {
                    Logger.Write(this, LogLevel.Error, "Failed to determine channel count while creating interleaved FFT buffer.");
                    return null;
                }
                length *= channels;
            }
            return new float[length];
        }

        public int GetData(float[] buffer, int fftSize, bool individual = false)
        {
            if (this.Output == null)
            {
                return 0;
            }
            return this.Output.GetData(buffer, fftSize, individual);
        }

        public int GetData(float[] buffer, int fftSize, out TimeSpan duration, bool individual = false)
        {
            if (this.Output == null)
            {
                duration = default(TimeSpan);
                return 0;
            }
            return this.Output.GetData(buffer, fftSize, out duration, individual);
        }

        #endregion

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
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.PipelineManager != null)
            {
                this.PipelineManager.Created -= this.OnPipelineManagerCreated;
                this.PipelineManager.Destroyed -= this.OnPipelineManagerDestroyed;
            }
        }

        ~BassOutputDataSource()
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
