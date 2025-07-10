using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class CreateAIPlaylistTask : PlaylistTaskBase
    {
        public CreateAIPlaylistTask(Playlist playlist, string prompt, int sequence = 0) : base(playlist, sequence)
        {
            this.Prompt = prompt;
        }

        public string Prompt { get; private set; }

        public TextConfigurationElement ModelLocation { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.ModelLocation = this.Configuration.GetElement<TextConfigurationElement>(
                AIPlaylistBehaviourConfiguration.SECTION,
                AIPlaylistBehaviourConfiguration.MODEL_LOCATION
            );
        }

        protected override Task OnRun()
        {
            throw new NotImplementedException();
        }
    }
}
