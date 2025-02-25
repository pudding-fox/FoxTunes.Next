using FoxTunes.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class ToolWindowManager : ViewModelBase
    {
        public ToolWindowBehaviour Behaviour { get; private set; }

        private ObservableCollection<ToolWindow> _Windows { get; set; }

        public ObservableCollection<ToolWindow> Windows
        {
            get
            {
                return this._Windows;
            }
            set
            {
                this._Windows = value;
                this.OnWindowsChanged();
            }
        }

        protected virtual void OnWindowsChanged()
        {
            if (this.WindowsChanged != null)
            {
                this.WindowsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Windows");
        }

        public event EventHandler WindowsChanged;

        private ToolWindow _SelectedWindow { get; set; }

        public ToolWindow SelectedWindow
        {
            get
            {
                return this._SelectedWindow;
            }
            set
            {
                this._SelectedWindow = value;
                this.OnSelectedWindowChanged();
            }
        }

        protected virtual void OnSelectedWindowChanged()
        {
            if (this.SelectedWindowChanged != null)
            {
                this.SelectedWindowChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedWindow");
        }

        public event EventHandler SelectedWindowChanged;

        protected override void InitializeComponent(ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<ToolWindowBehaviour>();
            this.Behaviour.Loaded += this.OnLoaded;
            this.Behaviour.Unloaded += this.OnUnloaded;
            this.Windows = new ObservableCollection<ToolWindow>();
            var task = this.Refresh();
            base.InitializeComponent(core);
        }

        protected virtual void OnLoaded(object sender, ToolWindowConfigurationEventArgs e)
        {
            var task = this.Refresh();
        }

        protected virtual void OnUnloaded(object sender, ToolWindowConfigurationEventArgs e)
        {
            var task = this.Refresh();
        }

        protected virtual Task Refresh()
        {
            return global::FoxTunes.Windows.Invoke(() =>
            {
                var selectedWindow = this.SelectedWindow;
                foreach (var window in this.Windows)
                {
                    window.Dispose();
                }
                this.Windows.Clear();
                this.Windows.AddRange(this.Behaviour.Windows.Keys.Select(configuration =>
                {
                    var window = new ToolWindow();
                    window.Configuration = configuration;
                    return window;
                }));
                if (selectedWindow != null)
                {
                    selectedWindow = this.Windows.FirstOrDefault(
                        window => object.ReferenceEquals(window.Configuration, selectedWindow.Configuration)
                    );
                }
                if (selectedWindow != null)
                {
                    this.SelectedWindow = selectedWindow;
                }
                else
                {
                    this.SelectedWindow = this.Windows.FirstOrDefault();
                }
            });
        }

        public ICommand AddCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Add);
            }
        }

        public async Task Add()
        {
            await this.Behaviour.New().ConfigureAwait(false);
            await this.Refresh().ConfigureAwait(false);
        }

        public ICommand CloseCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Close);
            }
        }

        public async Task Close()
        {
            await global::FoxTunes.Windows.Invoke(() => global::FoxTunes.Windows.Registrations.Hide(ToolWindowManagerWindow.ID)).ConfigureAwait(false);
            await this.Behaviour.Refresh().ConfigureAwait(false);
        }

        protected override void OnDisposing()
        {
            if (this.Behaviour != null)
            {
                this.Behaviour.Loaded -= this.OnLoaded;
                this.Behaviour.Unloaded -= this.OnUnloaded;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new ToolWindowManager();
        }
    }
}
