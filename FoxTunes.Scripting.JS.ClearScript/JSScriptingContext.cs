using FoxTunes.Interfaces;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FoxTunes
{
    public class JSScriptingContext : ScriptingContext
    {
        private JSScriptingContext()
        {
            this.Scripts = new Dictionary<string, V8Script>();
        }

        public JSScriptingContext(V8ScriptEngine engine) : this()
        {
            this.Engine = engine;
        }

        public IDictionary<string, V8Script> Scripts { get; private set; }

        public V8ScriptEngine Engine { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Engine.AddHostObject("Publication", typeof(Publication));
            this.Engine.AddHostObject("DateHelper", typeof(DateHelper));
            this.Engine.AddHostObject("NumberHelper", typeof(NumberHelper));
            //Note: Lower case to match tag, property etc.
            this.SetValue("strings", StringsHelper.Strings);
            this.Engine.Execute(JSCoreScripts.Instance.Utils);
            base.InitializeComponent(core);
        }

        public override object GetValue(string name)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            return this.Engine.Global.GetProperty(name);
        }

        public override void SetValue(string name, object value)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (value is IDictionary dictionary)
            {
                var propertyBag = new PropertyBag(true, StringComparer.OrdinalIgnoreCase);
                foreach (var key in dictionary.Keys)
                {
                    propertyBag.SetPropertyNoCheck(Convert.ToString(key), dictionary[key]);
                }
                this.Engine.Global.SetProperty(name, propertyBag);
            }
            else
            {
                this.Engine.Global.SetProperty(name, value);
            }
        }

        public override object Run(string script)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            try
            {
                var handle = this.Compile(script);
                return this.Engine.Evaluate(handle);
            }
            catch (ScriptEngineException e)
            {
                throw new ScriptingException(e.Message);
            }
        }

        protected virtual V8Script Compile(string script)
        {
            return this.Scripts.GetOrAdd(script, key => this.Engine.Compile(script));
        }

        protected override void OnDisposing()
        {
            if (this.Scripts != null)
            {
                foreach (var pair in this.Scripts)
                {
                    pair.Value.Dispose();
                }
            }
            if (this.Engine != null)
            {
                this.Engine.Dispose();
            }
            base.OnDisposing();
        }
    }
}