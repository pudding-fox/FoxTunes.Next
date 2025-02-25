using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Linq;

namespace FoxTunes
{
    public class BassSkipSilenceStreamComponent : BassStreamComponent
    {
        public BassSkipSilenceStreamComponent(BassSkipSilenceStreamAdvisorBehaviour behaviour, IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
        }

        public override string Name
        {
            get
            {
                return Strings.BassSkipSilenceStreamComponent_Name;
            }
        }

        public override string Description
        {
            get
            {
                if (!this.IsActive)
                {
                    return string.Format("{0} ({1})", this.Name, Strings.BassSkipSilenceStreamComponent_None);
                }
                var currentStream = this.PlaybackManager.CurrentStream as BassOutputStream;
                if (currentStream == null)
                {
                    return string.Format("{0} ({1})", this.Name, Strings.BassSkipSilenceStreamComponent_None);
                }
                var advice = currentStream.Advice.OfType<BassSkipSilenceStreamAdvice>().FirstOrDefault(
                    _advice => _advice.LeadIn != TimeSpan.Zero || _advice.LeadOut != TimeSpan.Zero
                );
                if (advice == null)
                {
                    return string.Format("{0} ({1})", this.Name, Strings.BassSkipSilenceStreamComponent_None);
                }
                return string.Format(
                    "{0} ({1:0.00}s/{2:0.00}s)",
                    this.Name,
                    advice.LeadIn.TotalSeconds,
                    advice.LeadOut.TotalSeconds
                );
            }
        }

        public BassSkipSilenceStreamAdvisorBehaviour Behaviour { get; private set; }

        public override int ChannelHandle { get; protected set; }

        public override bool IsActive
        {
            get
            {
                var currentStream = this.PlaybackManager.CurrentStream as BassOutputStream;
                if (currentStream == null)
                {
                    return false;
                }
                var advice = currentStream.Advice.OfType<BassSkipSilenceStreamAdvice>().FirstOrDefault(
                    _advice => _advice.LeadIn != TimeSpan.Zero || _advice.LeadOut != TimeSpan.Zero
                );
                return advice != null;
            }
        }

        public IPlaybackManager PlaybackManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            base.InitializeComponent(core);
        }

        public override void Connect(IBassStreamComponent previous)
        {
            this.ChannelHandle = previous.ChannelHandle;
        }

        protected override void OnDisposing()
        {
            //Nothing to do.
        }
    }
}
