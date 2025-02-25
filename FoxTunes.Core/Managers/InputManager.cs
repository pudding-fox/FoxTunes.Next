using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FoxTunes
{
    public abstract class InputManager : BaseManager, IInputManager
    {
        public InputManager()
        {
            this.Registrations = new ConcurrentDictionary<Tuple<int, int>, Action>();
        }

        public ConcurrentDictionary<Tuple<int, int>, Action> Registrations { get; private set; }

        public bool _Enabled { get; private set; }

        public bool Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                this._Enabled = value;
                this.OnEnabledChanged();
            }
        }

        protected virtual void OnEnabledChanged()
        {
            //Nothing to do.
        }

        public IConfiguration Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                InputManagerConfiguration.SECTION,
                InputManagerConfiguration.ENABLED_ELEMENT
            ).ConnectValue(value => this.Enabled = value);
            base.InitializeComponent(core);
        }

        public void AddInputHook(int modifiers, int keys, Action action)
        {
            var key = new Tuple<int, int>(modifiers, keys);
            var value = default(Action);
            if (this.Registrations.TryGetValue(key, out value))
            {
                this.RemoveInputHook(modifiers, keys);
            }
            this.Registrations.TryAdd(key, action);
        }

        public void RemoveInputHook(int modifiers, int keys)
        {
            var key = new Tuple<int, int>(modifiers, keys);
            this.Registrations.TryRemove(key);
        }

        protected virtual void OnInputEvent(int modifiers, int keys)
        {
            var value = default(Action);
            if (!this.Registrations.TryGetValue(new Tuple<int, int>(modifiers, keys), out value))
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Executing global keyboard shortcut: {0} => {1}", modifiers, keys);
            value();
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return InputManagerConfiguration.GetConfigurationSections();
        }
    }
}
