using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Memory;

namespace FoxTunes
{
    public class BassDsdMemoryStreamComponent : BassStreamComponent
    {
        public BassDsdMemoryStreamComponent(BassDsdBehaviour behaviour, IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
        }

        public override string Name
        {
            get
            {
                return Strings.BassDsdMemoryStreamComponent_Name;
            }
        }

        public override string Description
        {
            get
            {
                return string.Format(
                    "{0} ({1} MB)",
                    this.Name,
                    BassMemory.Dsd.Usage() / 1000000
                );
            }
        }

        public BassDsdBehaviour Behaviour { get; private set; }

        public override int ChannelHandle { get; protected set; }

        public override bool IsActive
        {
            get
            {
                return BassMemory.Dsd.Usage() > 0;
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

        public static bool ShouldCreate(BassDsdBehaviour behaviour, BassOutputStream stream, IBassStreamPipelineQueryResult query)
        {
            return behaviour.Memory;
        }
    }
}
