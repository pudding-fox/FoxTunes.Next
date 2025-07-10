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

        protected override Task OnRun()
        {
            throw new NotImplementedException();
        }
    }
}
