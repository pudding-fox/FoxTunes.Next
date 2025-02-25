using FoxTunes.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class WindowState : ViewModelBase
    {
        private WindowState() : base(false)
        {

        }

        public WindowState(string id) : this()
        {
            this.Id = id;
            this.InitializeComponent(Core.Instance);
        }

        public string Id { get; private set; }

        public bool Visible
        {
            get
            {
                return Windows.Registrations.IsVisible(this.Id);
            }
            set
            {
                if (value)
                {
                    Windows.Registrations.Show(this.Id);
                }
                else
                {
                    Windows.Registrations.Hide(this.Id);
                }
            }
        }

        protected virtual void OnVisibleChanged()
        {
            if (this.VisibleChanged != null)
            {
                this.VisibleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Visible");
        }

        public event EventHandler VisibleChanged;

        public ICommand ShowCommand
        {
            get
            {
                return new Command(() => this.Visible = true);
            }
        }

        public ICommand HideCommand
        {
            get
            {
                return new Command(() => this.Visible = false);
            }
        }

        public ICommand ToggleCommand
        {
            get
            {
                return new Command(() => this.Visible = !this.Visible);
            }
        }

        protected override void InitializeComponent(ICore core)
        {
            Windows.Registrations.AddCreated(this.Id, this.OnCreated);
            Windows.Registrations.AddIsVisibleChanged(this.Id, this.OnIsVisibleChanged);
            Windows.Registrations.AddClosed(this.Id, this.OnClosed);
            base.InitializeComponent(core);
        }

        protected virtual void OnCreated(object sender, EventArgs e)
        {
            this.OnVisibleChanged();
        }

        protected virtual void OnIsVisibleChanged(object sender, EventArgs e)
        {
            this.OnVisibleChanged();
        }

        protected virtual void OnClosed(object sender, EventArgs e)
        {
            this.OnVisibleChanged();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new WindowState();
        }

        protected override void Dispose(bool disposing)
        {
            Windows.Registrations.RemoveCreated(this.Id, this.OnCreated);
            Windows.Registrations.RemoveIsVisibleChanged(this.Id, this.OnIsVisibleChanged);
            Windows.Registrations.RemoveClosed(this.Id, this.OnClosed);
            base.Dispose(disposing);
        }
    }
}
