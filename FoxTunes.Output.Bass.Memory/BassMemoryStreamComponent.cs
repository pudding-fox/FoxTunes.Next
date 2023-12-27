using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Memory;

namespace FoxTunes
{
    public class BassMemoryStreamComponent : BassStreamComponent
    {
        public BassMemoryStreamComponent(BassMemoryBehaviour behaviour, IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
        }

        public override string Name
        {
            get
            {
                return Strings.BassMemoryStreamComponent_Name;
            }
        }

        public override string Description
        {
            get
            {
                return string.Format(
                    "{0} ({1} MB)",
                    this.Name,
                    BassMemory.Usage() / 1000000
                );
            }
        }

        public BassMemoryBehaviour Behaviour { get; private set; }

        public override int ChannelHandle { get; protected set; }

        public override bool IsActive
        {
            get
            {
                return BassMemory.Usage() > 0;
            }
        }

        public override void Connect(IBassStreamComponent previous)
        {
            this.ChannelHandle = previous.ChannelHandle;
        }

        protected override void OnDisposing()
        {
            //Nothing to do.
        }

        public static bool ShouldCreate(BassMemoryBehaviour behaviour, BassOutputStream stream, IBassStreamPipelineQueryResult query)
        {
            if (BassUtils.GetChannelDsdRaw(stream.ChannelHandle))
            {
                //Only PCM.
                return false;
            }
            return true;
        }
    }
}
