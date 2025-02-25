using FoxTunes.Interfaces;
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for ComponentSettingsDialog.xaml
    /// </summary>
    public partial class ComponentSettingsDialog : UserControl
    {
        public ComponentSettingsDialog()
        {
            this.InitializeComponent();
        }

        public IConfiguration Configuration
        {
            get
            {
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.ComponentSettings>("ViewModel");
                if (viewModel == null)
                {
                    return null;
                }
                return viewModel.Configuration;
            }
            set
            {
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.ComponentSettings>("ViewModel");
                if (viewModel == null)
                {
                    return;
                }
                viewModel.Configuration = value;
            }
        }

        public StringCollection Sections
        {
            get
            {
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.ComponentSettings>("ViewModel");
                if (viewModel == null)
                {
                    return null;
                }
                return viewModel.Sections;
            }
            set
            {
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.ComponentSettings>("ViewModel");
                if (viewModel == null)
                {
                    return;
                }
                viewModel.Sections = value;
            }
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            if (this.Configuration == null)
            {
                this.Configuration = Core.Instance.Components.Configuration;
            }
            this.Refresh();
            base.OnVisualParentChanged(oldParent);
        }

        public void Refresh()
        {
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.ComponentSettings>("ViewModel");
            if (viewModel == null)
            {
                return;
            }
            viewModel.Refresh();
        }
    }
}
