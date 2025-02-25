using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Sox;
using System;
using System.Linq;

namespace FoxTunes
{
    public class BassResamplerStreamComponent : BassStreamComponent, IBassStreamControllable
    {
        public BassResamplerStreamComponent(BassResamplerStreamComponentBehaviour behaviour, IBassStreamPipeline pipeline, IBassStreamPipelineQueryResult query, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
            this.Query = query;
        }

        public override string Name
        {
            get
            {
                return Strings.BassResamplerStreamComponent_Name;
            }
        }

        public override string Description
        {
            get
            {
                var rate = default(int);
                var channels = default(int);
                var flags = default(BassFlags);
                if (!this.GetFormat(out rate, out channels, out flags))
                {
                    rate = 0;
                    channels = 0;
                    flags = BassFlags.Default;
                }
                return string.Format(
                    "{0} ({1}/{2} -> {1}/{3})",
                    this.Name,
                    BassUtils.DepthDescription(flags),
                    MetaDataInfo.SampleRateDescription(this.InputRate),
                    MetaDataInfo.SampleRateDescription(rate)
                );
            }
        }

        public BassResamplerStreamComponentBehaviour Behaviour { get; private set; }

        public IBassStreamPipelineQueryResult Query { get; private set; }

        public int InputChannelHandle { get; protected set; }

        public int InputRate { get; protected set; }

        public override int ChannelHandle { get; protected set; }

        public override int BufferLength
        {
            get
            {
                var length = default(int);
                if (!BassSox.StreamBufferLength(this.ChannelHandle, out length))
                {
                    return 0;
                }
                return Convert.ToInt32(Bass.ChannelBytes2Seconds(this.InputChannelHandle, length) * 1000);
            }
        }

        public override bool IsActive
        {
            get
            {
                return true;
            }
        }

        public override void Connect(IBassStreamComponent previous)
        {
            var rate = default(int);
            var channels = default(int);
            var flags = default(BassFlags);
            previous.GetFormat(out rate, out channels, out flags);
            this.InputChannelHandle = previous.ChannelHandle;
            this.InputRate = rate;
            if (this.Behaviour.Output.EnforceRate)
            {
                rate = this.Behaviour.Output.Rate;
            }
            else
            {
                //We already established that the output does not support the stream rate so use the closest one.
                rate = this.Query.GetNearestRate(rate);
            }
            Logger.Write(this, LogLevel.Debug, "Creating BASS SOX stream with rate {0} => {1} and {2} channels.", this.InputRate, rate, channels);
            this.ChannelHandle = BassSox.StreamCreate(rate, flags, previous.ChannelHandle);
            if (this.ChannelHandle == 0)
            {
                BassUtils.Throw();
            }
            this.Config.ChannelHandle = this.ChannelHandle;
            this.Config.Configure();
        }

        public override void ClearBuffer()
        {
            Logger.Write(this, LogLevel.Debug, "Clearing BASS SOX buffer: {0}", this.ChannelHandle);
            BassUtils.OK(BassSox.StreamBufferClear(this.ChannelHandle));
            base.ClearBuffer();
        }

