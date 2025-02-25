using FoxTunes.Interfaces;
using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class LayoutEditingMode : ViewModelBase
    {
        public static readonly LayoutDesignerBehaviour LayoutDesignerBehaviour = ComponentRegistry.Instance.GetComponent<LayoutDesignerBehaviour>();

        public bool IsEditing
        {
            get
            {
                return LayoutDesignerBehaviour.IsDesigning;
            }
            set
            {
                LayoutDesignerBehaviour.IsDesigning = value;
            }
        }

        protected virtual void OnIsEditingChanged()
        {
            if (this.IsEditingChanged != null)
            {
                this.IsEditingChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsEditing");
        }

        public event EventHandler IsEditingChanged;

        protected override void InitializeComponent(ICore core)
        {
            LayoutDesignerBehaviour.IsDesigningChanged += this.OnIsDesigningChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnIsDesigningChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnIsEditingChanged);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new LayoutEditingMode();
        }

        protected override void OnDisposing()
        {
            if (LayoutDesignerBehaviour != null)
            {
                LayoutDesignerBehaviour.IsDesigningChanged -= this.OnIsDesigningChanged;
            }
            base.OnDisposing();
        }
    }
}
