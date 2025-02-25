using FoxTunes.Interfaces;
using FoxTunes.Proxies;
using System;

namespace FoxTunes
{
    [ComponentPreference(ComponentPreferenceAttribute.NORMAL)]
    [Component(ID, ComponentSlots.ScriptingRuntime)]
    public class JSScriptingRuntime : ScriptingRuntime
    {
        const string ID = "8D4693E0-6416-4B33-9DE7-89116D15F5EA";

        const string VERSION = "0.7.1";

        const int JS_INIT_ATTEMPTS = 10;

        public JSScriptingRuntime() : base(ID, string.Format(Strings.JSScriptingRuntime_Name, VERSION))
        {
            Loader.Load("msvcr100.dll");
            Loader.Load("msvcp100.dll");
        }

        public ICore Core { get; private set; }

        public override ICoreScripts CoreScripts
        {
            get
            {
                return JSCoreScripts.Instance;
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            base.InitializeComponent(core);
        }

        public override IScriptingContext CreateContext()
        {
            Logger.Write(this, LogLevel.Debug, "Creating javascript scripting context.");
            for (var a = 1; a <= JS_INIT_ATTEMPTS; a++)
            {
                try
                {
                    var context = new JSScriptingContext(JavascriptContextFactory.Create());
                    context.InitializeComponent(this.Core);
                    return context;
                }
                catch
                {
                    if (a < JS_INIT_ATTEMPTS)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to create javascript scripting context, will retry..");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            throw new NotImplementedException();
        }
    }
}