using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace FoxTunes
{
    public abstract class KeyBindingsBehaviourBase : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public KeyBindingsBehaviourBase()
        {
            this.Commands = new Dictionary<TextConfigurationElement, ICommand>();
        }

        public IKeyBindingsBehaviour Behaviour { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<IKeyBindingsBehaviour>();
            this.AddCommands();
            base.InitializeComponent(core);
        }

        public IDictionary<TextConfigurationElement, ICommand> Commands { get; private set; }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            if (sender is TextConfigurationElement element)
            {
                var command = default(ICommand);
                if (this.Commands.TryGetValue(element, out command))
                {
                    this.Behaviour.Add(element.Id, element.Value, command);
                }
            }
        }

        protected virtual void AddCommands()
        {
            foreach (var pair in this.Commands)
            {
                this.Behaviour.Add(pair.Key.Id, pair.Key.Value, pair.Value);
            }
        }

        protected virtual void RemoveCommands()
        {
            foreach (var pair in this.Commands)
            {
                this.Behaviour.Remove(pair.Key.Id);
            }
        }

        public abstract IEnumerable<ConfigurationSection> GetConfigurationSections();

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            this.RemoveCommands();
            foreach (var pair in this.Commands)
            {
                pair.Key.ValueChanged -= this.OnValueChanged;
            }
        }

        ~KeyBindingsBehaviourBase()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
