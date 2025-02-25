using FoxTunes.Interfaces;
using FoxTunes.Proxies;
using System;
using System.Diagnostics;

namespace FoxTunes
{
    public class JSScriptingContext : ScriptingContext
    {
        public JSScriptingContext(JavascriptContext context)
        {
            this.Context = context;
        }

        public JavascriptContext Context { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Context.SetParameter("Publication", new Publication());
            this.Context.SetParameter("DateHelper", new DateHelper());
            this.Context.SetParameter("NumberHelper", new NumberHelper());
            //Note: Lower case to match tag, property etc.
            this.Context.SetParameter("strings", StringsHelper.Strings);
            this.Context.Run(JSCoreScripts.Instance.Utils);
            base.InitializeComponent(core);
        }

        public override void SetValue(string name, object value)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            this.Context.SetParameter(name, value);
        }

        public override object GetValue(string name)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            return this.Context.GetParameter(name);
        }

        [DebuggerNonUserCode]
        public override object Run(string script)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            return this.Context.Run(script);
        }

        protected override void OnDisposing()
        {
            if (this.Context != null)
            {
                this.Context.Dispose();
            }
            base.OnDisposing();
        }

        private class Publication
        {
            public string Company
            {
                get
                {
                    return global::FoxTunes.Publication.Company;
                }
            }

            public string Product
            {
                get
                {
                    return global::FoxTunes.Publication.Product;
                }
            }

            public string Version
            {
                get
                {
                    return global::FoxTunes.Publication.Version;
                }
            }
        }
    }
}