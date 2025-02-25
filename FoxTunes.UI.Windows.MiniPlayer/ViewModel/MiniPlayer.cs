using FoxTunes.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class MiniPlayer : ViewModelBase
    {
        public static readonly MiniPlayerBehaviour Behaviour = ComponentRegistry.Instance.GetComponent<MiniPlayerBehaviour>();

        public bool Enabled
        {
            get
            {
                return Behaviour.Enabled;
            }
            set
            {
                Behaviour.Enabled = value;
            }
        }

        protected virtual void OnEnabledChanged()
        {
            if (this.EnabledChanged != null)
            {
                this.EnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Enabled");
        }

        public event EventHandler EnabledChanged;

        public ICommand ShowCommand
        {
            get
            {
                return new Command(() => this.Enabled = true);
            }
        }

        public ICommand HideCommand
        {
            get
            {
                return new Command(() => this.Enabled = false);
            }
        }

        public ICommand ToggleCommand
        {
            get
            {
                return new Command(() => this.Enabled = !this.Enabled);
            }
        }

        protected override void InitializeComponent(ICore core)
        {
            Behaviour.EnabledChanged += this.OnEnabledChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnEnabledChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnEnabledChanged);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new MiniPlayer();
        }

        protected override void OnDisposing()
        {
            if (Behaviour != null)
            {
                Behaviour.EnabledChanged -= this.OnEnabledChanged;
            }
            base.OnDisposing();
        }
    }
}
