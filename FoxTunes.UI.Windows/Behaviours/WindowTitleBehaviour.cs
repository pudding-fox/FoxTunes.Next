using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class WindowTitleBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public IPlaybackManager PlaybackManager { get; private set; }

        public IScriptingRuntime ScriptingRuntime { get; private set; }

        public IScriptingContext ScriptingContext { get; private set; }

        public IConfiguration Configuration { get; private set; }

        private string _Script { get; set; }

        public string Script
        {
            get
            {
                return this._Script;
            }
        }

        public Task SetScript(string value)
        {
            this._Script = value;
            return this.OnScriptChanged();
        }

        protected virtual async Task OnScriptChanged()
        {
            await this.Refresh().ConfigureAwait(false);
            if (this.ScriptChanged != null)
            {
                this.ScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Script");
        }

        public event EventHandler ScriptChanged;

        public override void InitializeComponent(ICore core)
        {
            Windows.Registrations.AddCreated(
                Windows.Registrations.IdsByRole(UserInterfaceWindowRole.Main),
                this.OnWindowCreated
            );
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
            this.ScriptingContext = this.ScriptingRuntime.CreateContext();
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<TextConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowTitleBehaviourConfiguration.WINDOW_TITLE_SCRIPT_ELEMENT
            ).ConnectValue(async value => await this.SetScript(value).ConfigureAwait(false));
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        protected virtual void OnWindowCreated(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                var task = this.Refresh(window);
            }
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        protected virtual Task Refresh()
        {
            var title = this.GetWindowTitle();
            return Windows.Invoke(() => this.SetWindowTitle(title));
        }

        protected virtual Task Refresh(Window window)
        {
            var title = this.GetWindowTitle();
            return Windows.Invoke(() => this.SetWindowTitle(window, title));
        }

        protected virtual string GetWindowTitle()
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            var runner = new PlaylistItemScriptRunner(
                this.ScriptingContext,
                outputStream != null ? outputStream.PlaylistItem : null,
                this.Script
            );
            runner.Prepare();
            return Convert.ToString(runner.Run());
        }

        protected virtual void SetWindowTitle(string title)
        {
            foreach (var window in Windows.Registrations.WindowsByRole(UserInterfaceWindowRole.Main))
            {
                this.SetWindowTitle(window, title);
            }
        }

        protected virtual void SetWindowTitle(Window window, string title)
        {
            Logger.Write(this, LogLevel.Debug, "Setting window title {0}: {1}", window.GetType().Name, title);
            window.Title = title;
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WindowTitleBehaviourConfiguration.GetConfigurationSections();
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
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            if (this.ScriptingContext != null)
            {
                this.ScriptingContext.Dispose();
            }
        }

        ~WindowTitleBehaviour()
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
