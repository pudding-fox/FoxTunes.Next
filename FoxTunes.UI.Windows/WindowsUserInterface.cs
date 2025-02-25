using FoxTunes.Integration;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FoxTunes
{
    [ComponentPreference(ComponentPreferenceAttribute.DEFAULT)]
    [Component(ID, ComponentSlots.UserInterface)]
    public class WindowsUserInterface : UserInterface, IConfigurableComponent, IDisposable
    {
        public const string ID = "B889313D-4F21-4794-8D16-C2FAE6A7B305";

        public static readonly Type[] References = new[]
        {
            typeof(global::System.Windows.Interactivity.Interaction)
        };

        static WindowsUserInterface()
        {
            //Main window.
            global::FoxTunes.Windows.Registrations.Add(new global::FoxTunes.Windows.WindowRegistration(MainWindow.ID, MainWindow.ROLE, () => new MainWindow()));
            //Common dialogs.
            global::FoxTunes.Windows.Registrations.Add(new global::FoxTunes.Windows.WindowRegistration(EqualizerWindow.ID, UserInterfaceWindowRole.None, () => new EqualizerWindow()));
            global::FoxTunes.Windows.Registrations.Add(new global::FoxTunes.Windows.WindowRegistration(InputBox.ID, UserInterfaceWindowRole.None, () => new InputBox()));
            global::FoxTunes.Windows.Registrations.Add(new global::FoxTunes.Windows.WindowRegistration(PlaylistManagerWindow.ID, UserInterfaceWindowRole.None, () => new PlaylistManagerWindow()));
            global::FoxTunes.Windows.Registrations.Add(new global::FoxTunes.Windows.WindowRegistration(SettingsWindow.ID, UserInterfaceWindowRole.None, () => new SettingsWindow()));
            global::FoxTunes.Windows.Registrations.Add(new global::FoxTunes.Windows.WindowRegistration(TempoWindow.ID, UserInterfaceWindowRole.None, () => new TempoWindow()));
        }

        public WindowsUserInterface()
        {
            global::FoxTunes.Windows.IsShuttingDown = false;
            this.Application = new Application();
            this.Application.DispatcherUnhandledException += this.OnApplicationDispatcherUnhandledException;
            WindowBase.Created += this.OnWindowCreated;
            WindowBase.Destroyed += this.OnWindowDestroyed;
        }

        private Application _Application { get; set; }

        public Application Application
        {
            get
            {
                return this._Application;
            }
            private set
            {
                this._Application = value;
                this.OnApplicationChanged();
            }
        }

        protected virtual void OnApplicationChanged()
        {
            if (this.ApplicationChanged != null)
            {
                this.ApplicationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Application");
        }

        public event EventHandler ApplicationChanged;

        public ICore Core { get; private set; }

        public ThemeLoader ThemeLoader { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();
            this.Configuration = core.Components.Configuration;
            base.InitializeComponent(core);
        }

        public override IEnumerable<IUserInterfaceWindow> Windows
        {
            get
            {
                return WindowBase.Active;
            }
        }

        public override Task Show()
        {
            this.ThemeLoader.EnsureTheme();
            var registrations = global::FoxTunes.Windows.Registrations.RegistrationsByRole(UserInterfaceWindowRole.Main);
            foreach (var registration in registrations)
            {
                var window = default(Window);
                if (registration.TryGetInstance(out window))
                {
                    Logger.Write(this, LogLevel.Debug, "Found window instance with role {0}, running.", Enum.GetName(typeof(UserInterfaceWindowRole), UserInterfaceWindowRole.Main));
                    this.Application.Run(window);
                    goto done;
                }
            }
            this.Application.Run(global::FoxTunes.Windows.Registrations.Show(MainWindow.ID));
        done:
            this.OnShuttingDown();
            return global::FoxTunes.Windows.Shutdown();
        }

        public override void Activate()
        {
            global::FoxTunes.Windows.Invoke(() =>
            {
                var window = global::FoxTunes.Windows.ActiveWindow;
                if (window != null)
                {
                    if (window.WindowState == WindowState.Minimized)
                    {
                        window.WindowState = WindowState.Normal;
                    }
                    window.Activate();
                }
            });
        }

        public override void Warn(string message)
        {
            if (global::FoxTunes.Windows.IsShuttingDown)
            {
                //Don't bother showing messages when shutting down.
                return;
            }
            MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public override void Fatal(Exception exception)
        {
            if (global::FoxTunes.Windows.IsShuttingDown)
            {
                //Don't bother showing messages when shutting down.
                return;
            }
            var message = exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace;
            MessageBox.Show(message, "Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public override bool Confirm(string message)
        {
            var result = default(bool);
            //TODO: Bad .Result().
            var window = GetActiveWindow().Result;
            if (window != null)
            {
                //TODO: This is the only MessageBox provided with a Window.
                //TODO: Bad .Wait().
                global::FoxTunes.Windows.Invoke(() => result = MessageBox.Show(window, message, "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK).Wait();
            }
            else
            {
                //TODO: Bad .Wait().
                global::FoxTunes.Windows.Invoke(() => result = MessageBox.Show(message, "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK).Wait();
            }
            return result;
        }

        public override string Prompt(string message, UserInterfacePromptFlags flags = UserInterfacePromptFlags.None)
        {
            var result = default(string);
            //TODO: Bad .Wait().
            global::FoxTunes.Windows.Invoke(() => result = InputBox.ShowDialog(message, string.Empty, flags)).Wait();
            return result;
        }

        public override string Prompt(string message, string value, UserInterfacePromptFlags flags = UserInterfacePromptFlags.None)
        {
            var result = default(string);
            //TODO: Bad .Wait().
            global::FoxTunes.Windows.Invoke(() => result = InputBox.ShowDialog(message, value, flags)).Wait();
            return result;
        }

        public override void SelectInShell(string fileName)
        {
            Explorer.Select(fileName);
        }

        public override void OpenInShell(string fileName)
        {
            Explorer.Open(fileName);
        }

        public override Task<bool> ShowSettings(string title, IEnumerable<string> sections)
        {
            return this.ShowSettings(title, this.Configuration, sections);
        }

        public override async Task<bool> ShowSettings(string title, IConfiguration configuration, IEnumerable<string> sections)
        {
            var settings = default(ComponentSettingsDialog);
            await global::FoxTunes.Windows.Invoke(() =>
            {
                settings = new ComponentSettingsDialog();
                settings.Configuration = configuration;
                settings.Sections = new StringCollection(sections);
            }).ConfigureAwait(false);
            var result = await global::FoxTunes.Windows.ShowDialog(this.Core, title, settings).ConfigureAwait(false);
            return result;
        }

        public override void Restart()
        {
            MessageBox.Show("Restart is required.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        protected virtual void OnApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Write(this, LogLevel.Fatal, e.Exception.Message, e);
            //Don't crash out.
            e.Handled = true;
        }

        protected virtual void OnWindowCreated(object sender, EventArgs e)
        {
            var window = sender as WindowBase;
            if (window == null)
            {
                return;
            }
            this.OnWindowCreated(window);
        }

        protected virtual void OnWindowDestroyed(object sender, EventArgs e)
        {
            var window = sender as WindowBase;
            if (window == null)
            {
                return;
            }
            this.OnWindowDestroyed(window);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WindowsUserInterfaceConfiguration.GetConfigurationSections();
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
            WindowBase.Created -= this.OnWindowCreated;
            WindowBase.Destroyed -= this.OnWindowDestroyed;
        }

        ~WindowsUserInterface()
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

        public async Task<Window> GetActiveWindow()
        {
            var activeWindow = default(Window);
            await global::FoxTunes.Windows.Invoke(() =>
            {
                //Try and get the focused window, fall back to one of the "main" windows.
                activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(
                    window => window.IsActive
                ) ?? global::FoxTunes.Windows.ActiveWindow;
            }).ConfigureAwait(false);
            return activeWindow;
        }
    }
}
