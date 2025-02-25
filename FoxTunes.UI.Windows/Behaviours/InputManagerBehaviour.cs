using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class InputManagerBehaviour : StandardBehaviour, IDisposable
    {
        public InputManagerBehaviour()
        {
            this.Bindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public HashSet<string> Bindings { get; private set; }

        public global::FoxTunes.ViewModel.Playback Playback { get; private set; }

        public IInputManager InputManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public TextConfigurationElement Play { get; private set; }

        public TextConfigurationElement Previous { get; private set; }

        public TextConfigurationElement Next { get; private set; }

        public TextConfigurationElement Stop { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Playback = new global::FoxTunes.ViewModel.Playback(false);
            this.InputManager = ComponentRegistry.Instance.GetComponent<IInputManager>();
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaybackManager = core.Managers.Playback;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            this.Configuration = core.Components.Configuration;
            this.Play = this.Configuration.GetElement<TextConfigurationElement>(
                InputManagerConfiguration.SECTION,
                InputManagerConfiguration.PLAY_ELEMENT
            );
            this.Previous = this.Configuration.GetElement<TextConfigurationElement>(
                InputManagerConfiguration.SECTION,
                InputManagerConfiguration.PREVIOUS_ELEMENT
            );
            this.Next = this.Configuration.GetElement<TextConfigurationElement>(
                InputManagerConfiguration.SECTION,
                InputManagerConfiguration.NEXT_ELEMENT
            );
            this.Stop = this.Configuration.GetElement<TextConfigurationElement>(
                InputManagerConfiguration.SECTION,
                InputManagerConfiguration.STOP_ELEMENT
            );
            foreach (var element in new[] { this.Play, this.Previous, this.Next, this.Stop })
            {
                if (element == null)
                {
                    continue;
                }
                element.ValueChanged += this.OnValueChanged;
            }
            this.Update();
            base.InitializeComponent(core);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.Update();
        }

        protected virtual void Update()
        {
            this.RemoveCommandBindings();
            if (this.Play != null)
            {
                this.AddCommandBinding(this.Play.Value, this.Playback.PlayCommand);
            }
            if (this.Next != null)
            {
                this.AddCommandBinding(this.Next.Value, this.Playback.NextCommand);
            }
            if (this.Previous != null)
            {
                this.AddCommandBinding(this.Previous.Value, this.Playback.PreviousCommand);
            }
            if (this.Stop != null)
            {
                this.AddCommandBinding(this.Stop.Value, this.Playback.StopOutputCommand);
            }
        }

        protected virtual void AddCommandBinding(string keys, ICommand command)
        {
            this.AddCommandBinding(keys, () =>
            {
                if (!command.CanExecute(null))
                {
                    return;
                }
                command.Execute(null);
            });
        }

        protected virtual void AddCommandBinding(string keys, Action action)
        {
            if (this.InputManager == null)
            {
                return;
            }
            if (this.InputManager.AddInputHook(keys, action))
            {
                this.Bindings.Add(keys);
            }
            else
            {
                this.ErrorEmitter.Send(this,string.Format("Failed to register input hook {0}", keys));
            }
        }

        protected virtual void RemoveCommandBindings()
        {
            foreach (var binding in this.Bindings)
            {
                this.RemoveCommandBinding(binding);
            }
            this.Bindings.Clear();
        }

        protected virtual void RemoveCommandBinding(string keys)
        {
            if (this.InputManager == null)
            {
                return;
            }
            try
            {
                this.InputManager.RemoveInputHook(keys);
            }
            catch
            {
                //Nothing can be done.
            }
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
            foreach (var element in new[] { this.Play, this.Previous, this.Next, this.Stop })
            {
                if (element == null)
                {
                    continue;
                }
                element.ValueChanged -= this.OnValueChanged;
            }
            this.RemoveCommandBindings();
        }

        ~InputManagerBehaviour()
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
