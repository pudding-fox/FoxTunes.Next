using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class BassMemoryStreamAdvisor : BassStreamAdvisor
    {
        public BassMemoryBehaviour Behaviour { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<BassMemoryBehaviour>();
            base.InitializeComponent(core);
        }

        public override void Advise(IBassStreamProvider provider, PlaylistItem playlistItem, IList<IBassStreamAdvice> advice, BassStreamUsageType type)
        {
            if (!this.Behaviour.Enabled)
            {
                return;
            }
            if (typeof(BassMemoryStreamProvider).IsAssignableFrom(provider.GetType()))
            {
                return;
            }
            if (type != BassStreamUsageType.Interactive)
            {
                return;
            }
            advice.Add(new BassMemoryStreamAdvise(playlistItem.FileName));
        }
    }
}
