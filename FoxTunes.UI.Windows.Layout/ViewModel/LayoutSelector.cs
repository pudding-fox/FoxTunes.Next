using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class LayoutSelector : ViewModelBase
    {
        public static readonly LayoutPresetBehaviour LayoutPresetBehaviour = ComponentRegistry.Instance.GetComponent<LayoutPresetBehaviour>();

        public static readonly LayoutDesignerBehaviour LayoutDesignerBehaviour = ComponentRegistry.Instance.GetComponent<LayoutDesignerBehaviour>();

        public IEnumerable<IUIComponentLayoutProviderPreset> Presets
        {
            get
            {
                return UIComponentLayoutProviderPresets.Instance.GetPresetsByCategory(
                    UIComponentLayoutProviderPreset.CATEGORY_MAIN
                );
            }
        }

        public IUIComponentLayoutProviderPreset SelectedPreset
        {
            get
            {
                return LayoutPresetBehaviour.ActivePreset;
            }
            set
            {
                LayoutPresetBehaviour.ActivePreset = value;
            }
        }

        protected virtual void OnSelectedPresetChanged()
        {
            if (this.SelectedPresetChanged != null)
            {
                this.SelectedPresetChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedPreset");
        }

        public event EventHandler SelectedPresetChanged;

        protected override void InitializeComponent(ICore core)
        {
            LayoutPresetBehaviour.ActivePresetChanged += this.OnActivePresetChanged;
            LayoutDesignerBehaviour.IsDesigningChanged += this.OnIsDesigningChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnActivePresetChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnSelectedPresetChanged);
        }

        protected virtual void OnIsDesigningChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnIsEditingChanged);
        }

        public ICommand AddPresetCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.AddPreset, () => this.CanAddPreset);
            }
        }

        public bool CanAddPreset
        {
            get
            {
                return false;
            }
        }

        public void AddPreset()
        {
            throw new NotImplementedException();
        }

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

        protected override Freezable CreateInstanceCore()
        {
            return new LayoutSelector();
        }

        protected override void OnDisposing()
        {
            if (LayoutPresetBehaviour != null)
            {
                LayoutPresetBehaviour.ActivePresetChanged -= this.OnActivePresetChanged;
            }
            if (LayoutDesignerBehaviour != null)
            {
                LayoutDesignerBehaviour.IsDesigningChanged -= this.OnIsDesigningChanged;
            }
            base.OnDisposing();
        }
    }
}