        public bool IsBackground
        {
            get
            {
                var background = default(int);
                BassUtils.OK(BassSox.ChannelGetAttribute(this.ChannelHandle, SoxChannelAttribute.Background, out background));
                return Convert.ToBoolean(background);
            }
            set
            {
                if (this.IsBackground == value)
                {
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.Background), value);
                BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.Background, value));
            }
        }

        #region IBassStreamControllable

        public void PreviewPlay()
        {
            //Nothing to do.
        }

        public void PreviewPause()
        {
            //Nothing to do.
        }

        public void PreviewResume()
        {
            //Nothing to do.
        }

        public void PreviewStop()
        {
            //Nothing to do.
        }

        public void Play()
        {
            this.IsBackground = true;
        }

        public void Pause()
        {
            this.IsBackground = false;
        }

        public void Resume()
        {
            this.IsBackground = true;
        }

        public void Stop()
        {
            this.IsBackground = false;
        }

        #endregion

        public BassResamplerStreamComponentConfig Config { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Config = new BassResamplerStreamComponentConfig();
            this.Config.InitializeComponent(core);
            base.InitializeComponent(core);
        }

        protected override void OnDisposing()
        {
            if (this.ChannelHandle != 0)
            {
                Logger.Write(this, LogLevel.Debug, "Freeing BASS SOX stream: {0}", this.ChannelHandle);
                BassUtils.OK(Bass.StreamFree(this.ChannelHandle));
            }
        }

        public static bool ShouldCreate(BassResamplerStreamComponentBehaviour behaviour, BassOutputStream stream, IBassStreamPipelineQueryResult query)
        {
            if (BassUtils.GetChannelDsdRaw(stream.ChannelHandle))
            {
                //Cannot resample DSD.
                return false;
            }
            if (behaviour.Output.EnforceRate && behaviour.Output.Rate != stream.Rate)
            {
                //Rate is enforced and not equal to the stream rate.
                return true;
            }
            if (!query.OutputRates.Contains(stream.Rate))
            {
                //Output does not support the stream rate.
                return true;
            }
            //Something else.
            return false;
        }

        public class BassResamplerStreamComponentConfig : BaseComponent
        {
            public int ChannelHandle { get; set; }

            public IConfiguration Configuration { get; private set; }

            private SoxChannelQuality _Quality { get; set; }

            public SoxChannelQuality Quality
            {
                get
                {
                    return this._Quality;
                }
                set
                {
                    this._Quality = value;
                    this.OnQualityChanged();
                }
            }

            protected virtual void OnQualityChanged()
            {
                this.Configure();
                if (this.QualityChanged != null)
                {
                    this.QualityChanged(this, EventArgs.Empty);
                }
                this.OnPropertyChanged("Quality");
            }

            public event EventHandler QualityChanged;

            private SoxChannelPhase _Phase { get; set; }

            public SoxChannelPhase Phase
            {
                get
                {
                    return this._Phase;
                }
                set
                {
                    this._Phase = value;
                    this.OnPhaseChanged();
                }
            }

            protected virtual void OnPhaseChanged()
            {
                this.Configure();
                if (this.PhaseChanged != null)
                {
                    this.PhaseChanged(this, EventArgs.Empty);
                }
                this.OnPropertyChanged("Phase");
            }

            public event EventHandler PhaseChanged;

            private bool _SteepFilter { get; set; }

            public bool SteepFilter
            {
                get
                {
                    return this._SteepFilter;
                }
                set
                {
                    this._SteepFilter = value;
                    this.OnSteepFilterChanged();
                }
            }

            protected virtual void OnSteepFilterChanged()
            {
                this.Configure();
                if (this.SteepFilterChanged != null)
                {
                    this.SteepFilterChanged(this, EventArgs.Empty);
                }
                this.OnPropertyChanged("SteepFilter");
            }

            public event EventHandler SteepFilterChanged;

            private int _InputBufferLength { get; set; }

            public int InputBufferLength
            {
                get
                {
                    return this._InputBufferLength;
                }
                set
                {
                    this._InputBufferLength = value;
                    this.OnInputBufferLengthChanged();
                }
            }

            protected virtual void OnInputBufferLengthChanged()
            {
                this.Configure();
                if (this.InputBufferLengthChanged != null)
                {
                    this.InputBufferLengthChanged(this, EventArgs.Empty);
                }
                this.OnPropertyChanged("InputBufferLength");
            }

            public event EventHandler InputBufferLengthChanged;

            private int _PlaybackBufferLength { get; set; }

            public int PlaybackBufferLength
            {
                get
                {
                    return this._PlaybackBufferLength;
                }
                set
                {
                    this._PlaybackBufferLength = value;
                    this.OnPlaybackBufferLengthChanged();
                }
            }

            protected virtual void OnPlaybackBufferLengthChanged()
            {
                this.Configure();
                if (this.PlaybackBufferLengthChanged != null)
                {
                    this.PlaybackBufferLengthChanged(this, EventArgs.Empty);
                }
                this.OnPropertyChanged("PlaybackBufferLength");
            }

            public event EventHandler PlaybackBufferLengthChanged;

            public override void InitializeComponent(ICore core)
            {
                this.Configuration = core.Components.Configuration;
                this.Configuration.GetElement<SelectionConfigurationElement>(
                    BassOutputConfiguration.SECTION,
                    BassResamplerStreamComponentConfiguration.QUALITY_ELEMENT
                ).ConnectValue(value => this.Quality = BassResamplerStreamComponentConfiguration.GetQuality(value));
                this.Configuration.GetElement<SelectionConfigurationElement>(
                    BassOutputConfiguration.SECTION,
                    BassResamplerStreamComponentConfiguration.PHASE_ELEMENT
                ).ConnectValue(value => this.Phase = BassResamplerStreamComponentConfiguration.GetPhase(value));
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    BassOutputConfiguration.SECTION,
                    BassResamplerStreamComponentConfiguration.STEEP_FILTER_ELEMENT
                ).ConnectValue(value => this.SteepFilter = value);
                this.Configuration.GetElement<IntegerConfigurationElement>(
                    BassOutputConfiguration.SECTION,
                    BassResamplerStreamComponentConfiguration.INPUT_BUFFER_LENGTH
                ).ConnectValue(value => this.InputBufferLength = value);
                this.Configuration.GetElement<IntegerConfigurationElement>(
                    BassOutputConfiguration.SECTION,
                    BassResamplerStreamComponentConfiguration.PLAYBACK_BUFFER_LENGTH_ELEMENT
                ).ConnectValue(value => this.PlaybackBufferLength = value);
                base.InitializeComponent(core);
            }

            public void Configure()
            {
                if (this.ChannelHandle == 0)
                {
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.Quality), Enum.GetName(typeof(SoxChannelQuality), this.Quality));
                BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.Quality, this.Quality));
                Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.Phase), Enum.GetName(typeof(SoxChannelPhase), this.Phase));
                BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.Phase, this.Phase));
                Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.SteepFilter), this.SteepFilter);
                BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.SteepFilter, this.SteepFilter));
                Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.InputBufferLength), this.InputBufferLength);
                BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.InputBufferLength, this.InputBufferLength));
                Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.PlaybackBufferLength), this.PlaybackBufferLength);
                BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.PlaybackBufferLength, this.PlaybackBufferLength));
                Logger.Write(this, LogLevel.Debug, "Setting BASS SOX attribute \"{0}\" = \"{1}\"", Enum.GetName(typeof(SoxChannelAttribute), SoxChannelAttribute.KeepAlive), true);
                BassUtils.OK(BassSox.ChannelSetAttribute(this.ChannelHandle, SoxChannelAttribute.KeepAlive, true));
            }
        }
    }
}
