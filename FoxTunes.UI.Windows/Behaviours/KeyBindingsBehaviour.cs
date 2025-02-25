using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class KeyBindingsBehaviour : StandardBehaviour, IKeyBindingsBehaviour
    {
        public KeyBindingsBehaviour()
        {
            this.Commands = new Dictionary<string, Tuple<string, ICommand>>(StringComparer.OrdinalIgnoreCase);
            this.Bindings = new Dictionary<Window, IDictionary<string, InputBinding>>();
        }

        public IDictionary<string, Tuple<string, ICommand>> Commands { get; private set; }

        public IDictionary<Window, IDictionary<string, InputBinding>> Bindings { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            WindowBase.Created += this.OnWindowCreated;
            WindowBase.Destroyed += this.OnWindowDestroyed;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            base.InitializeComponent(core);
        }

        protected virtual void OnWindowCreated(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                this.AddCommandBindings(window);
            }
        }

        protected virtual void OnWindowDestroyed(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                this.RemoveCommandBindings(window);
            }
        }

        protected virtual void AddCommandBindings(Window window)
        {
            foreach (var pair in this.Commands)
            {
                this.AddCommandBinding(window, pair.Key, pair.Value.Item1, pair.Value.Item2);
            }
        }

        protected virtual void AddCommandBinding(string id, string keys, ICommand command)
        {
            foreach (var window in Application.Current.Windows.OfType<Window>())
            {
                this.AddCommandBinding(window, id, keys, command);
            }
        }

        protected virtual void AddCommandBinding(Window window, string id, string keys, ICommand command)
        {
            var key = default(Key);
            var modifiers = default(ModifierKeys);
            if (keys.TryGetKeys(out modifiers, out key))
            {
                this.AddCommandBinding(window, id, key, modifiers, command);
            }
            else
            {
                this.ErrorEmitter.Send(this, string.Format("Failed to register input hook {0}", keys));
            }
        }

        protected virtual void AddCommandBinding(Window window, string id, Key key, ModifierKeys modifiers, ICommand command)
        {
            try
            {
                var gesture = new KeyGesture(key, modifiers);
                var binding = new InputBinding(command, gesture);
                this.Bindings.GetOrAdd(window, () => new Dictionary<string, InputBinding>(StringComparer.OrdinalIgnoreCase))[id] = binding;
                window.InputBindings.Add(binding);
                Logger.Write(this, LogLevel.Debug, "AddCommandBinding: {0}/{1} => {2}", window.GetType().Name, window.Title, id);
            }
            catch (Exception e)
            {
                this.ErrorEmitter.Send(this, string.Format("Failed to register input hook {0}: {1}", id, e.Message));
            }
        }

        protected virtual void RemoveCommandBindings(Window window)
        {
            var bindings = default(IDictionary<string, InputBinding>);
            if (!this.Bindings.TryGetValue(window, out bindings))
            {
                return;
            }
            var ids = bindings.Keys.ToArray();
            foreach (var id in ids)
            {
                this.RemoveCommandBinding(window, id);
            }
            this.Bindings.Remove(window);
        }

        protected virtual void RemoveCommandBinding(string id)
        {
            if (Application.Current == null)
            {
                return;
            }
            foreach (var window in Application.Current.Windows.OfType<Window>())
            {
                this.RemoveCommandBinding(window, id);
            }
        }

        protected virtual void RemoveCommandBinding(Window window, string id)
        {
            var bindings = default(IDictionary<string, InputBinding>);
            if (!this.Bindings.TryGetValue(window, out bindings))
            {
                return;
            }
            var binding = default(InputBinding);
            if (!bindings.TryGetValue(id, out binding))
            {
                return;
            }
            this.RemoveCommandBinding(window, id, binding);
            bindings.Remove(id);
        }

        protected virtual void RemoveCommandBinding(Window window, string id, InputBinding binding)
        {
            try
            {
                window.InputBindings.Remove(binding);
                Logger.Write(this, LogLevel.Debug, "RemoveCommandBinding: {0}/{1} => {2}", window.GetType().Name, window.Title, id);
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to remove command binding {0}: {1}", id, e.Message);
            }
        }

        public bool Add(string id, string keys, ICommand command)
        {
            if (this.Commands.TryAdd(id, new Tuple<string, ICommand>(keys, command)))
            {
                this.AddCommandBinding(id, keys, command);
                return true;
            }
            if (this.Commands.Remove(id))
            {
                this.RemoveCommandBinding(id);
                return this.Add(id, keys, command);
            }
            return false;
        }

        public bool Remove(string id)
        {
            if (!this.Commands.Remove(id))
            {
                return false;
            }
            this.RemoveCommandBinding(id);
            return true;
        }

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
            Windows.Registrations.RemoveCreated(
                Windows.Registrations.IdsByRole(UserInterfaceWindowRole.Main),
                this.OnWindowCreated
            );

        }

        ~KeyBindingsBehaviour()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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
