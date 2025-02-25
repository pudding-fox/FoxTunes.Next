using FoxTunes.Interfaces;
using Microsoft.ClearScript.V8;
using System;

namespace FoxTunes
{
    [ComponentPreference(ComponentPreferenceAttribute.DEFAULT)]
    [PlatformDependency(Major = 6, Minor = 0)]
    [Component(ID, ComponentSlots.ScriptingRuntime)]
    public class JSScriptingRuntime : ScriptingRuntime
    {
        const string ID = "BA421DD1-22AB-4E39-82FA-55BFD95EE768";

        public static string VERSION = typeof(V8ScriptEngine).Assembly.GetName().Version.ToString();

        public JSScriptingRuntime() : base(ID, string.Format(Strings.JSScriptingRuntime_Name, VERSION))
        {
            if (Environment.Is64BitProcess)
            {
                Loader.Load("ClearScriptV8.win-x64.dll");
            }
            else
            {
                Loader.Load("ClearScriptV8.win-x86.dll");
            }
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
            var context = new JSScriptingContext(new V8ScriptEngine());
            context.InitializeComponent(this.Core);
            return context;
        }
    }
}