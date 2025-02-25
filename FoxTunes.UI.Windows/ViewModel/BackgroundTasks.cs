using FoxTunes.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class BackgroundTasks : ViewModelBase
    {
        public BackgroundTasks()
        {
            this.Items = new ObservableCollection<BackgroundTask>();
            global::FoxTunes.BackgroundTask.ActiveChanged += this.OnActiveChanged;
        }

        private ObservableCollection<BackgroundTask> _Items { get; set; }

        public ObservableCollection<BackgroundTask> Items
        {
            get
            {
                return this._Items;
            }
            set
            {
                this.OnItemsChanging();
                this._Items = value;
                this.OnItemsChanged();
            }
        }

        protected virtual void OnItemsChanging()
        {
            if (this.Items == null)
            {
                return;
            }
            foreach (var item in this.Items)
            {
                item.Dispose();
            }
        }

        protected virtual void OnItemsChanged()
        {
            if (this.ItemsChanged != null)
            {
                this.ItemsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Items");
        }

        public event EventHandler ItemsChanged;

        protected override void InitializeComponent(ICore core)
        {
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        protected virtual void OnActiveChanged(object sender, EventArgs e)
        {
            this.Dispatch(this.Refresh);
        }

        public Task Refresh()
        {
            var items = global::FoxTunes.BackgroundTask.Active
                .Where(backgroundTask => backgroundTask.Visible)
                .Select(backgroundTask => new BackgroundTask(backgroundTask))
                .ToArray();
            return Windows.Invoke(() => this.Items = new ObservableCollection<BackgroundTask>(items));
        }

        protected override void OnDisposing()
        {
            global::FoxTunes.BackgroundTask.ActiveChanged -= this.OnActiveChanged;
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BackgroundTasks();
        }
    }
}
