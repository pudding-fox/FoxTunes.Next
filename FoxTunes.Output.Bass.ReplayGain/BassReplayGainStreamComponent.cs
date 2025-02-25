using FoxTunes.Interfaces;
using ManagedBass;
using System;

namespace FoxTunes
{
    public class BassReplayGainStreamComponent : BassStreamComponent
    {
        public BassReplayGainStreamComponent(BassReplayGainBehaviour behaviour, IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
        }

        public override string Name
        {
            get
            {
                return Strings.BassReplayGainStreamComponent_Name;
            }
        }

        public override string Description
        {
            get
            {
                var currentStream = this.PlaybackManager.CurrentStream as BassOutputStream;
                if (currentStream == null)
                {
                    return string.Format("{0} ({1})", this.Name, Strings.BassReplayGainStreamComponent_None);
                }
                var effect = default(ReplayGainEffect);
                if (!this.Behaviour.Effects.TryGetValue(currentStream, out effect))
                {
                    return string.Format("{0} ({1})", this.Name, Strings.BassReplayGainStreamComponent_None);
                }
                return string.Format(
                    "{0} ({1}{2}%/{3}dB/{4})",
                    this.Name,
                    effect.ReplayGain > 0 ? "+" : string.Empty,
                    Convert.ToInt32(effect.Volume * 100),
                    effect.ReplayGain,
                    Enum.GetName(typeof(ReplayGainMode), effect.Mode).ToLower()
                );
            }
        }

        public BassReplayGainBehaviour Behaviour { get; private set; }

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
                var effect = default(ReplayGainEffect);
                return this.Behaviour.Effects.TryGetValue(currentStream, out effect);
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
