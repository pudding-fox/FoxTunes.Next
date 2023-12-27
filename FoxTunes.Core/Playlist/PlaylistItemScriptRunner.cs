using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class PlaylistItemScriptRunner : ScriptRunner<PlaylistItem>
    {
        public PlaylistItemScriptRunner(IScriptingContext scriptingContext, PlaylistItem playlistItem, string script) : base(scriptingContext, playlistItem, script)
        {

        }
    }
}
