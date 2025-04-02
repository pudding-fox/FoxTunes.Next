using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ToolWindowBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent, IDisposable
    {
        const string MAIN_WINDOW_ID = MainWindow.ID;

        const string MINI_WINDOW_ID = "95FA900C-2B6C-4571-B119-D24834E2FC22";

        public const string NEW = "BBBB";

        public const string MANAGE = "CCCC";

        static ToolWindowBehaviour()
        {
            global::FoxTunes.Windows.Registrations.Add(new global::FoxTunes.Windows.WindowRegistration(ToolWindowManagerWindow.ID, UserInterfaceWindowRole.None, () => new ToolWindowManagerWindow()));
        }

        public ToolWindowBehaviour()
        {
            this.Windows = new ConcurrentDictionary<ToolWindowConfiguration, ToolWindow>();
            Instance = this;
        }

        public bool Enabled
        {
            get
            {
                return this.Layout != null && string.Equals(this.Layout.Value.Id, UIComponentLayoutProvider.ID) && !global::FoxTunes.Windows.IsShuttingDown;
            }
        }

        public ICore Core { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Layout { get; private set; }

        public TextConfigurationElement Element { get; private set; }

        public ConcurrentDictionary<ToolWindowConfiguration, ToolWindow> Windows { get; private set; }

        public bool IsLoaded { get; private set; }

        public bool IsSaving { get; private set; }

        protected virtual async Task Load()
        {
            var value = this.Element.Value;
            if (this.IsLoaded)
            {
                if (!this.HasChanges())
                {
                    return;
                }
                await this.Reset().ConfigureAwait(false);
            }
            else
            {
                this.IsLoaded = true;
            }
            if (string.IsNullOrEmpty(value))
            {
                Logger.Write(this, LogLevel.Debug, "No config to load.");
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Loading config..");
            try
            {
                using (var stream = new MemoryStream(Encoding.Default.GetBytes(value)))
                {
                    var configs = Serializer.LoadWindows(stream);
                    foreach (var config in configs)
                    {
                        await this.Load(config).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to load config: {0}", e.Message);
            }
        }

        protected virtual async Task<ToolWindow> Load(ToolWindowConfiguration config)
        {
            Logger.Write(this, LogLevel.Debug, "Loading config: {0}", ToolWindowConfiguration.GetTitle(config));
            try
            {
                if (!ScreenHelper.WindowBoundsVisible(new Rect(config.Left, config.Top, config.Width, config.Height)))
                {
                    //Prevent window from opening off screen.
                    config.Left = 0;
                    config.Top = 0;
                }
                var window = default(ToolWindow);
                await global::FoxTunes.Windows.Invoke(() =>
                {
                    Logger.Write(this, LogLevel.Debug, "Creating window: {0}", ToolWindowConfiguration.GetTitle(config));
                    window = new ToolWindow();
                    window.Configuration = config;
                    window.ShowActivated = false;
                    window.Closed += this.OnClosed;
                }).ConfigureAwait(false);
                this.Windows[config] = window;
                this.OnLoaded(config, window);
                return window;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to load config: {0}", e.Message);
                return null;
            }
        }

        protected virtual void OnLoaded(ToolWindowConfiguration config, ToolWindow window)
        {
            if (this.Loaded == null)
            {
                return;
            }
            this.Loaded(this, new ToolWindowConfigurationEventArgs(config, window));
        }

        public event ToolWindowConfigurationEventHandler Loaded;

        protected virtual async Task UpdateVisiblity()
        {
            foreach (var pair in this.Windows)
            {
                await this.UpdateVisiblity(pair.Key, pair.Value).ConfigureAwait(false);
            }
        }

        protected virtual Task UpdateVisiblity(ToolWindowConfiguration config, ToolWindow window)
        {
            Logger.Write(this, LogLevel.Debug, "Updating visiblity: {0}", ToolWindowConfiguration.GetTitle(config));
            var show = false;
            var id = default(string);
            if (global::FoxTunes.Windows.ActiveWindow is WindowBase activeWindow)
            {
                id = activeWindow.Id;
            }
            else
            {
                id = MAIN_WINDOW_ID;
            }
            if (config.ShowWithMainWindow && string.Equals(id, MAIN_WINDOW_ID, StringComparison.OrdinalIgnoreCase))
            {
                show = true;
            }
            else if (config.ShowWithMiniWindow && string.Equals(id, MINI_WINDOW_ID, StringComparison.OrdinalIgnoreCase))
            {
                show = true;
            }
            var action = default(Action);
            if (show)
            {
                action = () =>
                {
                    Logger.Write(this, LogLevel.Debug, "Showing window: {0}", ToolWindowConfiguration.GetTitle(config));
                    window.Show();
                };
            }
            else
            {
                action = () =>
                {
                    Logger.Write(this, LogLevel.Debug, "Hiding window: {0}", ToolWindowConfiguration.GetTitle(config));
                    window.Hide();
                };
            }
            return global::FoxTunes.Windows.Invoke(() =>
            {
                if (!this.Enabled)
                {
                    return;
                }
                action();
            });
        }

        protected virtual void Unload(ToolWindowConfiguration config, ToolWindow window)
        {
            Logger.Write(this, LogLevel.Debug, "Unloading config: {0}", ToolWindowConfiguration.GetTitle(config));
            this.Windows.TryGetValue(config, out window);
            if (!this.Windows.TryRemove(config))
            {
                return;
            }
            this.OnUnloaded(config, window);
        }

        protected virtual void OnUnloaded(ToolWindowConfiguration config, ToolWindow window)
        {
            if (this.Unloaded == null)
            {
                return;
            }
            this.Unloaded(this, new ToolWindowConfigurationEventArgs(config, window));
        }

        public event ToolWindowConfigurationEventHandler Unloaded;

        protected virtual bool HasChanges()
        {
            var value = default(string);
            return this.HasChanges(out value);
        }

        protected virtual bool HasChanges(out string value)
        {
            var configs = this.Windows.Keys.ToArray();
            if (configs.Length == 0)
            {
                value = null;
                return !string.IsNullOrEmpty(this.Element.Value);
            }
            using (var stream = new MemoryStream())
            {
                Serializer.Save(stream, configs);
                value = Encoding.Default.GetString(stream.ToArray());
            }
            return !string.Equals(this.Element.Value, value, StringComparison.OrdinalIgnoreCase);
        }

        protected virtual void Save()
        {
            if (this.Configuration == null || this.Element == null)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Saving config..");
            try
            {
                var value = default(string);
                if (!this.HasChanges(out value))
                {
                    //Nothing to do.
                    return;
                }
                this.IsSaving = true;
                try
                {
                    this.Element.Value = value;
                }
                finally
                {
                    this.IsSaving = false;
                }
                Logger.Write(this, LogLevel.Debug, "Saved config for {0} windows.", this.Windows.Count);
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to save config: {0}", e.Message);
            }
        }

        protected virtual void OnClosed(object sender, EventArgs e)
        {
            var window = sender as ToolWindow;
            if (window == null)
            {
                return;
            }
            this.Unload(window.Configuration, window);
        }

        public override void InitializeComponent(ICore core)
        {
            global::FoxTunes.LayoutManager.Instance.ProviderChanged += this.OnProviderChanged;
            global::FoxTunes.Windows.ActiveWindowChanged += this.OnActiveWindowChanged;
            global::FoxTunes.Windows.ShuttingDown += this.OnShuttingDown;
            global::FoxTunes.Windows.Registrations.AddIsVisibleChanged(ToolWindowManagerWindow.ID, this.OnWindowIsVisibleChanged);
            this.Core = core;
            this.Configuration = core.Components.Configuration;
            this.Configuration.Loaded += this.OnLoaded;
            this.Configuration.Saving += this.OnSaving;
            this.Layout = this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.LAYOUT_ELEMENT
            );
            this.Element = this.Configuration.GetElement<TextConfigurationElement>(
                ToolWindowBehaviourConfiguration.SECTION,
                ToolWindowBehaviourConfiguration.ELEMENT
            );
            if (this.Enabled)
            {
                var task = this.Load();
            }
            base.InitializeComponent(core);
        }

        protected virtual void OnProviderChanged(object sender, EventArgs e)
        {
            if (this.Enabled)
            {
                this.Dispatch(async () =>
                {
                    if (!this.IsLoaded)
                    {
                        await this.Load().ConfigureAwait(false);
                    }
                    await this.UpdateVisiblity().ConfigureAwait(false);
                });
            }
            else if (this.IsLoaded)
            {
                var task = this.Shutdown();
            }
        }

        protected virtual void OnActiveWindowChanged(object sender, EventArgs e)
        {
            if (this.Enabled)
            {
                this.Dispatch(async () =>
                {
                    if (!this.IsLoaded)
                    {
                        await this.Load().ConfigureAwait(false);
                    }
                    await this.UpdateVisiblity().ConfigureAwait(false);
                });
            }
        }

        protected virtual void OnShuttingDown(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Shutdown signal recieved.");
            if (!this.IsLoaded)
            {
                return;
            }
            var task = this.Shutdown();
        }

        protected virtual void OnWindowIsVisibleChanged(object sender, EventArgs e)
        {
            LayoutDesignerBehaviour.Instance.IsDesigning = global::FoxTunes.Windows.Registrations.IsVisible(ToolWindowManagerWindow.ID);
        }

        protected virtual void OnLoaded(object sender, EventArgs e)
        {
            if (this.Enabled)
            {
#if NET40
                var task = TaskEx.Run(async () =>
#else
                var task = Task.Run(async () =>
#endif
                {
                    await this.Load().ConfigureAwait(false);
                    await this.UpdateVisiblity().ConfigureAwait(false);
                });
            }
        }

        protected virtual void OnSaving(object sender, OrderedEventArgs e)
        {
            if (!this.IsLoaded)
            {
                return;
            }
            e.Add(this.Save, OrderedEventArgs.PRIORITY_LOW);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_SETTINGS;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled)
                {
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_SETTINGS,
                        NEW,
                        Strings.ToolWindowBehaviour_New,
                        path: Strings.ToolWindowBehaviour_Path
                    );
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_SETTINGS,
                        MANAGE,
                        Strings.ToolWindowBehaviour_Manage,
                        path: Strings.ToolWindowBehaviour_Path
                    );
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case NEW:
                    return this.New();
                case MANAGE:
                    return this.Manage();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task<ToolWindowConfiguration> New()
        {
            Logger.Write(this, LogLevel.Debug, "Creating new config..");
            var configs = this.Windows.Keys;
            var config = new ToolWindowConfiguration()
            {
                Width = 400,
                Height = 250,
                ShowWithMainWindow = true,
                ShowWithMiniWindow = false,
                Component = new UIComponentConfiguration()
            };
            var window = await this.Load(config).ConfigureAwait(false);
            if (window == null)
            {
                return null;
            }
            await this.UpdateVisiblity(config, window).ConfigureAwait(false);
            return config;
        }

        public Task Manage()
        {
            return global::FoxTunes.Windows.Invoke(() => global::FoxTunes.Windows.Registrations.Show(ToolWindowManagerWindow.ID));
        }

        public Task Shutdown()
        {
            this.Save();
            this.IsLoaded = false;
            return global::FoxTunes.Windows.Invoke(() =>
            {
                Logger.Write(this, LogLevel.Debug, "Shutting down..");
                global::FoxTunes.Windows.Registrations.Close(ToolWindowManagerWindow.ID);
                foreach (var pair in this.Windows)
                {
                    Logger.Write(this, LogLevel.Debug, "Closing window: {0}", pair.Value.Title);
                    pair.Value.Closed -= this.OnClosed;
                    pair.Value.Close();
                }
                this.Windows.Clear();
            });
        }

        public async Task Refresh()
        {
            foreach (var pair in this.Windows)
            {
                await this.UpdateVisiblity(pair.Key, pair.Value).ConfigureAwait(false);
            }
        }

        public Task Reset()
        {
            Logger.Write(this, LogLevel.Debug, "Resetting configuration.");
            return global::FoxTunes.Windows.Invoke(() =>
            {
                foreach (var pair in this.Windows)
                {
                    Logger.Write(this, LogLevel.Debug, "Closing window: {0}", pair.Value.Title);
                    pair.Value.Closed -= this.OnClosed;
                    pair.Value.Close();
                }
                this.Windows.Clear();
            });
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ToolWindowBehaviourConfiguration.GetConfigurationSections();
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
            global::FoxTunes.LayoutManager.Instance.ProviderChanged -= this.OnProviderChanged;
            global::FoxTunes.Windows.ActiveWindowChanged -= this.OnActiveWindowChanged;
            global::FoxTunes.Windows.ShuttingDown -= this.OnShuttingDown;
            global::FoxTunes.Windows.Registrations.RemoveIsVisibleChanged(ToolWindowManagerWindow.ID, this.OnWindowIsVisibleChanged);
            if (this.Configuration != null)
            {
                this.Configuration.Loaded -= this.OnLoaded;
                this.Configuration.Saving -= this.OnSaving;
            }
        }

        ~ToolWindowBehaviour()
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

        public static ToolWindowBehaviour Instance { get; private set; }
    }

    public class ToolWindowConfiguration
    {
        private string _Title { get; set; }

        public string Title
        {
            get
            {
                return this._Title;
            }
            set
            {
                this._Title = value;
                this.OnTitleChanged();
            }
        }

        protected virtual void OnTitleChanged()
        {
            if (this.TitleChanged != null)
            {
                this.TitleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Title");
        }

        public event EventHandler TitleChanged;

        private int _Left { get; set; }

        public int Left
        {
            get
            {
                return this._Left;
            }
            set
            {
                this._Left = value;
                this.OnLeftChanged();
            }
        }

        protected virtual void OnLeftChanged()
        {
            if (this.LeftChanged != null)
            {
                this.LeftChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Left");
        }

        public event EventHandler LeftChanged;

        private int _Top { get; set; }

        public int Top
        {
            get
            {
                return this._Top;
            }
            set
            {
                this._Top = value;
                this.OnTopChanged();
            }
        }

        protected virtual void OnTopChanged()
        {
            if (this.TopChanged != null)
            {
                this.TopChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Top");
        }

        public event EventHandler TopChanged;

        private int _Width { get; set; }

        public int Width
        {
            get
            {
                return this._Width;
            }
            set
            {
                this._Width = value;
                this.OnWidthChanged();
            }
        }

        protected virtual void OnWidthChanged()
        {
            if (this.WidthChanged != null)
            {
                this.WidthChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Width");
        }

        public event EventHandler WidthChanged;

        private int _Height { get; set; }

        public int Height
        {
            get
            {
                return this._Height;
            }
            set
            {
                this._Height = value;
                this.OnHeightChanged();
            }
        }

        protected virtual void OnHeightChanged()
        {
            if (this.HeightChanged != null)
            {
                this.HeightChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Height");
        }

        public event EventHandler HeightChanged;

        private UIComponentConfiguration _Component { get; set; }

        public UIComponentConfiguration Component
        {
            get
            {
                return this._Component;
            }
            set
            {
                this._Component = value;
                this.OnComponentChanged();
            }
        }

        protected virtual void OnComponentChanged()
        {
            if (this.ComponentChanged != null)
            {
                this.ComponentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Component");
        }

        public event EventHandler ComponentChanged;

        private bool _ShowWithMainWindow { get; set; }

        public bool ShowWithMainWindow
        {
            get
            {
                return this._ShowWithMainWindow;
            }
            set
            {
                this._ShowWithMainWindow = value;
                this.OnShowWithMainWindowChanged();
            }
        }

        protected virtual void OnShowWithMainWindowChanged()
        {
            if (this.ShowWithMainWindowChanged != null)
            {
                this.ShowWithMainWindowChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowWithMainWindow");
        }

        public event EventHandler ShowWithMainWindowChanged;

        private bool _ShowWithMiniWindow { get; set; }

        public bool ShowWithMiniWindow
        {
            get
            {
                return this._ShowWithMiniWindow;
            }
            set
            {
                this._ShowWithMiniWindow = value;
                this.OnShowWithMiniWindowChanged();
            }
        }

        protected virtual void OnShowWithMiniWindowChanged()
        {
            if (this.ShowWithMiniWindowChanged != null)
            {
                this.ShowWithMiniWindowChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowWithMiniWindow");
        }

        public event EventHandler ShowWithMiniWindowChanged;

        private bool _AlwaysOnTop { get; set; }

        public bool AlwaysOnTop
        {
            get
            {
                return this._AlwaysOnTop;
            }
            set
            {
                this._AlwaysOnTop = value;
                this.OnAlwaysOnTopChanged();
            }
        }

        protected virtual void OnAlwaysOnTopChanged()
        {
            if (this.AlwaysOnTopChanged != null)
            {
                this.AlwaysOnTopChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("AlwaysOnTop");
        }

        public event EventHandler AlwaysOnTopChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged == null)
            {
                return;
            }
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static string GetTitle(ToolWindowConfiguration instance)
        {
            if (!string.IsNullOrEmpty(instance.Title))
            {
                return instance.Title;
            }
            if (!instance.Component.Component.IsEmpty)
            {
                return instance.Component.Component.Name;
            }
            return Strings.ToolWindowBehaviour_NewWindow;
        }
    }

    public delegate void ToolWindowConfigurationEventHandler(object sender, ToolWindowConfigurationEventArgs e);

    public class ToolWindowConfigurationEventArgs : EventArgs
    {
        public ToolWindowConfigurationEventArgs(ToolWindowConfiguration configuration, ToolWindow window)
        {
            this.Configuration = configuration;
            this.Window = window;
        }

        public ToolWindowConfiguration Configuration { get; private set; }

        public ToolWindow Window { get; private set; }
    }
}
