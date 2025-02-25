using System;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class LayoutEditor : ViewModelBase
    {
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

        public ICommand SaveCommand
        {
            get
            {
                return new Command(this.Save);
            }
        }

        public void Save()
        {

        }

        public ICommand CancelCommand
        {
            get
            {
                return new Command(this.Cancel);
            }
        }

        public void Cancel()
        {

        }


        protected override Freezable CreateInstanceCore()
        {
            return new LayoutEditor();
        }
    }
}
