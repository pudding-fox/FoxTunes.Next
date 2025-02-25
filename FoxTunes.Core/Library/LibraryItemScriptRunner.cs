using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class LibraryItemScriptRunner : ScriptRunner<LibraryItem>
    {
        public LibraryItemScriptRunner(IScriptingContext scriptingContext, LibraryItem libraryItem, string script) : base(scriptingContext, libraryItem, script)
        {

        }
    }
}
