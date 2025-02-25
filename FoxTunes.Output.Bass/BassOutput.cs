using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    [Component("E0318CB1-57A0-4DC3-AA8D-F6E100F86190", ComponentSlots.Output)]
    public class BassOutput : Output, IBassOutput
    {
        const int START_STOP_TIMEOUT = 10000;

        const int START_ATTEMPTS = 5;

        const int START_ATTEMPT_INTERVAL = 400;

        public BassOutput()
        {
            this.Semaphore = new SemaphoreSlim(1, 1);
        }

        public SemaphoreSlim Semaphore { get; private set; }

        public override string Name
        {
            get
            {
                return "BASS";
            }
        }

        public override string Description
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append(this.Name);
                builder.Append(string.Format(" {0}", Bass.Version));
                var cpu = Bass.CPUUsage;
                if (cpu > 0)
                {
                    builder.Append(string.Format(" CPU {0:0.00}%", cpu));
                }
                this.PipelineManager.WithPipeline(pipeline =>
                {
                    if (pipeline != null)
                    {
                        builder.AppendLine();
                        builder.AppendLine(string.Format("{0}: {1}ms", Strings.BassOutput_Latency, pipeline.BufferLength));
                        builder.AppendLine(string.Format("{0}: {1}", Strings.BassOutput_Input, pipeline.Input.Description));
                        foreach (var component in pipeline.Components)
                        {
                            if (!component.IsActive)
                            {
                                continue;
                            }
                            builder.AppendLine(string.Format("{0}: {1}", Strings.BassOutput_Component, component.Description));
                        }
                        builder.Append(string.Format("{0}: {1}", Strings.BassOutput_Output, pipeline.Output.Description));
                    }
                });
                return builder.ToString();
            }
        }

        public ICore Core { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IBassLoader Loader { get; private set; }

        public IBassStreamFactory StreamFactory { get; private set; }

        public IBassStreamPipelineManager PipelineManager { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        private int _Rate { get; set; }

        public int Rate
        {
            get
            {
                return this._Rate;
            }
            set
            {
                this._Rate = value;
                Logger.Write(this, LogLevel.Debug, "Rate = {0}", this.Rate);
                var task = this.Shutdown();
            }
        }

        private bool _EnforceRate { get; set; }

        public bool EnforceRate
        {
            get
            {
                return this._EnforceRate;
            }
            set
            {
                this._EnforceRate = value;
                Logger.Write(this, LogLevel.Debug, "Enforce Rate = {0}", this.EnforceRate);
                var task = this.Shutdown();
            }
        }

        private bool _Float { get; set; }

        public bool Float
        {
            get
            {
                return this._Float;
            }
            set
            {
                this._Float = value;
                Logger.Write(this, LogLevel.Debug, "Float = {0}", this.Float);
                var task = this.Shutdown();
            }
        }

        private int _UpdatePeriod { get; set; }

        public int UpdatePeriod
        {
            get
            {
                return this._UpdatePeriod;
            }
            set
            {
                this._UpdatePeriod = value;
                Logger.Write(this, LogLevel.Debug, "UpdatePeriod = {0}", this.UpdatePeriod);
                var task = this.Shutdown();
            }
        }

        private int _UpdateThreads { get; set; }

        public int UpdateThreads
        {
            get
            {
                return this._UpdateThreads;
            }
            set
            {
                this._UpdateThreads = value;
                Logger.Write(this, LogLevel.Debug, "UpdateThreads = {0}", this.UpdateThreads);
                var task = this.Shutdown();
            }
        }

        private int _BufferLength { get; set; }

        public int BufferLength
        {
            get
            {
                return this._BufferLength;
            }
            set
            {
                this._BufferLength = value;
                Logger.Write(this, LogLevel.Debug, "BufferLength = {0}", this.BufferLength);
                var task = this.Shutdown();
            }
        }

        private int _MixerBufferLength { get; set; }

        public int MixerBufferLength
        {
            get
            {
                return this._MixerBufferLength;
            }
            set
            {
                this._MixerBufferLength = value;
                Logger.Write(this, LogLevel.Debug, "MixerBufferLength = {0}", this.MixerBufferLength);
                var task = this.Shutdown();
            }
        }

        private int _ResamplingQuality { get; set; }

        public int ResamplingQuality
        {
            get
            {
                return this._ResamplingQuality;
            }
            set
            {
                this._ResamplingQuality = value;
                Logger.Write(this, LogLevel.Debug, "ResamplingQuality = {0}", this.ResamplingQuality);
                var task = this.Shutdown();
            }
        }

        public override bool ShowBuffering
        {
            get
            {
                return this.PipelineManager.WithPipeline(pipeline => pipeline == null);
            }
        }

        public override Task Start()
        {
            return this.Start(false);
        }

        protected virtual async Task Start(bool force)
        {
#if NET40
            if (!this.Semaphore.Wait(START_STOP_TIMEOUT))
#else
            if (!await this.Semaphore.WaitAsync(START_STOP_TIMEOUT).ConfigureAwait(false))
#endif
            {
                throw new InvalidOperationException(string.Format("{0} is already starting.", this.GetType().Name));
            }
            try
            {
                await this.OnStart(force).ConfigureAwait(false);
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        protected virtual async Task OnStart(bool force)
        {
            if (force || !this.IsStarted)
            {
                await this.ShutdownCore(false).ConfigureAwait(false);
                var exception = default(Exception);
                for (var a = 1; a <= START_ATTEMPTS; a++)
                {
                    Logger.Write(this, LogLevel.Debug, "Starting BASS, attempt: {0}", a);
                    try
                    {
                        this.OnInit();
                        await this.SetIsStarted(true).ConfigureAwait(false);
                        break;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        Logger.Write(this, LogLevel.Warn, "Failed to start BASS: {0}", e.Message);
                    }
                    await this.ShutdownCore(true).ConfigureAwait(false);
                    Thread.Sleep(START_ATTEMPT_INTERVAL);
                }
                if (this.IsStarted)
                {
                    Logger.Write(this, LogLevel.Debug, "Started BASS.");
                    return;
                }
                else if (exception != null)
                {
                    throw exception;
                }
            }
        }

        public override async Task Shutdown()
        {
#if NET40
            if (!this.Semaphore.Wait(START_STOP_TIMEOUT))
#else
            if (!await this.Semaphore.WaitAsync(START_STOP_TIMEOUT).ConfigureAwait(false))
#endif
            {
                throw new InvalidOperationException(string.Format("{0} is already stopping.", this.GetType().Name));
            }
            try
            {
                await this.ShutdownCore(false).ConfigureAwait(false);
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        protected virtual async Task ShutdownCore(bool force)
        {
            if (force || this.IsStarted)
            {
                var exception = default(Exception);
                Logger.Write(this, LogLevel.Debug, "Stopping BASS.");
                try
                {
                    await this.PipelineManager.FreePipeline().ConfigureAwait(false);
                    this.OnFree();
                    Logger.Write(this, LogLevel.Debug, "Stopped BASS.");
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Error, "Failed to stop BASS: {0}", e.Message);
                    exception = e;
                }
                await this.SetIsStarted(false).ConfigureAwait(false);
                if (exception != null)
                {
                    await this.ErrorEmitter.Send(this, exception).ConfigureAwait(false);
                }
            }
        }

        protected virtual void OnInit()
        {
            if (this.Init == null)
            {
                return;
            }
            this.Init(this, EventArgs.Empty);
        }

        public event EventHandler Init;

        protected virtual void OnFree()
        {
            if (this.Free == null)
            {
                return;
            }
            this.Free(this, EventArgs.Empty);
        }

        public event EventHandler Free;

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.RATE_ELEMENT
            ).ConnectValue(value => this.Rate = BassOutputConfiguration.GetRate(value));
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.ENFORCE_RATE_ELEMENT
            ).ConnectValue(value => this.EnforceRate = value);
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.DEPTH_ELEMENT
            ).ConnectValue(value => this.Float = BassOutputConfiguration.GetFloat(value));
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.UPDATE_PERIOD_ELEMENT
            ).ConnectValue(value => this.UpdatePeriod = value);
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.UPDATE_THREADS_ELEMENT
            ).ConnectValue(value => this.UpdateThreads = value);
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.BUFFER_LENGTH_ELEMENT
            ).ConnectValue(value => this.BufferLength = value);
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.MIXER_BUFFER_LENGTH_ELEMENT
            ).ConnectValue(value => this.MixerBufferLength = value);
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.RESAMPLE_QUALITY_ELEMENT
            ).ConnectValue(value => this.ResamplingQuality = value);
            this.Loader = ComponentRegistry.Instance.GetComponent<IBassLoader>();
            this.StreamFactory = ComponentRegistry.Instance.GetComponent<IBassStreamFactory>();
            this.PipelineManager = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineManager>();
            this.ErrorEmitter = core.Components.ErrorEmitter;
            base.InitializeComponent(core);
        }

        public override IEnumerable<string> SupportedExtensions
        {
            get
            {
                return this.Loader.Extensions;
            }
        }

        public override bool IsSupported(string fileName)
        {
            var extension = fileName.GetExtension();
            return this.Loader.IsSupported(extension);
        }

        public override bool IsLoaded(string fileName)
        {
            return this.IsStarted && this.StreamFactory.HasActiveStream(fileName);
        }

        public override async Task<IOutputStream> Load(PlaylistItem playlistItem, bool immidiate)
        {
            if (!this.IsStarted)
            {
                await this.Start().ConfigureAwait(false);
            }
            Logger.Write(this, LogLevel.Debug, "Loading stream: {0} => {1}", playlistItem.Id, playlistItem.FileName);
            var flags = BassFlags.Default;
            if (this.Float)
            {
                flags |= BassFlags.Float;
            }
            var stream = this.StreamFactory.CreateInteractiveStream(playlistItem, immidiate, flags);
            if (stream.IsEmpty)
            {
                Logger.Write(this, LogLevel.Debug, "Failed to load stream: {0} => {1} => {2}", playlistItem.Id, playlistItem.FileName, Enum.GetName(typeof(Errors), stream.Errors));
                return null;
            }
            var outputStream = new BassOutputStream(this, this.PipelineManager, stream, playlistItem);
            outputStream.InitializeComponent(this.Core);
            this.OnLoaded(outputStream);
            return outputStream;
        }

        public override IOutputStream Duplicate(IOutputStream stream)
        {
            var outputStream = stream as BassOutputStream;
            if (outputStream.Provider.Flags.HasFlag(BassStreamProviderFlags.Serial))
            {
                Logger.Write(this, LogLevel.Warn, "Cannot duplicate stream for file \"{0}\" with serial provider.", stream.FileName);
                return null;
            }
            Logger.Write(this, LogLevel.Debug, "Duplicating stream for file \"{0}\".", stream.FileName);
            var flags = BassFlags.Default;
            if (stream.Format == OutputStreamFormat.Float)
            {
                flags |= BassFlags.Float;
            }
            var result = this.StreamFactory.CreateBasicStream(stream.PlaylistItem, flags);
            if (result.IsEmpty)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to duplicate stream for file \"{0}\".", stream.FileName);
                return null;
            }
            outputStream = new BassOutputStream(this, this.PipelineManager, result, stream.PlaylistItem);
            outputStream.InitializeComponent(this.Core);
            return outputStream;
        }

        public override Task<bool> Preempt(IOutputStream stream)
        {
            var outputStream = stream as BassOutputStream;
            if (this.IsStarted)
            {
                return this.PipelineManager.WithPipelineExclusive(pipeline =>
                {
                    if (pipeline != null)
                    {
                        if (pipeline.Input.CheckFormat(outputStream))
                        {
                            if (pipeline.Input.Add(outputStream, this.OnStreamAdded))
                            {
                                Logger.Write(this, LogLevel.Debug, "Pre-empted playback of stream from file {0}: {1}", outputStream.FileName, outputStream.ChannelHandle);
                                return true;
                            }
                            else
                            {
                                //Probably already in the queue.
                            }
                        }
                        else
                        {
                            Logger.Write(this, LogLevel.Debug, "Properties differ from current configuration, cannot pre-empt playback of stream from file {0}: {1}", outputStream.FileName, outputStream.ChannelHandle);
                        }
                    }
                    return false;
                });
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Not yet started, cannot pre-empt playback of stream from file {0}: {1}", outputStream.FileName, outputStream.ChannelHandle);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.FromResult(false);
#endif
        }

        protected virtual void OnStreamAdded(BassOutputStream stream)
        {
            //Nothing to do.
        }

        public override async Task Unload(IOutputStream stream)
        {
            var outputStream = stream as BassOutputStream;
            if (this.IsStarted)
            {
                Logger.Write(this, LogLevel.Debug, "Unloading stream for file {0}: {1}", outputStream.FileName, outputStream.ChannelHandle);
                await this.PipelineManager.WithPipelineExclusive(pipeline =>
                {
                    if (pipeline != null)
                    {
                        var resume = false;
                        if (!outputStream.IsEnded && pipeline.Input.Contains(outputStream))
                        {
                            if (!pipeline.Input.PreserveBuffer)
                            {
                                Logger.Write(this, LogLevel.Debug, "Track was manually skipped, clearing the playback buffer.");
                                pipeline.Pause();
                                pipeline.ClearBuffer();
                                resume = true;
                            }
                        }
                        if (!pipeline.Input.Remove(outputStream, this.OnStreamRemoved))
                        {
                            //Probably not in the queue.
                            this.Unload(outputStream);
                        }
                        if (resume)
                        {
                            pipeline.Resume();
                        }
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                //TODO: Is this possible? 
                this.Unload(outputStream);
            }
        }

        protected virtual void OnStreamRemoved(BassOutputStream outputStream)
        {
            this.Unload(outputStream);
        }

        protected virtual void Unload(BassOutputStream outputStream)
        {
            this.OnUnloaded(outputStream);
            outputStream.Dispose();
            Logger.Write(this, LogLevel.Debug, "Unloaded stream for file {0}: {1}", outputStream.FileName, outputStream.ChannelHandle);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassOutputConfiguration.GetConfigurationSections();
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
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.IsStarted)
            {
                //TODO: Bad .Wait().
                this.Shutdown().Wait();
            }
            if (this.Semaphore != null)
            {
                this.Semaphore.Dispose();
            }
        }

        ~BassOutput()
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
