using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for ReportWindow.xaml
    /// </summary>
    public partial class ReportWindow : WindowBase
    {
        const int STARTUP_LOCATION_OFFSET = 90;

        public const string ID = "5AB7D852-198E-465C-BA26-11237E1A6B2A";

        public ReportWindow()
        {
            var instance = Active.OfType<ReportWindow>().LastOrDefault();
            if (instance != null)
            {
                this.Left = instance.Left + STARTUP_LOCATION_OFFSET;
                this.Top = instance.Top + STARTUP_LOCATION_OFFSET;
                this.Width = instance.Width;
                this.Height = instance.Height;
            }
            else if (!global::FoxTunes.Properties.Settings.Default.ReportWindowBounds.IsEmpty())
            {
                if (ScreenHelper.WindowBoundsVisible(global::FoxTunes.Properties.Settings.Default.ReportWindowBounds))
                {
                    this.Left = global::FoxTunes.Properties.Settings.Default.ReportWindowBounds.Left;
                    this.Top = global::FoxTunes.Properties.Settings.Default.ReportWindowBounds.Top;
                }
                this.Width = global::FoxTunes.Properties.Settings.Default.ReportWindowBounds.Width;
                this.Height = global::FoxTunes.Properties.Settings.Default.ReportWindowBounds.Height;
            }
            else
            {
                this.Width = 600;
                this.Height = 400;
            }
            if (double.IsNaN(this.Left) || double.IsNaN(this.Top))
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            this.InitializeComponent();
        }

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        public IEnumerable<global::FoxTunes.ViewModel.Report> ViewModels
        {
            get
            {
                return new FrameworkElement[] { this, this.Report }.Select(
                    element => element.FindResource<global::FoxTunes.ViewModel.Report>("ViewModel")
                );
            }
        }

        public IReportComponent Source
        {
            get
            {
                foreach (var viewModel in this.ViewModels)
                {
                    return viewModel.Source;
                }
                return default(IReportComponent);
            }
            set
            {
                foreach (var viewModel in this.ViewModels)
                {
                    viewModel.Source = value;
                }
            }
        }

        protected virtual void OnCommandExecuted(object sender, ButtonExtensions.CommandExecutedEventArgs e)
        {
            if (string.Equals(e.Behaviour, ButtonExtensions.COMMAND_BEHAVIOUR_DISMISS, StringComparison.OrdinalIgnoreCase))
            {
                this.Close();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            global::FoxTunes.Properties.Settings.Default.ReportWindowBounds = this.RestoreBounds;
            global::FoxTunes.Properties.Settings.Default.Save();
            base.OnClosing(e);
        }
    }
}
