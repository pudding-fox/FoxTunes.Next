using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class LLamaAICorePrompts : BaseComponent, ICorePrompts
    {
        public string AvailableTracks
        {
            get
            {
                return Resources.AvailableTracks;
            }
        }

        public string CreatePlaylist(string prompt)
        {
            return string.Format(Resources.CreatePlaylist, prompt);
        }
    }
}
